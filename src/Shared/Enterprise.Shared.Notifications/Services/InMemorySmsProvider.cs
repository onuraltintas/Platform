using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Notifications.Services;

/// <summary>
/// In-memory SMS notification provider for testing
/// </summary>
public class InMemorySmsProvider : ISmsNotificationProvider
{
    private readonly ILogger<InMemorySmsProvider> _logger;
    private readonly NotificationSettings _settings;
    private static readonly ConcurrentDictionary<Guid, SmsNotification> _sentSms = new();
    private static readonly ConcurrentDictionary<Guid, DeliveryStatus> _deliveryStatuses = new();
    private static readonly ConcurrentDictionary<string, SmsDeliveryReport> _deliveryReports = new();

    public string ProviderName => "InMemory";

    public InMemorySmsProvider(ILogger<InMemorySmsProvider> logger, IOptions<NotificationSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task SendAsync(SmsNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending SMS notification {NotificationId} to {ToPhoneNumber} with message: '{Message}'",
            notification.NotificationId, notification.ToPhoneNumber, 
            notification.Message.Length > 50 ? notification.Message[..50] + "..." : notification.Message);

        // Validate phone number format (basic check)
        if (string.IsNullOrWhiteSpace(notification.ToPhoneNumber) || 
            !notification.ToPhoneNumber.StartsWith('+'))
        {
            _logger.LogWarning("Invalid phone number format for SMS notification {NotificationId}: {ToPhoneNumber}",
                notification.NotificationId, notification.ToPhoneNumber);
            _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Failed);
            return Task.CompletedTask;
        }

        // Check message length
        if (notification.Message.Length > _settings.SMS.MaxMessageLength)
        {
            _logger.LogWarning("SMS message too long for notification {NotificationId}: {Length} characters (max: {Max})",
                notification.NotificationId, notification.Message.Length, _settings.SMS.MaxMessageLength);
        }

        // Simulate SMS sending
        _sentSms.TryAdd(notification.NotificationId, notification);
        _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);

        // Create delivery report
        var messageId = Guid.NewGuid().ToString();
        var deliveryReport = new SmsDeliveryReport
        {
            MessageId = messageId,
            PhoneNumber = notification.ToPhoneNumber,
            Status = SmsDeliveryStatus.Sent,
            StatusDescription = "Message sent successfully",
            DeliveredAt = DateTime.UtcNow.AddSeconds(Random.Shared.Next(1, 30)) // Simulate delivery delay
        };
        _deliveryReports.TryAdd(messageId, deliveryReport);

        // Simulate some processing time
        return Task.Delay(15, cancellationToken);
    }

    public Task SendBulkAsync(IEnumerable<SmsNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var smsList = notifications.ToList();
        _logger.LogInformation("Sending bulk SMS notifications: {Count} messages", smsList.Count);

        foreach (var notification in smsList)
        {
            _sentSms.TryAdd(notification.NotificationId, notification);
            _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);

            var messageId = Guid.NewGuid().ToString();
            var deliveryReport = new SmsDeliveryReport
            {
                MessageId = messageId,
                PhoneNumber = notification.ToPhoneNumber,
                Status = SmsDeliveryStatus.Sent,
                StatusDescription = "Message sent successfully"
            };
            _deliveryReports.TryAdd(messageId, deliveryReport);
        }

        // Simulate bulk processing time
        return Task.Delay(smsList.Count * 3, cancellationToken);
    }

    public Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Verifying delivery status for SMS notification {NotificationId}", notificationId);
        
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
        _logger.LogDebug("Health check for InMemory SMS provider: Healthy");
        return Task.FromResult(true);
    }

    public Task<IEnumerable<SmsDeliveryReport>> GetDeliveryReportsAsync(IEnumerable<string> messageIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        _logger.LogDebug("Getting delivery reports for {Count} message IDs", messageIds.Count());

        var reports = messageIds
            .Where(_deliveryReports.ContainsKey)
            .Select(id => _deliveryReports[id])
            .ToList();

        return Task.FromResult<IEnumerable<SmsDeliveryReport>>(reports);
    }

    public Task<PhoneNumberValidation> ValidatePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        _logger.LogDebug("Validating phone number: {PhoneNumber}", phoneNumber);

        // Simple validation for in-memory provider
        var validation = new PhoneNumberValidation
        {
            IsValid = phoneNumber.StartsWith('+') && phoneNumber.Length >= 10,
            FormattedNumber = phoneNumber,
            CountryCode = phoneNumber.StartsWith("+1") ? "US" : "Unknown",
            CountryName = phoneNumber.StartsWith("+1") ? "United States" : "Unknown",
            PhoneType = "Mobile", // Assume mobile for simplicity
            Carrier = "Unknown"
        };

        return Task.FromResult(validation);
    }

    /// <summary>
    /// Get all sent SMS messages (for testing purposes)
    /// </summary>
    /// <returns>All sent SMS messages</returns>
    public static IEnumerable<SmsNotification> GetSentSms()
    {
        return _sentSms.Values;
    }

    /// <summary>
    /// Get sent SMS by notification ID (for testing purposes)
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <returns>SMS notification or null</returns>
    public static SmsNotification? GetSentSms(Guid notificationId)
    {
        _sentSms.TryGetValue(notificationId, out var sms);
        return sms;
    }

    /// <summary>
    /// Clear all sent SMS messages (for testing purposes)
    /// </summary>
    public static void ClearSentSms()
    {
        _sentSms.Clear();
        _deliveryStatuses.Clear();
        _deliveryReports.Clear();
    }

    /// <summary>
    /// Get sent SMS count
    /// </summary>
    /// <returns>Count of sent SMS messages</returns>
    public static int GetSentSmsCount()
    {
        return _sentSms.Count;
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

    /// <summary>
    /// Get all delivery reports (for testing purposes)
    /// </summary>
    /// <returns>All delivery reports</returns>
    public static IEnumerable<SmsDeliveryReport> GetAllDeliveryReports()
    {
        return _deliveryReports.Values;
    }
}