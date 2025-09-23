using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Enterprise.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Dynamic permission configuration management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class PermissionConfigurationController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<PermissionConfigurationController> _logger;

    public PermissionConfigurationController(
        IUserService userService,
        ILogger<PermissionConfigurationController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available permissions
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<PagedResult<PermissionDto>>> GetPermissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? service = null)
    {
        try
        {
            var permissions = await _userService.GetAllPermissionsAsync(page, pageSize, search, category, service);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions");
            return StatusCode(500, new { error = "Failed to get permissions" });
        }
    }

    /// <summary>
    /// Get permission by code
    /// </summary>
    [HttpGet("permissions/{permissionCode}")]
    public async Task<ActionResult<PermissionDto>> GetPermission(string permissionCode)
    {
        try
        {
            var permission = await _userService.GetPermissionByCodeAsync(permissionCode);

            if (permission == null)
            {
                return NotFound(new { error = $"Permission '{permissionCode}' not found" });
            }

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission {PermissionCode}", permissionCode);
            return StatusCode(500, new { error = "Failed to get permission" });
        }
    }

    /// <summary>
    /// Create new permission
    /// </summary>
    [HttpPost("permissions")]
    public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        try
        {
            var permission = await _userService.CreatePermissionAsync(request);

            _logger.LogInformation("Created permission {PermissionCode} by admin {AdminUser}",
                request.Code, User.FindFirst(ClaimTypes.Name)?.Value);

            return CreatedAtAction(nameof(GetPermission), new { permissionCode = permission.Code }, permission);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            return StatusCode(500, new { error = "Failed to create permission" });
        }
    }

    /// <summary>
    /// Update permission
    /// </summary>
    [HttpPut("permissions/{permissionCode}")]
    public async Task<ActionResult<PermissionDto>> UpdatePermission(string permissionCode, [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            var permission = await _userService.UpdatePermissionAsync(permissionCode, request);

            if (permission == null)
            {
                return NotFound(new { error = $"Permission '{permissionCode}' not found" });
            }

            _logger.LogInformation("Updated permission {PermissionCode} by admin {AdminUser}",
                permissionCode, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(permission);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionCode}", permissionCode);
            return StatusCode(500, new { error = "Failed to update permission" });
        }
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    [HttpDelete("permissions/{permissionCode}")]
    public async Task<IActionResult> DeletePermission(string permissionCode)
    {
        try
        {
            var success = await _userService.DeletePermissionAsync(permissionCode);

            if (!success)
            {
                return NotFound(new { error = $"Permission '{permissionCode}' not found" });
            }

            _logger.LogWarning("Deleted permission {PermissionCode} by admin {AdminUser}",
                permissionCode, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Permission deleted successfully", permissionCode, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionCode}", permissionCode);
            return StatusCode(500, new { error = "Failed to delete permission" });
        }
    }

    /// <summary>
    /// Assign permission to role
    /// </summary>
    [HttpPost("roles/{roleId}/permissions")]
    public async Task<IActionResult> AssignPermissionToRole(string roleId, [FromBody] AssignPermissionRequest request)
    {
        try
        {
            await _userService.AssignPermissionToRoleAsync(roleId, request.PermissionCode, request.IsWildcard, request.PermissionPattern);

            _logger.LogInformation("Assigned permission {PermissionCode} to role {RoleId} by admin {AdminUser}",
                request.PermissionCode, roleId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Permission assigned to role successfully", roleId, request.PermissionCode, timestamp = DateTime.UtcNow });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to role");
            return StatusCode(500, new { error = "Failed to assign permission to role" });
        }
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    [HttpDelete("roles/{roleId}/permissions/{permissionCode}")]
    public async Task<IActionResult> RemovePermissionFromRole(string roleId, string permissionCode)
    {
        try
        {
            var success = await _userService.RemovePermissionFromRoleAsync(roleId, permissionCode);

            if (!success)
            {
                return NotFound(new { error = "Permission assignment not found" });
            }

            _logger.LogInformation("Removed permission {PermissionCode} from role {RoleId} by admin {AdminUser}",
                permissionCode, roleId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Permission removed from role successfully", roleId, permissionCode, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role");
            return StatusCode(500, new { error = "Failed to remove permission from role" });
        }
    }

    /// <summary>
    /// Assign direct permission to user
    /// </summary>
    [HttpPost("users/{userId}/permissions")]
    public async Task<IActionResult> AssignPermissionToUser(string userId, [FromBody] AssignUserPermissionRequest request)
    {
        try
        {
            await _userService.AssignDirectPermissionToUserAsync(userId, request.PermissionCode,
                request.Type, request.IsWildcard, request.PermissionPattern);

            _logger.LogInformation("Assigned {PermissionType} permission {PermissionCode} to user {UserId} by admin {AdminUser}",
                request.Type, request.PermissionCode, userId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Permission assigned to user successfully", userId, request.PermissionCode, request.Type, timestamp = DateTime.UtcNow });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to user");
            return StatusCode(500, new { error = "Failed to assign permission to user" });
        }
    }

    /// <summary>
    /// Remove direct permission from user
    /// </summary>
    [HttpDelete("users/{userId}/permissions/{permissionCode}")]
    public async Task<IActionResult> RemovePermissionFromUser(string userId, string permissionCode)
    {
        try
        {
            var success = await _userService.RemoveDirectPermissionFromUserAsync(userId, permissionCode);

            if (!success)
            {
                return NotFound(new { error = "Permission assignment not found" });
            }

            _logger.LogInformation("Removed permission {PermissionCode} from user {UserId} by admin {AdminUser}",
                permissionCode, userId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Permission removed from user successfully", userId, permissionCode, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from user");
            return StatusCode(500, new { error = "Failed to remove permission from user" });
        }
    }

    /// <summary>
    /// Get permission categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetPermissionCategories()
    {
        try
        {
            var categories = await _userService.GetPermissionCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission categories");
            return StatusCode(500, new { error = "Failed to get permission categories" });
        }
    }

    /// <summary>
    /// Get permission services
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<List<string>>> GetPermissionServices()
    {
        try
        {
            var services = await _userService.GetPermissionServicesAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission services");
            return StatusCode(500, new { error = "Failed to get permission services" });
        }
    }
}


/// <summary>
/// Assign permission request model
/// </summary>
public class AssignPermissionRequest
{
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsWildcard { get; set; }
    public string? PermissionPattern { get; set; }
}

/// <summary>
/// Assign user permission request model
/// </summary>
public class AssignUserPermissionRequest
{
    public string PermissionCode { get; set; } = string.Empty;
    public string Type { get; set; } = "Grant"; // Grant or Deny
    public bool IsWildcard { get; set; }
    public string? PermissionPattern { get; set; }
}