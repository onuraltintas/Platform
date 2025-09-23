using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string userId, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> LockAsync(string userId, string? reason = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> UnlockAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<bool>> ResetPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<bool>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
    Task<Result<bool>> ConfirmEmailByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateEmailConfirmationTokenAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsEmailConfirmedAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserGroupDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsUserInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);

    // Permission management methods
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    Task<PagedResult<PermissionDto>> GetAllPermissionsAsync(int page = 1, int pageSize = 50, string? search = null, string? category = null, string? service = null);
    Task<PermissionDto?> GetPermissionByCodeAsync(string permissionCode);
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request);
    Task<PermissionDto?> UpdatePermissionAsync(string permissionCode, UpdatePermissionRequest request);
    Task<bool> DeletePermissionAsync(string permissionCode);
    Task AssignPermissionToRoleAsync(string roleId, string permissionCode, bool isWildcard = false, string? permissionPattern = null);
    Task<bool> RemovePermissionFromRoleAsync(string roleId, string permissionCode);
    Task AssignDirectPermissionToUserAsync(string userId, string permissionCode, string type = "Grant", bool isWildcard = false, string? permissionPattern = null);
    Task<bool> RemoveDirectPermissionFromUserAsync(string userId, string permissionCode);
    Task<List<string>> GetPermissionCategoriesAsync();
    Task<List<string>> GetPermissionServicesAsync();
}

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<PagedResult<ApplicationUser>> GetUsersAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<ApplicationUser> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<ApplicationUser> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsEmailTakenAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUserNameTakenAsync(string userName, string? excludeUserId = null, CancellationToken cancellationToken = default);
}