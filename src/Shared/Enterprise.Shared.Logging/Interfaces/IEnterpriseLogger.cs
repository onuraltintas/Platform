namespace Enterprise.Shared.Logging.Interfaces;

/// <summary>
/// Enhanced enterprise logger interface with structured logging capabilities
/// </summary>
public interface IEnterpriseLogger<T> : ILogger<T>
{
    /// <summary>
    /// Logs performance metrics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation being measured</param>
    /// <param name="duration">Time taken for the operation</param>
    /// <param name="properties">Additional properties to log</param>
    void LogPerformance(string operationName, TimeSpan duration, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a business event with structured data
    /// </summary>
    /// <param name="eventName">Name of the business event</param>
    /// <param name="properties">Event-specific properties</param>
    void LogBusinessEvent(string eventName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a security-related event
    /// </summary>
    /// <param name="eventType">Type of security event</param>
    /// <param name="properties">Security event properties</param>
    void LogSecurityEvent(string eventType, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs user activity
    /// </summary>
    /// <param name="action">Action performed by the user</param>
    /// <param name="userId">ID of the user</param>
    /// <param name="properties">Additional user activity properties</param>
    void LogUserActivity(string action, string userId, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs API call performance and status
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="duration">Request duration</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="properties">Additional properties</param>
    void LogApiCall(string method, string endpoint, TimeSpan duration, int statusCode, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs database operation performance
    /// </summary>
    /// <param name="operation">Database operation type</param>
    /// <param name="commandText">SQL command or operation description</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="properties">Additional properties</param>
    void LogDatabaseOperation(string operation, string commandText, TimeSpan duration, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Creates a logging scope with operation name and properties
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="properties">Properties to include in the scope</param>
    /// <returns>Disposable scope</returns>
    IDisposable BeginScope(string operationName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Creates a timed operation scope that automatically logs performance when disposed
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="properties">Properties to include in the scope</param>
    /// <returns>Disposable timed scope</returns>
    IDisposable BeginTimedScope(string operationName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an exception with context information
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="context">Context where the exception occurred</param>
    /// <param name="properties">Additional properties</param>
    void LogException(Exception exception, string context, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a system health check result
    /// </summary>
    /// <param name="componentName">Name of the component being checked</param>
    /// <param name="isHealthy">Health status</param>
    /// <param name="duration">Time taken for the health check</param>
    /// <param name="properties">Additional health check properties</param>
    void LogHealthCheck(string componentName, bool isHealthy, TimeSpan duration, Dictionary<string, object>? properties = null);
}

/// <summary>
/// Factory interface for creating enterprise loggers
/// </summary>
public interface IEnterpriseLoggerFactory
{
    /// <summary>
    /// Creates an enterprise logger for the specified type
    /// </summary>
    /// <typeparam name="T">Type to create logger for</typeparam>
    /// <returns>Enterprise logger instance</returns>
    IEnterpriseLogger<T> CreateLogger<T>();

    /// <summary>
    /// Creates an enterprise logger with the specified name
    /// </summary>
    /// <param name="name">Logger name</param>
    /// <returns>Enterprise logger instance</returns>
    IEnterpriseLogger<object> CreateLogger(string name);
}

/// <summary>
/// Disposable timed operation scope
/// </summary>
public interface ITimedOperationScope : IDisposable
{
    /// <summary>
    /// Operation name
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Elapsed time since scope creation
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Additional properties for the operation
    /// </summary>
    Dictionary<string, object> Properties { get; }

    /// <summary>
    /// Adds a property to the operation scope
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    void AddProperty(string key, object value);

    /// <summary>
    /// Marks the operation as failed
    /// </summary>
    /// <param name="exception">Exception that caused the failure</param>
    void MarkAsFailed(Exception? exception = null);
}