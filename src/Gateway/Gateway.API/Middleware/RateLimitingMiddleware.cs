using Gateway.Core.Configuration;
using Gateway.Core.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;

namespace Gateway.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly GatewayOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitCache;
    private readonly Timer _cleanupTimer;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<GatewayOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _rateLimitCache = new ConcurrentDictionary<string, RateLimitInfo>();
        
        // Cleanup expired entries every minute
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.RateLimiting.Enabled)
        {
            await _next(context);
            return;
        }

        var rateLimitResult = await CheckRateLimitAsync(context);
        
        if (rateLimitResult.IsExceeded)
        {
            await HandleRateLimitExceededAsync(context, rateLimitResult);
            return;
        }

        // Add rate limit headers to response
        AddRateLimitHeaders(context.Response, rateLimitResult);

        await _next(context);
    }

    private async Task<RateLimitInfo> CheckRateLimitAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        var userId = GetUserIdentifier(context);
        
        var key = $"{clientId}:{endpoint}:{userId}";
        var now = DateTime.UtcNow;

        var rateLimitInfo = _rateLimitCache.GetOrAdd(key, _ => new RateLimitInfo
        {
            Key = key,
            WindowStart = now,
            Window = TimeSpan.FromMinutes(1),
            Limit = GetRateLimitForEndpoint(endpoint, userId != null),
            RequestCount = 0
        });

        lock (rateLimitInfo)
        {
            // Check if we need to reset the window
            if (now - rateLimitInfo.WindowStart >= rateLimitInfo.Window)
            {
                rateLimitInfo.WindowStart = now;
                rateLimitInfo.RequestCount = 0;
                rateLimitInfo.RetryAfter = null;
            }

            rateLimitInfo.RequestCount++;

            if (rateLimitInfo.RequestCount > rateLimitInfo.Limit)
            {
                var resetTime = rateLimitInfo.WindowStart.Add(rateLimitInfo.Window);
                rateLimitInfo.RetryAfter = resetTime - now;
                
                _logger.LogWarning("Rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}", 
                    key, rateLimitInfo.RequestCount, rateLimitInfo.Limit);
            }
        }

        return rateLimitInfo;
    }

    private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitInfo rateLimitInfo)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        if (rateLimitInfo.RetryAfter.HasValue)
        {
            context.Response.Headers.Add("Retry-After", ((int)rateLimitInfo.RetryAfter.Value.TotalSeconds).ToString());
        }

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {rateLimitInfo.Limit} per minute",
            retryAfter = rateLimitInfo.RetryAfter?.TotalSeconds
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private static void AddRateLimitHeaders(HttpResponse response, RateLimitInfo rateLimitInfo)
    {
        response.Headers.Add("X-RateLimit-Limit", rateLimitInfo.Limit.ToString());
        response.Headers.Add("X-RateLimit-Remaining", Math.Max(0, rateLimitInfo.Limit - rateLimitInfo.RequestCount).ToString());
        
        var resetTime = rateLimitInfo.WindowStart.Add(rateLimitInfo.Window);
        response.Headers.Add("X-RateLimit-Reset", ((DateTimeOffset)resetTime).ToUnixTimeSeconds().ToString());
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get API key first
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            return $"apikey:{apiKeyHeader.FirstOrDefault()?[..8]}...";
        }

        // Get IP address
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var clientIp = !string.IsNullOrEmpty(forwardedFor) ? forwardedFor.Split(',')[0].Trim() : remoteIp;

        return $"ip:{clientIp}";
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method.ToUpper();

        // Group similar endpoints
        if (path.StartsWith("/api/identity"))
            return "identity";
        if (path.StartsWith("/api/users"))
            return "users";
        if (path.StartsWith("/api/notifications"))
            return "notifications";
        if (path.StartsWith("/health"))
            return "health";

        return $"{method}:{path}";
    }

    private string? GetUserIdentifier(HttpContext context)
    {
        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst("sub")?.Value ?? 
                   context.User.FindFirst("user_id")?.Value ?? 
                   context.User.Identity.Name;
        }

        return null;
    }

    private int GetRateLimitForEndpoint(string endpoint, bool isAuthenticated)
    {
        // Check endpoint-specific limits first
        var endpointLimit = _options.RateLimiting.EndpointLimits
            .FirstOrDefault(el => el.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase));

        if (endpointLimit != null)
        {
            return endpointLimit.RequestsPerMinute;
        }

        // Use per-user limits for authenticated users
        if (isAuthenticated && _options.RateLimiting.EnablePerUserLimits)
        {
            return _options.RateLimiting.AuthenticatedUserRequestsPerMinute;
        }

        // Default limit
        return _options.RateLimiting.RequestsPerMinute;
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new List<string>();

        foreach (var kvp in _rateLimitCache)
        {
            var info = kvp.Value;
            lock (info)
            {
                // Remove entries that haven't been used in the last hour
                if (now - info.WindowStart > TimeSpan.FromHours(1))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
        }

        foreach (var key in expiredKeys)
        {
            _rateLimitCache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredKeys.Count);
        }
    }
}