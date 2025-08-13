using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Services;
using System.Text.Json;

namespace EgitimPlatform.Shared.Privacy.Middleware;

public class PrivacyComplianceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PrivacyComplianceMiddleware> _logger;
    private readonly PrivacyOptions _options;

    public PrivacyComplianceMiddleware(RequestDelegate next, ILogger<PrivacyComplianceMiddleware> logger,
        IOptions<PrivacyOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IConsentService consentService)
    {
        // Add privacy headers to response
        AddPrivacyHeaders(context);

        // Handle cookie consent
        if (_options.CookieConsent.EnableCookieConsent)
        {
            await HandleCookieConsentAsync(context);
        }

        // Log privacy-relevant activities
        if (_options.Compliance.EnableComplianceAuditing)
        {
            await LogPrivacyActivityAsync(context);
        }

        await _next(context);
    }

    private void AddPrivacyHeaders(HttpContext context)
    {
        var response = context.Response;

        // Add privacy policy link
        if (!string.IsNullOrEmpty(_options.Compliance.PrivacyPolicyUrl))
        {
            response.Headers["X-Privacy-Policy"] = _options.Compliance.PrivacyPolicyUrl;
        }

        // Add data controller information
        if (!string.IsNullOrEmpty(_options.Compliance.DataControllerName))
        {
            response.Headers["X-Data-Controller"] = _options.Compliance.DataControllerName;
        }

        // Add DPO contact if available
        if (!string.IsNullOrEmpty(_options.Compliance.DataProtectionOfficerEmail))
        {
            response.Headers["X-DPO-Contact"] = _options.Compliance.DataProtectionOfficerEmail;
        }

        // Add applicable regulations
        if (_options.Compliance.ApplicableRegulations.Any())
        {
            response.Headers["X-Privacy-Regulations"] = string.Join(", ", _options.Compliance.ApplicableRegulations);
        }
    }

    private async Task HandleCookieConsentAsync(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // Check for existing cookie consent
        var consentCookie = request.Cookies["privacy-consent"];
        
        if (string.IsNullOrEmpty(consentCookie))
        {
            // No consent cookie found - user needs to provide consent
            response.Headers["X-Requires-Cookie-Consent"] = "true";
            response.Headers["X-Cookie-Policy"] = _options.CookieConsent.CookiePolicyUrl;
        }
        else
        {
            try
            {
                var consent = JsonSerializer.Deserialize<CookieConsent>(consentCookie);
                if (consent?.ExpiryDate < DateTime.UtcNow)
                {
                    // Consent expired
                    response.Headers["X-Cookie-Consent-Expired"] = "true";
                }
                else
                {
                    // Valid consent
                    response.Headers["X-Cookie-Consent-Status"] = "valid";
                }
            }
            catch (JsonException)
            {
                // Invalid consent cookie
                response.Headers["X-Cookie-Consent-Invalid"] = "true";
            }
        }

        await Task.CompletedTask;
    }

    private async Task LogPrivacyActivityAsync(HttpContext context)
    {
        var request = context.Request;
        
        // Log requests that might involve personal data processing
        var sensitiveEndpoints = new[]
        {
            "/api/users", "/api/profile", "/api/identity", "/api/admin",
            "/api/notifications", "/api/consent", "/api/privacy"
        };

        var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        if (sensitiveEndpoints.Any(endpoint => path.StartsWith(endpoint)))
        {
            var userId = GetUserIdFromContext(context);
            var ipAddress = GetClientIpAddress(context);
            var userAgent = request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Privacy-relevant request: {Method} {Path} by user {UserId} from {IpAddress}",
                request.Method, path, userId ?? "anonymous", ipAddress);

            // This could be extended to write to an audit log
            var auditLog = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Method = request.Method,
                Path = path,
                QueryString = request.QueryString.Value
            };

            // In a real implementation, you'd persist this to an audit store
        }

        await Task.CompletedTask;
    }

    private string? GetUserIdFromContext(HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("userId")?.Value
            : null;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var request = context.Request;
        
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class CookieConsent
{
    public bool Essential { get; set; } = true;
    public bool Analytical { get; set; }
    public bool Marketing { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Version { get; set; } = "1.0";
}