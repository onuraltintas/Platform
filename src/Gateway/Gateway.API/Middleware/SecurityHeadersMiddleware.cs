using Gateway.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Gateway.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly GatewayOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IOptions<GatewayOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // /health ve /healthz için bypass (minimum header)
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var isHealth = path == "/health" || path == "/healthz";

        if (!isHealth)
        {
            // Add security headers before processing the request
            AddSecurityHeaders(context);

            // Register OnStarting to safely add post-processing headers
            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state!;
                AddPostProcessingHeaders(httpContext);
                return Task.CompletedTask;
            }, context);
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        var request = context.Request;

        // Remove server information disclosure
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");

        // Content Security Policy (CSP)
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            var cspPolicy = BuildContentSecurityPolicy();
            response.Headers.Add("Content-Security-Policy", cspPolicy);
        }

        // X-Frame-Options - Prevent clickjacking
        if (!response.Headers.ContainsKey("X-Frame-Options"))
        {
            response.Headers.Add("X-Frame-Options", "DENY");
        }

        // X-Content-Type-Options - Prevent MIME sniffing
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
        {
            response.Headers.Add("X-Content-Type-Options", "nosniff");
        }

        // X-XSS-Protection - XSS protection
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
        {
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
        }

        // Referrer-Policy - Control referrer information
        if (!response.Headers.ContainsKey("Referrer-Policy"))
        {
            response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        // Permissions-Policy - Control browser features
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            response.Headers.Add("Permissions-Policy", 
                "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()");
        }

        // Strict-Transport-Security (HSTS) - Force HTTPS
        if (_options.Security.RequireHttps && request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
        {
            response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // Cross-Origin-Embedder-Policy
        if (!response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        {
            response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
        }

        // Cross-Origin-Opener-Policy
        if (!response.Headers.ContainsKey("Cross-Origin-Opener-Policy"))
        {
            response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
        }

        // Cross-Origin-Resource-Policy
        if (!response.Headers.ContainsKey("Cross-Origin-Resource-Policy"))
        {
            response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");
        }

        // Cache-Control for security-sensitive endpoints
        if (IsSecuritySensitiveEndpoint(request.Path))
        {
            response.Headers.Remove("Cache-Control");
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");
        }

        // Custom security headers
        AddCustomSecurityHeaders(response);
    }

    private void AddPostProcessingHeaders(HttpContext context)
    {
        var response = context.Response;

        if (!response.HasStarted)
        {
            // Add processing time header for monitoring
            if (!response.Headers.ContainsKey("X-Gateway-Processed"))
                response.Headers.Add("X-Gateway-Processed", "true");
            if (!response.Headers.ContainsKey("X-Gateway-Version"))
                response.Headers.Add("X-Gateway-Version", "1.0.0");

            // Add request ID for tracing
            if (!string.IsNullOrEmpty(context.TraceIdentifier) && !response.Headers.ContainsKey("X-Request-ID"))
            {
                response.Headers.Add("X-Request-ID", context.TraceIdentifier);
            }

            // Add rate limiting headers if they exist
            if (response.Headers.ContainsKey("X-RateLimit-Limit"))
            {
                _logger.LogDebug("Rate limiting headers added for request {RequestId}", context.TraceIdentifier);
            }
        }
    }

    private string BuildContentSecurityPolicy()
    {
        var cspDirectives = new List<string>
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // Relaxed for development
            "style-src 'self' 'unsafe-inline'", // Allow inline styles for frameworks
            "img-src 'self' data: https:",
            "font-src 'self' https:",
            "connect-src 'self' https:",
            "media-src 'none'",
            "object-src 'none'",
            "child-src 'none'",
            "frame-src 'none'",
            "worker-src 'none'",
            "manifest-src 'self'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'",
            "block-all-mixed-content",
            "upgrade-insecure-requests"
        };

        // In development, we might want to be more permissive
        if (_options.Environment == "Development")
        {
            // Allow unsafe-eval for development tools
            cspDirectives[1] = "script-src 'self' 'unsafe-inline' 'unsafe-eval' localhost:* ws: wss:";
            cspDirectives[4] = "connect-src 'self' https: ws: wss: localhost:*";
        }

        return string.Join("; ", cspDirectives);
    }

    private void AddCustomSecurityHeaders(HttpResponse response)
    {
        // Add API Gateway specific headers
        response.Headers.Add("X-Gateway-Security", "enabled");
        
        // Add timestamp for security auditing
        response.Headers.Add("X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        // Add environment indicator (useful for debugging)
        if (_options.Environment != "Production")
        {
            response.Headers.Add("X-Gateway-Environment", _options.Environment);
        }

        // Content type options for API responses
        if (response.ContentType?.Contains("application/json") == true)
        {
            // Ensure JSON responses are not executed as scripts
            response.Headers.Add("X-Content-Type-Validation", "strict");
        }
    }

    private static bool IsSecuritySensitiveEndpoint(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/auth",
            "/api/identity/login",
            "/api/identity/register",
            "/api/identity/refresh",
            "/api/users/profile",
            "/health/detailed" // Detailed health info should not be cached
        };

        var pathValue = path.Value?.ToLower() ?? "";
        return sensitiveEndpoints.Any(endpoint => pathValue.StartsWith(endpoint.ToLower()));
    }
}