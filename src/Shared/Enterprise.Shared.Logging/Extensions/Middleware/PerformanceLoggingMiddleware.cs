using Enterprise.Shared.Logging.Interfaces;
using Enterprise.Shared.Logging.Models;

namespace Enterprise.Shared.Logging.Extensions.Middleware;

/// <summary>
/// Middleware for logging HTTP request performance and correlation
/// </summary>
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnterpriseLogger<PerformanceLoggingMiddleware> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public PerformanceLoggingMiddleware(
        RequestDelegate next,
        IEnterpriseLogger<PerformanceLoggingMiddleware> logger,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set up correlation context
        var correlationId = GetOrCreateCorrelationId(context);
        var correlationContext = new CorrelationContext
        {
            CorrelationId = correlationId,
            RequestId = context.TraceIdentifier,
            UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            SessionId = context.Session?.Id
        };

        _correlationContextAccessor.SetCorrelationContext(correlationContext);

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var properties = new Dictionary<string, object>
            {
                ["Path"] = context.Request.Path.Value ?? string.Empty,
                ["Method"] = context.Request.Method,
                ["StatusCode"] = context.Response.StatusCode,
                ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
                ["RemoteIP"] = GetClientIpAddress(context),
                ["QueryString"] = context.Request.QueryString.Value ?? string.Empty,
                ["ContentLength"] = context.Response.ContentLength ?? 0,
                ["Protocol"] = context.Request.Protocol,
                ["Scheme"] = context.Request.Scheme,
                ["Host"] = context.Request.Host.ToString(),
                ["ResponseSize"] = context.Response.ContentLength ?? 0
            };

            // Add user information if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                properties["UserId"] = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                properties["UserEmail"] = context.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            }

            // Add exception information if occurred
            if (exception != null)
            {
                properties["Exception"] = exception.Message;
                properties["ExceptionType"] = exception.GetType().Name;
                properties["HasException"] = true;
            }
            else
            {
                properties["HasException"] = false;
            }

            _logger.LogApiCall(
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                stopwatch.Elapsed,
                context.Response.StatusCode,
                properties);

            // Clear correlation context
            _correlationContextAccessor.ClearCorrelationContext();
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from headers
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                           context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
                           context.Request.Headers["Request-ID"].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Set the correlation ID in response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        return correlationId;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers, proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}