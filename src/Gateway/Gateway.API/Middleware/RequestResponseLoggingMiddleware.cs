using Gateway.Core.Configuration;
using Gateway.Core.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace Gateway.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly GatewayOptions _options;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<GatewayOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldLog(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        // Log request
        var gatewayRequest = await LogRequestAsync(context, requestId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, gatewayRequest, stopwatch.Elapsed, responseBody);

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<GatewayRequest> LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        var gatewayRequest = new GatewayRequest
        {
            RequestId = requestId,
            Method = request.Method,
            Path = request.Path.Value ?? "",
            QueryString = request.QueryString.Value ?? "",
            IpAddress = GetClientIpAddress(context),
            UserAgent = request.Headers.UserAgent.ToString(),
            ContentLength = request.ContentLength ?? 0,
            ContentType = request.ContentType,
            Timestamp = DateTime.UtcNow
        };

        // Extract user information if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            gatewayRequest.UserId = context.User.FindFirst("sub")?.Value ?? 
                                   context.User.FindFirst("user_id")?.Value;
            gatewayRequest.UserEmail = context.User.FindFirst("email")?.Value;
        }

        // Copy headers (excluding sensitive ones)
        foreach (var header in request.Headers)
        {
            if (!IsSensitiveHeader(header.Key))
            {
                gatewayRequest.Headers[header.Key] = string.Join(",", header.Value);
            }
        }

        if (_options.EnableRequestLogging)
        {
            _logger.LogInformation("Gateway Request: {RequestId} {Method} {Path} from {IpAddress} User: {UserId}",
                requestId, gatewayRequest.Method, gatewayRequest.Path, gatewayRequest.IpAddress, gatewayRequest.UserId ?? "Anonymous");

            // Log request body for POST/PUT requests (if enabled and not too large)
            if (ShouldLogRequestBody(request) && request.ContentLength < 10000) // Max 10KB
            {
                var requestBody = await ReadRequestBodyAsync(request);
                if (!string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogDebug("Request Body {RequestId}: {Body}", requestId, requestBody);
                }
            }
        }

        return gatewayRequest;
    }

    private async Task LogResponseAsync(HttpContext context, GatewayRequest gatewayRequest, TimeSpan duration, MemoryStream responseBody)
    {
        var response = context.Response;
        var gatewayResponse = new GatewayResponse
        {
            RequestId = gatewayRequest.RequestId,
            StatusCode = response.StatusCode,
            ContentLength = responseBody.Length,
            ContentType = response.ContentType,
            Duration = duration,
            Timestamp = DateTime.UtcNow
        };

        // Copy response headers (excluding sensitive ones)
        foreach (var header in response.Headers)
        {
            if (!IsSensitiveHeader(header.Key))
            {
                gatewayResponse.Headers[header.Key] = string.Join(",", header.Value);
            }
        }

        // Add error message for error responses
        if (response.StatusCode >= 400)
        {
            gatewayResponse.ErrorMessage = GetErrorMessage(response.StatusCode);
        }

        if (_options.EnableResponseLogging)
        {
            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            
            _logger.Log(logLevel, "Gateway Response: {RequestId} {StatusCode} {Duration}ms Size: {ContentLength}bytes User: {UserId}",
                gatewayResponse.RequestId, gatewayResponse.StatusCode, duration.TotalMilliseconds, 
                gatewayResponse.ContentLength, gatewayRequest.UserId ?? "Anonymous");

            // Log response body for errors (if enabled and not too large)
            if (ShouldLogResponseBody(response) && responseBody.Length < 10000) // Max 10KB
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);
                
                if (!string.IsNullOrEmpty(responseBodyText))
                {
                    _logger.LogDebug("Response Body {RequestId}: {Body}", gatewayResponse.RequestId, responseBodyText);
                }
            }
        }

        // Log performance metrics
        if (duration.TotalMilliseconds > 5000) // Slow requests (>5s)
        {
            _logger.LogWarning("Slow Request: {RequestId} {Method} {Path} took {Duration}ms",
                gatewayRequest.RequestId, gatewayRequest.Method, gatewayRequest.Path, duration.TotalMilliseconds);
        }
    }

    private bool ShouldLog(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Don't log health check requests (too noisy)
        if (path.StartsWith("/health"))
            return false;

        // Don't log static files
        if (path.Contains(".") && (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".ico")))
            return false;

        return true;
    }

    private bool ShouldLogRequestBody(HttpRequest request)
    {
        return (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH") &&
               request.ContentType?.Contains("application/json") == true;
    }

    private bool ShouldLogResponseBody(HttpResponse response)
    {
        return response.StatusCode >= 400 && 
               response.ContentType?.Contains("application/json") == true;
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization", "x-api-key", "cookie", "set-cookie", 
            "x-forwarded-for", "x-real-ip", "x-original-forwarded-for"
        };

        return sensitiveHeaders.Contains(headerName.ToLower());
    }

    private static string GetClientIpAddress(HttpContext context)
    {
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

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            
            return body;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => $"HTTP {statusCode}"
        };
    }
}