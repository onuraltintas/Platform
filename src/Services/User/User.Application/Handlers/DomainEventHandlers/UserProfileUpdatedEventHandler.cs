using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using User.Core.Events;
using User.Core.Interfaces;

namespace User.Application.Handlers.DomainEventHandlers;

/// <summary>
/// Domain event handler for UserProfileUpdatedEvent
/// </summary>
public class UserProfileUpdatedEventHandler : INotificationHandler<UserProfileUpdatedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserProfileUpdatedEventHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserProfileUpdatedEventHandler(
        IPublishEndpoint publishEndpoint,
        ILogger<UserProfileUpdatedEventHandler> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle UserProfileUpdatedEvent
    /// </summary>
    /// <param name="notification">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(UserProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing UserProfileUpdatedEvent for UserId: {UserId}, UpdatedFields: {Fields}", 
                notification.UserId, string.Join(", ", notification.UpdatedFields));

            // Create integration event for other services
            var integrationEvent = new UserProfileUpdatedIntegrationEvent
            {
                UserId = notification.UserId,
                FirstName = notification.FirstName,
                LastName = notification.LastName,
                PhoneNumber = notification.PhoneNumber,
                ProfilePictureUrl = notification.ProfilePictureUrl,
                UpdatedFields = notification.UpdatedFields,
                UpdatedBy = notification.UpdatedBy,
                Source = "UserService",
                CorrelationId = notification.CorrelationId
            };

            // Publish integration event to message bus
            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully published UserProfileUpdatedIntegrationEvent for UserId: {UserId}", 
                notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserProfileUpdatedEvent for UserId: {UserId}", notification.UserId);
            // Don't re-throw - we don't want to break the main operation
        }
    }
}

/// <summary>
/// Integration event published when user profile is updated
/// </summary>
public record UserProfileUpdatedIntegrationEvent : Enterprise.Shared.Events.Models.IntegrationEvent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Updated first name
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Updated last name
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Updated phone number
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Updated profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; init; }

    /// <summary>
    /// Fields that were updated
    /// </summary>
    public List<string> UpdatedFields { get; init; } = new();

    /// <summary>
    /// Who updated the profile
    /// </summary>
    public string UpdatedBy { get; init; } = string.Empty;
}