namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Represents a security-specific audit event
/// </summary>
public class SecurityAuditEvent : AuditEvent
{
    /// <summary>
    /// The type of security event
    /// </summary>
    [Required]
    public SecurityEventType EventType { get; set; }

    /// <summary>
    /// The security outcome (Success, Failed, Blocked, etc.)
    /// </summary>
    [Required]
    public SecurityOutcome Outcome { get; set; } = SecurityOutcome.Success;

    /// <summary>
    /// The authentication method used (if applicable)
    /// </summary>
    [StringLength(50)]
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// The role or permissions involved
    /// </summary>
    [StringLength(200)]
    public string? Role { get; set; }

    /// <summary>
    /// The permission or claim being checked
    /// </summary>
    [StringLength(200)]
    public string? Permission { get; set; }

    /// <summary>
    /// Risk score associated with the event (0-100)
    /// </summary>
    [Range(0, 100)]
    public int RiskScore { get; set; } = 0;

    /// <summary>
    /// Whether this event triggered an alert
    /// </summary>
    public bool IsAlert { get; set; }

    /// <summary>
    /// The source of the security event
    /// </summary>
    [StringLength(100)]
    public string? EventSource { get; set; }

    /// <summary>
    /// Geolocation information (if available)
    /// </summary>
    [StringLength(200)]
    public string? GeoLocation { get; set; }

    /// <summary>
    /// Device fingerprint (if available)
    /// </summary>
    [StringLength(500)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Whether the action was performed by an administrator
    /// </summary>
    public bool IsAdministrativeAction { get; set; }

    /// <summary>
    /// The target user (for administrative actions)
    /// </summary>
    [StringLength(100)]
    public string? TargetUserId { get; set; }

    /// <summary>
    /// The target username (for administrative actions)
    /// </summary>
    [StringLength(256)]
    public string? TargetUsername { get; set; }

    /// <summary>
    /// Additional security context
    /// </summary>
    public SecurityContext? SecurityContext { get; set; }

    /// <summary>
    /// Creates a new security audit event
    /// </summary>
    public static SecurityAuditEvent Create(
        SecurityEventType eventType, 
        string action, 
        string resource, 
        SecurityOutcome outcome = SecurityOutcome.Success)
    {
        return new SecurityAuditEvent
        {
            EventType = eventType,
            Action = action,
            Resource = resource,
            Outcome = outcome,
            Result = outcome.ToString(),
            Category = AuditEventCategory.Security,
            Severity = outcome == SecurityOutcome.Success ? AuditSeverity.Information : AuditSeverity.Warning
        };
    }

    /// <summary>
    /// Sets authentication-related information
    /// </summary>
    public SecurityAuditEvent WithAuthentication(string authMethod, string? role = null)
    {
        AuthenticationMethod = authMethod;
        Role = role;
        return this;
    }

    /// <summary>
    /// Sets authorization-related information
    /// </summary>
    public SecurityAuditEvent WithAuthorization(string? permission, string? role = null)
    {
        Permission = permission;
        Role = role;
        return this;
    }

    /// <summary>
    /// Sets risk assessment information
    /// </summary>
    public SecurityAuditEvent WithRisk(int riskScore, bool isAlert = false)
    {
        RiskScore = Math.Clamp(riskScore, 0, 100);
        IsAlert = isAlert;
        return this;
    }

    /// <summary>
    /// Sets location and device information
    /// </summary>
    public SecurityAuditEvent WithDevice(string? geoLocation = null, string? deviceFingerprint = null)
    {
        GeoLocation = geoLocation;
        DeviceFingerprint = deviceFingerprint;
        return this;
    }

    /// <summary>
    /// Sets target user information for administrative actions
    /// </summary>
    public SecurityAuditEvent WithTarget(string? targetUserId, string? targetUsername = null)
    {
        TargetUserId = targetUserId;
        TargetUsername = targetUsername;
        IsAdministrativeAction = !string.IsNullOrEmpty(targetUserId);
        return this;
    }

    /// <summary>
    /// Sets additional security context
    /// </summary>
    public SecurityAuditEvent WithSecurityContext(SecurityContext context)
    {
        SecurityContext = context;
        return this;
    }
}