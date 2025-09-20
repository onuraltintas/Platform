using System.Collections.Concurrent;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Resilience.Bulkhead;

public class BulkheadService : IBulkheadService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, BulkheadHealthInfo> _healthInfo = new();
    private readonly ResilienceSettings _settings;
    private readonly ILogger<BulkheadService> _logger;

    public BulkheadService(
        IOptions<ResilienceSettings> settings,
        ILogger<BulkheadService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string bulkheadKey = "default", 
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetOrCreateSemaphore(bulkheadKey);
        var healthInfo = GetOrCreateHealthInfo(bulkheadKey);
        
        var waitStartTime = DateTime.UtcNow;
        var acquired = await semaphore.WaitAsync(_settings.Timeout.DefaultTimeoutMs, cancellationToken);
        
        if (!acquired)
        {
            healthInfo.TotalRejections++;
            
            if (_settings.Bulkhead.EnableBulkheadLogging)
            {
                _logger.LogWarning("Bulkhead {BulkheadKey} rejected operation due to capacity limit", bulkheadKey);
            }
            
            throw new BulkheadRejectedException(bulkheadKey, 
                $"Operation rejected by bulkhead: {bulkheadKey}");
        }

        var waitTime = DateTime.UtcNow - waitStartTime;
        healthInfo.TotalWaitTime += waitTime;
        healthInfo.AverageWaitTime = TimeSpan.FromTicks(healthInfo.TotalWaitTime.Ticks / Math.Max(healthInfo.TotalExecutions + 1, 1));

        try
        {
            healthInfo.CurrentExecutions++;
            healthInfo.TotalExecutions++;
            
            var executionStartTime = DateTime.UtcNow;
            var result = await operation().ConfigureAwait(false);
            var executionTime = DateTime.UtcNow - executionStartTime;
            
            healthInfo.TotalExecutionTime += executionTime;
            healthInfo.AverageExecutionTime = TimeSpan.FromTicks(healthInfo.TotalExecutionTime.Ticks / healthInfo.TotalExecutions);
            healthInfo.LastSuccessfulExecution = DateTime.UtcNow;

            if (_settings.Bulkhead.EnableBulkheadLogging)
            {
                _logger.LogDebug("Bulkhead {BulkheadKey} operation completed in {ExecutionTime}ms", 
                    bulkheadKey, executionTime.TotalMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            healthInfo.TotalFailures++;
            healthInfo.LastFailure = DateTime.UtcNow;
            healthInfo.LastFailureReason = ex.Message;
            
            if (_settings.Bulkhead.EnableBulkheadLogging)
            {
                _logger.LogError(ex, "Operation failed in bulkhead {BulkheadKey}", bulkheadKey);
            }
            
            throw;
        }
        finally
        {
            healthInfo.CurrentExecutions--;
            semaphore.Release();
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string bulkheadKey = "default", 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, bulkheadKey, cancellationToken);
    }

    public BulkheadStats GetBulkheadStats(string bulkheadKey)
    {
        var healthInfo = GetOrCreateHealthInfo(bulkheadKey);

        var semaphore = _semaphores.TryGetValue(bulkheadKey, out var sem) ? sem : null;
        var maxParallelization = healthInfo.MaxParallelization > 0 
            ? healthInfo.MaxParallelization 
            : _settings.Bulkhead.MaxParallelization;

        return new BulkheadStats
        {
            BulkheadKey = bulkheadKey,
            MaxParallelization = maxParallelization,
            CurrentExecutions = healthInfo.CurrentExecutions,
            AvailableSlots = semaphore?.CurrentCount ?? maxParallelization,
            TotalExecutions = healthInfo.TotalExecutions,
            TotalRejections = healthInfo.TotalRejections,
            TotalFailures = healthInfo.TotalFailures,
            AverageWaitTime = healthInfo.AverageWaitTime,
            AverageExecutionTime = healthInfo.AverageExecutionTime,
            SuccessRate = healthInfo.TotalExecutions > 0 
                ? (double)(healthInfo.TotalExecutions - healthInfo.TotalFailures) / healthInfo.TotalExecutions 
                : 0
        };
    }

    public Task<BulkheadHealthInfo> GetHealthInfoAsync(string bulkheadKey)
    {
        var healthInfo = GetOrCreateHealthInfo(bulkheadKey);
        return Task.FromResult(healthInfo);
    }

    public Dictionary<string, BulkheadStats> GetAllBulkheadStats()
    {
        return _healthInfo.Keys.ToDictionary(key => key, GetBulkheadStats);
    }

    private SemaphoreSlim GetOrCreateSemaphore(string bulkheadKey)
    {
        return _semaphores.GetOrAdd(bulkheadKey, 
            key => new SemaphoreSlim(_settings.Bulkhead.MaxParallelization, _settings.Bulkhead.MaxParallelization));
    }

    private BulkheadHealthInfo GetOrCreateHealthInfo(string bulkheadKey)
    {
        return _healthInfo.GetOrAdd(bulkheadKey, key => new BulkheadHealthInfo
        {
            BulkheadKey = key,
            MaxParallelization = _settings.Bulkhead.MaxParallelization,
            CreatedAt = DateTime.UtcNow
        });
    }
}