using System.Security.Cryptography;
using System.Text;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Observability.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly ObservabilitySettings _settings;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger,
        IOptions<ObservabilitySettings> settings)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContextAccessor correlationContextAccessor)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Create correlation context
        var correlationContext = new CorrelationContext
        {
            CorrelationId = correlationId,
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            SpanId = System.Diagnostics.Activity.Current?.SpanId.ToString()
        };
        
        // Extract user ID from claims if available
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            correlationContext.UserId = context.User.FindFirst("sub")?.Value 
                                      ?? context.User.FindFirst("user_id")?.Value;
        }
        
        // Set correlation context
        correlationContextAccessor.CorrelationContext = correlationContext;
        
        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_settings.CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        
        // Add correlation ID to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = correlationContext.TraceId ?? "",
            ["HashedUserId"] = HashUserId(correlationContext.UserId)
        }))
        {
            await _next(context);
        }
    }
    
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(_settings.CorrelationId.HeaderName, out var correlationId))
        {
            var id = correlationId.ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }
        
        // Try to get from W3C Trace Context
        if (context.Request.Headers.TryGetValue("traceparent", out var traceParent))
        {
            var parts = traceParent.ToString().Split('-');
            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }
        
        // Generate new correlation ID if configured to do so
        if (_settings.CorrelationId.GenerateIfMissing)
        {
            var newId = Guid.NewGuid().ToString();
            return newId;
        }
        
        return string.Empty;
    }
    
    private static string HashUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "anonymous";
            
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes)[..8]; // Take first 8 chars for brevity
    }
}