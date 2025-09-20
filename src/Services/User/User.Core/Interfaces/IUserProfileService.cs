using Enterprise.Shared.Common.Models;
using User.Core.DTOs;
using User.Core.DTOs.Requests;

namespace User.Core.Interfaces;

/// <summary>
/// Service interface for user profile operations
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile or error</returns>
    Task<Result<UserProfileDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new user profile
    /// </summary>
    /// <param name="userId">User ID from Identity Service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile or error</returns>
    Task<Result<UserProfileDto>> CreateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile or error</returns>
    Task<Result<UserProfileDto>> UpdateAsync(string userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user profile (GDPR compliance)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<Result<bool>> DeleteAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update profile picture
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fileStream">Image file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">Content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated profile URL or error</returns>
    Task<Result<string>> UpdateProfilePictureAsync(string userId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profiles by multiple user IDs
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user profiles</returns>
    Task<Result<IEnumerable<UserProfileDto>>> GetMultipleAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}