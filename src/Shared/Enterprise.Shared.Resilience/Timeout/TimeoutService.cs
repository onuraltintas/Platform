using System.Collections.Concurrent;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Resilience.Timeout;

public class TimeoutService : ITimeoutService
{
    private readonly Dictionary<string, TimeSpan> _namedTimeouts;
    private readonly ConcurrentDictionary<string, TimeoutHealthInfo> _healthInfo = new();
    private readonly ResilienceSettings _settings;
    private readonly ILogger<TimeoutService> _logger;

    public TimeoutService(
        IOptions<ResilienceSettings> settings,
        ILogger<TimeoutService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        _namedTimeouts = new Dictionary<string, TimeSpan>
        {
            ["default"] = TimeSpan.FromMilliseconds(_settings.Timeout.DefaultTimeoutMs),
            ["http"] = TimeSpan.FromMilliseconds(_settings.Timeout.HttpTimeoutMs),
            ["database"] = TimeSpan.FromMilliseconds(_settings.Timeout.DatabaseTimeoutMs),
            ["file"] = TimeSpan.FromMilliseconds(5000),
            ["external-api"] = TimeSpan.FromMilliseconds(30000)
        };
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await operation().ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;
            
            if (_settings.Timeout.EnableTimeoutLogging && 
                duration > TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * 0.8))
            {
                _logger.LogWarning("Operation completed close to timeout. Duration: {Duration}, Timeout: {Timeout}",
                    duration, timeout);
            }
            
            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            var duration = DateTime.UtcNow - startTime;
            
            if (_settings.Timeout.EnableTimeoutLogging)
            {
                _logger.LogWarning("Operation timed out after {Duration} (timeout: {Timeout})", 
                    duration, timeout);
            }
            
            throw new TimeoutException($"Operation timed out after {duration} (timeout: {timeout})");
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string timeoutKey = "default", 
        CancellationToken cancellationToken = default)
    {
        var timeout = GetTimeoutForKey(timeoutKey);
        var healthInfo = GetOrCreateHealthInfo(timeoutKey);
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            healthInfo.TotalAttempts++;
            var result = await ExecuteAsync(operation, timeout, cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            healthInfo.TotalSuccesses++;
            healthInfo.TotalDuration += duration;
            healthInfo.AverageDuration = TimeSpan.FromTicks(healthInfo.TotalDuration.Ticks / healthInfo.TotalAttempts);
            
            if (duration > healthInfo.MaxDuration)
                healthInfo.MaxDuration = duration;
            
            if (duration < healthInfo.MinDuration || healthInfo.MinDuration == TimeSpan.Zero)
                healthInfo.MinDuration = duration;

            if (_settings.Timeout.EnableTimeoutLogging)
            {
                _logger.LogDebug("Timeout operation {TimeoutKey} completed in {Duration}ms", 
                    timeoutKey, duration.TotalMilliseconds);
            }
                
            return result;
        }
        catch (TimeoutException)
        {
            var duration = DateTime.UtcNow - startTime;
            healthInfo.TotalTimeouts++;
            healthInfo.LastTimeout = DateTime.UtcNow;
            healthInfo.LastTimeoutDuration = duration;

            if (_settings.Timeout.EnableTimeoutLogging)
            {
                _logger.LogError("Timeout operation {TimeoutKey} timed out after {Duration}ms", 
                    timeoutKey, duration.TotalMilliseconds);
            }
            
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, timeout, cancellationToken);
    }

    public async Task ExecuteAsync(Func<Task> operation, string timeoutKey = "default", 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, timeoutKey, cancellationToken);
    }

    public TimeoutStats GetTimeoutStats(string timeoutKey)
    {
        var healthInfo = _healthInfo.TryGetValue(timeoutKey, out var info) 
            ? info 
            : new TimeoutHealthInfo { TimeoutKey = timeoutKey };

        return new TimeoutStats
        {
            TimeoutKey = timeoutKey,
            ConfiguredTimeout = healthInfo.ConfiguredTimeout,
            TotalAttempts = healthInfo.TotalAttempts,
            TotalSuccesses = healthInfo.TotalSuccesses,
            TotalTimeouts = healthInfo.TotalTimeouts,
            AverageDuration = healthInfo.AverageDuration,
            MinDuration = healthInfo.MinDuration,
            MaxDuration = healthInfo.MaxDuration,
            TimeoutRate = healthInfo.TotalAttempts > 0 
                ? (double)healthInfo.TotalTimeouts / healthInfo.TotalAttempts 
                : 0,
            LastTimeout = healthInfo.LastTimeout
        };
    }

    public Dictionary<string, TimeoutStats> GetAllTimeoutStats()
    {
        return _healthInfo.Keys.ToDictionary(key => key, GetTimeoutStats);
    }

    private TimeSpan GetTimeoutForKey(string timeoutKey)
    {
        return _namedTimeouts.TryGetValue(timeoutKey, out var timeout) 
            ? timeout 
            : TimeSpan.FromMilliseconds(_settings.Timeout.DefaultTimeoutMs);
    }

    private TimeoutHealthInfo GetOrCreateHealthInfo(string timeoutKey)
    {
        return _healthInfo.GetOrAdd(timeoutKey, key => new TimeoutHealthInfo
        {
            TimeoutKey = key,
            ConfiguredTimeout = GetTimeoutForKey(key),
            CreatedAt = DateTime.UtcNow
        });
    }
}