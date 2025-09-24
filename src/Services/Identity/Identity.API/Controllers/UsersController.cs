using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Core.Constants;
using Identity.Infrastructure.Data;
using Identity.Application.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[RequirePermission(PermissionConstants.Identity.Users.Read)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IGroupService _groupService;
    private readonly IdentityDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IGroupService groupService,
        IdentityDbContext db,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _groupService = groupService;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search;
                query = query.Where(u => (u.Email ?? string.Empty).Contains(s) ||
                                        (u.UserName ?? string.Empty).Contains(s) ||
                                        (u.FirstName ?? string.Empty).Contains(s) ||
                                        (u.LastName ?? string.Empty).Contains(s));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (request.IsEmailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == request.IsEmailConfirmed.Value);
            }

            // Group filtering
            if (!string.IsNullOrEmpty(request.GroupId) && Guid.TryParse(request.GroupId, out var groupId))
            {
                var groupUserIds = await _db.UserGroups
                    .Where(ug => ug.GroupId == groupId && ug.IsActive)
                    .Select(ug => ug.UserId)
                    .ToListAsync();

                query = query.Where(u => groupUserIds.Contains(u.Id));
            }

            var totalCount = await query.CountAsync();

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
                    UserName = u.UserName ?? u.Email ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    FullName = $"{u.FirstName ?? string.Empty} {u.LastName ?? string.Empty}".Trim(),
                    PhoneNumber = u.PhoneNumber,
                    IsEmailConfirmed = u.EmailConfirmed,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Roles = new List<string>(),
                    Groups = new List<UserGroupInfo>(),
                    Permissions = new List<string>()
                }).ToArray(),
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize
            };

            // Populate roles for each user (batch)
            var userIds = users.Select(u => u.Id).ToList();
            var roleLinks = await _db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();
            var roleIds = roleLinks.Select(ur => ur.RoleId).Distinct().ToList();
            var roleNames = await _db.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();
            var roleNameById = roleNames.ToDictionary(r => r.Id, r => r.Name ?? string.Empty);
            var userIdToRoleNames = roleLinks
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ur => roleNameById.TryGetValue(ur.RoleId, out var n) ? n : string.Empty)
                                               .Where(n => !string.IsNullOrEmpty(n))
                                               .ToList());

            // Populate groups for each user (batch)
            var userGroups = await _db.UserGroups
                .Where(ug => userIds.Contains(ug.UserId) && ug.IsActive)
                .Include(ug => ug.Group)
                .Where(ug => ug.Group != null && !ug.Group.IsDeleted && ug.Group.IsActive)
                .Select(ug => new
                {
                    ug.UserId,
                    GroupId = ug.Group.Id,
                    GroupName = ug.Group.Name,
                    GroupType = ug.Group.Type.ToString(),
                    ug.Role,
                    ug.JoinedAt,
                    IsDefault = ug.UserId == ug.Group.CreatedBy // Simple default check
                })
                .ToListAsync();

            var userIdToGroups = userGroups
                .GroupBy(ug => ug.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ug => new UserGroupInfo
                {
                    GroupId = ug.GroupId,
                    GroupName = ug.GroupName,
                    GroupType = ug.GroupType,
                    UserRole = ug.Role.ToString(),
                    JoinedAt = ug.JoinedAt,
                    IsDefault = ug.IsDefault
                }).ToList());

            foreach (var userSummary in result.Users)
            {
                // Populate roles
                if (userIdToRoleNames.TryGetValue(userSummary.Id, out var names))
                {
                    userSummary.Roles = names;
                }

                // Populate groups
                if (userIdToGroups.TryGetValue(userSummary.Id, out var groups))
                {
                    userSummary.Groups = groups;
                    userSummary.DefaultGroup = groups.FirstOrDefault(g => g.IsDefault);
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{userId}")]
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

            // Get user's groups
            var userGroupsResult = await _groupService.GetUserGroupsAsync(userId);
            var userGroups = new List<UserGroupInfo>();

            if (userGroupsResult.IsSuccess)
            {
                foreach (var group in userGroupsResult.Value ?? new List<GroupDto>())
                {
                    var roleResult = await _groupService.GetUserRoleInGroupAsync(userId, group.Id);
                    userGroups.Add(new UserGroupInfo
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        GroupType = group.Type.ToString(),
                        UserRole = roleResult.IsSuccess && roleResult.Value.HasValue ? roleResult.Value.ToString()! : "Member",
                        JoinedAt = DateTime.UtcNow, // You may want to get this from UserGroup relation
                        IsDefault = user.DefaultGroupId == group.Id
                    });
                }
            }

            var result = new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Email ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim(),
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Roles = roles.ToList(),
                Groups = userGroups,
                DefaultGroup = userGroups.FirstOrDefault(g => g.IsDefault),
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

    [HttpPost]
    [RequirePermission(PermissionConstants.Identity.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "User with this email already exists" });
            }

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

            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = "Password is required" });
            }

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

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

    [HttpPut("{userId}")]
    [RequirePermission(PermissionConstants.Identity.Users.Update)]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (user.Email != request.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    return BadRequest(new { error = "Email already in use by another user" });
                }
            }

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

            if (request.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var desiredRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var rolesToRemove = currentRoles.Where(r => !desiredRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (rolesToRemove.Length > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
                    }
                }

                var rolesToAdd = desiredRoles.Where(r => !currentRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (rolesToAdd.Length > 0)
                {
                    foreach (var roleName in rolesToAdd)
                    {
                        if (await _roleManager.RoleExistsAsync(roleName))
                        {
                            await _userManager.AddToRoleAsync(user, roleName);
                        }
                    }
                }
            }

            var updated = new UserSummaryDto
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
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                Groups = new List<UserGroupInfo>(),
                Permissions = new List<string>()
            };
            return Ok(new { data = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{userId}")]
    [RequirePermission(PermissionConstants.Identity.Users.Delete)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

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

    [HttpPatch("{userId}/activate")]
    [RequirePermission(PermissionConstants.Identity.Users.Activate)]
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

    [HttpPatch("{userId}/deactivate")]
    [RequirePermission(PermissionConstants.Identity.Users.Deactivate)]
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

    [HttpGet("statistics")]
    [RequirePermission(PermissionConstants.Identity.Users.Read)]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var emailConfirmedUsers = await _userManager.Users.CountAsync(u => u.EmailConfirmed);
            var unconfirmedEmailUsers = totalUsers - emailConfirmedUsers;

            var totalRoles = await _roleManager.Roles.CountAsync();
            var totalGroups = await _db.Groups.CountAsync(g => !g.IsDeleted && g.IsActive);

            var statistics = new
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                EmailConfirmedUsers = emailConfirmedUsers,
                UnconfirmedEmailUsers = unconfirmedEmailUsers,
                TotalRoles = totalRoles,
                TotalGroups = totalGroups,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}

