namespace EgitimPlatform.Shared.Logging.Services;

public interface IStructuredLogger
{
    void LogTrace(string messageTemplate, params object[] args);
    void LogDebug(string messageTemplate, params object[] args);
    void LogInformation(string messageTemplate, params object[] args);
    void LogWarning(string messageTemplate, params object[] args);
    void LogError(string messageTemplate, params object[] args);
    void LogError(Exception exception, string messageTemplate, params object[] args);
    void LogCritical(string messageTemplate, params object[] args);
    void LogCritical(Exception exception, string messageTemplate, params object[] args);
    
    IStructuredLogger ForContext(string propertyName, object value);
    IStructuredLogger ForContext<T>();
    IStructuredLogger ForContext(Type sourceContext);
    
    void LogUserAction(string userId, string action, object? data = null);
    void LogSystemEvent(string eventName, object? data = null);
    void LogPerformance(string operationName, TimeSpan duration, object? data = null);
    void LogSecurityEvent(string eventType, string? userId = null, object? data = null);
}