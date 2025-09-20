using Enterprise.Shared.Logging.Interfaces;
using Enterprise.Shared.Logging.Models;

namespace Enterprise.Shared.Logging.Services;

/// <summary>
/// Factory for creating enterprise loggers
/// </summary>
public class EnterpriseLoggerFactory : IEnterpriseLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<LoggingSettings> _settings;
    private readonly ICorrelationContextAccessor? _correlationContextAccessor;

    public EnterpriseLoggerFactory(
        ILoggerFactory loggerFactory,
        IOptions<LoggingSettings> settings,
        ICorrelationContextAccessor? correlationContextAccessor = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _correlationContextAccessor = correlationContextAccessor;
    }

    /// <summary>
    /// Creates an enterprise logger for the specified type
    /// </summary>
    /// <typeparam name="T">Type to create logger for</typeparam>
    /// <returns>Enterprise logger instance</returns>
    public IEnterpriseLogger<T> CreateLogger<T>()
    {
        var logger = _loggerFactory.CreateLogger<T>();
        return new EnterpriseLogger<T>(logger, _settings, _correlationContextAccessor);
    }

    /// <summary>
    /// Creates an enterprise logger with the specified name
    /// </summary>
    /// <param name="name">Logger name</param>
    /// <returns>Enterprise logger instance</returns>
    public IEnterpriseLogger<object> CreateLogger(string name)
    {
        var logger = _loggerFactory.CreateLogger<object>();
        return new EnterpriseLogger<object>(logger, _settings, _correlationContextAccessor);
    }
}

