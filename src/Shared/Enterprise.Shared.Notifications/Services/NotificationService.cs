using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Notifications.Services;

/// <summary>
/// Main notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailNotificationProvider _emailProvider;
    private readonly ISmsNotificationProvider _smsProvider;
    private readonly IPushNotificationProvider _pushProvider;
    private readonly IInAppNotificationProvider _inAppProvider;
    private readonly IWebhookNotificationProvider _webhookProvider;
    private readonly ITemplateService _templateService;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationSettings _settings;

    public NotificationService(
        IEmailNotificationProvider emailProvider,
        ISmsNotificationProvider smsProvider,
        IPushNotificationProvider pushProvider,
        IInAppNotificationProvider inAppProvider,
        IWebhookNotificationProvider webhookProvider,
        ITemplateService templateService,
        INotificationPreferencesService preferencesService,
        ILogger<NotificationService> logger,
        IOptions<NotificationSettings> settings)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
        _smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
        _pushProvider = pushProvider ?? throw new ArgumentNullException(nameof(pushProvider));
        _inAppProvider = inAppProvider ?? throw new ArgumentNullException(nameof(inAppProvider));
        _webhookProvider = webhookProvider ?? throw new ArgumentNullException(nameof(webhookProvider));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.General.Enabled)
        {
            _logger.LogInformation("Notifications are disabled. Skipping notification {NotificationId}", request.NotificationId);
            return;
        }

        try
        {
            _logger.LogInformation("Sending notification {NotificationId} to user {UserId} via channels [{Channels}]",
                request.NotificationId, request.UserId, string.Join(", ", request.Channels));

            // Check if notification has expired
            if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Notification {NotificationId} has expired. Skipping.", request.NotificationId);
                return;
            }

            // Get user preferences
            var preferences = await _preferencesService.GetUserPreferencesAsync(request.UserId, cancellationToken);
            
            // Check do not disturb and quiet hours
            if (preferences.DoNotDisturb || await _preferencesService.IsInQuietHoursAsync(request.UserId, cancellationToken))
            {
                // Only send critical notifications during quiet hours/DND
                if (request.Priority != NotificationPriority.Critical)
                {
                    _logger.LogInformation("User {UserId} is in DND/quiet hours. Skipping non-critical notification {NotificationId}",
                        request.UserId, request.NotificationId);
                    return;
                }
            }

            // Filter channels by user preferences
            var enabledChannels = await FilterChannelsByPreferencesAsync(request.Channels, preferences, request.Type, cancellationToken);
            
            if (!enabledChannels.Any())
            {
                _logger.LogInformation("All channels disabled for user {UserId} and notification type {Type}. Skipping notification {NotificationId}",
                    request.UserId, request.Type, request.NotificationId);
                return;
            }

            // Render template
            RenderedTemplate? renderedContent = null;
            if (!string.IsNullOrEmpty(request.TemplateKey))
            {
                renderedContent = await _templateService.RenderAsync(
                    request.TemplateKey,
                    request.Data,
                    preferences.Language,
                    cancellationToken);
            }

            // Send to each enabled channel
            var sendTasks = enabledChannels.Select(channel => 
                SendToChannelAsync(request, channel, renderedContent, preferences, cancellationToken));

            await Task.WhenAll(sendTasks);

            _logger.LogInformation("Notification {NotificationId} sent successfully to user {UserId}",
                request.NotificationId, request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId} to user {UserId}",
                request.NotificationId, request.UserId);
            throw;
        }
    }

    public async Task SendBulkAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.General.Enabled)
        {
            _logger.LogInformation("Notifications are disabled. Skipping bulk notification");
            return;
        }

        _logger.LogInformation("Sending bulk notification of type {Type} to {UserCount} users via channels [{Channels}]",
            request.Type, request.UserIds.Length, string.Join(", ", request.Channels));

        try
        {
            var batchSize = Math.Min(request.BatchSize, _settings.Delivery.BatchSize);
            var userBatches = request.UserIds.Chunk(batchSize);

            foreach (var userBatch in userBatches)
            {
                var batchTasks = userBatch.Select(userId => SendAsync(new NotificationRequest
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = userId,
                    Type = request.Type,
                    Channels = request.Channels,
                    TemplateKey = request.TemplateKey,
                    Data = request.Data,
                    Priority = request.Priority,
                    CorrelationId = request.CorrelationId
                }, cancellationToken));

                await Task.WhenAll(batchTasks);
                
                // Small delay between batches to prevent overwhelming providers
                if (batchSize < request.UserIds.Length)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            _logger.LogInformation("Bulk notification completed for {UserCount} users", request.UserIds.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notification of type {Type}", request.Type);
            throw;
        }
    }

    public async Task ScheduleAsync(ScheduledNotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Scheduling notification {NotificationId} for {ScheduledAt}",
            request.NotificationId, request.ScheduledAt);

        // For now, we'll implement a simple delay-based scheduling
        // In a real implementation, this would use a job scheduler like Hangfire or Quartz
        var delay = request.ScheduledAt - DateTime.UtcNow;
        if (delay > TimeSpan.Zero)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, cancellationToken);
                    await SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing scheduled notification {NotificationId}", request.NotificationId);
                }
            }, cancellationToken);
        }
        else
        {
            // Send immediately if scheduled time is in the past
            await SendAsync(request, cancellationToken);
        }
    }

    public Task<NotificationStatus> GetStatusAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        // This would typically query a database or cache
        // For now, return a simple implementation
        _logger.LogDebug("Getting status for notification {NotificationId}", notificationId);
        return Task.FromResult(NotificationStatus.Sent);
    }

    public Task<IEnumerable<NotificationHistory>> GetHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // This would typically query a database
        // For now, return empty collection
        _logger.LogDebug("Getting notification history for user {UserId}, page {Page}, size {PageSize}", 
            userId, page, pageSize);
        return Task.FromResult(Enumerable.Empty<NotificationHistory>());
    }

    public Task<NotificationStatistics> GetStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // This would typically query a database and calculate statistics
        // For now, return empty statistics
        _logger.LogDebug("Getting notification statistics from {FromDate} to {ToDate}", fromDate, toDate);
        var statistics = new NotificationStatistics
        {
            GeneratedAt = DateTime.UtcNow
        };
        return Task.FromResult(statistics);
    }

    public Task CancelAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling notification {NotificationId}", notificationId);
        // Implementation would cancel scheduled notification
        return Task.CompletedTask;
    }

    public Task RetryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying notification {NotificationId}", notificationId);
        // Implementation would retry failed notification
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<NotificationChannel>> FilterChannelsByPreferencesAsync(
        NotificationChannel[] requestedChannels,
        UserNotificationPreferences preferences,
        NotificationType notificationType,
        CancellationToken cancellationToken)
    {
        var enabledChannels = new List<NotificationChannel>();

        foreach (var channel in requestedChannels)
        {
            var isChannelEnabled = channel switch
            {
                NotificationChannel.Email => preferences.EmailEnabled,
                NotificationChannel.SMS => preferences.SmsEnabled,
                NotificationChannel.Push => preferences.PushEnabled,
                NotificationChannel.InApp => preferences.InAppEnabled,
                NotificationChannel.Webhook => preferences.WebhookEnabled,
                _ => false
            };

            // Check type-specific preferences
            if (isChannelEnabled && preferences.TypePreferences.TryGetValue(notificationType, out var typePreference))
            {
                isChannelEnabled = channel switch
                {
                    NotificationChannel.Email => typePreference.EmailEnabled,
                    NotificationChannel.SMS => typePreference.SmsEnabled,
                    NotificationChannel.Push => typePreference.PushEnabled,
                    NotificationChannel.InApp => typePreference.InAppEnabled,
                    NotificationChannel.Webhook => typePreference.WebhookEnabled,
                    _ => false
                };
            }

            if (isChannelEnabled)
            {
                enabledChannels.Add(channel);
            }
        }

        return enabledChannels;
    }

    private async Task SendToChannelAsync(
        NotificationRequest request,
        NotificationChannel channel,
        RenderedTemplate? template,
        UserNotificationPreferences preferences,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Sending notification {NotificationId} via {Channel}", 
                request.NotificationId, channel);

            switch (channel)
            {
                case NotificationChannel.Email:
                    await SendEmailNotificationAsync(request, template, cancellationToken);
                    break;

                case NotificationChannel.SMS:
                    await SendSmsNotificationAsync(request, template, cancellationToken);
                    break;

                case NotificationChannel.Push:
                    await SendPushNotificationAsync(request, template, cancellationToken);
                    break;

                case NotificationChannel.InApp:
                    await SendInAppNotificationAsync(request, template, cancellationToken);
                    break;

                case NotificationChannel.Webhook:
                    await SendWebhookNotificationAsync(request, template, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unsupported notification channel: {Channel}", channel);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId} via {Channel}",
                request.NotificationId, channel);

            // TODO: Implement retry logic using background job service
            // await ScheduleRetryAsync(request, channel, template, cancellationToken);
        }
    }

    private async Task SendEmailNotificationAsync(NotificationRequest request, RenderedTemplate? template, CancellationToken cancellationToken)
    {
        var emailNotification = new EmailNotification
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            Subject = template?.Subject ?? request.Subject ?? "Notification",
            HtmlContent = template?.HtmlContent ?? string.Empty,
            TextContent = template?.TextContent ?? request.CustomMessage ?? "Notification",
            ToEmail = "user@example.com", // This would come from user service
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        await _emailProvider.SendAsync(emailNotification, cancellationToken);
    }

    private async Task SendSmsNotificationAsync(NotificationRequest request, RenderedTemplate? template, CancellationToken cancellationToken)
    {
        var smsNotification = new SmsNotification
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            Message = template?.SmsContent ?? template?.TextContent ?? request.CustomMessage ?? "Notification",
            ToPhoneNumber = "+1234567890", // This would come from user service
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        await _smsProvider.SendAsync(smsNotification, cancellationToken);
    }

    private async Task SendPushNotificationAsync(NotificationRequest request, RenderedTemplate? template, CancellationToken cancellationToken)
    {
        var pushNotification = new PushNotification
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            Title = template?.PushTitle ?? template?.Subject ?? request.Subject ?? "Notification",
            Body = template?.PushBody ?? template?.TextContent ?? request.CustomMessage ?? "Notification",
            Data = request.Data,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        await _pushProvider.SendAsync(pushNotification, cancellationToken);
    }

    private async Task SendInAppNotificationAsync(NotificationRequest request, RenderedTemplate? template, CancellationToken cancellationToken)
    {
        var inAppNotification = new InAppNotification
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            Title = template?.Subject ?? request.Subject ?? "Notification",
            Content = template?.TextContent ?? request.CustomMessage ?? "Notification",
            Priority = request.Priority,
            Type = MapToInAppNotificationType(request.Type),
            ExpiresAt = request.ExpiresAt,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        await _inAppProvider.SendAsync(inAppNotification, cancellationToken);
    }

    private async Task SendWebhookNotificationAsync(NotificationRequest request, RenderedTemplate? template, CancellationToken cancellationToken)
    {
        var webhookNotification = new WebhookNotification
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            Url = "https://webhook.example.com", // This would come from configuration
            Payload = new
            {
                notificationId = request.NotificationId,
                userId = request.UserId,
                type = request.Type.ToString(),
                data = request.Data,
                template = template,
                timestamp = DateTime.UtcNow
            },
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        await _webhookProvider.SendAsync(webhookNotification, cancellationToken);
    }

    private static InAppNotificationType MapToInAppNotificationType(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.SecurityAlert => InAppNotificationType.Error,
            NotificationType.SystemMaintenance => InAppNotificationType.Warning,
            NotificationType.PaymentSuccess or NotificationType.OrderConfirmation => InAppNotificationType.Success,
            NotificationType.PaymentFailed => InAppNotificationType.Error,
            _ => InAppNotificationType.Info
        };
    }
}