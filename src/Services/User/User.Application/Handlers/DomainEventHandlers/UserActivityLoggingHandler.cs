using MediatR;
using Microsoft.Extensions.Logging;
using User.Core.Entities;
using User.Core.Events;
using User.Core.Interfaces;

namespace User.Application.Handlers.DomainEventHandlers;

/// <summary>
/// Handler for logging user activities when domain events occur
/// </summary>
public class UserActivityLoggingHandler : 
    INotificationHandler<UserProfileUpdatedEvent>,
    INotificationHandler<UserPreferencesChangedEvent>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<UserActivityLoggingHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserActivityLoggingHandler(
        IUserProfileRepository userProfileRepository,
        ILogger<UserActivityLoggingHandler> logger)
    {
        _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle UserProfileUpdatedEvent for activity logging
    /// </summary>
    /// <param name="notification">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(UserProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await LogUserActivity(new UserActivity
            {
                UserId = notification.UserId,
                ActivityType = "ProfileUpdate",
                Description = $"Profile updated: {string.Join(", ", notification.UpdatedFields)}",
                Success = true,
                CreatedBy = notification.UpdatedBy,
                UpdatedBy = notification.UpdatedBy,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    UpdatedFields = notification.UpdatedFields,
                    Source = notification.Source,
                    CorrelationId = notification.CorrelationId
                })
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity for UserProfileUpdatedEvent, UserId: {UserId}", notification.UserId);
        }
    }

    /// <summary>
    /// Handle UserPreferencesChangedEvent for activity logging
    /// </summary>
    /// <param name="notification">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Handle(UserPreferencesChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var changedKeys = string.Join(", ", notification.ChangedPreferences.Keys);
            var activityType = notification.GdprConsentChanged ? "GdprConsentChange" : "PreferencesUpdate";
            var description = notification.GdprConsentChanged 
                ? $"GDPR consent changed: {changedKeys}"
                : $"Preferences updated: {changedKeys}";

            await LogUserActivity(new UserActivity
            {
                UserId = notification.UserId,
                ActivityType = activityType,
                Description = description,
                Success = true,
                CreatedBy = notification.ChangedBy,
                UpdatedBy = notification.ChangedBy,
                RiskScore = notification.GdprConsentChanged ? 10 : 0, // Higher risk for GDPR changes
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ChangedPreferences = notification.ChangedPreferences,
                    PreviousValues = notification.PreviousValues,
                    GdprConsentChanged = notification.GdprConsentChanged,
                    Source = notification.Source,
                    CorrelationId = notification.CorrelationId
                })
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity for UserPreferencesChangedEvent, UserId: {UserId}", notification.UserId);
        }
    }

    /// <summary>
    /// Log user activity
    /// </summary>
    /// <param name="activity">Activity to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task LogUserActivity(UserActivity activity, CancellationToken cancellationToken)
    {
        // For now, we'll implement a simple repository-based logging
        // In a production system, this could be enhanced with dedicated activity tracking service
        
        _logger.LogInformation("Logging user activity: UserId={UserId}, Type={ActivityType}, Description={Description}", 
            activity.UserId, activity.ActivityType, activity.Description);

        // TODO: Implement actual user activity repository save
        // await _userActivityRepository.CreateAsync(activity, cancellationToken);
        
        // For now, just log it
        await Task.CompletedTask;
    }
}

/// <summary>
/// Handler for logging authentication and security-related activities
/// </summary>
public class SecurityActivityLoggingHandler
{
    private readonly ILogger<SecurityActivityLoggingHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public SecurityActivityLoggingHandler(ILogger<SecurityActivityLoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log security activity for user registration
    /// </summary>
    /// <param name="registrationEvent">Registration event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LogUserRegistrationAsync(UserRegisteredFromIdentityEvent registrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Security Activity: User registered - UserId={UserId}, Email={Email}, Method={Method}, EmailConfirmed={EmailConfirmed}", 
                registrationEvent.UserId, 
                registrationEvent.Email, 
                registrationEvent.RegistrationMethod, 
                registrationEvent.EmailConfirmed);

            // Log as high-value security event
            var securityActivity = new UserActivity
            {
                UserId = registrationEvent.UserId,
                ActivityType = "UserRegistration",
                Description = $"User registered via {registrationEvent.RegistrationMethod}",
                Success = true,
                IpAddress = registrationEvent.IpAddress,
                UserAgent = registrationEvent.UserAgent,
                RiskScore = registrationEvent.EmailConfirmed ? 5 : 15, // Higher risk if email not confirmed
                CreatedBy = "System",
                UpdatedBy = "System",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Email = registrationEvent.Email,
                    RegistrationMethod = registrationEvent.RegistrationMethod,
                    EmailConfirmed = registrationEvent.EmailConfirmed,
                    HasPhoneNumber = !string.IsNullOrWhiteSpace(registrationEvent.PhoneNumber),
                    CorrelationId = registrationEvent.CorrelationId
                })
            };

            // TODO: Save to security audit log
            await Task.CompletedTask;

            _logger.LogInformation("Successfully logged user registration security activity for UserId: {UserId}", registrationEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security activity for UserRegisteredFromIdentityEvent, UserId: {UserId}", registrationEvent.UserId);
        }
    }
}