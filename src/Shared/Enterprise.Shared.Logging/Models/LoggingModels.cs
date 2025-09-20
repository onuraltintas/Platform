namespace Enterprise.Shared.Logging.Models;

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "LoggingSettings";

    /// <summary>
    /// Whether to log sensitive data (should be false in production)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable performance logging
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Threshold in milliseconds for slow query logging
    /// </summary>
    public double SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to enable distributed tracing
    /// </summary>
    public bool EnableDistributedTracing { get; set; } = true;

    /// <summary>
    /// Sampling rate for distributed tracing (0.0 to 1.0)
    /// </summary>
    public double SamplingRate { get; set; } = 0.1;

    /// <summary>
    /// List of sensitive field names to mask in logs
    /// </summary>
    public List<string> MaskingSensitiveFields { get; set; } = new()
    {
        "password", "creditCard", "ssn", "token", "secret", "key", "authorization"
    };

    /// <summary>
    /// Maximum number of properties to log per event
    /// </summary>
    public int MaxPropertiesPerEvent { get; set; } = 50;

    /// <summary>
    /// Maximum length of string properties before truncation
    /// </summary>
    public int MaxPropertyLength { get; set; } = 2000;

    /// <summary>
    /// Whether to enable structured logging
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable correlation ID enrichment
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>
    /// Whether to enable user context enrichment
    /// </summary>
    public bool EnableUserEnrichment { get; set; } = true;

    /// <summary>
    /// Whether to enable environment enrichment
    /// </summary>
    public bool EnableEnvironmentEnrichment { get; set; } = true;

    /// <summary>
    /// Service name for logging
    /// </summary>
    public string ServiceName { get; set; } = "Unknown Service";

    /// <summary>
    /// Service version for logging
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";
}

/// <summary>
/// Log event category enumeration
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// General application logs
    /// </summary>
    Application,

    /// <summary>
    /// Performance and timing logs
    /// </summary>
    Performance,

    /// <summary>
    /// Business event logs
    /// </summary>
    Business,

    /// <summary>
    /// Security-related logs
    /// </summary>
    Security,

    /// <summary>
    /// User activity logs
    /// </summary>
    UserActivity,

    /// <summary>
    /// API call logs
    /// </summary>
    Api,

    /// <summary>
    /// Database operation logs
    /// </summary>
    Database,

    /// <summary>
    /// Health check logs
    /// </summary>
    HealthCheck,

    /// <summary>
    /// System diagnostic logs
    /// </summary>
    System
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// Authentication events
    /// </summary>
    Authentication,

    /// <summary>
    /// Authorization events
    /// </summary>
    Authorization,

    /// <summary>
    /// Data access events
    /// </summary>
    DataAccess,

    /// <summary>
    /// Configuration changes
    /// </summary>
    ConfigurationChange,

    /// <summary>
    /// Privilege escalation
    /// </summary>
    PrivilegeEscalation,

    /// <summary>
    /// Suspicious activity
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Security policy violations
    /// </summary>
    PolicyViolation
}

/// <summary>
/// Performance metrics for logging
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Operation name
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Start time of the operation
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the operation
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metrics data
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();

    /// <summary>
    /// Creates performance metrics from a timespan
    /// </summary>
    public static PerformanceMetrics FromDuration(string operationName, TimeSpan duration, bool isSuccessful = true)
    {
        var endTime = DateTime.UtcNow;
        return new PerformanceMetrics
        {
            OperationName = operationName,
            Duration = duration,
            StartTime = endTime.Subtract(duration),
            EndTime = endTime,
            IsSuccessful = isSuccessful
        };
    }
}

/// <summary>
/// Log health status information
/// </summary>
public class LogHealthStatus
{
    /// <summary>
    /// Whether logging system is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Whether Seq sink is healthy
    /// </summary>
    public bool IsSeqHealthy { get; set; }

    /// <summary>
    /// Whether file sink is accessible
    /// </summary>
    public bool LogFileAccess { get; set; }

    /// <summary>
    /// Last log entry timestamp
    /// </summary>
    public DateTime? LastLogEntry { get; set; }

    /// <summary>
    /// Number of log entries today
    /// </summary>
    public long LogVolumeToday { get; set; }

    /// <summary>
    /// Current log level
    /// </summary>
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// Active enrichers
    /// </summary>
    public List<string> ActiveEnrichers { get; set; } = new();

    /// <summary>
    /// Active sinks
    /// </summary>
    public List<string> ActiveSinks { get; set; } = new();
}

/// <summary>
/// Correlation context for distributed tracing
/// </summary>
public class CorrelationContext
{
    /// <summary>
    /// Correlation ID for the current request
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Parent correlation ID for nested operations
    /// </summary>
    public string? ParentCorrelationId { get; set; }

    /// <summary>
    /// User ID associated with the request
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Session ID for the request
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Request ID for the current HTTP request
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Additional context properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Creates a new correlation context with generated ID
    /// </summary>
    public static CorrelationContext Create(string? parentId = null)
    {
        return new CorrelationContext
        {
            CorrelationId = Guid.NewGuid().ToString(),
            ParentCorrelationId = parentId
        };
    }
}