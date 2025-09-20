using Enterprise.Shared.Logging.Interfaces;
using Enterprise.Shared.Logging.Models;

namespace Enterprise.Shared.Logging.Services;

/// <summary>
/// Enhanced enterprise logger implementation with structured logging
/// </summary>
public class EnterpriseLogger<T> : IEnterpriseLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly LoggingSettings _settings;
    private readonly ICorrelationContextAccessor? _correlationContextAccessor;

    public EnterpriseLogger(
        ILogger<T> logger,
        IOptions<LoggingSettings> settings,
        ICorrelationContextAccessor? correlationContextAccessor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _correlationContextAccessor = correlationContextAccessor;
    }

    #region ILogger<T> Implementation

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);

    #endregion

    #region Performance Logging

    public void LogPerformance(string operationName, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        if (!_settings.EnablePerformanceLogging) return;

        var logProperties = CreateBaseProperties(LogCategory.Performance);
        logProperties["OperationName"] = operationName;
        logProperties["DurationMs"] = Math.Round(duration.TotalMilliseconds, 2);

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        var level = duration.TotalMilliseconds > _settings.SlowQueryThresholdMs 
            ? LogLevel.Warning 
            : LogLevel.Information;

        _logger.Log(level, "Performance: {OperationName} completed in {DurationMs}ms {@Properties}",
            operationName, Math.Round(duration.TotalMilliseconds, 2), logProperties);
    }

    public void LogDatabaseOperation(string operation, string commandText, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        if (!_settings.EnablePerformanceLogging) return;

        var logProperties = CreateBaseProperties(LogCategory.Database);
        logProperties["Operation"] = operation;
        logProperties["CommandText"] = TruncateString(commandText, 500); // Limit SQL length
        logProperties["DurationMs"] = Math.Round(duration.TotalMilliseconds, 2);

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        var level = duration.TotalMilliseconds > _settings.SlowQueryThresholdMs 
            ? LogLevel.Warning 
            : LogLevel.Debug;

        _logger.Log(level, "Database: {Operation} completed in {DurationMs}ms {@Properties}",
            operation, Math.Round(duration.TotalMilliseconds, 2), logProperties);
    }

    #endregion

    #region Business and Security Logging

    public void LogBusinessEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.Business);
        logProperties["EventName"] = eventName;

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        _logger.LogInformation("Business Event: {EventName} {@Properties}", 
            eventName, logProperties);
    }

    public void LogSecurityEvent(string eventType, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.Security);
        logProperties["EventType"] = eventType;
        logProperties["Timestamp"] = DateTime.UtcNow;

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        _logger.LogWarning("Security Event: {EventType} {@Properties}", 
            eventType, logProperties);
    }

    public void LogUserActivity(string action, string userId, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.UserActivity);
        logProperties["Action"] = action;
        logProperties["UserId"] = userId;

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        _logger.LogInformation("User Activity: {Action} by {UserId} {@Properties}",
            action, userId, logProperties);
    }

    #endregion

    #region API and System Logging

    public void LogApiCall(string method, string endpoint, TimeSpan duration, int statusCode, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.Api);
        logProperties["Method"] = method;
        logProperties["Endpoint"] = endpoint;
        logProperties["DurationMs"] = Math.Round(duration.TotalMilliseconds, 2);
        logProperties["StatusCode"] = statusCode;

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        var level = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(level, "API Call: {Method} {Endpoint} returned {StatusCode} in {DurationMs}ms {@Properties}",
            method, endpoint, statusCode, Math.Round(duration.TotalMilliseconds, 2), logProperties);
    }

    public void LogException(Exception exception, string context, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.System);
        logProperties["Context"] = context;
        logProperties["ExceptionType"] = exception.GetType().Name;

        MergeProperties(logProperties, properties);
        MaskSensitiveData(logProperties);

        _logger.LogError(exception, "Exception in {Context}: {ExceptionMessage} {@Properties}",
            context, exception.Message, logProperties);
    }

    public void LogHealthCheck(string componentName, bool isHealthy, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        var logProperties = CreateBaseProperties(LogCategory.HealthCheck);
        logProperties["ComponentName"] = componentName;
        logProperties["IsHealthy"] = isHealthy;
        logProperties["DurationMs"] = Math.Round(duration.TotalMilliseconds, 2);

        MergeProperties(logProperties, properties);

        var level = isHealthy ? LogLevel.Information : LogLevel.Warning;

        _logger.Log(level, "Health Check: {ComponentName} is {HealthStatus} (checked in {DurationMs}ms) {@Properties}",
            componentName, isHealthy ? "Healthy" : "Unhealthy", Math.Round(duration.TotalMilliseconds, 2), logProperties);
    }

    #endregion

    #region Scope Management

    public IDisposable BeginScope(string operationName, Dictionary<string, object>? properties = null)
    {
        var scopeProperties = CreateBaseProperties();
        scopeProperties["OperationName"] = operationName;

        MergeProperties(scopeProperties, properties);
        MaskSensitiveData(scopeProperties);

        return _logger.BeginScope("Operation: {OperationName} {@Properties}", operationName, scopeProperties) ?? 
               new NullDisposable();
    }

    public IDisposable BeginTimedScope(string operationName, Dictionary<string, object>? properties = null)
    {
        return new TimedOperationScope<T>(this, operationName, properties);
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, object> CreateBaseProperties(LogCategory? category = null)
    {
        var properties = new Dictionary<string, object>();

        if (category.HasValue)
        {
            properties["Category"] = category.Value.ToString();
        }

        // Add correlation context if available
        var correlationContext = _correlationContextAccessor?.CorrelationContext;
        if (correlationContext != null)
        {
            if (!string.IsNullOrEmpty(correlationContext.CorrelationId))
                properties["CorrelationId"] = correlationContext.CorrelationId;
            
            if (!string.IsNullOrEmpty(correlationContext.UserId))
                properties["UserId"] = correlationContext.UserId;
            
            if (!string.IsNullOrEmpty(correlationContext.SessionId))
                properties["SessionId"] = correlationContext.SessionId;
            
            if (!string.IsNullOrEmpty(correlationContext.RequestId))
                properties["RequestId"] = correlationContext.RequestId;
        }

        return properties;
    }

    private void MergeProperties(Dictionary<string, object> target, Dictionary<string, object>? source)
    {
        if (source == null) return;

        foreach (var kvp in source.Take(_settings.MaxPropertiesPerEvent))
        {
            if (kvp.Value is string stringValue)
            {
                target[kvp.Key] = TruncateString(stringValue, _settings.MaxPropertyLength);
            }
            else
            {
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    private void MaskSensitiveData(Dictionary<string, object> properties)
    {
        if (!_settings.EnableSensitiveDataLogging)
        {
            foreach (var sensitiveField in _settings.MaskingSensitiveFields)
            {
                foreach (var key in properties.Keys.ToList())
                {
                    if (key.Contains(sensitiveField, StringComparison.OrdinalIgnoreCase))
                    {
                        properties[key] = "***MASKED***";
                    }
                }
            }
        }
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..(maxLength - 3)] + "...";
    }

    #endregion
}

/// <summary>
/// Timed operation scope that automatically logs performance metrics
/// </summary>
internal class TimedOperationScope<T> : ITimedOperationScope
{
    private readonly IEnterpriseLogger<T> _logger;
    private readonly Stopwatch _stopwatch;
    private bool _disposed = false;
    private bool _failed = false;
    private Exception? _exception;

    public string OperationName { get; }
    public Dictionary<string, object> Properties { get; }
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TimedOperationScope(IEnterpriseLogger<T> logger, string operationName, Dictionary<string, object>? properties = null)
    {
        _logger = logger;
        OperationName = operationName;
        Properties = properties ?? new Dictionary<string, object>();
        _stopwatch = Stopwatch.StartNew();
    }

    public void AddProperty(string key, object value)
    {
        Properties[key] = value;
    }

    public void MarkAsFailed(Exception? exception = null)
    {
        _failed = true;
        _exception = exception;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _stopwatch.Stop();
        
        Properties["Success"] = !_failed;
        if (_exception != null)
        {
            Properties["Error"] = _exception.Message;
        }

        _logger.LogPerformance(OperationName, _stopwatch.Elapsed, Properties);
        
        _disposed = true;
    }
}

/// <summary>
/// Null implementation of IDisposable for safe null handling
/// </summary>
internal class NullDisposable : IDisposable
{
    public void Dispose()
    {
        // No-op
    }
}