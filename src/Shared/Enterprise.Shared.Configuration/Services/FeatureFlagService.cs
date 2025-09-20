using System.Security.Cryptography;
using System.Text;
using Enterprise.Shared.Configuration.Interfaces;
using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Services;

/// <summary>
/// Feature flag service implementation with user-based targeting and rollout support
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IUserContextService? _userContextService;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly ConfigurationSettings _settings;
    private readonly IConfigurationChangeTracker? _changeTracker;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ConfigurationChangedEventArgs>? FeatureFlagChanged;

    public FeatureFlagService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<FeatureFlagService> logger,
        IOptions<ConfigurationSettings> settings,
        IUserContextService? userContextService = null,
        IConfigurationChangeTracker? changeTracker = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _userContextService = userContextService;
        _changeTracker = changeTracker;
    }

    /// <inheritdoc/>
    public bool IsEnabled(string featureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        var userId = _userContextService?.GetCurrentUserId();
        return IsEnabled(featureName, userId ?? "anonymous");
    }

    /// <inheritdoc/>
    public bool IsEnabled(string featureName, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var cacheKey = GetFeatureCacheKey(featureName, userId);
        
        if (_settings.FeatureFlagCacheTimeout > TimeSpan.Zero && 
            _cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        var isEnabled = EvaluateFeatureFlag(featureName, userId);
        
        if (_settings.FeatureFlagCacheTimeout > TimeSpan.Zero)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _settings.FeatureFlagCacheTimeout,
                Priority = CacheItemPriority.High
            };
            
            _cache.Set(cacheKey, isEnabled, cacheOptions);
        }
        
        return isEnabled;
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var userId = _userContextService?.GetCurrentUserId();
        return await IsEnabledAsync(featureName, userId ?? "anonymous", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default)
    {
        // For future external feature flag service integration
        await Task.CompletedTask;
        return IsEnabled(featureName, userId);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, bool>> GetAllFlagsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var flagsSection = _configuration.GetSection("FeatureFlags");
            var flags = new Dictionary<string, bool>();
            var targetUserId = userId ?? _userContextService?.GetCurrentUserId() ?? "anonymous";

            foreach (var child in flagsSection.GetChildren())
            {
                // Skip nested sections (like rollout configs)
                if (bool.TryParse(child.Value, out _) || child.Value == null)
                {
                    flags[child.Key] = IsEnabled(child.Key, targetUserId);
                }
            }

            
            await Task.CompletedTask;
            return flags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all feature flags for user {UserId}", userId);
            return new Dictionary<string, bool>();
        }
    }

    /// <inheritdoc/>
    public async Task SetFlagAsync(string featureName, bool enabled, string? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        try
        {
            var key = userId is null 
                ? $"FeatureFlags:{featureName}" 
                : $"FeatureFlags:{featureName}:Users:{userId}";
            
            var oldValue = _configuration.GetValue<bool?>(key);
            
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot[key] = enabled.ToString();
                
                // Clear cache for this feature
                ClearCache(featureName);
                
                // Track change
                if (_changeTracker is not null)
                {
                    await _changeTracker.TrackChangeAsync(key, oldValue, enabled, 
                        "FeatureFlagService", $"Feature flag {(enabled ? "enabled" : "disabled")}", cancellationToken);
                }
                
                // Fire event
                FeatureFlagChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = enabled
                });
                
                _logger.LogInformation("Feature flag updated: {FeatureName} = {Enabled} for user {UserId}", 
                    featureName, enabled, userId ?? "all users");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting feature flag: {FeatureName} = {Enabled} for user {UserId}", 
                featureName, enabled, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FeatureFlagResult> GetFeatureFlagResultAsync(string featureName, string? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        var targetUserId = userId ?? _userContextService?.GetCurrentUserId();
        var isEnabled = IsEnabled(featureName, targetUserId ?? "anonymous");
        
        var result = new FeatureFlagResult
        {
            Name = featureName,
            IsEnabled = isEnabled,
            UserId = targetUserId,
            Source = "Configuration",
            Metadata = new Dictionary<string, object>
            {
                ["EvaluationStrategy"] = DetermineEvaluationStrategy(featureName, targetUserId),
                ["CacheHit"] = IsCacheHit(featureName, targetUserId ?? "anonymous")
            }
        };

        await Task.CompletedTask;
        return result;
    }

    /// <inheritdoc/>
    public async Task SetRolloutPercentageAsync(string featureName, int percentage, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
        }

        try
        {
            var key = $"FeatureFlags:{featureName}:RolloutPercentage";
            var oldValue = _configuration.GetValue<int?>(key);
            
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot[key] = percentage.ToString();
                
                // Clear cache for this feature
                ClearCache(featureName);
                
                // Track change
                if (_changeTracker is not null)
                {
                    await _changeTracker.TrackChangeAsync(key, oldValue, percentage, 
                        "FeatureFlagService", $"Rollout percentage set to {percentage}%", cancellationToken);
                }
                
                _logger.LogInformation("Feature flag rollout percentage updated: {FeatureName} = {Percentage}%", 
                    featureName, percentage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting rollout percentage for feature flag: {FeatureName} = {Percentage}%", 
                featureName, percentage);
            throw;
        }
    }

    /// <inheritdoc/>
    public void ClearCache(string featureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        try
        {
            // Remove all cache entries for this feature (all users)
            var keysToRemove = new List<object>();
            
            if (_cache is MemoryCache mc)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field?.GetValue(mc) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(coherentState);
                    
                    if (entriesCollection is IDictionary<object, object> entries)
                    {
                        foreach (var key in entries.Keys)
                        {
                            if (key?.ToString()?.StartsWith($"feature:{featureName}:") == true)
                            {
                                keysToRemove.Add(key);
                            }
                        }
                    }
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cache for feature flag: {FeatureName}", featureName);
            // Non-critical operation, continue execution
        }
    }

    /// <inheritdoc/>
    public void ClearAllCache()
    {
        try
        {
            if (_cache is MemoryCache mc)
            {
                mc.Compact(1.0);
            }
            
            _logger.LogInformation("All feature flag cache cleared");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing all feature flag cache");
        }
    }

    private bool EvaluateFeatureFlag(string featureName, string userId)
    {
        try
        {
            // 1. Check base flag value
            var baseEnabled = _configuration.GetValue<bool>($"FeatureFlags:{featureName}", false);
            
            if (!baseEnabled)
            {
                return false;
            }

            // 2. Check user-specific override
            var userSpecificFlag = _configuration.GetValue<bool?>($"FeatureFlags:{featureName}:Users:{userId}");
            if (userSpecificFlag.HasValue)
            {
                return userSpecificFlag.Value;
            }

            // 3. Check role-based flags
            if (_userContextService is not null)
            {
                var userRoles = _userContextService.GetCurrentUserRoles();
                foreach (var role in userRoles)
                {
                    var roleSpecificFlag = _configuration.GetValue<bool?>($"FeatureFlags:{featureName}:Roles:{role}");
                    if (roleSpecificFlag.HasValue)
                    {
                        return roleSpecificFlag.Value;
                    }
                }
            }

            // 4. Check percentage rollout
            var rolloutPercentage = _configuration.GetValue<int?>($"FeatureFlags:{featureName}:RolloutPercentage");
            if (rolloutPercentage.HasValue && rolloutPercentage.Value > 0)
            {
                var userPercentile = CalculateUserPercentile(userId);
                var isInRollout = userPercentile < rolloutPercentage.Value;
                
                
                return isInRollout;
            }

            return baseEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag: {FeatureName} for user {UserId}", featureName, userId);
            return false;
        }
    }

    private static int CalculateUserPercentile(string userId)
    {
        // Use a consistent hash function to determine user percentile
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        var hash = BitConverter.ToUInt32(hashBytes, 0);
        return (int)(hash % 100);
    }

    private string DetermineEvaluationStrategy(string featureName, string? userId)
    {
        if (_configuration.GetValue<bool?>($"FeatureFlags:{featureName}:Users:{userId}").HasValue)
        {
            return "UserSpecific";
        }

        if (_configuration.GetValue<int?>($"FeatureFlags:{featureName}:RolloutPercentage").HasValue)
        {
            return "PercentageRollout";
        }

        if (_userContextService is not null)
        {
            var userRoles = _userContextService.GetCurrentUserRoles();
            foreach (var role in userRoles)
            {
                if (_configuration.GetValue<bool?>($"FeatureFlags:{featureName}:Roles:{role}").HasValue)
                {
                    return "RoleBased";
                }
            }
        }

        return "Global";
    }

    private bool IsCacheHit(string featureName, string userId)
    {
        var cacheKey = GetFeatureCacheKey(featureName, userId);
        return _cache.TryGetValue(cacheKey, out _);
    }

    private static string GetFeatureCacheKey(string featureName, string userId) => $"feature:{featureName}:{userId}";

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            FeatureFlagChanged = null;
            _disposed = true;
        }
    }
}