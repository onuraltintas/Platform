using Enterprise.Shared.Notifications.Models;

namespace Enterprise.Shared.Notifications.Interfaces;

/// <summary>
/// Base notification provider interface
/// </summary>
public interface INotificationProvider<T> where T : NotificationBase
{
    /// <summary>
    /// Send notification
    /// </summary>
    /// <param name="notification">Notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendAsync(T notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify delivery status
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delivery status</returns>
    Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Is provider healthy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Email notification provider interface
/// </summary>
public interface IEmailNotificationProvider : INotificationProvider<EmailNotification>
{
    /// <summary>
    /// Send bulk emails
    /// </summary>
    /// <param name="notifications">Email notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendBulkAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bounce list
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bounced email addresses</returns>
    Task<IEnumerable<string>> GetBounceListAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// SMS notification provider interface
/// </summary>
public interface ISmsNotificationProvider : INotificationProvider<SmsNotification>
{
    /// <summary>
    /// Send bulk SMS
    /// </summary>
    /// <param name="notifications">SMS notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendBulkAsync(IEnumerable<SmsNotification> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get delivery reports
    /// </summary>
    /// <param name="messageIds">Message IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delivery reports</returns>
    Task<IEnumerable<SmsDeliveryReport>> GetDeliveryReportsAsync(IEnumerable<string> messageIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<PhoneNumberValidation> ValidatePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// SMS delivery report
/// </summary>
public class SmsDeliveryReport
{
    /// <summary>
    /// Message ID
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Delivery status
    /// </summary>
    public SmsDeliveryStatus Status { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Delivered at
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// SMS delivery status
/// </summary>
public enum SmsDeliveryStatus
{
    Unknown,
    Queued,
    Sent,
    Delivered,
    Failed,
    Undelivered
}

/// <summary>
/// Phone number validation result
/// </summary>
public class PhoneNumberValidation
{
    /// <summary>
    /// Is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Formatted phone number
    /// </summary>
    public string FormattedNumber { get; set; } = string.Empty;

    /// <summary>
    /// Country code
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    public string CountryName { get; set; } = string.Empty;

    /// <summary>
    /// Phone type (mobile, landline, etc.)
    /// </summary>
    public string PhoneType { get; set; } = string.Empty;

    /// <summary>
    /// Carrier information
    /// </summary>
    public string? Carrier { get; set; }
}

/// <summary>
/// Push notification provider interface
/// </summary>
public interface IPushNotificationProvider : INotificationProvider<PushNotification>
{
    /// <summary>
    /// Send to topic
    /// </summary>
    /// <param name="topic">Topic name</param>
    /// <param name="notification">Push notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendToTopicAsync(string topic, PushNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk push notifications
    /// </summary>
    /// <param name="notifications">Push notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendBulkAsync(IEnumerable<PushNotification> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to topic
    /// </summary>
    /// <param name="tokens">Device tokens</param>
    /// <param name="topic">Topic name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SubscribeToTopicAsync(IEnumerable<string> tokens, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from topic
    /// </summary>
    /// <param name="tokens">Device tokens</param>
    /// <param name="topic">Topic name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UnsubscribeFromTopicAsync(IEnumerable<string> tokens, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate device tokens
    /// </summary>
    /// <param name="tokens">Device tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Valid tokens</returns>
    Task<IEnumerable<string>> ValidateTokensAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-app notification provider interface
/// </summary>
public interface IInAppNotificationProvider : INotificationProvider<InAppNotification>
{
    /// <summary>
    /// Mark notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread count for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unread count</returns>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="unreadOnly">Only unread notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notifications</returns>
    Task<IEnumerable<InAppNotification>> GetNotificationsAsync(Guid userId, int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear expired notifications
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ClearExpiredNotificationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Webhook notification provider interface
/// </summary>
public interface IWebhookNotificationProvider : INotificationProvider<WebhookNotification>
{
    /// <summary>
    /// Register webhook endpoint
    /// </summary>
    /// <param name="url">Webhook URL</param>
    /// <param name="secret">Secret for signature</param>
    /// <param name="eventTypes">Event types to subscribe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RegisterWebhookAsync(string url, string secret, IEnumerable<NotificationType> eventTypes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister webhook endpoint
    /// </summary>
    /// <param name="url">Webhook URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UnregisterWebhookAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test webhook endpoint
    /// </summary>
    /// <param name="url">Webhook URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<WebhookTestResult> TestWebhookAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Webhook test result
/// </summary>
public class WebhookTestResult
{
    /// <summary>
    /// Is successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response headers
    /// </summary>
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();

    /// <summary>
    /// Tested at
    /// </summary>
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}