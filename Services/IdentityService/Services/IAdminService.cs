// StyleCop: Dosya başlığı uyarısını proje standardı gereği bastırıyoruz
using System.Diagnostics.CodeAnalysis;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Shared.Errors.Models;

[assembly: SuppressMessage("Style", "SA1633:File header is missing or not located at the top of the file", Justification = "Interface dosyası için basit başlık politikası")]

namespace EgitimPlatform.Services.IdentityService.Services;

public interface IAdminService
{
    Task<ServiceResult<List<RoleDto>>> GetRolesAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<RoleDto?>> GetRoleByIdAsync(string roleId);
    Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<ServiceResult<RoleDto>> UpdateRoleAsync(string roleId, UpdateRoleRequest request);
    Task<ServiceResult<bool>> DeleteRoleAsync(string roleId);

    Task<ServiceResult<List<CategoryDto>>> GetCategoriesAsync(string? search = null, string? type = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<CategoryDto?>> GetCategoryByIdAsync(string categoryId);
    Task<ServiceResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(string categoryId, UpdateCategoryRequest request);
    Task<ServiceResult<bool>> DeleteCategoryAsync(string categoryId);

    Task<ServiceResult<List<PermissionDto>>> GetPermissionsAsync(string? search = null, string? group = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<PermissionDto?>> GetPermissionByIdAsync(string permissionId);
    Task<ServiceResult<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest request);
    Task<ServiceResult<PermissionDto>> UpdatePermissionAsync(string permissionId, UpdatePermissionRequest request);
    Task<ServiceResult<bool>> DeletePermissionAsync(string permissionId);

    Task<ServiceResult<List<UserRoleDto>>> GetUserRolesAsync(string? userId = null, string? roleId = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<UserRoleDto>> AssignRoleToUserAsync(AssignRoleToUserRequest request);
    Task<ServiceResult<bool>> RemoveRoleFromUserAsync(string userId, string roleId);
    Task<ServiceResult<UserRoleDto>> UpdateUserRoleAsync(string userId, string roleId, UpdateUserRoleRequest request);

    Task<ServiceResult<List<UserCategoryDto>>> GetUserCategoriesAsync(string? userId = null, string? categoryId = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<UserCategoryDto>> AssignCategoryToUserAsync(AssignCategoryToUserRequest request);
    Task<ServiceResult<bool>> RemoveCategoryFromUserAsync(string userId, string categoryId);
    Task<ServiceResult<UserCategoryDto>> UpdateUserCategoryAsync(string userId, string categoryId, UpdateUserCategoryRequest request);

    Task<ServiceResult<List<string>>> GetRolePermissionsAsync(string roleId);
    Task<ServiceResult<bool>> AssignPermissionToRoleAsync(string roleId, string permissionId);
    Task<ServiceResult<bool>> RemovePermissionFromRoleAsync(string roleId, string permissionId);
    Task<ServiceResult<bool>> UpdateRolePermissionsAsync(string roleId, List<string> permissionIds);

    Task<ServiceResult<List<RefreshTokenDto>>> GetActiveRefreshTokensAsync(string? userId = null, int page = 1, int pageSize = 20);
    Task<ServiceResult<bool>> RevokeRefreshTokenAsync(string tokenId, string reason);
    Task<ServiceResult<bool>> RevokeAllUserTokensAsync(string userId, string reason);

    // Users
    Task<ServiceResult<PagedUsersResponse>> GetUsersAsync(GetUsersRequest request);
    Task<ServiceResult<UserDto?>> GetUserByIdAsync(string userId);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ServiceResult<UserDto>> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<ServiceResult<bool>> DeactivateUserAsync(string userId);
    Task<ServiceResult<bool>> ActivateUserAsync(string userId);
    Task<ServiceResult<bool>> DeleteUserAsync(string userId);
}