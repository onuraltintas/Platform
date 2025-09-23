using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Role hierarchy management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class RoleHierarchyController : ControllerBase
{
    private readonly IRoleHierarchyService _roleHierarchyService;
    private readonly ILogger<RoleHierarchyController> _logger;

    public RoleHierarchyController(
        IRoleHierarchyService roleHierarchyService,
        ILogger<RoleHierarchyController> logger)
    {
        _roleHierarchyService = roleHierarchyService;
        _logger = logger;
    }

    /// <summary>
    /// Get role hierarchy tree
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<List<RoleHierarchyNode>>> GetRoleHierarchy([FromQuery] Guid? groupId = null)
    {
        try
        {
            var hierarchy = await _roleHierarchyService.GetRoleHierarchyAsync(groupId);
            return Ok(hierarchy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role hierarchy for group {GroupId}", groupId);
            return StatusCode(500, new { error = "Failed to get role hierarchy" });
        }
    }

    /// <summary>
    /// Get effective permissions for a role (including inherited)
    /// </summary>
    [HttpGet("roles/{roleId}/effective-permissions")]
    public async Task<ActionResult<HashSet<string>>> GetEffectivePermissions(string roleId)
    {
        try
        {
            var permissions = await _roleHierarchyService.GetEffectivePermissionsAsync(roleId);
            return Ok(new { roleId, permissions, count = permissions.Count, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get effective permissions" });
        }
    }

    /// <summary>
    /// Get child roles of a role
    /// </summary>
    [HttpGet("roles/{roleId}/children")]
    public async Task<ActionResult<List<RoleHierarchyResponse>>> GetChildRoles(
        string roleId,
        [FromQuery] bool recursive = true)
    {
        try
        {
            var childRoles = await _roleHierarchyService.GetChildRolesAsync(roleId, recursive);
            var response = childRoles.Select(r => new RoleHierarchyResponse
            {
                RoleId = r.Id,
                RoleName = r.Name!,
                Description = r.Description,
                Level = r.HierarchyLevel,
                Priority = r.Priority,
                IsActive = r.IsActive,
                InheritPermissions = r.InheritPermissions,
                HierarchyPath = r.HierarchyPath
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child roles for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get child roles" });
        }
    }

    /// <summary>
    /// Get parent roles of a role
    /// </summary>
    [HttpGet("roles/{roleId}/parents")]
    public async Task<ActionResult<List<RoleHierarchyResponse>>> GetParentRoles(string roleId)
    {
        try
        {
            var parentRoles = await _roleHierarchyService.GetParentRolesAsync(roleId);
            var response = parentRoles.Select(r => new RoleHierarchyResponse
            {
                RoleId = r.Id,
                RoleName = r.Name!,
                Description = r.Description,
                Level = r.HierarchyLevel,
                Priority = r.Priority,
                IsActive = r.IsActive,
                InheritPermissions = r.InheritPermissions,
                HierarchyPath = r.HierarchyPath
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent roles for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get parent roles" });
        }
    }

    /// <summary>
    /// Set parent role for a role
    /// </summary>
    [HttpPut("roles/{roleId}/parent")]
    public async Task<IActionResult> SetParentRole(string roleId, [FromBody] SetParentRoleRequest request)
    {
        try
        {
            var success = await _roleHierarchyService.SetParentRoleAsync(roleId, request.ParentRoleId);

            if (!success)
            {
                return BadRequest(new { error = "Failed to set parent role. Check for circular dependencies." });
            }

            _logger.LogInformation("Set parent role {ParentRoleId} for role {RoleId} by user {UserId}",
                request.ParentRoleId, roleId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(new { message = "Parent role set successfully", roleId, parentRoleId = request.ParentRoleId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting parent role for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to set parent role" });
        }
    }

    /// <summary>
    /// Move role to a different parent
    /// </summary>
    [HttpPost("roles/{roleId}/move")]
    public async Task<IActionResult> MoveRole(string roleId, [FromBody] MoveRoleRequest request)
    {
        try
        {
            var success = await _roleHierarchyService.MoveRoleAsync(roleId, request.NewParentRoleId);

            if (!success)
            {
                return BadRequest(new { error = "Failed to move role. Check for circular dependencies." });
            }

            _logger.LogInformation("Moved role {RoleId} to parent {NewParentRoleId} by user {UserId}",
                roleId, request.NewParentRoleId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(new { message = "Role moved successfully", roleId, newParentRoleId = request.NewParentRoleId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to move role" });
        }
    }

    /// <summary>
    /// Validate hierarchy setup
    /// </summary>
    [HttpPost("roles/{roleId}/validate-hierarchy")]
    public async Task<ActionResult<bool>> ValidateHierarchy(string roleId, [FromBody] ValidateHierarchyRequest request)
    {
        try
        {
            var isValid = await _roleHierarchyService.ValidateHierarchyAsync(roleId, request.ParentRoleId);
            return Ok(new { isValid, roleId, parentRoleId = request.ParentRoleId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hierarchy for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to validate hierarchy" });
        }
    }

    /// <summary>
    /// Rebuild hierarchy paths and levels
    /// </summary>
    [HttpPost("rebuild")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RebuildHierarchy([FromQuery] Guid? groupId = null)
    {
        try
        {
            await _roleHierarchyService.RebuildHierarchyAsync(groupId);

            _logger.LogInformation("Rebuilt role hierarchy for group {GroupId} by user {UserId}",
                groupId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(new { message = "Hierarchy rebuilt successfully", groupId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding hierarchy for group {GroupId}", groupId);
            return StatusCode(500, new { error = "Failed to rebuild hierarchy" });
        }
    }

    /// <summary>
    /// Get roles that current user can assign
    /// </summary>
    [HttpGet("assignable-roles")]
    public async Task<ActionResult<List<RoleHierarchyResponse>>> GetAssignableRoles()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not found" });
            }

            var assignableRoles = await _roleHierarchyService.GetAssignableRolesAsync(userId);
            var response = assignableRoles.Select(r => new RoleHierarchyResponse
            {
                RoleId = r.Id,
                RoleName = r.Name!,
                Description = r.Description,
                Level = r.HierarchyLevel,
                Priority = r.Priority,
                IsActive = r.IsActive,
                InheritPermissions = r.InheritPermissions,
                HierarchyPath = r.HierarchyPath
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignable roles for user");
            return StatusCode(500, new { error = "Failed to get assignable roles" });
        }
    }

    /// <summary>
    /// Check if a role can manage another role
    /// </summary>
    [HttpGet("roles/{managerRoleId}/can-manage/{targetRoleId}")]
    public async Task<ActionResult<bool>> CanManageRole(string managerRoleId, string targetRoleId)
    {
        try
        {
            var canManage = await _roleHierarchyService.CanManageRoleAsync(managerRoleId, targetRoleId);
            return Ok(new { canManage, managerRoleId, targetRoleId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role {ManagerRoleId} can manage role {TargetRoleId}",
                managerRoleId, targetRoleId);
            return StatusCode(500, new { error = "Failed to check role management permission" });
        }
    }

    /// <summary>
    /// Get role inheritance chain
    /// </summary>
    [HttpGet("roles/{roleId}/inheritance-chain")]
    public async Task<ActionResult<List<RoleInheritanceInfo>>> GetInheritanceChain(string roleId)
    {
        try
        {
            var inheritanceChain = await _roleHierarchyService.GetInheritanceChainAsync(roleId);
            return Ok(inheritanceChain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inheritance chain for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get inheritance chain" });
        }
    }

    /// <summary>
    /// Get permission conflicts in role hierarchy
    /// </summary>
    [HttpGet("roles/{roleId}/permission-conflicts")]
    public async Task<ActionResult<List<PermissionConflict>>> GetPermissionConflicts(string roleId)
    {
        try
        {
            var conflicts = await _roleHierarchyService.GetPermissionConflictsAsync(roleId);
            return Ok(conflicts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission conflicts for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get permission conflicts" });
        }
    }

    /// <summary>
    /// Get effective priority for a role
    /// </summary>
    [HttpGet("roles/{roleId}/effective-priority")]
    public async Task<ActionResult<int>> GetEffectivePriority(string roleId)
    {
        try
        {
            var priority = await _roleHierarchyService.GetEffectivePriorityAsync(roleId);
            return Ok(new { roleId, effectivePriority = priority });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective priority for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get effective priority" });
        }
    }
}

/// <summary>
/// Role hierarchy response model
/// </summary>
public class RoleHierarchyResponse
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool InheritPermissions { get; set; }
    public string HierarchyPath { get; set; } = string.Empty;
}

/// <summary>
/// Set parent role request model
/// </summary>
public class SetParentRoleRequest
{
    public string? ParentRoleId { get; set; }
}

/// <summary>
/// Move role request model
/// </summary>
public class MoveRoleRequest
{
    public string? NewParentRoleId { get; set; }
}

/// <summary>
/// Validate hierarchy request model
/// </summary>
public class ValidateHierarchyRequest
{
    public string ParentRoleId { get; set; } = string.Empty;
}