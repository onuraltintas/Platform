namespace Enterprise.Shared.Auditing.Middleware;

/// <summary>
/// Middleware for automatic HTTP request auditing
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly AuditConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the AuditMiddleware
    /// </summary>
    public AuditMiddleware(
        RequestDelegate next,
        IAuditService auditService,
        IOptions<AuditConfiguration> configuration,
        ILogger<AuditMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_configuration.Enabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var originalBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var auditEvent = AuditEvent.Create(
            action: $"{request.Method} {request.Path}",
            resource: "HTTP_REQUEST",
            result: "Success"
        );

        try
        {
            // Store original request information
            auditEvent = auditEvent
                .WithMetadata(new Dictionary<string, object>
                {
                    ["HttpMethod"] = request.Method,
                    ["RequestPath"] = request.Path.ToString(),
                    ["QueryString"] = request.QueryString.ToString(),
                    ["ContentType"] = request.ContentType ?? "",
                    ["ContentLength"] = request.ContentLength?.ToString() ?? "0",
                    ["Protocol"] = request.Protocol,
                    ["Scheme"] = request.Scheme,
                    ["Host"] = request.Host.ToString(),
                    ["Headers"] = GetSafeHeaders(request.Headers)
                })
                .WithCorrelation(GetCorrelationId(context));

            await _next(context);

            // Capture response information
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            
            auditEvent.Result = statusCode >= 200 && statusCode < 300 ? "Success" : "Failed";
            auditEvent.DurationMs = stopwatch.ElapsedMilliseconds;
            auditEvent.Severity = GetSeverityFromStatusCode(statusCode);

            var responseMetadata = new Dictionary<string, object>
            {
                ["StatusCode"] = statusCode,
                ["ResponseContentType"] = context.Response.ContentType ?? "",
                ["ResponseContentLength"] = context.Response.ContentLength?.ToString() ?? "0",
                ["ResponseHeaders"] = GetSafeHeaders(context.Response.Headers)
            };

            foreach (var item in responseMetadata)
            {
                auditEvent.Properties[item.Key] = item.Value;
            }

            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            auditEvent.Result = "Failed";
            auditEvent.DurationMs = stopwatch.ElapsedMilliseconds;
            auditEvent.Severity = AuditSeverity.Error;
            auditEvent.Details = $"Request processing failed: {ex.Message}";

            auditEvent.Properties["Exception"] = ex.GetType().Name;
            auditEvent.Properties["StatusCode"] = context.Response.StatusCode;

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;

            try
            {
                // Only audit if it meets the configuration criteria
                if (_configuration.ShouldAuditEvent(auditEvent))
                {
                    await _auditService.LogEventAsync(auditEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event for {Method} {Path}", 
                    request.Method, request.Path);
            }
        }
    }

    /// <summary>
    /// Gets safe headers for auditing (excludes sensitive headers)
    /// </summary>
    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token",
            "Authentication",
            "Proxy-Authorization"
        };

        return headers
            .Where(h => !sensitiveHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray() ?? Array.Empty<string>()), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets correlation ID from various sources
    /// </summary>
    private static string GetCorrelationId(HttpContext context)
    {
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
               ?? context.TraceIdentifier;
    }

    /// <summary>
    /// Maps HTTP status code to audit severity
    /// </summary>
    private static AuditSeverity GetSeverityFromStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => AuditSeverity.Critical,
            >= 400 => AuditSeverity.Warning,
            >= 300 => AuditSeverity.Information,
            _ => AuditSeverity.Information
        };
    }
}