using EgitimPlatform.Shared.Security.Models;

namespace EgitimPlatform.Shared.Security.Services;

public interface ICategoryRoleService
{
    Task<bool> AssignCategoryRoleAsync(string userId, string category, string role, DateTime? expiresAt = null, string? assignedBy = null, string? notes = null);
    Task<bool> RemoveCategoryRoleAsync(string userId, string category, string role);
    Task<bool> RemoveAllCategoryRolesAsync(string userId, string? category = null);
    Task<List<UserCategoryRole>> GetUserCategoryRolesAsync(string userId);
    Task<List<UserCategoryRole>> GetCategoryUsersAsync(string category);
    Task<List<UserCategoryRole>> GetRoleUsersAsync(string role);
    Task<bool> HasCategoryRoleAsync(string userId, string category, string role);
    Task<List<string>> GetUserCategoriesAsync(string userId);
    Task<List<string>> GetUserRolesInCategoryAsync(string userId, string category);
    Task<List<string>> GetUserCategoriesForRoleAsync(string userId, string role);
    Task<bool> UpdateCategoryRoleExpirationAsync(string userId, string category, string role, DateTime? expiresAt);
    Task<bool> ActivateDeactivateCategoryRoleAsync(string userId, string category, string role, bool isActive);
    Task<List<UserCategoryRole>> GetExpiringCategoryRolesAsync(DateTime beforeDate);
    Task<int> CleanupExpiredCategoryRolesAsync();
}