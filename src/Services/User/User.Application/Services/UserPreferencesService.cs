using AutoMapper;
using Enterprise.Shared.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using User.Core.DTOs;
using User.Core.DTOs.Requests;
using User.Core.Entities;
using User.Core.Events;
using User.Core.Interfaces;

namespace User.Application.Services;

/// <summary>
/// User preferences service implementation
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<UserPreferencesService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserPreferencesService(
        IUserPreferencesRepository userPreferencesRepository,
        IUserProfileRepository userProfileRepository,
        IMapper mapper,
        IMediator mediator,
        ILogger<UserPreferencesService> logger)
    {
        _userPreferencesRepository = userPreferencesRepository ?? throw new ArgumentNullException(nameof(userPreferencesRepository));
        _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user preferences by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences or error</returns>
    public async Task<Result<UserPreferencesDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserPreferencesDto>.Failure("UserId cannot be empty");
            }

            _logger.LogInformation("Getting user preferences for UserId: {UserId}", userId);

            var userPreferences = await _userPreferencesRepository.GetByUserIdAsync(userId, cancellationToken);

            if (userPreferences == null)
            {
                _logger.LogWarning("User preferences not found for UserId: {UserId}", userId);
                return Result<UserPreferencesDto>.Failure("User preferences not found");
            }

            var userPreferencesDto = _mapper.Map<UserPreferencesDto>(userPreferences);
            
            _logger.LogInformation("Successfully retrieved user preferences for UserId: {UserId}", userId);
            return Result<UserPreferencesDto>.Success(userPreferencesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user preferences for UserId: {UserId}", userId);
            return Result<UserPreferencesDto>.Failure("An error occurred while retrieving user preferences");
        }
    }

    /// <summary>
    /// Create user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferencesDto">Preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user preferences or error</returns>
    public async Task<Result<UserPreferencesDto>> CreateAsync(string userId, UserPreferencesDto preferencesDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserPreferencesDto>.Failure("UserId cannot be empty");
            }

            if (preferencesDto == null)
            {
                return Result<UserPreferencesDto>.Failure("Preferences data cannot be null");
            }

            _logger.LogInformation("Creating user preferences for UserId: {UserId}", userId);

            // Check if user profile exists
            var userExists = await _userProfileRepository.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("User profile not found for UserId: {UserId}", userId);
                return Result<UserPreferencesDto>.Failure("User profile not found");
            }

            // Check if preferences already exist
            var existingPreferences = await _userPreferencesRepository.ExistsAsync(userId, cancellationToken);
            if (existingPreferences)
            {
                _logger.LogWarning("User preferences already exist for UserId: {UserId}", userId);
                return Result<UserPreferencesDto>.Failure("User preferences already exist");
            }

            // Create new preferences
            var userPreferences = _mapper.Map<UserPreferences>(preferencesDto);
            userPreferences.UserId = userId;
            userPreferences.CreatedBy = userId;
            userPreferences.UpdatedBy = userId;

            var createdPreferences = await _userPreferencesRepository.CreateAsync(userPreferences, cancellationToken);
            var createdPreferencesDto = _mapper.Map<UserPreferencesDto>(createdPreferences);

            _logger.LogInformation("Successfully created user preferences for UserId: {UserId}", userId);
            
            return Result<UserPreferencesDto>.Success(createdPreferencesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user preferences for UserId: {UserId}", userId);
            return Result<UserPreferencesDto>.Failure("An error occurred while creating user preferences");
        }
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferencesDto">Updated preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user preferences or error</returns>
    public async Task<Result<UserPreferencesDto>> UpdateAsync(string userId, UserPreferencesDto preferencesDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserPreferencesDto>.Failure("UserId cannot be empty");
            }

            if (preferencesDto == null)
            {
                return Result<UserPreferencesDto>.Failure("Preferences data cannot be null");
            }

            _logger.LogInformation("Updating user preferences for UserId: {UserId}", userId);

            var existingPreferences = await _userPreferencesRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existingPreferences == null)
            {
                _logger.LogWarning("User preferences not found for update, UserId: {UserId}", userId);
                return Result<UserPreferencesDto>.Failure("User preferences not found");
            }

            // Store previous values for event
            var previousValues = new Dictionary<string, object>();
            var changedPreferences = new Dictionary<string, object>();

            // Track changes for notification preferences
            if (preferencesDto.EmailNotifications != existingPreferences.EmailNotifications)
            {
                previousValues["EmailNotifications"] = existingPreferences.EmailNotifications;
                changedPreferences["EmailNotifications"] = preferencesDto.EmailNotifications;
                existingPreferences.EmailNotifications = preferencesDto.EmailNotifications;
            }

            if (preferencesDto.SmsNotifications != existingPreferences.SmsNotifications)
            {
                previousValues["SmsNotifications"] = existingPreferences.SmsNotifications;
                changedPreferences["SmsNotifications"] = preferencesDto.SmsNotifications;
                existingPreferences.SmsNotifications = preferencesDto.SmsNotifications;
            }

            if (preferencesDto.PushNotifications != existingPreferences.PushNotifications)
            {
                previousValues["PushNotifications"] = existingPreferences.PushNotifications;
                changedPreferences["PushNotifications"] = preferencesDto.PushNotifications;
                existingPreferences.PushNotifications = preferencesDto.PushNotifications;
            }

            // Track changes for privacy preferences
            if (preferencesDto.ProfileVisibility != existingPreferences.ProfileVisibility)
            {
                previousValues["ProfileVisibility"] = existingPreferences.ProfileVisibility;
                changedPreferences["ProfileVisibility"] = preferencesDto.ProfileVisibility;
                existingPreferences.ProfileVisibility = preferencesDto.ProfileVisibility;
            }

            // Track GDPR-related changes
            bool gdprConsentChanged = false;
            if (preferencesDto.DataProcessingConsent != existingPreferences.DataProcessingConsent)
            {
                previousValues["DataProcessingConsent"] = existingPreferences.DataProcessingConsent;
                changedPreferences["DataProcessingConsent"] = preferencesDto.DataProcessingConsent;
                existingPreferences.DataProcessingConsent = preferencesDto.DataProcessingConsent;
                gdprConsentChanged = true;
            }

            if (preferencesDto.MarketingEmailsConsent != existingPreferences.MarketingEmailsConsent)
            {
                previousValues["MarketingEmailsConsent"] = existingPreferences.MarketingEmailsConsent;
                changedPreferences["MarketingEmailsConsent"] = preferencesDto.MarketingEmailsConsent;
                existingPreferences.MarketingEmailsConsent = preferencesDto.MarketingEmailsConsent;
                gdprConsentChanged = true;
            }

            // AnalyticsConsent not available in entity

            // Update other preferences
            if (preferencesDto.Theme != existingPreferences.Theme)
            {
                previousValues["Theme"] = existingPreferences.Theme;
                changedPreferences["Theme"] = preferencesDto.Theme;
                existingPreferences.Theme = preferencesDto.Theme;
            }

            // TwoFactorEnabled not available in entity

            existingPreferences.UpdatedBy = userId;

            var updatedPreferences = await _userPreferencesRepository.UpdateAsync(existingPreferences, cancellationToken);

            // Publish domain event if there were changes
            if (changedPreferences.Count > 0)
            {
                var domainEvent = new UserPreferencesChangedEvent
                {
                    UserId = userId,
                    ChangedPreferences = changedPreferences,
                    PreviousValues = previousValues,
                    ChangedBy = userId,
                    Source = "UserService",
                    GdprConsentChanged = gdprConsentChanged
                };

                await _mediator.Publish(domainEvent, cancellationToken);
            }

            var userPreferencesDto = _mapper.Map<UserPreferencesDto>(updatedPreferences);

            _logger.LogInformation("Successfully updated user preferences for UserId: {UserId}, Changed: {ChangedCount}", 
                userId, changedPreferences.Count);

            return Result<UserPreferencesDto>.Success(userPreferencesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences for UserId: {UserId}", userId);
            return Result<UserPreferencesDto>.Failure("An error occurred while updating user preferences");
        }
    }

    /// <summary>
    /// Delete user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error result</returns>
    public async Task<Result<bool>> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("UserId cannot be empty");
            }

            _logger.LogInformation("Deleting user preferences for UserId: {UserId}", userId);

            var deleted = await _userPreferencesRepository.DeleteAsync(userId, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("User preferences not found for deletion, UserId: {UserId}", userId);
                return Result<bool>.Failure("User preferences not found");
            }

            _logger.LogInformation("Successfully deleted user preferences for UserId: {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user preferences for UserId: {UserId}", userId);
            return Result<bool>.Failure("An error occurred while deleting user preferences");
        }
    }

    /// <summary>
    /// Get users by notification preferences
    /// </summary>
    /// <param name="emailNotifications">Email notifications enabled</param>
    /// <param name="smsNotifications">SMS notifications enabled</param>
    /// <param name="pushNotifications">Push notifications enabled</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user IDs or error</returns>
    public async Task<Result<List<string>>> GetUsersByNotificationPreferencesAsync(
        bool? emailNotifications = null,
        bool? smsNotifications = null,
        bool? pushNotifications = null,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting users by notification preferences: Email={Email}, SMS={SMS}, Push={Push}", 
                emailNotifications, smsNotifications, pushNotifications);

            var userIds = await _userPreferencesRepository.GetUsersByNotificationPreferencesAsync(
                emailNotifications, smsNotifications, pushNotifications, skip, take, cancellationToken);

            _logger.LogInformation("Found {Count} users matching notification preferences", userIds.Count);

            return Result<List<string>>.Success(userIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by notification preferences");
            return Result<List<string>>.Failure("An error occurred while retrieving users by notification preferences");
        }
    }

    /// <summary>
    /// Get users by privacy settings
    /// </summary>
    /// <param name="profileVisibility">Profile visibility setting</param>
    /// <param name="dataProcessingConsent">Data processing consent</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user IDs or error</returns>
    public async Task<Result<List<string>>> GetUsersByPrivacySettingsAsync(
        string? profileVisibility = null,
        bool? dataProcessingConsent = null,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting users by privacy settings: Visibility={Visibility}, DataConsent={DataConsent}", 
                profileVisibility, dataProcessingConsent);

            var userIds = await _userPreferencesRepository.GetUsersByPrivacySettingsAsync(
                profileVisibility, dataProcessingConsent, skip, take, cancellationToken);

            _logger.LogInformation("Found {Count} users matching privacy settings", userIds.Count);

            return Result<List<string>>.Success(userIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by privacy settings");
            return Result<List<string>>.Failure("An error occurred while retrieving users by privacy settings");
        }
    }

    /// <summary>
    /// Update user preferences using UpdateUserPreferencesRequest
    /// </summary>
    public async Task<Result<UserPreferencesDto>> UpdateAsync(string userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        // Convert UpdateUserPreferencesRequest to UserPreferencesDto and call existing method
        var currentPreferences = await GetByUserIdAsync(userId, cancellationToken);
        if (!currentPreferences.IsSuccess)
            return currentPreferences;

        var preferencesDto = currentPreferences.Value!;
        
        // Update fields from request
        if (request.EmailNotifications.HasValue)
            preferencesDto.EmailNotifications = request.EmailNotifications.Value;
        if (request.SmsNotifications.HasValue)
            preferencesDto.SmsNotifications = request.SmsNotifications.Value;
        if (request.PushNotifications.HasValue)
            preferencesDto.PushNotifications = request.PushNotifications.Value;
        if (!string.IsNullOrWhiteSpace(request.ProfileVisibility))
            preferencesDto.ProfileVisibility = request.ProfileVisibility;
        if (!string.IsNullOrWhiteSpace(request.Theme))
            preferencesDto.Theme = request.Theme;

        return await UpdateAsync(userId, preferencesDto, cancellationToken);
    }

    /// <summary>
    /// Reset preferences to default
    /// </summary>
    public async Task<Result<UserPreferencesDto>> ResetToDefaultAsync(string userId, CancellationToken cancellationToken = default)
    {
        var defaultPreferences = new UserPreferencesDto
        {
            EmailNotifications = true,
            SmsNotifications = false,
            PushNotifications = true,
            ProfileVisibility = "Public",
            Theme = "Light",
            DataProcessingConsent = true,
            MarketingEmailsConsent = false,
            AnalyticsConsent = false,
            TwoFactorEnabled = false
        };

        return await UpdateAsync(userId, defaultPreferences, cancellationToken);
    }

    /// <summary>
    /// Update GDPR consent
    /// </summary>
    public async Task<Result<bool>> UpdateConsentAsync(string userId, bool marketingConsent, bool dataProcessingConsent, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPreferences = await GetByUserIdAsync(userId, cancellationToken);
            if (!currentPreferences.IsSuccess)
                return Result<bool>.Failure(currentPreferences.Error);

            var preferencesDto = currentPreferences.Value!;
            preferencesDto.MarketingEmailsConsent = marketingConsent;
            preferencesDto.DataProcessingConsent = dataProcessingConsent;

            var updateResult = await UpdateAsync(userId, preferencesDto, cancellationToken);
            return updateResult.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(updateResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating consent for UserId: {UserId}", userId);
            return Result<bool>.Failure("An error occurred while updating consent");
        }
    }
}