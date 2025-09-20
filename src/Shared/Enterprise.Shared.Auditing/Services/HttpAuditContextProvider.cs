namespace Enterprise.Shared.Auditing.Services;

/// <summary>
/// HTTP-based implementation of audit context provider
/// </summary>
public class HttpAuditContextProvider : IAuditContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpAuditContextProvider> _logger;
    private readonly AuditConfiguration _configuration;
    private readonly Dictionary<string, object> _contextProperties = new();

    /// <summary>
    /// Initializes a new instance of the HttpAuditContextProvider
    /// </summary>
    public HttpAuditContextProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuditConfiguration> configuration,
        ILogger<HttpAuditContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string? GetCurrentUserId()
    {
        try
        {
            var user = GetCurrentUser();
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user?.FindFirst("sub")?.Value
                   ?? user?.FindFirst("user_id")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current user ID");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentUsername()
    {
        try
        {
            var user = GetCurrentUser();
            return user?.FindFirst(ClaimTypes.Name)?.Value 
                   ?? user?.FindFirst("username")?.Value
                   ?? user?.FindFirst(ClaimTypes.Email)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current username");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentSessionId()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                return httpContext.Session.Id;
            }

            // Try to get from claims
            var user = GetCurrentUser();
            return user?.FindFirst("session_id")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current session ID");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentIpAddress()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Check for forwarded IP first (load balancer scenarios)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if there are multiple
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check other common forwarded headers
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current IP address");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentUserAgent()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current user agent");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentCorrelationId()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Check common correlation ID headers
            var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                               ?? httpContext.Request.Headers["X-Request-ID"].FirstOrDefault()
                               ?? httpContext.Request.Headers["Request-ID"].FirstOrDefault();

            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }

            // Try to get from trace identifier
            return httpContext.TraceIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current correlation ID");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentTraceId()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Try OpenTelemetry trace ID first
            var activity = System.Diagnostics.Activity.Current;
            if (activity != null)
            {
                return activity.TraceId.ToString();
            }

            // Fallback to HTTP context trace identifier
            return httpContext.TraceIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current trace ID");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentServiceName()
    {
        return _configuration.DefaultServiceName;
    }

    /// <inheritdoc />
    public string? GetCurrentEnvironment()
    {
        return _configuration.DefaultEnvironment;
    }

    /// <inheritdoc />
    public SecurityContext? GetCurrentSecurityContext()
    {
        try
        {
            var user = GetCurrentUser();
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return SecurityContext.FromClaimsPrincipal(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current security context");
            return null;
        }
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetCurrentUser()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.User;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving current user");
            return null;
        }
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetContextProperties()
    {
        return new Dictionary<string, object>(_contextProperties);
    }

    /// <inheritdoc />
    public void SetContextProperty(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            _contextProperties[key] = value;
        }
    }

    /// <inheritdoc />
    public void RemoveContextProperty(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            _contextProperties.Remove(key);
        }
    }

    /// <inheritdoc />
    public void ClearContextProperties()
    {
        _contextProperties.Clear();
    }
}