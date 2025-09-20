using User.Core.Entities;

namespace User.Core.Interfaces;

/// <summary>
/// Repository interface for UserPreferences entity
/// </summary>
public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetUsersByNotificationPreferencesAsync(bool? emailNotifications = null, bool? smsNotifications = null, bool? pushNotifications = null, int skip = 0, int take = 1000, CancellationToken cancellationToken = default);
    Task<List<string>> GetUsersByPrivacySettingsAsync(string? profileVisibility = null, bool? dataProcessingConsent = null, int skip = 0, int take = 1000, CancellationToken cancellationToken = default);
}