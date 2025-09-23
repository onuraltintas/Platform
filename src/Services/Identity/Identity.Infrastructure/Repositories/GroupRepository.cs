using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Enterprise.Shared.Common.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    private readonly IdentityDbContext _identityContext;
    private readonly ILogger<GroupRepository> _logger;

    public GroupRepository(
        IdentityDbContext context,
        ILogger<GroupRepository> logger)
        : base(context)
    {
        _identityContext = context;
        _logger = logger;
    }

    public override async Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.Groups
                .Include(g => g.UserGroups)
                    .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by ID: {GroupId}", groupId);
            return null;
        }
    }

    public async Task<Group?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.Groups
                .FirstOrDefaultAsync(g => g.Name == name && !g.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by name: {Name}", name);
            return null;
        }
    }

    public async Task<PagedResult<Group>> GetGroupsAsync(
        int page,
        int pageSize,
        string? search = null,
        GroupType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _identityContext.Groups
                .Where(g => !g.IsDeleted)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g =>
                    g.Name.Contains(search) ||
                    (g.Description != null && g.Description.Contains(search)));
            }

            // Type filter
            if (type.HasValue)
            {
                query = query.Where(g => g.Type == type.Value);
            }

            // Order by name
            query = query.OrderBy(g => g.Name);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(g => g.UserGroups)
                .ToListAsync(cancellationToken);

            return new PagedResult<Group>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged groups");
            return new PagedResult<Group> { Data = new List<Group>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }

    public async Task<Group> CreateAsync(Group group, CancellationToken cancellationToken = default)
    {
        try
        {
            group.CreatedAt = DateTime.UtcNow;
            group.IsActive = true;
            group.IsDeleted = false;

            _identityContext.Groups.Add(group);
            await _identityContext.SaveChangesAsync(cancellationToken);

            return group;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group: {GroupName}", group.Name);
            throw;
        }
    }

    public async Task<Group> UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        try
        {
            group.LastModifiedAt = DateTime.UtcNow;

            _identityContext.Groups.Update(group);
            await _identityContext.SaveChangesAsync(cancellationToken);

            return group;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group: {GroupId}", group.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await GetByIdAsync(groupId, cancellationToken);
            if (group == null) return false;

            // Soft delete
            group.IsDeleted = true;
            group.DeletedAt = DateTime.UtcNow;

            await UpdateAsync(group, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group: {GroupId}", groupId);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.Groups
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if group exists: {GroupId}", groupId);
            return false;
        }
    }

    public async Task<bool> IsNameTakenAsync(string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _identityContext.Groups
                .Where(g => g.Name == name && !g.IsDeleted);

            if (excludeGroupId.HasValue)
            {
                query = query.Where(g => g.Id != excludeGroupId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if group name is taken: {Name}", name);
            return false;
        }
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.UserGroups
                .Where(ug => ug.UserId == userId && ug.IsActive)
                .Include(ug => ug.Group)
                .Where(ug => ug.Group != null && !ug.Group.IsDeleted && ug.Group.IsActive)
                .Select(ug => ug.Group!)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups: {UserId}", userId);
            return new List<Group>();
        }
    }

    public async Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.UserGroups
                .CountAsync(ug => ug.GroupId == groupId && ug.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member count: {GroupId}", groupId);
            return 0;
        }
    }

    public async Task<UserGroup?> GetUserGroupRelationAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _identityContext.UserGroups
                .Include(ug => ug.User)
                .Include(ug => ug.Group)
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user-group relation: {UserId}-{GroupId}", userId, groupId);
            return null;
        }
    }

    public async Task<UserGroup> AddUserToGroupAsync(UserGroup userGroup, CancellationToken cancellationToken = default)
    {
        try
        {
            userGroup.JoinedAt = DateTime.UtcNow;
            userGroup.IsActive = true;

            _identityContext.UserGroups.Add(userGroup);
            await _identityContext.SaveChangesAsync(cancellationToken);

            return userGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to group: {UserId}-{GroupId}", userGroup.UserId, userGroup.GroupId);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userGroup = await GetUserGroupRelationAsync(userId, groupId, cancellationToken);
            if (userGroup == null) return false;

            _identityContext.UserGroups.Remove(userGroup);
            await _identityContext.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from group: {UserId}-{GroupId}", userId, groupId);
            return false;
        }
    }

    public async Task<PagedResult<ApplicationUser>> GetGroupMembersAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _identityContext.UserGroups
                .Where(ug => ug.GroupId == groupId && ug.IsActive)
                .Include(ug => ug.User)
                .Select(ug => ug.User);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ApplicationUser>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group members: {GroupId}", groupId);
            return new PagedResult<ApplicationUser> { Data = new List<ApplicationUser>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }
}