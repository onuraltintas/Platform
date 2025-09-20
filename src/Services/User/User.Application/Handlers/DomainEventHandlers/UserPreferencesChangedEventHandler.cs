using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using User.Core.Events;

namespace User.Application.Handlers.DomainEventHandlers;

/// <summary>
/// Domain event handler for UserPreferencesChangedEvent
/// </summary>
public class UserPreferencesChangedEventHandler : INotificationHandler<UserPreferencesChangedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserPreferencesChangedEventHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserPreferencesChangedEventHandler(
        IPublishEndpoint publishEndpoint,
        ILogger<UserPreferencesChangedEventHandler> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle UserPreferencesChangedEvent
    /// </summary>
    /// <param name="notification">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(UserPreferencesChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing UserPreferencesChangedEvent for UserId: {UserId}, Changes: {ChangeCount}, GDPRConsentChanged: {GdprChanged}", 
                notification.UserId, notification.ChangedPreferences.Count, notification.GdprConsentChanged);

            // Create integration event for other services
            var integrationEvent = new UserPreferencesChangedIntegrationEvent
            {
                UserId = notification.UserId,
                ChangedPreferences = notification.ChangedPreferences,
                PreviousValues = notification.PreviousValues,
                ChangedBy = notification.ChangedBy,
                Source = "UserService",
                GdprConsentChanged = notification.GdprConsentChanged,
                CorrelationId = notification.CorrelationId
            };

            // Publish integration event to message bus
            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully published UserPreferencesChangedIntegrationEvent for UserId: {UserId}", 
                notification.UserId);

            // If GDPR consent changed, publish specific event for compliance services
            if (notification.GdprConsentChanged)
            {
                var gdprEvent = new UserGdprConsentChangedIntegrationEvent
                {
                    UserId = notification.UserId,
                    DataProcessingConsent = notification.ChangedPreferences.ContainsKey("DataProcessingConsent") 
                        ? (bool)notification.ChangedPreferences["DataProcessingConsent"] 
                        : null,
                    MarketingConsent = notification.ChangedPreferences.ContainsKey("MarketingConsent") 
                        ? (bool)notification.ChangedPreferences["MarketingConsent"] 
                        : null,
                    AnalyticsConsent = notification.ChangedPreferences.ContainsKey("AnalyticsConsent") 
                        ? (bool)notification.ChangedPreferences["AnalyticsConsent"] 
                        : null,
                    ChangedBy = notification.ChangedBy,
                    Source = "UserService",
                    CorrelationId = notification.CorrelationId
                };

                await _publishEndpoint.Publish(gdprEvent, cancellationToken);
                
                _logger.LogInformation("Successfully published UserGdprConsentChangedIntegrationEvent for UserId: {UserId}", 
                    notification.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserPreferencesChangedEvent for UserId: {UserId}", notification.UserId);
            // Don't re-throw - we don't want to break the main operation
        }
    }
}

/// <summary>
/// Integration event published when user preferences are changed
/// </summary>
public record UserPreferencesChangedIntegrationEvent : Enterprise.Shared.Events.Models.IntegrationEvent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Changed preferences as key-value pairs
    /// </summary>
    public Dictionary<string, object> ChangedPreferences { get; init; } = new();

    /// <summary>
    /// Previous values for audit
    /// </summary>
    public Dictionary<string, object> PreviousValues { get; init; } = new();

    /// <summary>
    /// Who made the changes
    /// </summary>
    public string ChangedBy { get; init; } = string.Empty;

    /// <summary>
    /// Whether GDPR consent was affected
    /// </summary>
    public bool GdprConsentChanged { get; init; }
}

/// <summary>
/// Integration event published when GDPR consent is changed
/// </summary>
public record UserGdprConsentChangedIntegrationEvent : Enterprise.Shared.Events.Models.IntegrationEvent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Data processing consent
    /// </summary>
    public bool? DataProcessingConsent { get; init; }

    /// <summary>
    /// Marketing consent
    /// </summary>
    public bool? MarketingConsent { get; init; }

    /// <summary>
    /// Analytics consent
    /// </summary>
    public bool? AnalyticsConsent { get; init; }

    /// <summary>
    /// Who made the changes
    /// </summary>
    public string ChangedBy { get; init; } = string.Empty;
}