using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public class CircuitBreakerFactory
{
    private readonly CircuitBreakerSettings _settings;
    private readonly ILogger<CircuitBreakerFactory> _logger;

    public CircuitBreakerFactory(IOptions<ErrorHandlingSettings> options, ILogger<CircuitBreakerFactory> logger)
    {
        _settings = options.Value.CircuitBreaker;
        _logger = logger;
    }

    public IAsyncPolicy<T> CreateCircuitBreaker<T>(string circuitName = "Default")
    {
        return Policy<T>
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _settings.FailureThreshold,
                durationOfBreak: _settings.BreakDuration,
                onBreak: (result, duration) =>
                {
                    _logger.LogWarning("Circuit breaker '{CircuitName}' opened for {Duration}. Recent failures: {FailureCount}", 
                        circuitName, duration, _settings.FailureThreshold);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker '{CircuitName}' reset successfully", circuitName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker '{CircuitName}' is half-open, testing next request", circuitName);
                });
    }

    public IAsyncPolicy<T> CreateAdvancedCircuitBreaker<T>(string circuitName = "Advanced")
    {
        return Policy<T>
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5, // 50% failure rate
                samplingDuration: _settings.SamplingDuration,
                minimumThroughput: _settings.MinimumThroughput,
                durationOfBreak: _settings.BreakDuration,
                onBreak: (result, duration) =>
                {
                    _logger.LogWarning("Advanced circuit breaker '{CircuitName}' opened for {Duration} due to high failure rate", 
                        circuitName, duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Advanced circuit breaker '{CircuitName}' reset successfully", circuitName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Advanced circuit breaker '{CircuitName}' is half-open, testing next request", circuitName);
                });
    }

    public IAsyncPolicy<HttpResponseMessage> CreateHttpCircuitBreaker(string circuitName = "Http")
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _settings.FailureThreshold,
                durationOfBreak: _settings.BreakDuration,
                onBreak: (result, duration) =>
                {
                    _logger.LogWarning("HTTP circuit breaker '{CircuitName}' opened for {Duration}", circuitName, duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("HTTP circuit breaker '{CircuitName}' reset successfully", circuitName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("HTTP circuit breaker '{CircuitName}' is half-open", circuitName);
                });
    }

    public IAsyncPolicy CreateCombinedPolicy(string policyName = "Combined")
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var retryLogger = loggerFactory.CreateLogger<RetryPolicyFactory>();
        
        var retryFactory = new RetryPolicyFactory(
            Options.Create(new ErrorHandlingSettings { RetryPolicy = new RetryPolicySettings() }), 
            retryLogger);

        var retryPolicy = retryFactory.CreateGenericRetryPolicy<object>();
        var circuitBreakerPolicy = CreateCircuitBreaker<object>(policyName);

        return (IAsyncPolicy)Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }
}