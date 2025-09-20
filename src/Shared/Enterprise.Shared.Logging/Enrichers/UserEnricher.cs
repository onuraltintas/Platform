using Serilog.Core;
using Serilog.Events;

namespace Enterprise.Shared.Logging.Enrichers;

/// <summary>
/// Serilog enricher that adds user information to log events
/// </summary>
public class UserEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public UserEnricher(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        var user = httpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true) return;

        // Add user ID
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                    user.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserId", userId));
        }

        // Add user email
        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                       user.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(userEmail))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserEmail", userEmail));
        }

        // Add user name
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? 
                      user.FindFirst("name")?.Value;
        if (!string.IsNullOrEmpty(userName))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserName", userName));
        }

        // Add user roles
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Length > 0)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserRoles", roles));
        }

        // Add client IP
        var clientIp = GetClientIpAddress(httpContext);
        if (!string.IsNullOrEmpty(clientIp))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ClientIp", clientIp));
        }

        // Add user agent
        var userAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserAgent", userAgent));
        }
    }

    private static string? GetClientIpAddress(HttpContext? context)
    {
        if (context == null) return null;

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

        return context.Connection.RemoteIpAddress?.ToString();
    }
}