using System.Threading.RateLimiting;

namespace EgitimPlatform.Gateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("too many requests"))
        {
            _logger.LogWarning("Rate limit exceeded for {IpAddress} on {Path}", 
                context.Connection.RemoteIpAddress, 
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    error = "Too Many Requests",
                    message = "Rate limit exceeded. Please try again later.",
                    statusCode = 429,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        }
    }
}