using System.Diagnostics;

namespace EgitimPlatform.Gateway.Middleware;

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
        var request = context.Request;
        var requestId = context.TraceIdentifier;
        
        _logger.LogInformation("Gateway Request: {Method} {Path} {QueryString} - RequestId: {RequestId} - IP: {RemoteIp} - UserAgent: {UserAgent}",
            request.Method,
            request.Path,
            request.QueryString,
            requestId,
            context.Connection.RemoteIpAddress,
            request.Headers.UserAgent.FirstOrDefault());

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway Request Error: {Method} {Path} - RequestId: {RequestId} - Error: {Error}",
                request.Method,
                request.Path,
                requestId,
                ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var response = context.Response;
            
            _logger.LogInformation("Gateway Response: {Method} {Path} - RequestId: {RequestId} - StatusCode: {StatusCode} - Duration: {Duration}ms",
                request.Method,
                request.Path,
                requestId,
                response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}