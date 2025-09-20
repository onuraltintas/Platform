namespace Enterprise.Shared.Logging.Extensions.Attributes;

/// <summary>
/// Attribute for marking methods that should have performance logging
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LogPerformanceAttribute : Attribute
{
    /// <summary>
    /// Custom operation name for logging (optional)
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Whether to log method parameters (be careful with sensitive data)
    /// </summary>
    public bool LogParameters { get; set; } = false;

    /// <summary>
    /// Whether to log method return value (be careful with sensitive data)
    /// </summary>
    public bool LogResult { get; set; } = false;

    /// <summary>
    /// Whether to log exceptions that occur during method execution
    /// </summary>
    public bool LogExceptions { get; set; } = true;

    /// <summary>
    /// Minimum duration in milliseconds to log (helps reduce noise)
    /// </summary>
    public double MinimumDurationMs { get; set; } = 0;

    /// <summary>
    /// Log level for successful operations
    /// </summary>
    public LogLevel SuccessLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Log level for slow operations
    /// </summary>
    public LogLevel SlowLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Threshold in milliseconds to consider an operation slow
    /// </summary>
    public double SlowThresholdMs { get; set; } = 1000;
}

/// <summary>
/// Attribute for marking methods that should log business events
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogBusinessEventAttribute : Attribute
{
    /// <summary>
    /// Business event name
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Whether to include method parameters in event properties
    /// </summary>
    public bool IncludeParameters { get; set; } = false;

    /// <summary>
    /// Whether to include method result in event properties
    /// </summary>
    public bool IncludeResult { get; set; } = false;

    public LogBusinessEventAttribute(string eventName)
    {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
    }
}

/// <summary>
/// Attribute for marking methods that should log security events
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogSecurityEventAttribute : Attribute
{
    /// <summary>
    /// Security event type
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Whether to include method parameters in event properties
    /// </summary>
    public bool IncludeParameters { get; set; } = true;

    /// <summary>
    /// Whether to always log, regardless of success/failure
    /// </summary>
    public bool AlwaysLog { get; set; } = true;

    public LogSecurityEventAttribute(string eventType)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
    }
}