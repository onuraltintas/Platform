using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Constants;
using Identity.Infrastructure.Data;
using Identity.Application.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IdentityDbContext _db;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        IdentityDbContext db,
        ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.Identity.Roles.Read)]
    public async Task<IActionResult> GetRoles([FromQuery] GetRolesRequest request)
    {
        try
        {
            var query = _roleManager.Roles.AsQueryable();

            // Apply group scope: include roles for current group or system roles (GroupId is null)
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var groupId))
            {
                query = query.Where(r => r.GroupId == null || r.GroupId == groupId);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(r => r.Name!.Contains(request.Search) ||
                                        r.Description.Contains(request.Search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var roles = await query
                .OrderBy(r => r.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name!,
                    Description = r.Description,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            return Ok(new { data = roles, totalCount, currentPage = request.Page, pageSize = request.PageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{roleId}")]
    [RequirePermission(PermissionConstants.Identity.Roles.Read)]
    public async Task<IActionResult> GetRole(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");
            var dto = new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description, IsActive = role.IsActive };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    public class AdminCreateRoleBody
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? PermissionIds { get; set; }
    }

    public class AdminUpdateRoleBody
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    [HttpPost]
    [RequirePermission(PermissionConstants.Identity.Roles.Write)]
    public async Task<IActionResult> CreateRole([FromBody] AdminCreateRoleBody body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body.Name)) return BadRequest(new { error = "Name is required" });

            var existsByName = await _roleManager.Roles.AnyAsync(r => r.Name == body.Name);
            if (existsByName) return BadRequest(new { error = "Role with same name already exists" });

            var role = new ApplicationRole
            {
                Name = body.Name,
                NormalizedName = body.Name.ToUpperInvariant(),
                Description = body.Description ?? string.Empty,
                IsActive = body.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            // Set group scope on role if available
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var groupId))
            {
                role.GroupId = groupId;
            }

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            if (body.PermissionIds != null && body.PermissionIds.Any())
            {
                var permissionGuids = body.PermissionIds.Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
                var existingPermissions = await _db.Permissions.Where(p => permissionGuids.Contains(p.Id)).Select(p => p.Id).ToListAsync();
                foreach (var pid in existingPermissions)
                {
                    _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid, GrantedAt = DateTime.UtcNow, GrantedBy = User?.Identity?.Name, GroupId = role.GroupId });
                }
                await _db.SaveChangesAsync();
            }

            var created = new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description, IsActive = role.IsActive };
            return CreatedAtAction(nameof(GetRole), new { roleId = role.Id }, new { data = created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{roleId}")]
    [RequirePermission(PermissionConstants.Identity.Roles.Write)]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] AdminUpdateRoleBody body)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            role.Name = body.Name;
            role.NormalizedName = body.Name.ToUpperInvariant();
            role.Description = body.Description ?? string.Empty;
            role.IsActive = body.IsActive;
            role.LastModifiedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            var updated = new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description, IsActive = role.IsActive };
            return Ok(new { data = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{roleId}")]
    [RequirePermission(PermissionConstants.Identity.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    // Role permissions
    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            // Only return permissions in current group scope or system scope
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            IQueryable<RolePermission> rpQuery = _db.RolePermissions.Where(rp => rp.RoleId == roleId);
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var groupId))
            {
                rpQuery = rpQuery.Where(rp => rp.GroupId == null || rp.GroupId == groupId);
            }
            var permissionIds = await rpQuery.Select(rp => rp.PermissionId.ToString()).ToListAsync();
            return Ok(new { data = permissionIds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions for {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{roleId}/permissions/{permissionId}")]
    public async Task<IActionResult> AssignPermissionToRole(string roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");
            var exists = await _db.Permissions.AnyAsync(p => p.Id == permissionId);
            if (!exists) return NotFound("Permission not found");
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            Guid? groupId = null;
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var g)) groupId = g;

            var already = await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && rp.GroupId == groupId);
            if (already) return Ok();

            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId, GrantedAt = DateTime.UtcNow, GrantedBy = User?.Identity?.Name, GroupId = groupId });
            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{roleId}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermissionFromRole(string roleId, Guid permissionId)
    {
        try
        {
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            Guid? groupId = null;
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var g)) groupId = g;

            var link = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && rp.GroupId == groupId);
            if (link == null) return NotFound();
            _db.RolePermissions.Remove(link);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<IActionResult> ReplaceRolePermissions(string roleId, [FromBody] List<string> permissionIds)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            var newIds = (permissionIds ?? new()).Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToHashSet();
            var groupIdClaim = User.FindFirst("GroupId")?.Value;
            Guid? groupId = null;
            if (!string.IsNullOrEmpty(groupIdClaim) && Guid.TryParse(groupIdClaim, out var g)) groupId = g;

            var currentLinks = await _db.RolePermissions.Where(rp => rp.RoleId == roleId && rp.GroupId == groupId).ToListAsync();
            var currentIds = currentLinks.Select(l => l.PermissionId).ToHashSet();

            var toRemove = currentLinks.Where(l => !newIds.Contains(l.PermissionId)).ToList();
            if (toRemove.Count > 0) _db.RolePermissions.RemoveRange(toRemove);

            var toAdd = newIds.Where(id => !currentIds.Contains(id)).ToList();
            if (toAdd.Count > 0)
            {
                foreach (var pid in toAdd)
                {
                    _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid, GrantedAt = DateTime.UtcNow, GrantedBy = User?.Identity?.Name, GroupId = groupId });
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing permissions for role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets role usage statistics
    /// </summary>
    [HttpGet("stats")]
    [RequirePermission(PermissionConstants.Identity.Roles.Read)]
    public async Task<IActionResult> GetRoleUsageStats()
    {
        try
        {
            var totalRoles = await _roleManager.Roles.CountAsync();
            var activeRoles = await _roleManager.Roles.Where(r => r.IsActive).CountAsync();
            var systemRoles = await _roleManager.Roles.Where(r => r.IsSystemRole).CountAsync();
            var customRoles = totalRoles - systemRoles;

            var rolesWithUsers = await _db.UserRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, UserCount = g.Count() })
                .Join(_roleManager.Roles, rp => rp.RoleId, r => r.Id, (rp, r) => new { r.Name, rp.UserCount })
                .OrderByDescending(x => x.UserCount)
                .Take(10)
                .ToListAsync();

            var rolesWithUsersCount = await _db.UserRoles
                .Select(ur => ur.RoleId)
                .Distinct()
                .CountAsync();

            var stats = new
            {
                TotalRoles = totalRoles,
                ActiveRoles = activeRoles,
                SystemRoles = systemRoles,
                CustomRoles = customRoles,
                RolesWithUsers = rolesWithUsersCount,
                MostUsedRoles = rolesWithUsers
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role usage statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Clones a role with its permissions
    /// </summary>
    [HttpPost("{roleId}/clone")]
    [RequirePermission(PermissionConstants.Identity.Roles.Write)]
    public async Task<IActionResult> CloneRole(string roleId, [FromBody] CloneRoleRequest request)
    {
        try
        {
            var sourceRole = await _roleManager.FindByIdAsync(roleId);
            if (sourceRole == null) return NotFound("Source role not found");

            if (await _roleManager.RoleExistsAsync(request.Name))
                return BadRequest("Role with this name already exists");

            var newRole = new ApplicationRole
            {
                Name = request.Name,
                Description = request.Description,
                IsSystemRole = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User?.Identity?.Name,
                GroupId = sourceRole.GroupId
            };

            var result = await _roleManager.CreateAsync(newRole);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Copy permissions (respect group scope)
            var sourcePermissions = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            if (sourcePermissions.Any())
            {
                var newPermissions = sourcePermissions.Select(sp => new RolePermission
                {
                    RoleId = newRole.Id,
                    PermissionId = sp.PermissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = User?.Identity?.Name,
                    GroupId = sp.GroupId
                });

                _db.RolePermissions.AddRange(newPermissions);
                await _db.SaveChangesAsync();
            }

            return Ok(new { Id = newRole.Id, Name = newRole.Name, Description = newRole.Description });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Compares two roles and their permissions
    /// </summary>
    [HttpGet("compare/{roleId1}/{roleId2}")]
    [RequirePermission(PermissionConstants.Identity.Roles.Read)]
    public async Task<IActionResult> CompareRoles(string roleId1, string roleId2)
    {
        try
        {
            var role1 = await _roleManager.FindByIdAsync(roleId1);
            var role2 = await _roleManager.FindByIdAsync(roleId2);

            if (role1 == null || role2 == null)
                return NotFound("One or both roles not found");

            var role1Permissions = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId1)
                .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
                .ToListAsync();

            var role2Permissions = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId2)
                .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
                .ToListAsync();

            var role1PermissionIds = role1Permissions.Select(p => p.Id).ToHashSet();
            var role2PermissionIds = role2Permissions.Select(p => p.Id).ToHashSet();

            var commonPermissions = role1Permissions.Where(p => role2PermissionIds.Contains(p.Id)).ToList();
            var role1OnlyPermissions = role1Permissions.Where(p => !role2PermissionIds.Contains(p.Id)).ToList();
            var role2OnlyPermissions = role2Permissions.Where(p => !role1PermissionIds.Contains(p.Id)).ToList();

            var comparison = new
            {
                Role1 = new { role1.Id, role1.Name, role1.Description, PermissionCount = role1Permissions.Count },
                Role2 = new { role2.Id, role2.Name, role2.Description, PermissionCount = role2Permissions.Count },
                CommonPermissions = commonPermissions.Select(p => new { p.Id, p.Name, p.Resource, p.Action }),
                Role1OnlyPermissions = role1OnlyPermissions.Select(p => new { p.Id, p.Name, p.Resource, p.Action }),
                Role2OnlyPermissions = role2OnlyPermissions.Select(p => new { p.Id, p.Name, p.Resource, p.Action }),
                Summary = new
                {
                    CommonCount = commonPermissions.Count,
                    Role1UniqueCount = role1OnlyPermissions.Count,
                    Role2UniqueCount = role2OnlyPermissions.Count,
                    SimilarityPercentage = role1Permissions.Count + role2Permissions.Count > 0
                        ? Math.Round((commonPermissions.Count * 2.0) / (role1Permissions.Count + role2Permissions.Count) * 100, 2)
                        : 0
                }
            };

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing roles {RoleId1} and {RoleId2}", roleId1, roleId2);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CloneRoleRequest
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(256, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 256 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$", ErrorMessage = "Role name can only contain letters, numbers, spaces, hyphens, and underscores")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;
}

