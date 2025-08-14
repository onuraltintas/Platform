using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Services;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Security.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Logging.Attributes;
using EgitimPlatform.Shared.Logging.Services;

namespace EgitimPlatform.Services.IdentityService.Controllers;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Style", "SA1101:Prefix local calls with this", Justification = "Ekip stili gereği this prefix zorunlu değil")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IStructuredLogger _logger;

    public AdminController(IAdminService adminService, IStructuredLogger logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    #region Role Management

    /// <summary>
    /// Test endpoint
    /// </summary>
    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return Ok("AdminController is working!");
    }

    /// <summary>
    /// Rolleri listele
    /// </summary>
    [HttpGet("roles")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetRolesAsync(search, isActive, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve roles"));
    }

    /// <summary>
    /// Rol detaylarını getir
    /// </summary>
    [HttpGet("roles/{roleId}")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<RoleDto?>>> GetRole(string roleId)
    {
        var result = await _adminService.GetRoleByIdAsync(roleId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data ?? new RoleDto()))
            : NotFound(ApiResponse.Fail(ErrorCodes.NOT_FOUND, result.ErrorMessage ?? "Role not found"));
    }

    /// <summary>
    /// Yeni rol oluştur
    /// </summary>
    [HttpPost("roles")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await _adminService.CreateRoleAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to create role"));
    }

    /// <summary>
    /// Rol güncelle
    /// </summary>
    [HttpPut("roles/{roleId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request)
    {
        var result = await _adminService.UpdateRoleAsync(roleId, request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update role"));
    }

    /// <summary>
    /// Rol sil
    /// </summary>
    [HttpDelete("roles/{roleId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(string roleId)
    {
        var result = await _adminService.DeleteRoleAsync(roleId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Role deleted successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to delete role"));
    }

    #endregion

    #region Category Management

    /// <summary>
    /// Kategorileri listele
    /// </summary>
    [HttpGet("categories")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetCategoriesAsync(search, type, isActive, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve categories"));
    }

    /// <summary>
    /// Kategori detaylarını getir
    /// </summary>
    [HttpGet("categories/{categoryId}")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<CategoryDto?>>> GetCategory(string categoryId)
    {
        var result = await _adminService.GetCategoryByIdAsync(categoryId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data ?? new CategoryDto()))
            : NotFound(ApiResponse.Fail(ErrorCodes.NOT_FOUND, result.ErrorMessage ?? "Category not found"));
    }

    /// <summary>
    /// Yeni kategori oluştur
    /// </summary>
    [HttpPost("categories")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await _adminService.CreateCategoryAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to create category"));
    }

    /// <summary>
    /// Kategori güncelle
    /// </summary>
    [HttpPut("categories/{categoryId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(string categoryId, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _adminService.UpdateCategoryAsync(categoryId, request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update category"));
    }

    /// <summary>
    /// Kategori sil
    /// </summary>
    [HttpDelete("categories/{categoryId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(string categoryId)
    {
        var result = await _adminService.DeleteCategoryAsync(categoryId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Category deleted successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to delete category"));
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// İzinleri listele
    /// </summary>
    [HttpGet("permissions")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissions(
        [FromQuery] string? search = null,
        [FromQuery] string? group = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetPermissionsAsync(search, group, isActive, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve permissions"));
    }

    /// <summary>
    /// İzin detaylarını getir
    /// </summary>
    [HttpGet("permissions/{permissionId}")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<PermissionDto?>>> GetPermission(string permissionId)
    {
        var result = await _adminService.GetPermissionByIdAsync(permissionId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data ?? new PermissionDto()))
            : NotFound(ApiResponse.Fail(ErrorCodes.NOT_FOUND, result.ErrorMessage ?? "Permission not found"));
    }

    /// <summary>
    /// Yeni izin oluştur
    /// </summary>
    [HttpPost("permissions")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var result = await _adminService.CreatePermissionAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to create permission"));
    }

    /// <summary>
    /// İzin güncelle
    /// </summary>
    [HttpPut("permissions/{permissionId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> UpdatePermission(string permissionId, [FromBody] UpdatePermissionRequest request)
    {
        var result = await _adminService.UpdatePermissionAsync(permissionId, request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update permission"));
    }

    /// <summary>
    /// İzin sil
    /// </summary>
    [HttpDelete("permissions/{permissionId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> DeletePermission(string permissionId)
    {
        var result = await _adminService.DeletePermissionAsync(permissionId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Permission deleted successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to delete permission"));
    }

    #endregion

    #region User Role Management

    /// <summary>
    /// Kullanıcı rollerini listele
    /// </summary>
    [HttpGet("user-roles")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<UserRoleDto>>>> GetUserRoles(
        [FromQuery] string? userId = null,
        [FromQuery] string? roleId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetUserRolesAsync(userId, roleId, isActive, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve user roles"));
    }

    /// <summary>
    /// Kullanıcıya rol ata
    /// </summary>
    [HttpPost("user-roles")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<UserRoleDto>>> AssignRoleToUser([FromBody] AssignRoleToUserRequest request)
    {
        var result = await _adminService.AssignRoleToUserAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to assign role to user"));
    }

    /// <summary>
    /// Kullanıcıdan rol kaldır
    /// </summary>
    [HttpDelete("user-roles/{userId}/{roleId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> RemoveRoleFromUser(string userId, string roleId)
    {
        var result = await _adminService.RemoveRoleFromUserAsync(userId, roleId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Role removed from user successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to remove role from user"));
    }

    /// <summary>
    /// Kullanıcı rolünü güncelle
    /// </summary>
    [HttpPut("user-roles/{userId}/{roleId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<UserRoleDto>>> UpdateUserRole(string userId, string roleId, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _adminService.UpdateUserRoleAsync(userId, roleId, request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update user role"));
    }

    #endregion

    #region User Category Management

    /// <summary>
    /// Kullanıcı kategorilerini listele
    /// </summary>
    [HttpGet("user-categories")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<UserCategoryDto>>>> GetUserCategories(
        [FromQuery] string? userId = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetUserCategoriesAsync(userId, categoryId, isActive, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve user categories"));
    }

    /// <summary>
    /// Kullanıcıya kategori ata
    /// </summary>
    [HttpPost("user-categories")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<UserCategoryDto>>> AssignCategoryToUser([FromBody] AssignCategoryToUserRequest request)
    {
        var result = await _adminService.AssignCategoryToUserAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to assign category to user"));
    }

    /// <summary>
    /// Kullanıcıdan kategori kaldır
    /// </summary>
    [HttpDelete("user-categories/{userId}/{categoryId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> RemoveCategoryFromUser(string userId, string categoryId)
    {
        var result = await _adminService.RemoveCategoryFromUserAsync(userId, categoryId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Category removed from user successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to remove category from user"));
    }

    /// <summary>
    /// Kullanıcı kategorisini güncelle
    /// </summary>
    [HttpPut("user-categories/{userId}/{categoryId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<UserCategoryDto>>> UpdateUserCategory(string userId, string categoryId, [FromBody] UpdateUserCategoryRequest request)
    {
        var result = await _adminService.UpdateUserCategoryAsync(userId, categoryId, request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update user category"));
    }

    #endregion

    #region Role Permission Management

    /// <summary>
    /// Rol izinlerini listele
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetRolePermissions(string roleId)
    {
        var result = await _adminService.GetRolePermissionsAsync(roleId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve role permissions"));
    }

    /// <summary>
    /// Role izin ata
    /// </summary>
    [HttpPost("roles/{roleId}/permissions/{permissionId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> AssignPermissionToRole(string roleId, string permissionId)
    {
        var result = await _adminService.AssignPermissionToRoleAsync(roleId, permissionId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Permission assigned to role successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to assign permission to role"));
    }

    /// <summary>
    /// Rolden izin kaldır
    /// </summary>
    [HttpDelete("roles/{roleId}/permissions/{permissionId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> RemovePermissionFromRole(string roleId, string permissionId)
    {
        var result = await _adminService.RemovePermissionFromRoleAsync(roleId, permissionId);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Permission removed from role successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to remove permission from role"));
    }

    /// <summary>
    /// Rol izinlerini toplu güncelle
    /// </summary>
    [HttpPut("roles/{roleId}/permissions")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> UpdateRolePermissions(string roleId, [FromBody] List<string> permissionIds)
    {
        var result = await _adminService.UpdateRolePermissionsAsync(roleId, permissionIds);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Role permissions updated successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update role permissions"));
    }

    #endregion

    #region Refresh Token Management

    /// <summary>
    /// Aktif refresh token'ları listele
    /// </summary>
    [HttpGet("refresh-tokens")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<List<RefreshTokenDto>>>> GetActiveRefreshTokens(
        [FromQuery] string? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetActiveRefreshTokensAsync(userId, page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to retrieve refresh tokens"));
    }

    /// <summary>
    /// Refresh token'ı iptal et
    /// </summary>
    [HttpDelete("refresh-tokens/{tokenId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> RevokeRefreshToken(string tokenId, [FromQuery] string reason = "Revoked by admin")
    {
        var result = await _adminService.RevokeRefreshTokenAsync(tokenId, reason);
        return result.IsSuccess ? Ok(ApiResponse.Ok("Refresh token revoked successfully")) : BadRequest(result);
    }

    /// <summary>
    /// Kullanıcının tüm token'larını iptal et
    /// </summary>
    [HttpDelete("users/{userId}/refresh-tokens")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAllUserTokens(string userId, [FromQuery] string reason = "All tokens revoked by admin")
    {
        var result = await _adminService.RevokeAllUserTokensAsync(userId, reason);
        return result.IsSuccess ? Ok(ApiResponse.Ok("All user tokens revoked successfully")) : BadRequest(result);
    }

    #endregion

    #region User Management (Additional endpoints for completeness)

    /// <summary>
    /// Kullanıcıları listele
    /// </summary>
    [HttpGet("users")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? roleId = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isEmailConfirmed = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var request = new GetUsersRequest
        {
            Search = search,
            RoleId = roleId,
            CategoryId = categoryId,
            IsActive = isActive,
            IsEmailConfirmed = isEmailConfirmed,
            Page = page,
            PageSize = pageSize
        };

        var result = await _adminService.GetUsersAsync(request);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data!)) : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to get users"));
    }

    /// <summary>
    /// Kullanıcı detaylarını getir
    /// </summary>
    [HttpGet("users/{userId}")]
    [Permission(Permissions.Users.Read)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string userId)
    {
        Console.WriteLine($"GetUser called for userId: {userId}");
        var result = await _adminService.GetUserByIdAsync(userId);
        return result.IsSuccess && result.Data != null
            ? Ok(ApiResponse.Ok(result.Data))
            : NotFound(ApiResponse.Fail(ErrorCodes.NOT_FOUND, result.ErrorMessage ?? "User not found"));
    }

    /// <summary>
    /// Yeni kullanıcı oluştur
    /// </summary>
    [HttpPost("users")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        _logger.LogInformation("CONTROLLER: CreateUser called with payload: {@Request}", new
        {
            UserName = request.UserName,
            Email = request.Email,
            RoleIds = request.RoleIds ?? new List<string>(),
            CategoryIds = request.CategoryIds ?? new List<string>(),
            RoleIdsCount = request.RoleIds?.Count ?? 0,
            CategoryIdsCount = request.CategoryIds?.Count ?? 0
        });

        var result = await _adminService.CreateUserAsync(request).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            _logger.LogError("CONTROLLER: CreateUser failed. IsSuccess: {IsSuccess}, ErrorMessage: {ErrorMessage}, Data: {Data}",
                result.IsSuccess, result.ErrorMessage ?? string.Empty, (object?)result.Data ?? new object());
        }

        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data ?? new UserDto()))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to create user"));
    }

    /// <summary>
    /// Kullanıcı güncelle
    /// </summary>
    [HttpPut("users/{userId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var result = await _adminService.UpdateUserAsync(userId, request).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to update user"));
    }

    /// <summary>
    /// Kullanıcıyı deaktif et
    /// </summary>
    [HttpPatch("users/{userId}/deactivate")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateUser(string userId)
    {
        var result = await _adminService.DeactivateUserAsync(userId).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User deactivated successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to deactivate user"));
    }

    /// <summary>
    /// Kullanıcıyı aktif et
    /// </summary>
    [HttpPatch("users/{userId}/activate")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> ActivateUser(string userId)
    {
        var result = await _adminService.ActivateUserAsync(userId).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User activated successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to activate user"));
    }

    /// <summary>
    /// Kullanıcı sil
    /// </summary>
    [HttpDelete("users/{userId}")]
    [Permission(Permissions.Users.Write)]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string userId)
    {
        var result = await _adminService.DeleteUserAsync(userId).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("User deleted successfully"))
            : BadRequest(ApiResponse.Fail("ERROR", result.ErrorMessage ?? "Failed to delete user"));
    }

    #endregion
}