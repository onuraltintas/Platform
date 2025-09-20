namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for security auditing and logging
/// </summary>
public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly IMemoryCache _cache;
    private readonly SecuritySettings _settings;

    // In-memory storage for demo (should use database in production)
    private readonly List<SecurityEvent> _events = new();
    private readonly Dictionary<string, DateTime> _blockedIps = new();
    private readonly Dictionary<string, List<DateTime>> _failedAttempts = new();

    public SecurityAuditService(
        ILogger<SecurityAuditService> logger,
        IMemoryCache cache,
        IOptions<SecuritySettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _logger.LogDebug("Security audit service initialized");
    }

    public async Task LogAuthenticationSuccessAsync(string userId, string method, Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.Authentication,
            Description = $"Successful authentication via {method}",
            Severity = SecurityEventSeverity.Information,
            OccurredAt = DateTime.UtcNow,
            UserId = userId,
            AdditionalData = additionalData ?? new Dictionary<string, object>()
        };

        _events.Add(securityEvent);
        _logger.LogInformation("Authentication success logged for user {UserId} via {Method}", userId, method);

        await Task.CompletedTask;
    }

    public async Task LogAuthenticationFailureAsync(string identifier, string method, string reason, Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.Authentication,
            Description = $"Failed authentication via {method}: {reason}",
            Severity = SecurityEventSeverity.Medium,
            OccurredAt = DateTime.UtcNow,
            UserId = identifier,
            AdditionalData = additionalData ?? new Dictionary<string, object>()
        };

        _events.Add(securityEvent);

        // Track failed attempts
        if (!_failedAttempts.ContainsKey(identifier))
        {
            _failedAttempts[identifier] = new List<DateTime>();
        }
        _failedAttempts[identifier].Add(DateTime.UtcNow);

        // Check if should trigger lockout
        var maxAttempts = _settings.MaxFailedLoginAttempts ?? 5;
        var window = TimeSpan.FromMinutes(_settings.FailedLoginWindowMinutes ?? 30);
        var recentAttempts = await GetFailedLoginAttemptsAsync(identifier, window);

        if (recentAttempts >= maxAttempts)
        {
            await LogSecurityEventAsync(
                SecurityEventType.AccountLockout,
                $"Account locked due to {recentAttempts} failed login attempts",
                SecurityEventSeverity.High,
                new Dictionary<string, object> { ["identifier"] = identifier });
        }

        _logger.LogWarning("Authentication failure logged for {Identifier} via {Method}: {Reason}", identifier, method, reason);

        await Task.CompletedTask;
    }

    public async Task LogAuthorizationFailureAsync(string userId, string resource, string action, Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.Authorization,
            Description = $"Authorization denied for action '{action}' on resource '{resource}'",
            Severity = SecurityEventSeverity.Medium,
            OccurredAt = DateTime.UtcNow,
            UserId = userId,
            AdditionalData = additionalData ?? new Dictionary<string, object>()
        };

        _events.Add(securityEvent);
        _logger.LogWarning("Authorization failure for user {UserId}: {Action} on {Resource}", userId, action, resource);

        await Task.CompletedTask;
    }

    public async Task LogSecurityEventAsync(SecurityEventType eventType, string description, SecurityEventSeverity severity, Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Description = description,
            Severity = severity,
            OccurredAt = DateTime.UtcNow,
            AdditionalData = additionalData ?? new Dictionary<string, object>()
        };

        _events.Add(securityEvent);

        // Log based on severity
        switch (severity)
        {
            case SecurityEventSeverity.Critical:
                _logger.LogCritical("Critical security event: {EventType} - {Description}", eventType, description);
                break;
            case SecurityEventSeverity.High:
                _logger.LogError("High severity security event: {EventType} - {Description}", eventType, description);
                break;
            case SecurityEventSeverity.Medium:
                _logger.LogWarning("Medium severity security event: {EventType} - {Description}", eventType, description);
                break;
            default:
                _logger.LogInformation("Security event: {EventType} - {Description}", eventType, description);
                break;
        }

        // Limit events in memory (keep last 10000)
        if (_events.Count > 10000)
        {
            _events.RemoveRange(0, _events.Count - 10000);
        }

        await Task.CompletedTask;
    }

    public async Task LogSuspiciousActivityAsync(string source, string activity, Dictionary<string, object>? additionalData = null)
    {
        await LogSecurityEventAsync(
            SecurityEventType.SuspiciousActivity,
            $"Suspicious activity detected from {source}: {activity}",
            SecurityEventSeverity.High,
            additionalData);
    }

    public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(DateTime from, DateTime to, SecurityEventType? eventType = null)
    {
        var query = _events.Where(e => e.OccurredAt >= from && e.OccurredAt <= to);

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        var results = query.OrderByDescending(e => e.OccurredAt).ToList();

        await Task.CompletedTask;
        return results;
    }

    public async Task<int> GetFailedLoginAttemptsAsync(string identifier, TimeSpan window)
    {
        if (string.IsNullOrEmpty(identifier))
            return 0;

        if (!_failedAttempts.TryGetValue(identifier, out var attempts))
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.Subtract(window);
        var recentAttempts = attempts.Count(a => a > cutoff);

        // Clean old attempts
        _failedAttempts[identifier] = attempts.Where(a => a > cutoff).ToList();

        await Task.CompletedTask;
        return recentAttempts;
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        // Check cache first
        var cacheKey = $"blocked_ip_{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out bool _))
        {
            return true;
        }

        // Check in-memory storage
        if (_blockedIps.TryGetValue(ipAddress, out var blockedUntil))
        {
            if (blockedUntil > DateTime.UtcNow)
            {
                _cache.Set(cacheKey, true, blockedUntil - DateTime.UtcNow);
                return true;
            }
            else
            {
                _blockedIps.Remove(ipAddress);
            }
        }

        await Task.CompletedTask;
        return false;
    }

    public async Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason)
    {
        if (string.IsNullOrEmpty(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        var blockedUntil = DateTime.UtcNow.Add(duration);
        _blockedIps[ipAddress] = blockedUntil;

        // Cache the block
        var cacheKey = $"blocked_ip_{ipAddress}";
        _cache.Set(cacheKey, true, duration);

        // Log the event
        await LogSecurityEventAsync(
            SecurityEventType.SuspiciousActivity,
            $"IP address {ipAddress} blocked: {reason}",
            SecurityEventSeverity.High,
            new Dictionary<string, object>
            {
                ["ipAddress"] = ipAddress,
                ["blockedUntil"] = blockedUntil,
                ["reason"] = reason
            });

        _logger.LogWarning("IP address {IpAddress} blocked until {BlockedUntil}: {Reason}", ipAddress, blockedUntil, reason);
    }

    public async Task UnblockIpAddressAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        _blockedIps.Remove(ipAddress);

        // Remove from cache
        var cacheKey = $"blocked_ip_{ipAddress}";
        _cache.Remove(cacheKey);

        _logger.LogInformation("IP address {IpAddress} unblocked", ipAddress);

        await Task.CompletedTask;
    }
}