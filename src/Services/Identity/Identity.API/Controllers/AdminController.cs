using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Identity.Infrastructure.Data;
using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Identity.API.Controllers;

// Admin-specific API models for simple permission CRUD (matching AdminPanel payloads)
public class AdminCreatePermissionBody
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}


public class AdminUpdatePermissionBody
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

// Admin-specific models for Role CRUD (matching AdminPanel payloads)
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

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IGroupService _groupService;
    private readonly IdentityDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IGroupService groupService,
        ILogger<AdminController> logger,
        IdentityDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _groupService = groupService;
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// List users with pagination and filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(u => u.Email.Contains(request.Search) ||
                                        u.UserName.Contains(request.Search) ||
                                        u.FirstName.Contains(request.Search) ||
                                        u.LastName.Contains(request.Search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (request.IsEmailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == request.IsEmailConfirmed.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.Email)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var result = new PagedUsersResponse
            {
                Users = users.Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? u.Email,
                    Email = u.Email,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    PhoneNumber = u.PhoneNumber,
                    IsEmailConfirmed = u.EmailConfirmed,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Roles = new List<string>(), // Will be populated separately
                    Groups = new List<UserGroupInfo>(),
                    Permissions = new List<string>()
                }).ToArray(),
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize
            };

            // Get roles for each user
            foreach (var user in result.Users)
            {
                var appUser = users.First(u => u.Id == user.Id);
                var roles = await _userManager.GetRolesAsync(appUser);
                user.Roles = roles.ToList();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var result = new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Roles = roles.ToList(),
                Groups = new List<UserGroupInfo>(),
                Permissions = new List<string>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "User with this email already exists" });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = request.EmailConfirmed,
                IsActive = request.IsActive
            };

            // Create user with password
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = "Password is required" });
            }

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Add roles if specified
            if (request.RoleIds != null && request.RoleIds.Any())
            {
                foreach (var roleId in request.RoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name!);
                    }
                }
            }

            // Get roles for response
            var roles = await _userManager.GetRolesAsync(user);

            var response = new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                Groups = new List<UserGroupInfo>(),
                Permissions = new List<string>()
            };

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("users/{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if email is being changed and already exists
            if (user.Email != request.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    return BadRequest(new { error = "Email already in use by another user" });
                }
            }

            // Update user properties
            user.UserName = request.UserName;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.EmailConfirmed = request.EmailConfirmed;
            user.IsActive = request.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Update user roles if provided
            if (request.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var desiredRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Remove roles that are no longer desired
                var rolesToRemove = currentRoles.Where(r => !desiredRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (rolesToRemove.Length > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
                    }
                }

                // Add missing roles
                var rolesToAdd = desiredRoles.Where(r => !currentRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (rolesToAdd.Length > 0)
                {
                    var addResult = await _roleManager.Roles
                        .Where(rr => rolesToAdd.Contains(rr.Name!))
                        .Select(rr => rr.Name!)
                        .ToListAsync();

                    // Ensure all desired roles actually exist
                    var nonExisting = rolesToAdd.Where(r => !addResult.Contains(r)).ToArray();
                    if (nonExisting.Length > 0)
                    {
                        return BadRequest(new { error = $"Roles not found: {string.Join(", ", nonExisting)}" });
                    }

                    var addToRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!addToRolesResult.Succeeded)
                    {
                        return BadRequest(new { errors = addToRolesResult.Errors.Select(e => e.Description) });
                    }
                }
            }

            // Get roles for response
            var roles = await _userManager.GetRolesAsync(user);

            var response = new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Roles = roles.ToList(),
                Groups = new List<UserGroupInfo>(),
                Permissions = new List<string>()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Optionally remove all roles first to avoid FK issues in custom schemas
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles != null && currentRoles.Count > 0)
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    return BadRequest(new { errors = removeRolesResult.Errors.Select(e => e.Description) });
                }
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return BadRequest(new { errors = deleteResult.Errors.Select(e => e.Description) });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Activate user
    /// </summary>
    [HttpPatch("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new { message = "User activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deactivate user
    /// </summary>
    [HttpPatch("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// List all roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles([FromQuery] GetRolesRequest request)
    {
        try
        {
            var query = _roleManager.Roles.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(r => r.Name.Contains(request.Search) ||
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

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("roles/{roleId}")]
    public async Task<IActionResult> GetRole(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            var dto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create role
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] AdminCreateRoleBody body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            var existsByName = await _roleManager.Roles.AnyAsync(r => r.Name == body.Name);
            if (existsByName)
            {
                return BadRequest(new { error = "Role with same name already exists" });
            }

            var role = new ApplicationRole
            {
                Name = body.Name,
                NormalizedName = body.Name.ToUpperInvariant(),
                Description = body.Description ?? string.Empty,
                IsActive = body.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Assign permissions if provided
            if (body.PermissionIds != null && body.PermissionIds.Any())
            {
                var permissionGuids = body.PermissionIds
                    .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();

                var existingPermissions = await _db.Permissions
                    .Where(p => permissionGuids.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var pid in existingPermissions)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = pid,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = User?.Identity?.Name
                    });
                }
                await _db.SaveChangesAsync();
            }

            var created = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive
            };
            return CreatedAtAction(nameof(GetRole), new { roleId = role.Id }, new { data = created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("roles/{roleId}")]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] AdminUpdateRoleBody body)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            role.Name = body.Name;
            role.NormalizedName = body.Name.ToUpperInvariant();
            role.Description = body.Description ?? string.Empty;
            role.IsActive = body.IsActive;
            role.LastModifiedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            var updated = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive
            };
            return Ok(new { data = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("roles/{roleId}")]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }


    /// <summary>
    /// List permissions (placeholder - can be extended)
    /// </summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] GetPermissionsRequest request)
    {
        try
        {
            var query = _db.Permissions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(p => p.Name.Contains(search) ||
                                         (p.DisplayName != null && p.DisplayName.Contains(search)) ||
                                         (p.Description != null && p.Description.Contains(search)) ||
                                         p.Resource.Contains(search) ||
                                         p.Action.Contains(search) ||
                                         p.Action.Contains(search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    ServiceId = p.ServiceId,
                    ServiceName = _db.Services.Where(s => s.Id == p.ServiceId).Select(s => s.Name).FirstOrDefault() ?? string.Empty,
                    Type = p.Type,
                    Priority = p.Priority,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    LastModifiedAt = p.LastModifiedAt,
                    RoleCount = _db.RolePermissions.Count(rp => rp.PermissionId == p.Id),
                    UserCount = 0 // optional: compute via joins if needed
                })
                .ToListAsync();

            return Ok(new { data = items, totalCount, currentPage = request.Page, pageSize = request.PageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("permissions/{permissionId}")]
    public async Task<IActionResult> GetPermission(Guid permissionId)
    {
        try
        {
            var p = await _db.Permissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == permissionId);
            if (p == null)
            {
                return NotFound("Permission not found");
            }

            var dto = new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Resource = p.Resource,
                Action = p.Action,
                ServiceId = p.ServiceId,
                ServiceName = await _db.Services.Where(s => s.Id == p.ServiceId).Select(s => s.Name).FirstOrDefaultAsync() ?? string.Empty,
                Type = p.Type,
                Priority = p.Priority,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                LastModifiedAt = p.LastModifiedAt,
                RoleCount = await _db.RolePermissions.CountAsync(rp => rp.PermissionId == p.Id),
                UserCount = 0
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create permission
    /// </summary>
    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] AdminCreatePermissionBody request)
    {
        try
        {
            // AdminPanel basit model gönderiyor: name, description, group, isActive
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            // Varsayılan bir Service seçelim (ilk aktif Service)
            var serviceId = await _db.Services.OrderBy(s => s.Name).Select(s => s.Id).FirstOrDefaultAsync();
            if (serviceId == Guid.Empty)
            {
                // Service yoksa bir placeholder service oluştur
                var svc = new Service { Id = Guid.NewGuid(), Name = "Core", DisplayName = "Core", Endpoint = "/", Type = ServiceType.Internal, RegisteredAt = DateTime.UtcNow, Status = ServiceStatus.Healthy, IsActive = true };
                _db.Services.Add(svc);
                await _db.SaveChangesAsync();
                serviceId = svc.Id;
            }

            var exists = await _db.Permissions.AnyAsync(p => p.ServiceId == serviceId && p.Name == request.Name);
            if (exists)
            {
                return BadRequest(new { error = "Permission with same name already exists for the service" });
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.Name,
                Description = request.Description,
                Resource = "generic",
                Action = "custom",
                ServiceId = serviceId,
                Type = PermissionType.Custom,
                Priority = 0,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermission), new { permissionId = permission.Id }, new { id = permission.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get permissions of a role
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            var permissionIds = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId.ToString())
                .ToListAsync();

            return Ok(new { data = permissionIds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions for {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Assign a single permission to role
    /// </summary>
    [HttpPost("roles/{roleId}/permissions/{permissionId}")]
    public async Task<IActionResult> AssignPermissionToRole(string roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            var exists = await _db.Permissions.AnyAsync(p => p.Id == permissionId);
            if (!exists) return NotFound("Permission not found");

            var already = await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (already) return Ok();

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = User?.Identity?.Name
            });
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove a single permission from role
    /// </summary>
    [HttpDelete("roles/{roleId}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermissionFromRole(string roleId, Guid permissionId)
    {
        try
        {
            var link = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
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

    /// <summary>
    /// Replace role permissions with provided set
    /// </summary>
    [HttpPut("roles/{roleId}/permissions")]
    public async Task<IActionResult> ReplaceRolePermissions(string roleId, [FromBody] List<string> permissionIds)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound("Role not found");

            var newIds = (permissionIds ?? new()).Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToHashSet();

            var currentLinks = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            var currentIds = currentLinks.Select(l => l.PermissionId).ToHashSet();

            // Remove
            var toRemove = currentLinks.Where(l => !newIds.Contains(l.PermissionId)).ToList();
            if (toRemove.Count > 0)
            {
                _db.RolePermissions.RemoveRange(toRemove);
            }

            // Add
            var toAdd = newIds.Where(id => !currentIds.Contains(id)).ToList();
            if (toAdd.Count > 0)
            {
                foreach (var pid in toAdd)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = pid,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = User?.Identity?.Name
                    });
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
    /// Update permission
    /// </summary>
    [HttpPut("permissions/{permissionId}")]
    public async Task<IActionResult> UpdatePermission(Guid permissionId, [FromBody] AdminUpdatePermissionBody request)
    {
        try
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId);
            if (permission == null)
            {
                return NotFound("Permission not found");
            }

            permission.DisplayName = string.IsNullOrWhiteSpace(request.Name) ? permission.DisplayName : request.Name;
            permission.Description = request.Description ?? permission.Description;
            permission.IsActive = request.IsActive;
            permission.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { id = permission.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    [HttpDelete("permissions/{permissionId}")]
    public async Task<IActionResult> DeletePermission(Guid permissionId)
    {
        try
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId);
            if (permission == null)
            {
                return NotFound("Permission not found");
            }

            // Remove role associations
            var rolePerms = _db.RolePermissions.Where(rp => rp.PermissionId == permissionId);
            _db.RolePermissions.RemoveRange(rolePerms);

            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }
}