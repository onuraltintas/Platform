using Microsoft.EntityFrameworkCore;
using User.Core.Entities;
using User.Core.Interfaces;
using User.Infrastructure.Data;

namespace User.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserPreferences entity
/// </summary>
public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly UserDbContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public UserPreferencesRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Get user preferences by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences or null</returns>
    public async Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences
            .Include(p => p.UserProfile)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Get user preferences by ID
    /// </summary>
    /// <param name="id">Preferences ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences or null</returns>
    public async Task<UserPreferences?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences
            .Include(p => p.UserProfile)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Check if user preferences exist
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    public async Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences.AnyAsync(p => p.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Create user preferences
    /// </summary>
    /// <param name="preferences">User preferences to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user preferences</returns>
    public async Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        preferences.CreatedAt = DateTime.UtcNow;
        preferences.UpdatedAt = DateTime.UtcNow;

        _context.UserPreferences.Add(preferences);
        await _context.SaveChangesAsync(cancellationToken);
        
        return preferences;
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    /// <param name="preferences">User preferences to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user preferences</returns>
    public async Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        preferences.UpdatedAt = DateTime.UtcNow;

        _context.UserPreferences.Update(preferences);
        await _context.SaveChangesAsync(cancellationToken);
        
        return preferences;
    }

    /// <summary>
    /// Delete user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted</returns>
    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var preferences = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        if (preferences == null)
            return false;

        _context.UserPreferences.Remove(preferences);
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
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
    /// <returns>List of user IDs</returns>
    public async Task<List<string>> GetUsersByNotificationPreferencesAsync(
        bool? emailNotifications = null,
        bool? smsNotifications = null, 
        bool? pushNotifications = null,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserPreferences.AsQueryable();

        if (emailNotifications.HasValue)
            query = query.Where(p => p.EmailNotifications == emailNotifications.Value);

        if (smsNotifications.HasValue)
            query = query.Where(p => p.SmsNotifications == smsNotifications.Value);

        if (pushNotifications.HasValue)
            query = query.Where(p => p.PushNotifications == pushNotifications.Value);

        return await query
            .Skip(skip)
            .Take(take)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get users by privacy settings
    /// </summary>
    /// <param name="profileVisibility">Profile visibility setting</param>
    /// <param name="dataProcessingConsent">Data processing consent</param>
    /// <param name="skip">Records to skip</param>
    /// <param name="take">Records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user IDs</returns>
    public async Task<List<string>> GetUsersByPrivacySettingsAsync(
        string? profileVisibility = null,
        bool? dataProcessingConsent = null,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserPreferences.AsQueryable();

        if (!string.IsNullOrWhiteSpace(profileVisibility))
            query = query.Where(p => p.ProfileVisibility == profileVisibility);

        if (dataProcessingConsent.HasValue)
            query = query.Where(p => p.DataProcessingConsent == dataProcessingConsent.Value);

        return await query
            .Skip(skip)
            .Take(take)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
    }
}