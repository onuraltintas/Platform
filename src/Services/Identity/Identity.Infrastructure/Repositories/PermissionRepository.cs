using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Enterprise.Shared.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Repositories;

public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    private readonly IdentityDbContext _identityContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionRepository(
        IdentityDbContext context,
        UserManager<ApplicationUser> userManager) : base(context)
    {
        _identityContext = context;
        _userManager = userManager;
    }

    public new async Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _identityContext.Permissions
            .Include(p => p.Service)
            .Include(p => p.RolePermissions)
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken);
    }

    public async Task<Permission?> GetByNameAsync(string name, Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _identityContext.Permissions
            .Include(p => p.Service)
            .FirstOrDefaultAsync(p => p.Name == name && p.ServiceId == serviceId, cancellationToken);
    }

    public async Task<PagedResult<Permission>> GetPermissionsAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? serviceId = null,
        PermissionType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _identityContext.Permissions
            .Include(p => p.Service)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.DisplayName != null && p.DisplayName.Contains(search)) ||
                (p.Description != null && p.Description.Contains(search)) ||
                p.Resource.Contains(search) ||
                p.Action.Contains(search));
        }

        if (serviceId.HasValue)
        {
            query = query.Where(p => p.ServiceId == serviceId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Permission>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<Permission>> GetServicePermissionsAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return await _identityContext.Permissions
            .Include(p => p.Service)
            .Where(p => p.ServiceId == serviceId && p.IsActive)
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Permission> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        permission.CreatedAt = DateTime.UtcNow;
        await _identityContext.Permissions.AddAsync(permission, cancellationToken);
        await _identityContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<Permission> UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        permission.LastModifiedAt = DateTime.UtcNow;
        _identityContext.Permissions.Update(permission);
        await _identityContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<bool> DeleteAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _identityContext.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (permission == null)
            return false;

        _identityContext.Permissions.Remove(permission);
        var result = await _identityContext.SaveChangesAsync(cancellationToken);
        return result > 0;
    }

    public async Task<bool> ExistsAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _identityContext.Permissions
            .AnyAsync(p => p.Id == permissionId, cancellationToken);
    }

    public async Task<bool> IsNameTakenAsync(
        string name,
        Guid serviceId,
        Guid? excludePermissionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _identityContext.Permissions
            .Where(p => p.Name == name && p.ServiceId == serviceId);

        if (excludePermissionId.HasValue)
        {
            query = query.Where(p => p.Id != excludePermissionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(
        string userId,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new List<Permission>();

        var userRoles = await _userManager.GetRolesAsync(user);

        var query = _identityContext.RolePermissions
            .Include(rp => rp.Permission)
                .ThenInclude(p => p.Service)
            .Include(rp => rp.Role)
            .Where(rp => userRoles.Contains(rp.Role.Name!) && rp.Permission.IsActive);

        if (groupId.HasValue)
        {
            query = query.Where(rp => rp.GroupId == null || rp.GroupId == groupId.Value);
        }

        var now = DateTime.UtcNow;
        query = query.Where(rp =>
            (rp.ValidFrom == null || rp.ValidFrom <= now) &&
            (rp.ValidUntil == null || rp.ValidUntil >= now));

        var permissions = await query
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(
        string roleId,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _identityContext.RolePermissions
            .Include(rp => rp.Permission)
                .ThenInclude(p => p.Service)
            .Where(rp => rp.RoleId == roleId && rp.Permission.IsActive);

        if (groupId.HasValue)
        {
            query = query.Where(rp => rp.GroupId == null || rp.GroupId == groupId.Value);
        }

        var now = DateTime.UtcNow;
        query = query.Where(rp =>
            (rp.ValidFrom == null || rp.ValidFrom <= now) &&
            (rp.ValidUntil == null || rp.ValidUntil >= now));

        var permissions = await query
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<RolePermission?> GetRolePermissionAsync(
        string roleId,
        Guid permissionId,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _identityContext.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (groupId.HasValue)
        {
            query = query.Where(rp => rp.GroupId == groupId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RolePermission> AssignPermissionToRoleAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default)
    {
        rolePermission.GrantedAt = DateTime.UtcNow;
        await _identityContext.RolePermissions.AddAsync(rolePermission, cancellationToken);
        await _identityContext.SaveChangesAsync(cancellationToken);
        return rolePermission;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(
        string roleId,
        Guid permissionId,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _identityContext.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (groupId.HasValue)
        {
            query = query.Where(rp => rp.GroupId == groupId.Value);
        }

        var rolePermission = await query.FirstOrDefaultAsync(cancellationToken);

        if (rolePermission == null)
            return false;

        _identityContext.RolePermissions.Remove(rolePermission);
        var result = await _identityContext.SaveChangesAsync(cancellationToken);
        return result > 0;
    }

    public async Task<int> GetRoleCountAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _identityContext.RolePermissions
            .Where(rp => rp.PermissionId == permissionId)
            .Select(rp => rp.RoleId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetUserCountAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _identityContext.RolePermissions
            .Where(rp => rp.PermissionId == permissionId)
            .Select(rp => rp.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!roleIds.Any())
            return 0;

        var userCount = await _identityContext.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        return userCount;
    }
}