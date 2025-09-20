using System.Collections.Concurrent;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace Enterprise.Shared.Resilience.CircuitBreaker;

public class PollyCircuitBreakerService : ICircuitBreakerService
{
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _circuitBreakers = new();
    private readonly ConcurrentDictionary<string, CircuitBreakerHealthInfo> _healthInfo = new();
    private readonly ResilienceSettings _settings;
    private readonly ILogger<PollyCircuitBreakerService> _logger;

    public PollyCircuitBreakerService(
        IOptions<ResilienceSettings> settings,
        ILogger<PollyCircuitBreakerService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string circuitBreakerKey, 
        CancellationToken cancellationToken = default)
    {
        var pipeline = GetOrCreateCircuitBreaker(circuitBreakerKey);
        var healthInfo = GetOrCreateHealthInfo(circuitBreakerKey);

        var startTime = DateTime.UtcNow;
        
        try
        {
            healthInfo.TotalRequests++;
            
            var result = await pipeline.ExecuteAsync(async (context) =>
            {
                return await operation().ConfigureAwait(false);
            }, cancellationToken);

            healthInfo.SuccessfulRequests++;
            healthInfo.LastSuccessTime = DateTime.UtcNow;
            UpdateHealthInfo(healthInfo);

            if (_settings.CircuitBreaker.EnableLogging)
            {
                _logger.LogDebug("Circuit breaker {CircuitBreakerKey} executed successfully in {Duration}ms",
                    circuitBreakerKey, (DateTime.UtcNow - startTime).TotalMilliseconds);
            }

            return result;
        }
        catch (BrokenCircuitException)
        {
            if (_settings.CircuitBreaker.EnableLogging)
            {
                _logger.LogWarning("Circuit breaker {CircuitBreakerKey} is open, rejecting call",
                    circuitBreakerKey);
            }

            throw new ServiceUnavailableException(circuitBreakerKey, 
                $"Circuit breaker is open for {circuitBreakerKey}");
        }
        catch (Exception ex)
        {
            healthInfo.FailedRequests++;
            healthInfo.LastFailureTime = DateTime.UtcNow;
            UpdateHealthInfo(healthInfo);

            if (_settings.CircuitBreaker.EnableLogging)
            {
                _logger.LogError(ex, "Circuit breaker {CircuitBreakerKey} operation failed", 
                    circuitBreakerKey);
            }

            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string circuitBreakerKey, 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, circuitBreakerKey, cancellationToken);
    }

    public CircuitBreakerState GetCircuitBreakerState(string key)
    {
        if (!_circuitBreakers.TryGetValue(key, out var pipeline))
        {
            return CircuitBreakerState.Closed;
        }

        var context = ResilienceContextPool.Shared.Get();
        try
        {
            // Try to get circuit breaker state through pipeline properties
            // This is a simplified approach - in real implementation you'd need to track state
            var healthInfo = GetOrCreateHealthInfo(key);
            return healthInfo.State;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    public void ResetCircuitBreaker(string key)
    {
        if (_circuitBreakers.TryRemove(key, out var pipeline))
        {
            if (_settings.CircuitBreaker.EnableLogging)
            {
                _logger.LogInformation("Circuit breaker {CircuitBreakerKey} has been reset", key);
            }
        }

        if (_healthInfo.TryGetValue(key, out var healthInfo))
        {
            healthInfo.State = CircuitBreakerState.Closed;
            healthInfo.StateLastChangedTime = DateTime.UtcNow;
        }
    }

    public void IsolateCircuitBreaker(string key)
    {
        // Remove existing pipeline to force recreation with isolated state
        if (_circuitBreakers.TryRemove(key, out var pipeline))
        {
            // Pipeline will be recreated on next access
        }

        var healthInfo = GetOrCreateHealthInfo(key);
        healthInfo.State = CircuitBreakerState.Isolated;
        healthInfo.StateLastChangedTime = DateTime.UtcNow;

        if (_settings.CircuitBreaker.EnableLogging)
        {
            _logger.LogWarning("Circuit breaker {CircuitBreakerKey} has been isolated", key);
        }
    }

    public void CloseCircuitBreaker(string key)
    {
        var healthInfo = GetOrCreateHealthInfo(key);
        healthInfo.State = CircuitBreakerState.Closed;
        healthInfo.StateLastChangedTime = DateTime.UtcNow;

        if (_settings.CircuitBreaker.EnableLogging)
        {
            _logger.LogInformation("Circuit breaker {CircuitBreakerKey} has been closed", key);
        }
    }

    public CircuitBreakerHealthInfo GetCircuitBreakerHealthInfo(string key)
    {
        return GetOrCreateHealthInfo(key);
    }

    public Dictionary<string, CircuitBreakerState> GetAllCircuitBreakerStates()
    {
        return _healthInfo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State);
    }

    private ResiliencePipeline GetOrCreateCircuitBreaker(string key)
    {
        return _circuitBreakers.GetOrAdd(key, CreateCircuitBreaker);
    }

    private ResiliencePipeline CreateCircuitBreaker(string key)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = (double)_settings.CircuitBreaker.FailureThreshold / 100.0,
                MinimumThroughput = _settings.CircuitBreaker.MinimumThroughput,
                SamplingDuration = _settings.CircuitBreaker.SamplingDuration,
                BreakDuration = _settings.CircuitBreaker.BreakDuration,
                OnOpened = args =>
                {
                    if (_healthInfo.TryGetValue(key, out var healthInfo))
                    {
                        healthInfo.State = CircuitBreakerState.Open;
                        healthInfo.StateLastChangedTime = DateTime.UtcNow;
                    }

                    if (_settings.CircuitBreaker.EnableLogging)
                    {
                        _logger.LogWarning("Circuit breaker {CircuitBreakerKey} opened due to failures", key);
                    }
                    return default;
                },
                OnClosed = args =>
                {
                    if (_healthInfo.TryGetValue(key, out var healthInfo))
                    {
                        healthInfo.State = CircuitBreakerState.Closed;
                        healthInfo.StateLastChangedTime = DateTime.UtcNow;
                    }

                    if (_settings.CircuitBreaker.EnableLogging)
                    {
                        _logger.LogInformation("Circuit breaker {CircuitBreakerKey} closed", key);
                    }
                    return default;
                },
                OnHalfOpened = args =>
                {
                    if (_healthInfo.TryGetValue(key, out var healthInfo))
                    {
                        healthInfo.State = CircuitBreakerState.HalfOpen;
                        healthInfo.StateLastChangedTime = DateTime.UtcNow;
                    }

                    if (_settings.CircuitBreaker.EnableLogging)
                    {
                        _logger.LogInformation("Circuit breaker {CircuitBreakerKey} half-opened", key);
                    }
                    return default;
                }
            })
            .Build();

        if (_settings.CircuitBreaker.EnableLogging)
        {
            _logger.LogDebug("Created circuit breaker for key: {CircuitBreakerKey}", key);
        }

        return pipeline;
    }

    private CircuitBreakerHealthInfo GetOrCreateHealthInfo(string key)
    {
        return _healthInfo.GetOrAdd(key, k => new CircuitBreakerHealthInfo
        {
            CircuitBreakerKey = k,
            State = CircuitBreakerState.Closed,
            CreatedAt = DateTime.UtcNow,
            StateLastChangedTime = DateTime.UtcNow
        });
    }

    private void UpdateHealthInfo(CircuitBreakerHealthInfo healthInfo)
    {
        healthInfo.FailureRate = healthInfo.TotalRequests > 0 
            ? (double)healthInfo.FailedRequests / healthInfo.TotalRequests 
            : 0;

        healthInfo.TimeInCurrentState = DateTime.UtcNow - (healthInfo.StateLastChangedTime ?? healthInfo.CreatedAt);
    }
}