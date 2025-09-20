using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Notifications.Services;

/// <summary>
/// In-memory email notification provider for testing
/// </summary>
public class InMemoryEmailProvider : IEmailNotificationProvider
{
    private readonly ILogger<InMemoryEmailProvider> _logger;
    private readonly NotificationSettings _settings;
    private static readonly ConcurrentDictionary<Guid, EmailNotification> _sentEmails = new();
    private static readonly ConcurrentDictionary<Guid, DeliveryStatus> _deliveryStatuses = new();

    public string ProviderName => "InMemory";

    public InMemoryEmailProvider(ILogger<InMemoryEmailProvider> logger, IOptions<NotificationSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task SendAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending email notification {NotificationId} to {ToEmail} with subject '{Subject}'",
            notification.NotificationId, notification.ToEmail, notification.Subject);

        // Simulate email sending
        _sentEmails.TryAdd(notification.NotificationId, notification);
        _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);

        // Simulate some processing time
        return Task.Delay(10, cancellationToken);
    }

    public Task SendBulkAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var emailList = notifications.ToList();
        _logger.LogInformation("Sending bulk email notifications: {Count} emails", emailList.Count);

        foreach (var notification in emailList)
        {
            _sentEmails.TryAdd(notification.NotificationId, notification);
            _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);
        }

        // Simulate bulk processing time
        return Task.Delay(emailList.Count * 2, cancellationToken);
    }

    public Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Verifying delivery status for email notification {NotificationId}", notificationId);
        
        if (_deliveryStatuses.TryGetValue(notificationId, out var status))
        {
            // Simulate delivery confirmation after some time
            if (status == DeliveryStatus.Sent)
            {
                _deliveryStatuses.TryUpdate(notificationId, DeliveryStatus.Delivered, status);
                return Task.FromResult(DeliveryStatus.Delivered);
            }
            return Task.FromResult(status);
        }

        return Task.FromResult(DeliveryStatus.Unknown);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        // Always healthy for in-memory provider
        _logger.LogDebug("Health check for InMemory email provider: Healthy");
        return Task.FromResult(true);
    }

    public Task<IEnumerable<string>> GetBounceListAsync(CancellationToken cancellationToken = default)
    {
        // Return empty bounce list for in-memory provider
        _logger.LogDebug("Getting bounce list for InMemory email provider");
        var bounceList = new List<string>();
        return Task.FromResult<IEnumerable<string>>(bounceList);
    }

    /// <summary>
    /// Get all sent emails (for testing purposes)
    /// </summary>
    /// <returns>All sent emails</returns>
    public static IEnumerable<EmailNotification> GetSentEmails()
    {
        return _sentEmails.Values;
    }

    /// <summary>
    /// Get sent email by notification ID (for testing purposes)
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <returns>Email notification or null</returns>
    public static EmailNotification? GetSentEmail(Guid notificationId)
    {
        _sentEmails.TryGetValue(notificationId, out var email);
        return email;
    }

    /// <summary>
    /// Clear all sent emails (for testing purposes)
    /// </summary>
    public static void ClearSentEmails()
    {
        _sentEmails.Clear();
        _deliveryStatuses.Clear();
    }

    /// <summary>
    /// Get sent emails count
    /// </summary>
    /// <returns>Count of sent emails</returns>
    public static int GetSentEmailsCount()
    {
        return _sentEmails.Count;
    }

    /// <summary>
    /// Get delivery status for notification (for testing purposes)
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <returns>Delivery status</returns>
    public static DeliveryStatus? GetDeliveryStatus(Guid notificationId)
    {
        return _deliveryStatuses.TryGetValue(notificationId, out var status) ? status : null;
    }
}