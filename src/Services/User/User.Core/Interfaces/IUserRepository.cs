using Enterprise.Shared.Common.Models;
using User.Core.Entities;

namespace User.Core.Interfaces;

/// <summary>
/// Repository interface for user profile data access
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile entity or null</returns>
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profiles by multiple user IDs
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user profiles</returns>
    Task<IEnumerable<UserProfile>> GetMultipleAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new user profile
    /// </summary>
    /// <param name="userProfile">User profile entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile</returns>
    Task<UserProfile> CreateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing user profile
    /// </summary>
    /// <param name="userProfile">User profile entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user profile exists
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated user profiles
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result</returns>
    Task<PagedResult<UserProfile>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
}