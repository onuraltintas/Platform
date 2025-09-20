using System.Diagnostics;

namespace SpeedReading.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        context.Items["RequestId"] = requestId;

        _logger.LogInformation("Started {Method} {Path} - RequestId: {RequestId}",
            context.Request.Method,
            context.Request.Path,
            requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            _logger.Log(logLevel, 
                "Completed {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - RequestId: {RequestId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                requestId);
        }
    }
}