using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Interfaces;

/// <summary>
/// Service for tracking configuration changes and auditing
/// </summary>
public interface IConfigurationChangeTracker
{
    /// <summary>
    /// Event fired when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Tracks a configuration change
    /// </summary>
    /// <param name="key">Configuration key that changed</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    /// <param name="changedBy">User or system that made the change</param>
    /// <param name="reason">Reason for the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TrackChangeAsync(string key, object? oldValue, object? newValue, 
        string? changedBy = null, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration change history for a specific key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configuration changes</returns>
    Task<IEnumerable<ConfigurationChangeRecord>> GetChangeHistoryAsync(string key, 
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration changes within a date range
    /// </summary>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="changedBy">Filter by who made the change (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configuration changes</returns>
    Task<IEnumerable<ConfigurationChangeRecord>> GetAllChangesAsync(
        DateTime? from = null, DateTime? to = null, string? changedBy = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration change statistics
    /// </summary>
    /// <param name="from">Start date for statistics</param>
    /// <param name="to">End date for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with change statistics</returns>
    Task<Dictionary<string, object>> GetChangeStatisticsAsync(DateTime from, DateTime to, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears change history older than specified date
    /// </summary>
    /// <param name="olderThan">Date threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    Task<int> CleanupHistoryAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}