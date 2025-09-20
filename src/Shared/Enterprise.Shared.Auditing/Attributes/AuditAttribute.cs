namespace Enterprise.Shared.Auditing.Attributes;

/// <summary>
/// Attribute to mark methods or classes for auditing
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AuditAttribute : Attribute
{
    /// <summary>
    /// The action name for the audit event (optional - defaults to method name)
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// The resource name for the audit event (optional - defaults to class name)
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// The category for the audit event
    /// </summary>
    public AuditEventCategory Category { get; set; } = AuditEventCategory.Application;

    /// <summary>
    /// The severity for the audit event
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

    /// <summary>
    /// Whether to include method parameters in the audit event
    /// </summary>
    public bool IncludeParameters { get; set; } = false;

    /// <summary>
    /// Whether to include the return value in the audit event
    /// </summary>
    public bool IncludeReturnValue { get; set; } = false;

    /// <summary>
    /// Whether to audit successful operations
    /// </summary>
    public bool AuditSuccess { get; set; } = true;

    /// <summary>
    /// Whether to audit failed operations
    /// </summary>
    public bool AuditFailure { get; set; } = true;

    /// <summary>
    /// Tags to add to the audit event
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Details to add to the audit event
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Whether to exclude this method from auditing (used at method level when class is audited)
    /// </summary>
    public bool Exclude { get; set; } = false;

    /// <summary>
    /// Custom properties to add to the audit event (JSON format)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Initializes a new instance of the AuditAttribute
    /// </summary>
    public AuditAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AuditAttribute with action and resource
    /// </summary>
    /// <param name="action">The action name</param>
    /// <param name="resource">The resource name</param>
    public AuditAttribute(string action, string resource)
    {
        Action = action;
        Resource = resource;
    }

    /// <summary>
    /// Initializes a new instance of the AuditAttribute with action, resource, and category
    /// </summary>
    /// <param name="action">The action name</param>
    /// <param name="resource">The resource name</param>
    /// <param name="category">The audit category</param>
    public AuditAttribute(string action, string resource, AuditEventCategory category)
    {
        Action = action;
        Resource = resource;
        Category = category;
    }
}

/// <summary>
/// Attribute to mark methods or classes for security auditing
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SecurityAuditAttribute : AuditAttribute
{
    /// <summary>
    /// The type of security event
    /// </summary>
    public SecurityEventType EventType { get; set; } = SecurityEventType.Authorization;

    /// <summary>
    /// The required permission for the operation (if applicable)
    /// </summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// The required role for the operation (if applicable)
    /// </summary>
    public string? RequiredRole { get; set; }

    /// <summary>
    /// The risk score for this operation (0-100)
    /// </summary>
    [Range(0, 100)]
    public int RiskScore { get; set; } = 0;

    /// <summary>
    /// Whether to generate an alert for this operation
    /// </summary>
    public bool GenerateAlert { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the SecurityAuditAttribute
    /// </summary>
    public SecurityAuditAttribute()
    {
        Category = AuditEventCategory.Security;
        Severity = AuditSeverity.Information;
    }

    /// <summary>
    /// Initializes a new instance of the SecurityAuditAttribute with event type
    /// </summary>
    /// <param name="eventType">The security event type</param>
    public SecurityAuditAttribute(SecurityEventType eventType) : this()
    {
        EventType = eventType;
    }

    /// <summary>
    /// Initializes a new instance of the SecurityAuditAttribute with action and event type
    /// </summary>
    /// <param name="action">The action name</param>
    /// <param name="eventType">The security event type</param>
    public SecurityAuditAttribute(string action, SecurityEventType eventType) : this(eventType)
    {
        Action = action;
    }
}

/// <summary>
/// Attribute to exclude methods or classes from auditing
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class NoAuditAttribute : Attribute
{
    /// <summary>
    /// The reason for excluding from audit (optional)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Initializes a new instance of the NoAuditAttribute
    /// </summary>
    public NoAuditAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the NoAuditAttribute with reason
    /// </summary>
    /// <param name="reason">The reason for excluding from audit</param>
    public NoAuditAttribute(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// Attribute to mark sensitive data that should be masked in audit logs
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// The masking strategy to use
    /// </summary>
    public SensitiveDataMaskingStrategy MaskingStrategy { get; set; } = SensitiveDataMaskingStrategy.FullMask;

    /// <summary>
    /// The reason this data is sensitive (optional)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Whether to completely exclude this data from audit logs
    /// </summary>
    public bool ExcludeFromAudit { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the SensitiveDataAttribute
    /// </summary>
    public SensitiveDataAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the SensitiveDataAttribute with masking strategy
    /// </summary>
    /// <param name="maskingStrategy">The masking strategy</param>
    public SensitiveDataAttribute(SensitiveDataMaskingStrategy maskingStrategy)
    {
        MaskingStrategy = maskingStrategy;
    }

    /// <summary>
    /// Initializes a new instance of the SensitiveDataAttribute with masking strategy and reason
    /// </summary>
    /// <param name="maskingStrategy">The masking strategy</param>
    /// <param name="reason">The reason this data is sensitive</param>
    public SensitiveDataAttribute(SensitiveDataMaskingStrategy maskingStrategy, string reason)
    {
        MaskingStrategy = maskingStrategy;
        Reason = reason;
    }
}

/// <summary>
/// Strategies for masking sensitive data in audit logs
/// </summary>
public enum SensitiveDataMaskingStrategy
{
    /// <summary>
    /// Replace all characters with asterisks
    /// </summary>
    FullMask = 0,

    /// <summary>
    /// Show first 2 and last 2 characters, mask the rest
    /// </summary>
    PartialMask = 1,

    /// <summary>
    /// Show only the first few characters
    /// </summary>
    ShowFirst = 2,

    /// <summary>
    /// Show only the last few characters
    /// </summary>
    ShowLast = 3,

    /// <summary>
    /// Replace with a fixed placeholder
    /// </summary>
    Placeholder = 4,

    /// <summary>
    /// Hash the value (irreversible)
    /// </summary>
    Hash = 5
}