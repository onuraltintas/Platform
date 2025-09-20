using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Notifications.Services;

/// <summary>
/// In-memory push notification provider for testing
/// </summary>
public class InMemoryPushProvider : IPushNotificationProvider
{
    private readonly ILogger<InMemoryPushProvider> _logger;
    private readonly NotificationSettings _settings;
    private static readonly ConcurrentDictionary<Guid, PushNotification> _sentPushes = new();
    private static readonly ConcurrentDictionary<Guid, DeliveryStatus> _deliveryStatuses = new();
    private static readonly ConcurrentDictionary<string, List<string>> _topicSubscriptions = new();

    public string ProviderName => "InMemory";

    public InMemoryPushProvider(ILogger<InMemoryPushProvider> logger, IOptions<NotificationSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task SendAsync(PushNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending push notification {NotificationId} to user {UserId} with title '{Title}'",
            notification.NotificationId, notification.UserId, notification.Title);

        _sentPushes.TryAdd(notification.NotificationId, notification);
        _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);

        return Task.Delay(5, cancellationToken);
    }

    public Task SendBulkAsync(IEnumerable<PushNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var pushList = notifications.ToList();
        _logger.LogInformation("Sending bulk push notifications: {Count} notifications", pushList.Count);

        foreach (var notification in pushList)
        {
            _sentPushes.TryAdd(notification.NotificationId, notification);
            _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);
        }

        return Task.Delay(pushList.Count * 2, cancellationToken);
    }

    public Task SendToTopicAsync(string topic, PushNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending push notification {NotificationId} to topic '{Topic}' with title '{Title}'",
            notification.NotificationId, topic, notification.Title);

        notification.Topic = topic;
        _sentPushes.TryAdd(notification.NotificationId, notification);
        _deliveryStatuses.TryAdd(notification.NotificationId, DeliveryStatus.Sent);

        return Task.Delay(5, cancellationToken);
    }

    public Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (_deliveryStatuses.TryGetValue(notificationId, out var status))
        {
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
        return Task.FromResult(true);
    }

    public Task SubscribeToTopicAsync(IEnumerable<string> tokens, string topic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        _logger.LogInformation("Subscribing {TokenCount} tokens to topic '{Topic}'", tokens.Count(), topic);

        if (!_topicSubscriptions.ContainsKey(topic))
        {
            _topicSubscriptions.TryAdd(topic, new List<string>());
        }

        if (_topicSubscriptions.TryGetValue(topic, out var subscribers))
        {
            lock (subscribers)
            {
                subscribers.AddRange(tokens.Except(subscribers));
            }
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTopicAsync(IEnumerable<string> tokens, string topic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        _logger.LogInformation("Unsubscribing {TokenCount} tokens from topic '{Topic}'", tokens.Count(), topic);

        if (_topicSubscriptions.TryGetValue(topic, out var subscribers))
        {
            lock (subscribers)
            {
                foreach (var token in tokens)
                {
                    subscribers.Remove(token);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ValidateTokensAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        // For in-memory provider, assume all tokens are valid if they're not empty
        var validTokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 10).ToList();
        
        _logger.LogDebug("Validated {ValidCount}/{TotalCount} device tokens", validTokens.Count, tokens.Count());

        return Task.FromResult<IEnumerable<string>>(validTokens);
    }

    public static IEnumerable<PushNotification> GetSentPushNotifications() => _sentPushes.Values;
    public static PushNotification? GetSentPushNotification(Guid notificationId) => _sentPushes.TryGetValue(notificationId, out var push) ? push : null;
    public static void ClearSentPushNotifications() { _sentPushes.Clear(); _deliveryStatuses.Clear(); _topicSubscriptions.Clear(); }
    public static int GetSentPushNotificationsCount() => _sentPushes.Count;
    public static IReadOnlyDictionary<string, List<string>> GetTopicSubscriptions() => _topicSubscriptions;
}

/// <summary>
/// In-memory in-app notification provider
/// </summary>
public class InMemoryInAppProvider : IInAppNotificationProvider
{
    private readonly ILogger<InMemoryInAppProvider> _logger;
    private static readonly ConcurrentDictionary<Guid, InAppNotification> _notifications = new();
    private static readonly ConcurrentDictionary<Guid, List<InAppNotification>> _userNotifications = new();

    public string ProviderName => "InMemory";

    public InMemoryInAppProvider(ILogger<InMemoryInAppProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending in-app notification {NotificationId} to user {UserId} with title '{Title}'",
            notification.NotificationId, notification.UserId, notification.Title);

        _notifications.TryAdd(notification.NotificationId, notification);

        if (!_userNotifications.ContainsKey(notification.UserId))
        {
            _userNotifications.TryAdd(notification.UserId, new List<InAppNotification>());
        }

        if (_userNotifications.TryGetValue(notification.UserId, out var userNotifs))
        {
            lock (userNotifs)
            {
                userNotifs.Add(notification);
                // Keep only the latest 100 notifications per user
                while (userNotifs.Count > 100)
                {
                    userNotifs.RemoveAt(0);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_notifications.ContainsKey(notificationId) ? DeliveryStatus.Delivered : DeliveryStatus.Unknown);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);

        if (_notifications.TryGetValue(notificationId, out var notification) && notification.UserId == userId)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking all notifications as read for user {UserId}", userId);

        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            lock (notifications)
            {
                foreach (var notification in notifications.Where(n => !n.IsRead))
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            lock (notifications)
            {
                var unreadCount = notifications.Count(n => !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));
                _logger.LogDebug("Unread notification count for user {UserId}: {Count}", userId, unreadCount);
                return Task.FromResult(unreadCount);
            }
        }

        return Task.FromResult(0);
    }

    public Task<IEnumerable<InAppNotification>> GetNotificationsAsync(Guid userId, int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting notifications for user {UserId}, page {Page}, size {PageSize}, unreadOnly {UnreadOnly}",
            userId, page, pageSize, unreadOnly);

        if (!_userNotifications.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(Enumerable.Empty<InAppNotification>());
        }

        lock (notifications)
        {
            var query = notifications.AsEnumerable();

            // Filter expired notifications
            query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var result = query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<IEnumerable<InAppNotification>>(result);
        }
    }

    public Task DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting notification {NotificationId} for user {UserId}", notificationId, userId);

        if (_notifications.TryGetValue(notificationId, out var notification) && notification.UserId == userId)
        {
            _notifications.TryRemove(notificationId, out _);

            if (_userNotifications.TryGetValue(userId, out var userNotifs))
            {
                lock (userNotifs)
                {
                    userNotifs.RemoveAll(n => n.NotificationId == notificationId);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task ClearExpiredNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing expired notifications");

        var expiredNotifications = _notifications.Values
            .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= DateTime.UtcNow)
            .ToList();

        foreach (var notification in expiredNotifications)
        {
            _notifications.TryRemove(notification.NotificationId, out _);

            if (_userNotifications.TryGetValue(notification.UserId, out var userNotifs))
            {
                lock (userNotifs)
                {
                    userNotifs.RemoveAll(n => n.NotificationId == notification.NotificationId);
                }
            }
        }

        _logger.LogInformation("Cleared {Count} expired notifications", expiredNotifications.Count);
        return Task.CompletedTask;
    }

    public static IEnumerable<InAppNotification> GetAllNotifications() => _notifications.Values;
    public static IEnumerable<InAppNotification> GetUserNotifications(Guid userId) => _userNotifications.TryGetValue(userId, out var notifications) ? notifications : Enumerable.Empty<InAppNotification>();
    public static void ClearAllNotifications() { _notifications.Clear(); _userNotifications.Clear(); }
    public static int GetTotalNotificationsCount() => _notifications.Count;
}

/// <summary>
/// In-memory webhook notification provider
/// </summary>
public class InMemoryWebhookProvider : IWebhookNotificationProvider
{
    private readonly ILogger<InMemoryWebhookProvider> _logger;
    private static readonly ConcurrentDictionary<Guid, WebhookNotification> _sentWebhooks = new();
    private static readonly ConcurrentDictionary<string, List<NotificationType>> _registeredWebhooks = new();

    public string ProviderName => "InMemory";

    public InMemoryWebhookProvider(ILogger<InMemoryWebhookProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendAsync(WebhookNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("Sending webhook notification {NotificationId} to {Url}",
            notification.NotificationId, notification.Url);

        _sentWebhooks.TryAdd(notification.NotificationId, notification);

        // Simulate HTTP request processing time
        return Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
    }

    public Task<DeliveryStatus> VerifyDeliveryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        // For in-memory provider, assume successful delivery if webhook was sent
        return Task.FromResult(_sentWebhooks.ContainsKey(notificationId) ? DeliveryStatus.Delivered : DeliveryStatus.Unknown);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task RegisterWebhookAsync(string url, string secret, IEnumerable<NotificationType> eventTypes, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(eventTypes);

        _logger.LogInformation("Registering webhook {Url} for event types: {EventTypes}", url, string.Join(", ", eventTypes));

        _registeredWebhooks.AddOrUpdate(url, eventTypes.ToList(), (_, _) => eventTypes.ToList());
        return Task.CompletedTask;
    }

    public Task UnregisterWebhookAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogInformation("Unregistering webhook {Url}", url);

        _registeredWebhooks.TryRemove(url, out _);
        return Task.CompletedTask;
    }

    public Task<WebhookTestResult> TestWebhookAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogInformation("Testing webhook {Url}", url);

        // Simulate webhook test
        var result = new WebhookTestResult
        {
            IsSuccessful = true,
            StatusCode = 200,
            ResponseTimeMs = Random.Shared.Next(50, 500),
            ResponseHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Server", "InMemory" }
            }
        };

        return Task.FromResult(result);
    }

    public static IEnumerable<WebhookNotification> GetSentWebhooks() => _sentWebhooks.Values;
    public static WebhookNotification? GetSentWebhook(Guid notificationId) => _sentWebhooks.TryGetValue(notificationId, out var webhook) ? webhook : null;
    public static void ClearSentWebhooks() { _sentWebhooks.Clear(); _registeredWebhooks.Clear(); }
    public static int GetSentWebhooksCount() => _sentWebhooks.Count;
    public static IReadOnlyDictionary<string, List<NotificationType>> GetRegisteredWebhooks() => _registeredWebhooks;
}