using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IGroupService
{
    Task<Result<GroupDto>> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<Result<GroupDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<GroupDto>>> GetGroupsAsync(int page = 1, int pageSize = 10, string? search = null, GroupType? type = null, CancellationToken cancellationToken = default);
    Task<Result<GroupDto>> CreateAsync(CreateGroupRequest request, string createdBy, CancellationToken cancellationToken = default);
    Task<Result<GroupDto>> UpdateAsync(Guid groupId, UpdateGroupRequest request, string modifiedBy, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid groupId, string deletedBy, CancellationToken cancellationToken = default);
    Task<Result<bool>> AddUserToGroupAsync(Guid groupId, string userId, UserGroupRole role = UserGroupRole.Member, string? invitedBy = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveUserFromGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserRoleInGroupAsync(Guid groupId, string userId, UserGroupRole role, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<GroupDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsUserInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<Result<UserGroupRole?>> GetUserRoleInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    // Group Invitation System
    Task<Result<GroupInvitationDto>> CreateInvitationAsync(Guid groupId, CreateGroupInvitationRequest request, string createdBy, CancellationToken cancellationToken = default);
    Task<Result<GroupDto>> JoinByInvitationAsync(string userId, string invitationCode, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeInvitationAsync(Guid invitationId, string revokedBy, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<GroupInvitationDto>>> GetGroupInvitationsAsync(Guid groupId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<Result<GroupInvitationDto>> GetInvitationByCodeAsync(string invitationCode, CancellationToken cancellationToken = default);

    // Legacy methods (kept for backward compatibility)
    Task<Result<string>> GenerateInvitationCodeAsync(Guid groupId, UserGroupRole role, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
    Task<Result<GroupDto>> JoinByInvitationCodeAsync(string userId, string invitationCode, CancellationToken cancellationToken = default);
    Task<Result<bool>> CanUserAccessGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);

    // Statistics
    Task<Result<GroupStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<Group?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PagedResult<Group>> GetGroupsAsync(int page, int pageSize, string? search = null, GroupType? type = null, CancellationToken cancellationToken = default);
    Task<Group> CreateAsync(Group group, CancellationToken cancellationToken = default);
    Task<Group> UpdateAsync(Group group, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> IsNameTakenAsync(string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<UserGroup?> GetUserGroupRelationAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<UserGroup> AddUserToGroupAsync(UserGroup userGroup, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserFromGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<PagedResult<ApplicationUser>> GetGroupMembersAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default);
}