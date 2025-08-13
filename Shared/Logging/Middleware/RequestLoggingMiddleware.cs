using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Text;

namespace EgitimPlatform.Shared.Logging.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly HashSet<string> _sensitiveHeaders;
    private readonly HashSet<string> _excludedPaths;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
        _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "Set-Cookie", "X-API-Key", "Authentication"
        };
        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/health", "/metrics", "/favicon.ico"
        };
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipLogging(context))
        {
            await _next(context);
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        var requestInfo = await CaptureRequestInfo(context);
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            await LogRequest(context, requestInfo, stopwatch.ElapsedMilliseconds, traceId);
        }
    }
    
    private bool ShouldSkipLogging(HttpContext context)
    {
        return _excludedPaths.Contains(context.Request.Path.Value ?? string.Empty);
    }
    
    private async Task<RequestInfo> CaptureRequestInfo(HttpContext context)
    {
        var request = context.Request;
        var requestInfo = new RequestInfo
        {
            Method = request.Method,
            Path = request.Path.Value ?? string.Empty,
            QueryString = request.QueryString.Value ?? string.Empty,
            ContentType = request.ContentType ?? string.Empty,
            UserAgent = request.Headers.UserAgent.ToString(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Headers = GetSafeHeaders(request.Headers)
        };
        
        if (ShouldLogRequestBody(request))
        {
            requestInfo.Body = await ReadRequestBody(request);
        }
        
        return requestInfo;
    }
    
    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        return headers
            .Where(h => !_sensitiveHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());
    }
    
    private bool ShouldLogRequestBody(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength > 50000) // 50KB limit
            return false;
            
        var contentType = request.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || 
               contentType.Contains("application/xml") || 
               contentType.Contains("text/");
    }
    
    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            var buffer = new byte[request.ContentLength ?? 0];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;
            return Encoding.UTF8.GetString(buffer);
        }
        catch
        {
            return "[Error reading request body]";
        }
    }
    
    private async Task LogRequest(HttpContext context, RequestInfo requestInfo, long elapsedMs, string traceId)
    {
        var response = context.Response;
        var logLevel = GetLogLevel(response.StatusCode, elapsedMs);
        
        _logger.ForContext("TraceId", traceId)
               .ForContext("RequestInfo", requestInfo, true)
               .ForContext("StatusCode", response.StatusCode)
               .ForContext("ElapsedMs", elapsedMs)
               .ForContext("ContentType", response.ContentType)
               .Write(logLevel, 
                      "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                      requestInfo.Method,
                      requestInfo.Path,
                      response.StatusCode,
                      elapsedMs);
    }
    
    private LogEventLevel GetLogLevel(int statusCode, long elapsedMs)
    {
        if (statusCode >= 500) return LogEventLevel.Error;
        if (statusCode >= 400) return LogEventLevel.Warning;
        if (elapsedMs > 5000) return LogEventLevel.Warning;
        return LogEventLevel.Information;
    }
}

public class RequestInfo
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string RemoteIpAddress { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}