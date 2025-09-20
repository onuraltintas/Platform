using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Resilience.RateLimit;

public class RateLimitService : IRateLimitService, IDisposable
{
    private readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new();
    private readonly ConcurrentDictionary<string, RateLimitHealthInfo> _healthInfo = new();
    private readonly ResilienceSettings _settings;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(
        IOptions<ResilienceSettings> settings,
        ILogger<RateLimitService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string rateLimitKey = "default", 
        CancellationToken cancellationToken = default)
    {
        var rateLimiter = GetOrCreateRateLimiter(rateLimitKey);
        var healthInfo = GetOrCreateHealthInfo(rateLimitKey);

        healthInfo.TotalRequests++;

        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);
        
        if (!lease.IsAcquired)
        {
            healthInfo.RejectedRequests++;
            healthInfo.LastRejection = DateTime.UtcNow;

            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterMetadata) 
                ? retryAfterMetadata 
                : _settings.RateLimit.Window;

            if (_settings.RateLimit.EnableRateLimitLogging)
            {
                _logger.LogWarning("Rate limit exceeded for {RateLimitKey}. Retry after {RetryAfter}", 
                    rateLimitKey, retryAfter);
            }

            throw new RateLimitExceededException(rateLimitKey, retryAfter,
                $"Rate limit exceeded for {rateLimitKey}. Retry after {retryAfter}");
        }

        try
        {
            healthInfo.PermittedRequests++;
            healthInfo.LastPermission = DateTime.UtcNow;

            var result = await operation().ConfigureAwait(false);

            if (_settings.RateLimit.EnableRateLimitLogging)
            {
                _logger.LogDebug("Rate limited operation {RateLimitKey} completed successfully", rateLimitKey);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (_settings.RateLimit.EnableRateLimitLogging)
            {
                _logger.LogError(ex, "Rate limited operation {RateLimitKey} failed", rateLimitKey);
            }
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string rateLimitKey = "default", 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, rateLimitKey, cancellationToken);
    }

    public async Task<bool> TryAcquireAsync(string rateLimitKey = "default", int permits = 1, 
        CancellationToken cancellationToken = default)
    {
        var rateLimiter = GetOrCreateRateLimiter(rateLimitKey);
        var healthInfo = GetOrCreateHealthInfo(rateLimitKey);

        healthInfo.TotalRequests += permits;

        using var lease = await rateLimiter.AcquireAsync(permits, cancellationToken);
        
        if (lease.IsAcquired)
        {
            healthInfo.PermittedRequests += permits;
            healthInfo.LastPermission = DateTime.UtcNow;

            if (_settings.RateLimit.EnableRateLimitLogging)
            {
                _logger.LogDebug("Rate limit {RateLimitKey} acquired {Permits} permits", 
                    rateLimitKey, permits);
            }

            return true;
        }
        else
        {
            healthInfo.RejectedRequests += permits;
            healthInfo.LastRejection = DateTime.UtcNow;

            if (_settings.RateLimit.EnableRateLimitLogging)
            {
                _logger.LogDebug("Rate limit {RateLimitKey} rejected {Permits} permits", 
                    rateLimitKey, permits);
            }

            return false;
        }
    }

    public RateLimitHealthInfo GetRateLimitHealthInfo(string rateLimitKey)
    {
        var rateLimiter = _rateLimiters.TryGetValue(rateLimitKey, out var limiter) ? limiter : null;
        var healthInfo = GetOrCreateHealthInfo(rateLimitKey);

        // Update current permits from rate limiter statistics if available
        if (limiter is FixedWindowRateLimiter fixedWindow)
        {
            var stats = fixedWindow.GetStatistics();
            if (stats != null)
            {
                healthInfo.CurrentPermits = (int)(healthInfo.PermitLimit - stats.CurrentQueuedCount);
            }
        }

        return healthInfo;
    }

    public Dictionary<string, RateLimitHealthInfo> GetAllRateLimitHealthInfo()
    {
        return _healthInfo.Keys.ToDictionary(key => key, GetRateLimitHealthInfo);
    }

    private RateLimiter GetOrCreateRateLimiter(string rateLimitKey)
    {
        return _rateLimiters.GetOrAdd(rateLimitKey, CreateRateLimiter);
    }

    private RateLimiter CreateRateLimiter(string rateLimitKey)
    {
        var options = new FixedWindowRateLimiterOptions
        {
            PermitLimit = _settings.RateLimit.PermitLimit,
            Window = _settings.RateLimit.Window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = _settings.RateLimit.QueueLimit,
            AutoReplenishment = _settings.RateLimit.AutoReplenishment
        };

        var rateLimiter = new FixedWindowRateLimiter(options);

        if (_settings.RateLimit.EnableRateLimitLogging)
        {
            _logger.LogDebug("Created rate limiter for key: {RateLimitKey} with {PermitLimit} permits per {Window}", 
                rateLimitKey, options.PermitLimit, options.Window);
        }

        return rateLimiter;
    }

    private RateLimitHealthInfo GetOrCreateHealthInfo(string rateLimitKey)
    {
        return _healthInfo.GetOrAdd(rateLimitKey, key => new RateLimitHealthInfo
        {
            RateLimitKey = key,
            PermitLimit = _settings.RateLimit.PermitLimit,
            CurrentPermits = _settings.RateLimit.PermitLimit,
            Window = _settings.RateLimit.Window,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        foreach (var rateLimiter in _rateLimiters.Values)
        {
            rateLimiter.Dispose();
        }
        _rateLimiters.Clear();
    }
}