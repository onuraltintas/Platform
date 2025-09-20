using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Interfaces;

/// <summary>
/// Service for managing configuration values with caching and validation
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value with the specified type
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration value or default(T) if not found</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Gets a configuration value with a fallback default value
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <returns>Configuration value or default value</returns>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Gets a configuration section
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <returns>Configuration section</returns>
    IConfigurationSection GetSection(string sectionName);

    /// <summary>
    /// Gets a configuration value asynchronously (for external providers)
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration value or default(T) if not found</returns>
    Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value dynamically
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to set</param>
    /// <param name="changedBy">User or system making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetValueAsync<T>(string key, T value, string? changedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads configuration from all providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature flag is enabled
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <returns>True if feature is enabled</returns>
    bool IsFeatureEnabled(string featureName);

    /// <summary>
    /// Checks if a feature flag is enabled for a specific user
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="userId">User ID for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if feature is enabled for the user</returns>
    Task<bool> IsFeatureEnabledAsync(string featureName, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration section
    /// </summary>
    /// <param name="sectionName">Section to validate</param>
    /// <returns>Validation result</returns>
    ConfigurationValidationResult ValidateSection(string sectionName);

    /// <summary>
    /// Gets all configuration keys matching a pattern
    /// </summary>
    /// <param name="pattern">Key pattern (supports wildcards)</param>
    /// <returns>Dictionary of matching keys and values</returns>
    Dictionary<string, string?> GetKeysByPattern(string pattern);

    /// <summary>
    /// Event fired when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}