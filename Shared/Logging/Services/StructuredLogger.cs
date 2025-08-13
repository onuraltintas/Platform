using Serilog;
using Serilog.Events;

namespace EgitimPlatform.Shared.Logging.Services;

public class StructuredLogger : IStructuredLogger
{
    private readonly ILogger _logger;
    
    public StructuredLogger(ILogger logger)
    {
        _logger = logger;
    }
    
    public void LogTrace(string messageTemplate, params object[] args)
    {
        _logger.Verbose(messageTemplate, args);
    }
    
    public void LogDebug(string messageTemplate, params object[] args)
    {
        _logger.Debug(messageTemplate, args);
    }
    
    public void LogInformation(string messageTemplate, params object[] args)
    {
        _logger.Information(messageTemplate, args);
    }
    
    public void LogWarning(string messageTemplate, params object[] args)
    {
        _logger.Warning(messageTemplate, args);
    }
    
    public void LogError(string messageTemplate, params object[] args)
    {
        _logger.Error(messageTemplate, args);
    }
    
    public void LogError(Exception exception, string messageTemplate, params object[] args)
    {
        _logger.Error(exception, messageTemplate, args);
    }
    
    public void LogCritical(string messageTemplate, params object[] args)
    {
        _logger.Fatal(messageTemplate, args);
    }
    
    public void LogCritical(Exception exception, string messageTemplate, params object[] args)
    {
        _logger.Fatal(exception, messageTemplate, args);
    }
    
    public IStructuredLogger ForContext(string propertyName, object value)
    {
        return new StructuredLogger(_logger.ForContext(propertyName, value));
    }
    
    public IStructuredLogger ForContext<T>()
    {
        return new StructuredLogger(_logger.ForContext<T>());
    }
    
    public IStructuredLogger ForContext(Type sourceContext)
    {
        return new StructuredLogger(_logger.ForContext(sourceContext));
    }
    
    public void LogUserAction(string userId, string action, object? data = null)
    {
        _logger.ForContext("EventType", "UserAction")
               .ForContext("UserId", userId)
               .ForContext("Action", action)
               .ForContext("Data", data, true)
               .Information("User {UserId} performed action {Action}", userId, action);
    }
    
    public void LogSystemEvent(string eventName, object? data = null)
    {
        _logger.ForContext("EventType", "SystemEvent")
               .ForContext("EventName", eventName)
               .ForContext("Data", data, true)
               .Information("System event {EventName} occurred", eventName);
    }
    
    public void LogPerformance(string operationName, TimeSpan duration, object? data = null)
    {
        var logLevel = duration.TotalMilliseconds > 5000 ? LogEventLevel.Warning : LogEventLevel.Information;
        
        _logger.ForContext("EventType", "Performance")
               .ForContext("OperationName", operationName)
               .ForContext("Duration", duration.TotalMilliseconds)
               .ForContext("Data", data, true)
               .Write(logLevel, "Operation {OperationName} completed in {Duration}ms", operationName, duration.TotalMilliseconds);
    }
    
    public void LogSecurityEvent(string eventType, string? userId = null, object? data = null)
    {
        _logger.ForContext("EventType", "Security")
               .ForContext("SecurityEventType", eventType)
               .ForContext("UserId", userId)
               .ForContext("Data", data, true)
               .Warning("Security event {SecurityEventType} for user {UserId}", eventType, userId ?? "Anonymous");
    }
}