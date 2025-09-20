namespace Enterprise.Shared.Auditing.Middleware;

/// <summary>
/// Middleware for managing correlation IDs across requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the CorrelationIdMiddleware
    /// </summary>
    public CorrelationIdMiddleware(
        RequestDelegate next,
        string headerName = "X-Correlation-ID",
        ILogger<CorrelationIdMiddleware>? logger = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        _logger = logger ?? NullLogger<CorrelationIdMiddleware>.Instance;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Set correlation ID in response headers
        if (!context.Response.Headers.ContainsKey(_headerName))
        {
            context.Response.Headers[_headerName] = correlationId;
        }

        // Set correlation ID in HttpContext items for easy access
        context.Items["CorrelationId"] = correlationId;

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed with correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Gets existing correlation ID or creates a new one
    /// </summary>
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get from request headers
        var correlationId = context.Request.Headers[_headerName].FirstOrDefault()
                           ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
                           ?? context.Request.Headers["Request-ID"].FirstOrDefault();

        if (!string.IsNullOrEmpty(correlationId))
        {
            return correlationId;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("D");
    }
}