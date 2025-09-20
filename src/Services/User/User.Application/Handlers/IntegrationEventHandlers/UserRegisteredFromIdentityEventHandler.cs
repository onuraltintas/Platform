using MassTransit;
using Microsoft.Extensions.Logging;
using User.Core.Events;
using User.Core.Interfaces;

namespace User.Application.Handlers.IntegrationEventHandlers;

/// <summary>
/// Handler for UserRegisteredFromIdentityEvent - creates user profile when user registers
/// </summary>
public class UserRegisteredFromIdentityEventHandler : IConsumer<UserRegisteredFromIdentityEvent>
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<UserRegisteredFromIdentityEventHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserRegisteredFromIdentityEventHandler(
        IUserProfileService userProfileService,
        ILogger<UserRegisteredFromIdentityEventHandler> logger)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle UserRegisteredFromIdentityEvent
    /// </summary>
    /// <param name="context">Message context</param>
    public async Task Consume(ConsumeContext<UserRegisteredFromIdentityEvent> context)
    {
        var message = context.Message;
        
        try
        {
            _logger.LogInformation("Processing UserRegisteredFromIdentityEvent for UserId: {UserId}, Email: {Email}", 
                message.UserId, message.Email);

            // Check if user profile already exists
            var existingProfile = await _userProfileService.GetByUserIdAsync(message.UserId);
            if (existingProfile.IsSuccess)
            {
                _logger.LogWarning("User profile already exists for UserId: {UserId}, skipping creation", message.UserId);
                return;
            }

            // Create new user profile
            var createResult = await _userProfileService.CreateAsync(message.UserId);
            
            if (!createResult.IsSuccess)
            {
                _logger.LogError("Failed to create user profile for UserId: {UserId}, Error: {Error}", 
                    message.UserId, createResult.Error);
                
                // Throw exception to trigger retry mechanism
                throw new InvalidOperationException($"Failed to create user profile: {createResult.Error}");
            }

            // Update profile with information from registration if available
            if (!string.IsNullOrWhiteSpace(message.FirstName) || !string.IsNullOrWhiteSpace(message.LastName))
            {
                var updateRequest = new User.Core.DTOs.Requests.UpdateUserProfileRequest
                {
                    FirstName = message.FirstName ?? string.Empty,
                    LastName = message.LastName ?? string.Empty,
                    PhoneNumber = message.PhoneNumber
                };

                var updateResult = await _userProfileService.UpdateAsync(message.UserId, updateRequest);
                if (!updateResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update user profile with registration data for UserId: {UserId}, Error: {Error}", 
                        message.UserId, updateResult.Error);
                }
            }

            _logger.LogInformation("Successfully created user profile for UserId: {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserRegisteredFromIdentityEvent for UserId: {UserId}", message.UserId);
            throw; // Re-throw to trigger retry mechanism
        }
    }
}