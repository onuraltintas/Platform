using System.Globalization;
using System.Text.RegularExpressions;
using Enterprise.Shared.Configuration.Interfaces;
using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Services;

/// <summary>
/// Configuration service implementation with caching and validation
/// </summary>
public sealed partial class ConfigurationService : IConfigurationService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly ConfigurationSettings _settings;
    private readonly IConfigurationChangeTracker? _changeTracker;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<ConfigurationService> logger,
        IOptions<ConfigurationSettings> settings,
        IConfigurationChangeTracker? changeTracker = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _changeTracker = changeTracker;
    }

    /// <inheritdoc/>
    public T? GetValue<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var cacheKey = GetCacheKey(key);
        
        if (_settings.CacheTimeout > TimeSpan.Zero && _cache.TryGetValue(cacheKey, out T? cachedValue))
        {
            return cachedValue;
        }

        try
        {
            var value = _configuration.GetValue<T>(key);
            
            if (value is not null && _settings.CacheTimeout > TimeSpan.Zero)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _settings.CacheTimeout,
                    SlidingExpiration = _settings.CacheTimeout / 2,
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(cacheKey, value, cacheOptions);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration value: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            var value = GetValue<T>(key);
            return value is not null ? value : defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving configuration value: {Key}, returning default: {DefaultValue}", 
                key, defaultValue);
            return defaultValue;
        }
    }

    /// <inheritdoc/>
    public IConfigurationSection GetSection(string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        
        try
        {
            var section = _configuration.GetSection(sectionName);
            return section;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration section: {SectionName}", sectionName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // For future external configuration providers
        await Task.CompletedTask;
        return GetValue<T>(key);
    }

    /// <inheritdoc/>
    public async Task SetValueAsync<T>(string key, T value, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            var oldValue = GetValue<T>(key);
            
            if (_configuration is IConfigurationRoot configRoot)
            {
                // Update in-memory configuration
                configRoot[key] = value?.ToString();
                
                // Clear cache
                var cacheKey = GetCacheKey(key);
                _cache.Remove(cacheKey);
                
                // Track change if tracker is available
                if (_changeTracker is not null && _settings.AuditChanges)
                {
                    await _changeTracker.TrackChangeAsync(key, oldValue, value, 
                        changedBy ?? "System", cancellationToken: cancellationToken);
                }
                
                // Fire event
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = value,
                    ChangedBy = changedBy
                });
                
                _logger.LogInformation("Configuration key updated: {Key} = {Value} by {ChangedBy}", 
                    key, value, changedBy ?? "System");
            }
            else
            {
                _logger.LogWarning("Cannot update configuration: Configuration root is not available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value: {Key} = {Value}", key, value);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                
                // Clear cache
                if (_cache is MemoryCache mc)
                {
                    mc.Compact(1.0);
                }
                
                _logger.LogInformation("Configuration reloaded successfully");
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            throw;
        }
    }

    /// <inheritdoc/>
    public bool IsFeatureEnabled(string featureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        var key = $"FeatureFlags:{featureName}";
        return GetValue<bool>(key, false);
    }

    /// <inheritdoc/>
    public async Task<bool> IsFeatureEnabledAsync(string featureName, string? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        
        // For now, just return sync version
        // In the future, this could support external feature flag services
        await Task.CompletedTask;
        return IsFeatureEnabled(featureName);
    }

    /// <inheritdoc/>
    public ConfigurationValidationResult ValidateSection(string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        
        try
        {
            var section = GetSection(sectionName);
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check if section exists
            if (!section.Exists())
            {
                errors.Add($"Configuration section '{sectionName}' does not exist");
                return ConfigurationValidationResult.Failure(errors, sectionName);
            }
            
            // Validate section has children or value
            if (!section.GetChildren().Any() && string.IsNullOrEmpty(section.Value))
            {
                warnings.Add($"Configuration section '{sectionName}' is empty");
            }
            
            // Additional validation based on section type
            ValidateSpecificSection(sectionName, section, errors, warnings);
            
            
            if (errors.Count > 0)
            {
                return ConfigurationValidationResult.Failure(errors, sectionName);
            }
            
            if (warnings.Count > 0)
            {
                return ConfigurationValidationResult.WithWarnings(warnings, sectionName);
            }
            
            return ConfigurationValidationResult.Success(sectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration section: {SectionName}", sectionName);
            return ConfigurationValidationResult.Failure(new[] { ex.Message }, sectionName);
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, string?> GetKeysByPattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        
        try
        {
            var result = new Dictionary<string, string?>();
            var regex = CreatePatternRegex(pattern);
            
            // Flatten configuration and find matching keys
            var flatConfig = FlattenConfiguration(_configuration);
            
            foreach (var (key, value) in flatConfig)
            {
                if (regex.IsMatch(key))
                {
                    result[key] = value;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration keys by pattern: {Pattern}", pattern);
            return new Dictionary<string, string?>();
        }
    }

    private static string GetCacheKey(string key) => $"config:{key}";

    private static void ValidateSpecificSection(string sectionName, IConfigurationSection section, List<string> errors, List<string> warnings)
    {
        switch (sectionName.ToUpperInvariant())
        {
            case "DATABASE":
                ValidateDatabaseSection(section, errors, warnings);
                break;
            case "REDIS":
                ValidateRedisSection(section, errors, warnings);
                break;
            case "RABBITMQ":
                ValidateRabbitMQSection(section, errors, warnings);
                break;
        }
    }

    private static void ValidateDatabaseSection(IConfigurationSection section, List<string> errors, List<string> warnings)
    {
        var connectionString = section["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("Database ConnectionString is required");
        }

        if (int.TryParse(section["CommandTimeout"], out var timeout) && (timeout < 5 || timeout > 300))
        {
            errors.Add("Database CommandTimeout must be between 5 and 300 seconds");
        }

        if (bool.TryParse(section["EnableSensitiveDataLogging"], out var enableLogging) && enableLogging)
        {
            warnings.Add("Sensitive data logging is enabled - ensure this is intended for the current environment");
        }
    }

    private static void ValidateRedisSection(IConfigurationSection section, List<string> errors, List<string> warnings)
    {
        var connectionString = section["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("Redis ConnectionString is required");
        }

        if (int.TryParse(section["Database"], out var db) && (db < 0 || db > 15))
        {
            errors.Add("Redis Database index must be between 0 and 15");
        }
    }

    private static void ValidateRabbitMQSection(IConfigurationSection section, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(section["Host"]))
        {
            errors.Add("RabbitMQ Host is required");
        }

        if (string.IsNullOrWhiteSpace(section["Username"]))
        {
            errors.Add("RabbitMQ Username is required");
        }

        if (string.IsNullOrWhiteSpace(section["Password"]))
        {
            errors.Add("RabbitMQ Password is required");
        }

        if (int.TryParse(section["Port"], out var port) && (port < 1 || port > 65535))
        {
            errors.Add("RabbitMQ Port must be between 1 and 65535");
        }
    }

    [GeneratedRegex(@"\*", RegexOptions.Compiled)]
    private static partial Regex WildcardRegex();

    private static Regex CreatePatternRegex(string pattern)
    {
        // Convert wildcard pattern to regex
        var regexPattern = Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".");
        return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static Dictionary<string, string?> FlattenConfiguration(IConfiguration configuration)
    {
        var result = new Dictionary<string, string?>();
        FlattenConfigurationRecursive(configuration, string.Empty, result);
        return result;
    }

    private static void FlattenConfigurationRecursive(IConfiguration config, string prefix, Dictionary<string, string?> result)
    {
        foreach (var child in config.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";
            
            if (child.Value is not null)
            {
                result[key] = child.Value;
            }
            
            FlattenConfigurationRecursive(child, key, result);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            ConfigurationChanged = null;
            _disposed = true;
        }
    }
}