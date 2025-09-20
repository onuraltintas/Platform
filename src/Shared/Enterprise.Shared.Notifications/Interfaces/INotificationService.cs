using Enterprise.Shared.Notifications.Models;

namespace Enterprise.Shared.Notifications.Interfaces;

/// <summary>
/// Main notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a single notification
    /// </summary>
    /// <param name="request">Notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk notifications
    /// </summary>
    /// <param name="request">Bulk notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendBulkAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a notification
    /// </summary>
    /// <param name="request">Scheduled notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ScheduleAsync(ScheduledNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification status
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification status</returns>
    Task<NotificationStatus> GetStatusAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification history</returns>
    Task<IEnumerable<NotificationHistory>> GetHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification statistics
    /// </summary>
    /// <param name="fromDate">From date</param>
    /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification statistics</returns>
    Task<NotificationStatistics> GetStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CancelAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry a failed notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RetryAsync(Guid notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification statistics
/// </summary>
public class NotificationStatistics
{
    /// <summary>
    /// Total notifications sent
    /// </summary>
    public long TotalSent { get; set; }

    /// <summary>
    /// Total notifications delivered
    /// </summary>
    public long TotalDelivered { get; set; }

    /// <summary>
    /// Total notifications failed
    /// </summary>
    public long TotalFailed { get; set; }

    /// <summary>
    /// Statistics by channel
    /// </summary>
    public Dictionary<NotificationChannel, ChannelStatistics> ByChannel { get; set; } = new();

    /// <summary>
    /// Statistics by type
    /// </summary>
    public Dictionary<NotificationType, TypeStatistics> ByType { get; set; } = new();

    /// <summary>
    /// Delivery rate percentage
    /// </summary>
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;

    /// <summary>
    /// Failure rate percentage
    /// </summary>
    public double FailureRate => TotalSent > 0 ? (double)TotalFailed / TotalSent * 100 : 0;

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Channel statistics
/// </summary>
public class ChannelStatistics
{
    /// <summary>
    /// Total sent
    /// </summary>
    public long TotalSent { get; set; }

    /// <summary>
    /// Total delivered
    /// </summary>
    public long TotalDelivered { get; set; }

    /// <summary>
    /// Total failed
    /// </summary>
    public long TotalFailed { get; set; }

    /// <summary>
    /// Average delivery time in seconds
    /// </summary>
    public double AverageDeliveryTime { get; set; }

    /// <summary>
    /// Delivery rate
    /// </summary>
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
}

/// <summary>
/// Type statistics
/// </summary>
public class TypeStatistics
{
    /// <summary>
    /// Total sent
    /// </summary>
    public long TotalSent { get; set; }

    /// <summary>
    /// Total delivered
    /// </summary>
    public long TotalDelivered { get; set; }

    /// <summary>
    /// Total failed
    /// </summary>
    public long TotalFailed { get; set; }

    /// <summary>
    /// Most used channel
    /// </summary>
    public NotificationChannel? MostUsedChannel { get; set; }

    /// <summary>
    /// Delivery rate
    /// </summary>
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
}