using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EgitimPlatform.Shared.Observability.Tracing;

namespace EgitimPlatform.Shared.Observability.Middleware;

public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TracingMiddleware> _logger;

    public TracingMiddleware(RequestDelegate next, ILogger<TracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var url = $"{context.Request.Scheme}://{context.Request.Host}{path}{context.Request.QueryString}";
        var route = context.GetEndpoint()?.DisplayName ?? path;

        using var activity = ApplicationActivitySource.StartHttpActivity("HTTP Request", method, url, route);
        
        try
        {
            // Add additional context to the span
            activity?.SetCustomTag("http.client_ip", GetClientIpAddress(context))
                    ?.SetCustomTag("http.request_id", context.TraceIdentifier)
                    ?.SetCustomTag("user.agent", context.Request.Headers.UserAgent.ToString());

            // Add user information if available
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value ?? 
                            context.User.FindFirst("user_id")?.Value ?? 
                            context.User.Identity.Name;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    activity?.SetUserTags(userId, context.User.FindFirst("email")?.Value);
                }
            }

            // Add request size if available
            if (context.Request.ContentLength.HasValue)
            {
                activity?.SetCustomTag("http.request_content_length", context.Request.ContentLength.Value);
            }

            // Add content type
            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                activity?.SetCustomTag("http.request_content_type", context.Request.ContentType);
            }

            activity?.AddEvent("request.start");

            await _next(context);

            var statusCode = context.Response.StatusCode;
            
            // Set response information
            activity?.SetHttpResult(statusCode, context.Request.Headers.UserAgent.ToString());
            
            if (context.Response.ContentLength.HasValue)
            {
                activity?.SetCustomTag("http.response_content_length", context.Response.ContentLength.Value);
            }

            if (!string.IsNullOrEmpty(context.Response.ContentType))
            {
                activity?.SetCustomTag("http.response_content_type", context.Response.ContentType);
            }

            activity?.AddEvent("request.complete");

            _logger.LogDebug("HTTP {Method} {Path} completed with status {StatusCode}. TraceId: {TraceId}",
                method, path, statusCode, activity?.GetTraceId());
        }
        catch (Exception ex)
        {
            activity?.AddException(ex)
                    ?.AddEvent("request.error");

            _logger.LogError(ex, "HTTP {Method} {Path} failed. TraceId: {TraceId}",
                method, path, activity?.GetTraceId());

            throw;
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}