using Enterprise.Shared.Notifications.Models;

namespace Enterprise.Shared.Notifications.Interfaces;

/// <summary>
/// User notification preferences service interface
/// </summary>
public interface INotificationPreferencesService
{
    /// <summary>
    /// Get user notification preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User notification preferences</returns>
    Task<UserNotificationPreferences> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    /// <param name="preferences">Updated preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateUserPreferencesAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset user preferences to default
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ResetToDefaultAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has opted out of a notification type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Notification type</param>
    /// <param name="channel">Notification channel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if opted out</returns>
    Task<bool> IsOptedOutAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is in quiet hours
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if in quiet hours</returns>
    Task<bool> IsInQuietHoursAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's preferred language
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Language code</returns>
    Task<string> GetUserLanguageAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's timezone
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Timezone</returns>
    Task<string> GetUserTimezoneAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get enabled channels for user and notification type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Notification type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enabled channels</returns>
    Task<IEnumerable<NotificationChannel>> GetEnabledChannelsAsync(Guid userId, NotificationType notificationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opt out user from specific notification type and channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Notification type</param>
    /// <param name="channel">Notification channel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task OptOutAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opt in user to specific notification type and channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Notification type</param>
    /// <param name="channel">Notification channel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task OptInAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user's do not disturb mode
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="enabled">Enable or disable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SetDoNotDisturbAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user's quiet hours
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SetQuietHoursAsync(Guid userId, TimeSpan? startTime, TimeSpan? endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import preferences from external source
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="source">External preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ImportPreferencesAsync(Guid userId, Dictionary<string, object> source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported preferences</returns>
    Task<Dictionary<string, object>> ExportPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get preference statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preference statistics</returns>
    Task<PreferenceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update preferences for multiple users
    /// </summary>
    /// <param name="updates">Preference updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task BulkUpdatePreferencesAsync(IEnumerable<UserNotificationPreferences> updates, CancellationToken cancellationToken = default);
}

/// <summary>
/// Preference statistics
/// </summary>
public class PreferenceStatistics
{
    /// <summary>
    /// Total users with preferences
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users by language
    /// </summary>
    public Dictionary<string, int> ByLanguage { get; set; } = new();

    /// <summary>
    /// Users by timezone
    /// </summary>
    public Dictionary<string, int> ByTimezone { get; set; } = new();

    /// <summary>
    /// Channel opt-out rates
    /// </summary>
    public Dictionary<NotificationChannel, double> OptOutRates { get; set; } = new();

    /// <summary>
    /// Notification type opt-out rates
    /// </summary>
    public Dictionary<NotificationType, double> TypeOptOutRates { get; set; } = new();

    /// <summary>
    /// Do not disturb users count
    /// </summary>
    public int DoNotDisturbUsers { get; set; }

    /// <summary>
    /// Users with quiet hours
    /// </summary>
    public int QuietHoursUsers { get; set; }

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}