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
/// User profile service implementation
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<UserProfileService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserProfileService(
        IUserProfileRepository userProfileRepository,
        IUserPreferencesRepository userPreferencesRepository,
        IMapper mapper,
        IMediator mediator,
        ILogger<UserProfileService> logger)
    {
        _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        _userPreferencesRepository = userPreferencesRepository ?? throw new ArgumentNullException(nameof(userPreferencesRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile or error</returns>
    public async Task<Result<UserProfileDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserProfileDto>.Failure("UserId cannot be empty");
            }

            _logger.LogInformation("Getting user profile for UserId: {UserId}", userId);

            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId, includeRelated: true, cancellationToken);

            if (userProfile == null)
            {
                _logger.LogWarning("User profile not found for UserId: {UserId}", userId);
                return Result<UserProfileDto>.Failure("User profile not found");
            }

            var userProfileDto = _mapper.Map<UserProfileDto>(userProfile);
            
            _logger.LogInformation("Successfully retrieved user profile for UserId: {UserId}", userId);
            return Result<UserProfileDto>.Success(userProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for UserId: {UserId}", userId);
            return Result<UserProfileDto>.Failure("An error occurred while retrieving user profile");
        }
    }

    /// <summary>
    /// Create user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile or error</returns>
    public async Task<Result<UserProfileDto>> CreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserProfileDto>.Failure("UserId cannot be empty");
            }

            _logger.LogInformation("Creating user profile for UserId: {UserId}", userId);

            // Check if profile already exists
            var existingProfile = await _userProfileRepository.ExistsAsync(userId, cancellationToken);
            if (existingProfile)
            {
                _logger.LogWarning("User profile already exists for UserId: {UserId}", userId);
                return Result<UserProfileDto>.Failure("User profile already exists");
            }

            // Create new profile
            var userProfile = new UserProfile
            {
                UserId = userId,
                FirstName = string.Empty,
                LastName = string.Empty,
                TimeZone = Enterprise.Shared.Common.Enums.TimeZone.Utc,
                Language = Enterprise.Shared.Common.Enums.LanguageCode.En,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var createdProfile = await _userProfileRepository.CreateAsync(userProfile, cancellationToken);

            // Create default preferences
            var preferences = new UserPreferences
            {
                UserId = userId,
                EmailNotifications = true,
                SmsNotifications = false,
                PushNotifications = true,
                ProfileVisibility = "Public",
                DataProcessingConsent = true,
                MarketingEmailsConsent = false,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            await _userPreferencesRepository.CreateAsync(preferences, cancellationToken);

            // Map to DTO
            var userProfileDto = _mapper.Map<UserProfileDto>(createdProfile);

            _logger.LogInformation("Successfully created user profile for UserId: {UserId}", userId);
            
            return Result<UserProfileDto>.Success(userProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user profile for UserId: {UserId}", userId);
            return Result<UserProfileDto>.Failure("An error occurred while creating user profile");
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile or error</returns>
    public async Task<Result<UserProfileDto>> UpdateAsync(string userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserProfileDto>.Failure("UserId cannot be empty");
            }

            if (request == null)
            {
                return Result<UserProfileDto>.Failure("Update request cannot be null");
            }

            _logger.LogInformation("Updating user profile for UserId: {UserId}", userId);

            var existingProfile = await _userProfileRepository.GetByUserIdAsync(userId, includeRelated: true, cancellationToken);
            if (existingProfile == null)
            {
                _logger.LogWarning("User profile not found for update, UserId: {UserId}", userId);
                return Result<UserProfileDto>.Failure("User profile not found");
            }

            // Store previous values for event
            var previousValues = new Dictionary<string, object?>();
            var changedFields = new List<string>();

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName != existingProfile.FirstName)
            {
                previousValues["FirstName"] = existingProfile.FirstName;
                existingProfile.FirstName = request.FirstName;
                changedFields.Add("FirstName");
            }

            if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName != existingProfile.LastName)
            {
                previousValues["LastName"] = existingProfile.LastName;
                existingProfile.LastName = request.LastName;
                changedFields.Add("LastName");
            }

            if (request.PhoneNumber != existingProfile.PhoneNumber)
            {
                previousValues["PhoneNumber"] = existingProfile.PhoneNumber;
                existingProfile.PhoneNumber = request.PhoneNumber;
                changedFields.Add("PhoneNumber");
            }

            if (request.DateOfBirth != existingProfile.DateOfBirth)
            {
                previousValues["DateOfBirth"] = existingProfile.DateOfBirth;
                existingProfile.DateOfBirth = request.DateOfBirth;
                changedFields.Add("DateOfBirth");
            }

            if (request.Bio != existingProfile.Bio)
            {
                previousValues["Bio"] = existingProfile.Bio;
                existingProfile.Bio = request.Bio;
                changedFields.Add("Bio");
            }

            if (request.TimeZone.HasValue && request.TimeZone != existingProfile.TimeZone)
            {
                previousValues["TimeZone"] = existingProfile.TimeZone;
                existingProfile.TimeZone = request.TimeZone.Value;
                changedFields.Add("TimeZone");
            }

            if (request.Language.HasValue && request.Language != existingProfile.Language)
            {
                previousValues["Language"] = existingProfile.Language;
                existingProfile.Language = request.Language.Value;
                changedFields.Add("Language");
            }

            existingProfile.UpdatedBy = userId;

            var updatedProfile = await _userProfileRepository.UpdateAsync(existingProfile, cancellationToken);

            // Publish domain event if there were changes
            if (changedFields.Count > 0)
            {
                var domainEvent = new UserProfileUpdatedEvent
                {
                    UserId = userId,
                    FirstName = updatedProfile.FirstName,
                    LastName = updatedProfile.LastName,
                    PhoneNumber = updatedProfile.PhoneNumber,
                    ProfilePictureUrl = updatedProfile.ProfilePictureUrl,
                    UpdatedFields = changedFields,
                    UpdatedBy = userId,
                    Source = "UserService"
                };

                await _mediator.Publish(domainEvent, cancellationToken);
            }

            var userProfileDto = _mapper.Map<UserProfileDto>(updatedProfile);

            _logger.LogInformation("Successfully updated user profile for UserId: {UserId}, Fields: {Fields}", 
                userId, string.Join(", ", changedFields));

            return Result<UserProfileDto>.Success(userProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for UserId: {UserId}", userId);
            return Result<UserProfileDto>.Failure("An error occurred while updating user profile");
        }
    }

    /// <summary>
    /// Delete user profile
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

            _logger.LogInformation("Deleting user profile for UserId: {UserId}", userId);

            var deleted = await _userProfileRepository.DeleteAsync(userId, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("User profile not found for deletion, UserId: {UserId}", userId);
                return Result<bool>.Failure("User profile not found");
            }

            // Also delete preferences
            await _userPreferencesRepository.DeleteAsync(userId, cancellationToken);

            _logger.LogInformation("Successfully deleted user profile for UserId: {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user profile for UserId: {UserId}", userId);
            return Result<bool>.Failure("An error occurred while deleting user profile");
        }
    }

    /// <summary>
    /// Search user profiles
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user profiles or error</returns>
    public async Task<Result<List<UserProfileDto>>> SearchAsync(string searchTerm, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching user profiles with term: {SearchTerm}, Skip: {Skip}, Take: {Take}", 
                searchTerm, skip, take);

            var userProfiles = await _userProfileRepository.SearchAsync(searchTerm, skip, take, cancellationToken);
            var userProfileDtos = _mapper.Map<List<UserProfileDto>>(userProfiles);

            _logger.LogInformation("Found {Count} user profiles for search term: {SearchTerm}", 
                userProfileDtos.Count, searchTerm);

            return Result<List<UserProfileDto>>.Success(userProfileDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching user profiles with term: {SearchTerm}", searchTerm);
            return Result<List<UserProfileDto>>.Failure("An error occurred while searching user profiles");
        }
    }

    /// <summary>
    /// Get user activities
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user activities or error</returns>
    public async Task<Result<List<UserActivityDto>>> GetUserActivitiesAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<List<UserActivityDto>>.Failure("UserId cannot be empty");
            }

            _logger.LogInformation("Getting user activities for UserId: {UserId}", userId);

            var activities = await _userProfileRepository.GetUserActivitiesAsync(userId, skip, take, cancellationToken);
            var activityDtos = _mapper.Map<List<UserActivityDto>>(activities);

            _logger.LogInformation("Retrieved {Count} activities for UserId: {UserId}", activityDtos.Count, userId);

            return Result<List<UserActivityDto>>.Success(activityDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activities for UserId: {UserId}", userId);
            return Result<List<UserActivityDto>>.Failure("An error occurred while retrieving user activities");
        }
    }

    /// <summary>
    /// Update profile picture
    /// </summary>
    public Task<Result<string>> UpdateProfilePictureAsync(string userId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // TODO: Implement profile picture upload to MinIO
        return Task.FromResult(Result<string>.Failure("Profile picture update not yet implemented"));
    }

    /// <summary>
    /// Get multiple user profiles by user IDs
    /// </summary>
    public async Task<Result<IEnumerable<UserProfileDto>>> GetMultipleAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var userProfiles = await _userProfileRepository.GetByUserIdsAsync(userIds, includeRelated: false, cancellationToken);
            var userProfileDtos = _mapper.Map<IEnumerable<UserProfileDto>>(userProfiles);
            return Result<IEnumerable<UserProfileDto>>.Success(userProfileDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple user profiles");
            return Result<IEnumerable<UserProfileDto>>.Failure("An error occurred while retrieving user profiles");
        }
    }
}