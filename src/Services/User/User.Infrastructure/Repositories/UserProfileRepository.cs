using Microsoft.EntityFrameworkCore;
using User.Core.Entities;
using User.Core.Interfaces;
using User.Infrastructure.Data;

namespace User.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserProfile entity
/// </summary>
public class UserProfileRepository : IUserProfileRepository
{
    private readonly UserDbContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public UserProfileRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile or null</returns>
    public async Task<UserProfile?> GetByUserIdAsync(string userId, bool includeRelated = true, CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(u => u.Preferences)
                .Include(u => u.Addresses)
                .Include(u => u.Activities.Take(10)) // Limit activities for performance
                .Include(u => u.Documents)
                .Include(u => u.EmailVerifications);
        }

        return await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile or null</returns>
    public async Task<UserProfile?> GetByIdAsync(int id, bool includeRelated = true, CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(u => u.Preferences)
                .Include(u => u.Addresses)
                .Include(u => u.Activities.Take(10))
                .Include(u => u.Documents)
                .Include(u => u.EmailVerifications);
        }

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get multiple user profiles by user IDs
    /// </summary>
    /// <param name="userIds">User IDs</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user profiles</returns>
    public async Task<List<UserProfile>> GetByUserIdsAsync(IEnumerable<string> userIds, bool includeRelated = false, CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles.Where(u => userIds.Contains(u.UserId));

        if (includeRelated)
        {
            query = query
                .Include(u => u.Preferences)
                .Include(u => u.Addresses);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Search user profiles
    /// </summary>
    /// <param name="searchTerm">Search term (name, email)</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user profiles</returns>
    public async Task<List<UserProfile>> SearchAsync(string searchTerm, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term));
        }

        return await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Check if user profile exists
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    public async Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.AnyAsync(u => u.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Create user profile
    /// </summary>
    /// <param name="userProfile">User profile to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile</returns>
    public async Task<UserProfile> CreateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        userProfile.CreatedAt = DateTime.UtcNow;
        userProfile.UpdatedAt = DateTime.UtcNow;

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
        
        return userProfile;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="userProfile">User profile to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    public async Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        userProfile.UpdatedAt = DateTime.UtcNow;

        _context.UserProfiles.Update(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
        
        return userProfile;
    }

    /// <summary>
    /// Delete user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted</returns>
    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        
        if (userProfile == null)
            return false;

        _context.UserProfiles.Remove(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    /// <summary>
    /// Get user activities
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user activities</returns>
    public async Task<List<UserActivity>> GetUserActivitiesAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.UserActivities
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}