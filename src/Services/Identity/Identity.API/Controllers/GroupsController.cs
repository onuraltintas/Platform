using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enterprise.Shared.Authorization.Attributes;
using Enterprise.Shared.Common.Models;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Groups management controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(
        IGroupService groupService,
        ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all groups with filtering and pagination
    /// </summary>
    /// <param name="request">Query parameters</param>
    /// <returns>Paginated list of groups</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGroups([FromQuery] GetGroupsRequest request)
    {
        try
        {
            request.Normalize();

            var result = await _groupService.GetGroupsAsync(
                request.Page,
                request.PageSize,
                request.Search,
                request.Type);

        if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new { error = result.Error });
            }

            var pagedResult = result.Value;
            return Ok(new {
                data = pagedResult.Data,
                totalCount = pagedResult.TotalCount,
                currentPage = request.Page,
                pageSize = request.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get group by ID
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Group details</returns>
    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGroup(Guid groupId)
    {
        try
        {
            var result = await _groupService.GetByIdAsync(groupId);

            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(new { error = result.Error ?? "Group not found" });
            }

            // Check if user can access this group
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var accessResult = await _groupService.CanUserAccessGroupAsync(userId, groupId);
                if (!accessResult.IsSuccess || !accessResult.Value)
                {
                    return Forbid("You don't have access to this group");
                }
            }

            return Ok(new { data = result.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new group
    /// </summary>
    /// <param name="request">Group creation data</param>
    /// <returns>Created group</returns>
    [HttpPost]
    [Authorize(Policy = "permission:groups.create")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var result = await _groupService.CreateAsync(request, currentUserId);

            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new { error = result.Error });
            }

            var group = result.Value;

            // Add creator as owner
            await _groupService.AddUserToGroupAsync(
                group.Id,
                currentUserId,
                UserGroupRole.Owner,
                currentUserId);

            _logger.LogInformation("Group {GroupId} created by user {UserId}", group.Id, currentUserId);

            return CreatedAtAction(
                nameof(GetGroup),
                new { groupId = group.Id },
                new { data = group });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="request">Group update data</param>
    /// <returns>Updated group</returns>
    [HttpPut("{groupId:guid}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] UpdateGroupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Check if user can manage this group
            var roleResult = await _groupService.GetUserRoleInGroupAsync(currentUserId, groupId);
            if (!roleResult.IsSuccess || !roleResult.Value.HasValue ||
                (roleResult.Value != UserGroupRole.Admin && roleResult.Value != UserGroupRole.Owner))
            {
                return Forbid("You don't have permission to update this group");
            }

            var result = await _groupService.UpdateAsync(groupId, request, currentUserId);

            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { data = result.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Check if user can manage this group
            var roleResult = await _groupService.GetUserRoleInGroupAsync(currentUserId, groupId);
            if (!roleResult.IsSuccess || !roleResult.Value.HasValue ||
                (roleResult.Value != UserGroupRole.Admin && roleResult.Value != UserGroupRole.Owner))
            {
                return Forbid("You don't have permission to delete this group");
            }

            var result = await _groupService.DeleteAsync(groupId, currentUserId);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get group members
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="request">Pagination parameters</param>
    /// <returns>Paginated list of group members</returns>
    [HttpGet("{groupId:guid}/members")]
    [ProducesResponseType(typeof(PagedResult<GroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGroupMembers(
        Guid groupId,
        [FromQuery] PagedRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Check if user is member of the group
            var isMemberResult = await _groupService.IsUserInGroupAsync(currentUserId, groupId);
            if (!isMemberResult.IsSuccess || !isMemberResult.Value)
            {
                return Forbid("You must be a member of this group to view members");
            }

            request.Normalize();

            var result = await _groupService.GetGroupMembersAsync(
                groupId,
                request.Page,
                request.PageSize);

            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new { error = result.Error });
            }

            var pagedResult = result.Value;
            return Ok(new {
                data = pagedResult.Data,
                totalCount = pagedResult.TotalCount,
                currentPage = request.Page,
                pageSize = request.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add user to group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="request">Member addition data</param>
    /// <returns>Success result</returns>
    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddMemberToGroup(
        Guid groupId,
        [FromBody] AddGroupMemberRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Check if user can manage group members
            var roleResult = await _groupService.GetUserRoleInGroupAsync(currentUserId, groupId);
            if (!roleResult.IsSuccess || !roleResult.Value.HasValue ||
                (roleResult.Value != UserGroupRole.Admin && roleResult.Value != UserGroupRole.Owner))
            {
                return Forbid("You don't have permission to add members to this group");
            }

            var result = await _groupService.AddUserToGroupAsync(
                groupId,
                request.UserId,
                request.Role,
                currentUserId);

            if (!result.IsSuccess || !result.Value)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("User {UserId} added to group {GroupId} by {AddedBy}",
                request.UserId, groupId, currentUserId);

            return CreatedAtAction(
                nameof(GetGroupMembers),
                new { groupId },
                new { success = true, message = "Member added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove user from group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>Success result</returns>
    [HttpDelete("{groupId:guid}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMemberFromGroup(Guid groupId, string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Users can remove themselves, or admins/owners can remove others
            if (currentUserId != userId)
            {
                var roleResult = await _groupService.GetUserRoleInGroupAsync(currentUserId, groupId);
                if (!roleResult.IsSuccess || !roleResult.Value.HasValue ||
                    (roleResult.Value != UserGroupRole.Admin && roleResult.Value != UserGroupRole.Owner))
                {
                    return Forbid("You don't have permission to remove members from this group");
                }
            }

            var result = await _groupService.RemoveUserFromGroupAsync(groupId, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("User {UserId} removed from group {GroupId} by {RemovedBy}",
                userId, groupId, currentUserId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update user role in group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="request">Role update data</param>
    /// <returns>Success result</returns>
    [HttpPut("{groupId:guid}/members/{userId}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid groupId,
        string userId,
        [FromBody] UpdateMemberRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Only owners can change roles
            var roleResult = await _groupService.GetUserRoleInGroupAsync(currentUserId, groupId);
            if (!roleResult.IsSuccess || roleResult.Value != UserGroupRole.Owner)
            {
                return Forbid("Only group owners can change member roles");
            }

            var result = await _groupService.UpdateUserRoleInGroupAsync(groupId, userId, request.Role);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("User {UserId} role updated to {Role} in group {GroupId} by {UpdatedBy}",
                userId, request.Role, groupId, currentUserId);

            return Ok(new { success = true, message = "Member role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member role in group {GroupId}", groupId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get current user's groups
    /// </summary>
    /// <returns>List of user's groups</returns>
    [HttpGet("my-groups")]
    [ProducesResponseType(typeof(IEnumerable<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGroups()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var result = await _groupService.GetUserGroupsAsync(currentUserId);

            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { data = result.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user groups");
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Methods

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #endregion
}

#region Request DTOs

public class GetGroupsRequest : PagedRequest
{
    public GroupType? Type { get; set; }
    public bool? IsActive { get; set; }
}

public class AddGroupMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public UserGroupRole Role { get; set; } = UserGroupRole.Member;
}

public class UpdateMemberRoleRequest
{
    public UserGroupRole Role { get; set; }
}

#endregion