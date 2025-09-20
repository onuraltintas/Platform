using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Interfaces;

/// <summary>
/// Service for managing feature flags and A/B testing
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled for the current user
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <returns>True if feature is enabled</returns>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Checks if a feature flag is enabled for a specific user
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="userId">User ID for context</param>
    /// <returns>True if feature is enabled for the user</returns>
    bool IsEnabled(string featureName, string userId);

    /// <summary>
    /// Checks if a feature flag is enabled asynchronously
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if feature is enabled</returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature flag is enabled for a specific user asynchronously
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="userId">User ID for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if feature is enabled for the user</returns>
    Task<bool> IsEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags and their status for a user
    /// </summary>
    /// <param name="userId">User ID for context (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of feature flags and their enabled status</returns>
    Task<Dictionary<string, bool>> GetAllFlagsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a feature flag value dynamically
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="enabled">Whether the feature should be enabled</param>
    /// <param name="userId">Optional user ID for user-specific flag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetFlagAsync(string featureName, bool enabled, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed feature flag evaluation result
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="userId">User ID for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed feature flag result</returns>
    Task<FeatureFlagResult> GetFeatureFlagResultAsync(string featureName, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a percentage-based rollout for a feature flag
    /// </summary>
    /// <param name="featureName">Feature flag name</param>
    /// <param name="percentage">Percentage of users to enable (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetRolloutPercentageAsync(string featureName, int percentage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cache for a specific feature flag
    /// </summary>
    /// <param name="featureName">Feature flag name to clear cache for</param>
    void ClearCache(string featureName);

    /// <summary>
    /// Clears all feature flag cache
    /// </summary>
    void ClearAllCache();

    /// <summary>
    /// Event fired when a feature flag changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? FeatureFlagChanged;
}