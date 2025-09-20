namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for security auditing and logging
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Logs a successful authentication
    /// </summary>
    Task LogAuthenticationSuccessAsync(string userId, string method, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Logs a failed authentication attempt
    /// </summary>
    Task LogAuthenticationFailureAsync(string identifier, string method, string reason, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Logs an authorization failure
    /// </summary>
    Task LogAuthorizationFailureAsync(string userId, string resource, string action, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(SecurityEventType eventType, string description, SecurityEventSeverity severity, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Logs suspicious activity
    /// </summary>
    Task LogSuspiciousActivityAsync(string source, string activity, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Gets security events
    /// </summary>
    Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(DateTime from, DateTime to, SecurityEventType? eventType = null);

    /// <summary>
    /// Gets failed login attempts for a user
    /// </summary>
    Task<int> GetFailedLoginAttemptsAsync(string identifier, TimeSpan window);

    /// <summary>
    /// Checks if an IP is blocked
    /// </summary>
    Task<bool> IsIpBlockedAsync(string ipAddress);

    /// <summary>
    /// Blocks an IP address
    /// </summary>
    Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason);

    /// <summary>
    /// Unblocks an IP address
    /// </summary>
    Task UnblockIpAddressAsync(string ipAddress);
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    Authentication,
    Authorization,
    TokenGeneration,
    TokenRevocation,
    PasswordChange,
    AccountLockout,
    SuspiciousActivity,
    DataAccess,
    ConfigurationChange,
    SecurityPolicyViolation
}

/// <summary>
/// Security event severity levels
/// </summary>
public enum SecurityEventSeverity
{
    Information,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents a security event
/// </summary>
public class SecurityEvent
{
    public Guid Id { get; set; }
    public SecurityEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}