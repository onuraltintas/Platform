namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Represents a generic audit event in the system
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Unique identifier for the audit event
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The action that was performed
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The resource or entity that was affected
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the resource that was affected (if applicable)
    /// </summary>
    [StringLength(100)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// The result of the action (Success, Failed, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// The user who performed the action
    /// </summary>
    [StringLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// The username of the user who performed the action
    /// </summary>
    [StringLength(256)]
    public string? Username { get; set; }

    /// <summary>
    /// The session ID (if applicable)
    /// </summary>
    [StringLength(200)]
    public string? SessionId { get; set; }

    /// <summary>
    /// The IP address of the client
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// The service or application that generated the event
    /// </summary>
    [StringLength(100)]
    public string? ServiceName { get; set; }

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Trace ID for distributed tracing
    /// </summary>
    [StringLength(100)]
    public string? TraceId { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Details about the event or error message
    /// </summary>
    [StringLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// The category of the audit event
    /// </summary>
    public AuditEventCategory Category { get; set; } = AuditEventCategory.Application;

    /// <summary>
    /// The severity level of the event
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// The version of the event schema
    /// </summary>
    [StringLength(10)]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Environment where the event occurred (Development, Staging, Production)
    /// </summary>
    [StringLength(50)]
    public string? Environment { get; set; }

    /// <summary>
    /// Tags for categorization and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets additional properties as a dictionary
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Serialized properties as JSON for storage
    /// </summary>
    [JsonPropertyName("properties")]
    public string? PropertiesJson
    {
        get => Properties.Count > 0 ? JsonSerializer.Serialize(Properties) : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(value) ?? new();
                }
                catch
                {
                    Properties = new();
                }
            }
        }
    }

    /// <summary>
    /// Creates a new audit event with the specified action and resource
    /// </summary>
    public static AuditEvent Create(string action, string resource, string result = "Success")
    {
        return new AuditEvent
        {
            Action = action,
            Resource = resource,
            Result = result
        };
    }

    /// <summary>
    /// Adds metadata to the audit event
    /// </summary>
    public AuditEvent WithMetadata(object metadata)
    {
        if (metadata != null)
        {
            Metadata = JsonSerializer.Serialize(metadata);
        }
        return this;
    }

    /// <summary>
    /// Adds a property to the audit event
    /// </summary>
    public AuditEvent WithProperty(string key, object value)
    {
        Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the user information for the audit event
    /// </summary>
    public AuditEvent WithUser(string? userId, string? username = null)
    {
        UserId = userId;
        Username = username;
        return this;
    }

    /// <summary>
    /// Sets the HTTP context information
    /// </summary>
    public AuditEvent WithHttpContext(string? ipAddress, string? userAgent, string? sessionId = null)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
        SessionId = sessionId;
        return this;
    }

    /// <summary>
    /// Sets the correlation and trace IDs
    /// </summary>
    public AuditEvent WithCorrelation(string? correlationId, string? traceId = null)
    {
        CorrelationId = correlationId;
        TraceId = traceId;
        return this;
    }

    /// <summary>
    /// Sets the duration of the operation
    /// </summary>
    public AuditEvent WithDuration(long durationMs)
    {
        DurationMs = durationMs;
        return this;
    }

    /// <summary>
    /// Adds tags to the audit event
    /// </summary>
    public AuditEvent WithTags(params string[] tags)
    {
        if (tags?.Length > 0)
        {
            Tags.AddRange(tags);
        }
        return this;
    }

    /// <summary>
    /// Sets the severity of the audit event
    /// </summary>
    public AuditEvent WithSeverity(AuditSeverity severity)
    {
        Severity = severity;
        return this;
    }

    /// <summary>
    /// Sets the category of the audit event
    /// </summary>
    public AuditEvent WithCategory(AuditEventCategory category)
    {
        Category = category;
        return this;
    }
}