using User.Core.Entities;

namespace User.Core.Interfaces;

/// <summary>
/// Repository interface for UserProfile entity
/// </summary>
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId, bool includeRelated = true, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdAsync(int id, bool includeRelated = true, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> GetByUserIdsAsync(IEnumerable<string> userIds, bool includeRelated = false, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> SearchAsync(string searchTerm, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserProfile> CreateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);
    Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserActivity>> GetUserActivitiesAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}