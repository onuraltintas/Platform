using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EgitimPlatform.Shared.Auditing.Services;

public class HttpAuditContextProvider : IAuditContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               context?.User?.FindFirst("sub")?.Value ??
               context?.User?.FindFirst("user_id")?.Value;
    }

    public string? GetCurrentUserName()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.FindFirst(ClaimTypes.Name)?.Value ??
               context?.User?.FindFirst("name")?.Value ??
               context?.User?.FindFirst("username")?.Value;
    }

    public string? GetCurrentSessionId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Session?.Id ??
               context?.Request?.Headers["X-Session-Id"].FirstOrDefault() ??
               context?.User?.FindFirst("session_id")?.Value;
    }

    public string? GetCurrentIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Check for forwarded IP addresses first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return ips[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetCurrentUserAgent()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request?.Headers["User-Agent"].FirstOrDefault();
    }

    public string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request?.Headers["X-Correlation-ID"].FirstOrDefault() ??
               context?.Request?.Headers["X-Request-ID"].FirstOrDefault() ??
               context?.TraceIdentifier;
    }

    public Dictionary<string, object> GetAdditionalContext()
    {
        var context = _httpContextAccessor.HttpContext;
        var additionalContext = new Dictionary<string, object>();

        if (context == null) return additionalContext;

        // Add request information
        additionalContext["RequestPath"] = context.Request.Path.Value ?? string.Empty;
        additionalContext["RequestMethod"] = context.Request.Method;
        additionalContext["RequestScheme"] = context.Request.Scheme;
        additionalContext["RequestHost"] = context.Request.Host.Value ?? string.Empty;

        // Add query string if present
        if (context.Request.QueryString.HasValue)
        {
            additionalContext["QueryString"] = context.Request.QueryString.Value ?? string.Empty;
        }

        // Add referer if present
        var referer = context.Request.Headers["Referer"].FirstOrDefault();
        if (!string.IsNullOrEmpty(referer))
        {
            additionalContext["Referer"] = referer;
        }

        // Add tenant information if available
        var tenantId = context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            additionalContext["TenantId"] = tenantId;
        }

        // Add client information
        var clientId = context.User?.FindFirst("client_id")?.Value;
        if (!string.IsNullOrEmpty(clientId))
        {
            additionalContext["ClientId"] = clientId;
        }

        // Add culture information
        var culture = context.Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrEmpty(culture))
        {
            additionalContext["Culture"] = culture;
        }

        // Add device information if available
        var deviceId = context.Request.Headers["X-Device-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceId))
        {
            additionalContext["DeviceId"] = deviceId;
        }

        var deviceType = context.Request.Headers["X-Device-Type"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceType))
        {
            additionalContext["DeviceType"] = deviceType;
        }

        // Add application version if available
        var appVersion = context.Request.Headers["X-App-Version"].FirstOrDefault();
        if (!string.IsNullOrEmpty(appVersion))
        {
            additionalContext["AppVersion"] = appVersion;
        }

        return additionalContext;
    }
}

public class SystemAuditContextProvider : IAuditContextProvider
{
    private readonly string _systemUserId;
    private readonly string _systemUserName;

    public SystemAuditContextProvider(string systemUserId = "SYSTEM", string systemUserName = "System")
    {
        _systemUserId = systemUserId;
        _systemUserName = systemUserName;
    }

    public string? GetCurrentUserId() => _systemUserId;

    public string? GetCurrentUserName() => _systemUserName;

    public string? GetCurrentSessionId() => null;

    public string? GetCurrentIpAddress() => "127.0.0.1";

    public string? GetCurrentUserAgent() => "System/1.0";

    public string? GetCorrelationId() => Guid.NewGuid().ToString();

    public Dictionary<string, object> GetAdditionalContext()
    {
        return new Dictionary<string, object>
        {
            ["Source"] = "System",
            ["MachineName"] = Environment.MachineName,
            ["ProcessId"] = Environment.ProcessId,
            ["ThreadId"] = Thread.CurrentThread.ManagedThreadId
        };
    }
}