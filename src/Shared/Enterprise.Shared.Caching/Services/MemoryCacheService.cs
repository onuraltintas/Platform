using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Caching.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ICacheMetricsService _metricsService;

    public MemoryCacheService(
        IMemoryCache memoryCache, 
        ILogger<MemoryCacheService> logger,
        ICacheMetricsService metricsService)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _metricsService = metricsService;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                _metricsService.RecordHit(l1Hit: true);
                _logger.LogDebug("Memory cache hit. Key: {Key}", key);
                return Task.FromResult(value);
            }

            _metricsService.RecordMiss();
            _logger.LogDebug("Memory cache miss. Key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory cache get error. Key: {Key}", key);
            _metricsService.RecordError();
            return Task.FromResult<T?>(default);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync<T>(key, cancellationToken);
        if (value != null) return value;

        var newValue = await factory();
        await SetAsync(key, newValue, expiry, cancellationToken);
        return newValue;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            _memoryCache.Set(key, value, options);
            _logger.LogDebug("Memory cache set. Key: {Key}, Expiry: {Expiry}", key, expiry);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory cache set error. Key: {Key}", key);
            _metricsService.RecordError();
            throw;
        }
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Memory cache remove. Key: {Key}", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory cache remove error. Key: {Key}", key);
            _metricsService.RecordError();
            return Task.FromResult(false);
        }
    }

    public Task<int> RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't support pattern removal directly
        // This would need to be implemented with a custom cache wrapper that tracks keys
        _logger.LogWarning("Memory cache pattern removal not supported. Pattern: {Pattern}", pattern);
        return Task.FromResult(0);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            _logger.LogDebug("Memory cache exists check. Key: {Key}, Exists: {Exists}", key, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory cache exists error. Key: {Key}", key);
            _metricsService.RecordError();
            return Task.FromResult(false);
        }
    }

    public Task<bool> RefreshAsync(string key, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                var options = new MemoryCacheEntryOptions();
                
                if (expiry.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiry;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                }

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("Memory cache refreshed. Key: {Key}, Expiry: {Expiry}", key, expiry);
                return Task.FromResult(true);
            }

            _logger.LogDebug("Memory cache refresh failed, key not found. Key: {Key}", key);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory cache refresh error. Key: {Key}", key);
            _metricsService.RecordError();
            return Task.FromResult(false);
        }
    }

    public Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't expose TTL information directly
        _logger.LogDebug("Memory cache TTL not available. Key: {Key}", key);
        return Task.FromResult<TimeSpan?>(null);
    }
}