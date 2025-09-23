using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Identity.Application.Services;

/// <summary>
/// Role hierarchy management service implementation
/// </summary>
public class RoleHierarchyService : IRoleHierarchyService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<RoleHierarchyService> _logger;

    public RoleHierarchyService(
        IdentityDbContext context,
        ILogger<RoleHierarchyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleHierarchyNode>> GetRoleHierarchyAsync(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive && (groupId == null || r.GroupId == groupId))
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.HierarchyLevel)
                .ThenBy(r => r.Priority)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var rootRoles = roles.Where(r => r.ParentRoleId == null).ToList();
            var hierarchyNodes = new List<RoleHierarchyNode>();

            foreach (var rootRole in rootRoles)
            {
                var node = await BuildHierarchyNodeAsync(rootRole, roles, cancellationToken);
                hierarchyNodes.Add(node);
            }

            return hierarchyNodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role hierarchy for group {GroupId}", groupId);
            return new List<RoleHierarchyNode>();
        }
    }

    public async Task<HashSet<string>> GetEffectivePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = new HashSet<string>();
            var visitedRoles = new HashSet<string>();

            await CollectPermissionsRecursivelyAsync(roleId, permissions, visitedRoles, cancellationToken);

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for role {RoleId}", roleId);
            return new HashSet<string>();
        }
    }

    public async Task<List<ApplicationRole>> GetChildRolesAsync(string roleId, bool recursive = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var childRoles = new List<ApplicationRole>();
            var visitedRoles = new HashSet<string>();

            await CollectChildRolesAsync(roleId, childRoles, visitedRoles, recursive, cancellationToken);

            return childRoles.OrderBy(r => r.HierarchyLevel).ThenBy(r => r.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child roles for role {RoleId}", roleId);
            return new List<ApplicationRole>();
        }
    }

    public async Task<List<ApplicationRole>> GetParentRolesAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var parentRoles = new List<ApplicationRole>();
            var currentRoleId = roleId;

            while (!string.IsNullOrEmpty(currentRoleId))
            {
                var role = await _context.Roles
                    .Include(r => r.ParentRole)
                    .FirstOrDefaultAsync(r => r.Id == currentRoleId, cancellationToken);

                if (role?.ParentRole == null)
                    break;

                parentRoles.Add(role.ParentRole);
                currentRoleId = role.ParentRole.Id;
            }

            return parentRoles.OrderBy(r => r.HierarchyLevel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent roles for role {RoleId}", roleId);
            return new List<ApplicationRole>();
        }
    }

    public async Task<bool> SetParentRoleAsync(string roleId, string? parentRoleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                return false;
            }

            // Validate hierarchy to prevent circular dependencies
            if (!string.IsNullOrEmpty(parentRoleId))
            {
                var isValid = await ValidateHierarchyAsync(roleId, parentRoleId, cancellationToken);
                if (!isValid)
                {
                    _logger.LogWarning("Setting parent {ParentRoleId} for role {RoleId} would create circular dependency",
                        parentRoleId, roleId);
                    return false;
                }
            }

            var oldParentRoleId = role.ParentRoleId;
            role.ParentRoleId = parentRoleId;
            role.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Rebuild hierarchy for affected subtree
            await RebuildHierarchyForRoleAsync(roleId, cancellationToken);

            _logger.LogInformation("Set parent role {ParentRoleId} for role {RoleId}, previous parent: {OldParentRoleId}",
                parentRoleId ?? "null", roleId, oldParentRoleId ?? "null");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting parent role {ParentRoleId} for role {RoleId}", parentRoleId, roleId);
            return false;
        }
    }

    public async Task<bool> MoveRoleAsync(string roleId, string? newParentRoleId, CancellationToken cancellationToken = default)
    {
        return await SetParentRoleAsync(roleId, newParentRoleId, cancellationToken);
    }

    public async Task<bool> ValidateHierarchyAsync(string roleId, string parentRoleId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if parentRoleId is the same as roleId
            if (roleId == parentRoleId)
                return false;

            // Check if parentRoleId is a descendant of roleId (would create circular dependency)
            var descendants = await GetChildRolesAsync(roleId, true, cancellationToken);
            return !descendants.Any(d => d.Id == parentRoleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hierarchy for role {RoleId} with parent {ParentRoleId}", roleId, parentRoleId);
            return false;
        }
    }

    public async Task RebuildHierarchyAsync(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => groupId == null || r.GroupId == groupId)
                .ToListAsync(cancellationToken);

            // Start with root roles (no parent)
            var rootRoles = roles.Where(r => r.ParentRoleId == null).ToList();

            foreach (var rootRole in rootRoles)
            {
                await UpdateHierarchyInfoRecursively(rootRole, roles, 0, $"/{rootRole.Name}", cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Rebuilt hierarchy for {RoleCount} roles in group {GroupId}",
                roles.Count, groupId?.ToString() ?? "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding hierarchy for group {GroupId}", groupId);
        }
    }

    public async Task<List<ApplicationRole>> GetAssignableRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's highest priority role
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.HierarchyLevel)
                .ToListAsync(cancellationToken);

            if (!userRoles.Any())
                return new List<ApplicationRole>();

            var highestRole = userRoles.First();

            // Users can assign roles that are:
            // 1. Lower in hierarchy (child roles)
            // 2. Same or lower priority
            var assignableRoles = await _context.Roles
                .Where(r => r.IsActive &&
                           r.GroupId == highestRole.GroupId &&
                           (r.HierarchyLevel > highestRole.HierarchyLevel ||
                            (r.HierarchyLevel == highestRole.HierarchyLevel && r.Priority <= highestRole.Priority)))
                .OrderBy(r => r.HierarchyLevel)
                .ThenByDescending(r => r.Priority)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);

            return assignableRoles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignable roles for user {UserId}", userId);
            return new List<ApplicationRole>();
        }
    }

    public async Task<bool> CanManageRoleAsync(string managerRoleId, string targetRoleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var managerRole = await _context.Roles.FindAsync(new object[] { managerRoleId }, cancellationToken);
            var targetRole = await _context.Roles.FindAsync(new object[] { targetRoleId }, cancellationToken);

            if (managerRole == null || targetRole == null)
                return false;

            // Can manage if:
            // 1. Higher in hierarchy (lower level number) OR
            // 2. Same level but higher priority
            return managerRole.HierarchyLevel < targetRole.HierarchyLevel ||
                   (managerRole.HierarchyLevel == targetRole.HierarchyLevel &&
                    managerRole.Priority > targetRole.Priority);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role {ManagerRoleId} can manage role {TargetRoleId}",
                managerRoleId, targetRoleId);
            return false;
        }
    }

    public async Task<List<RoleInheritanceInfo>> GetInheritanceChainAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var inheritanceChain = new List<RoleInheritanceInfo>();
            var currentRoleId = roleId;

            while (!string.IsNullOrEmpty(currentRoleId))
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .Include(r => r.ParentRole)
                    .FirstOrDefaultAsync(r => r.Id == currentRoleId, cancellationToken);

                if (role == null)
                    break;

                var directPermissions = role.RolePermissions
                    .Where(rp => rp.IsActive)
                    .Select(rp => rp.IsWildcard ? rp.PermissionPattern! : rp.Permission!.Code)
                    .ToHashSet();

                var inheritedPermissions = new HashSet<string>();
                if (role.InheritPermissions && role.ParentRole != null)
                {
                    inheritedPermissions = await GetEffectivePermissionsAsync(role.ParentRole.Id, cancellationToken);
                }

                inheritanceChain.Add(new RoleInheritanceInfo
                {
                    RoleId = role.Id,
                    RoleName = role.Name!,
                    Level = role.HierarchyLevel,
                    InheritPermissions = role.InheritPermissions,
                    DirectPermissions = directPermissions,
                    InheritedPermissions = inheritedPermissions
                });

                currentRoleId = role.ParentRoleId;
            }

            return inheritanceChain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inheritance chain for role {RoleId}", roleId);
            return new List<RoleInheritanceInfo>();
        }
    }

    public async Task<List<PermissionConflict>> GetPermissionConflictsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conflicts = new List<PermissionConflict>();
            var inheritanceChain = await GetInheritanceChainAsync(roleId, cancellationToken);

            for (int i = 0; i < inheritanceChain.Count; i++)
            {
                var currentRole = inheritanceChain[i];

                // Check for conflicts with parent roles
                for (int j = i + 1; j < inheritanceChain.Count; j++)
                {
                    var parentRole = inheritanceChain[j];

                    // Find overlapping permissions
                    var overlapping = currentRole.DirectPermissions
                        .Intersect(parentRole.DirectPermissions)
                        .ToList();

                    foreach (var permission in overlapping)
                    {
                        conflicts.Add(new PermissionConflict
                        {
                            PermissionCode = permission,
                            ConflictType = "Duplicate",
                            SourceRoleId = currentRole.RoleId,
                            SourceRoleName = currentRole.RoleName,
                            ConflictingRoleId = parentRole.RoleId,
                            ConflictingRoleName = parentRole.RoleName,
                            Description = $"Permission '{permission}' is defined in both roles",
                            Severity = "Medium"
                        });
                    }
                }
            }

            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission conflicts for role {RoleId}", roleId);
            return new List<PermissionConflict>();
        }
    }

    public async Task<int> GetEffectivePriorityAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
            if (role == null)
                return 0;

            // Base priority from role
            var effectivePriority = role.Priority;

            // Add hierarchy bonus (higher levels get priority boost)
            var hierarchyBonus = Math.Max(0, 10 - role.HierarchyLevel) * 10;

            return effectivePriority + hierarchyBonus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating effective priority for role {RoleId}", roleId);
            return 0;
        }
    }

    #region Private Helper Methods

    private async Task<RoleHierarchyNode> BuildHierarchyNodeAsync(
        ApplicationRole role,
        List<ApplicationRole> allRoles,
        CancellationToken cancellationToken)
    {
        var directPermissions = role.RolePermissions.Count;
        var effectivePermissions = await GetEffectivePermissionsAsync(role.Id, cancellationToken);
        var inheritedPermissions = effectivePermissions.Count - directPermissions;

        var node = new RoleHierarchyNode
        {
            RoleId = role.Id,
            RoleName = role.Name!,
            Description = role.Description,
            Level = role.HierarchyLevel,
            Priority = role.Priority,
            IsActive = role.IsActive,
            InheritPermissions = role.InheritPermissions,
            DirectPermissionCount = directPermissions,
            InheritedPermissionCount = inheritedPermissions,
            TotalPermissionCount = effectivePermissions.Count
        };

        // Add child nodes
        var childRoles = allRoles.Where(r => r.ParentRoleId == role.Id).ToList();
        foreach (var childRole in childRoles)
        {
            var childNode = await BuildHierarchyNodeAsync(childRole, allRoles, cancellationToken);
            node.Children.Add(childNode);
        }

        return node;
    }

    private async Task CollectPermissionsRecursivelyAsync(
        string roleId,
        HashSet<string> permissions,
        HashSet<string> visitedRoles,
        CancellationToken cancellationToken)
    {
        if (visitedRoles.Contains(roleId))
            return; // Prevent infinite loops

        visitedRoles.Add(roleId);

        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return;

        // Add direct permissions
        foreach (var rolePermission in role.RolePermissions.Where(rp => rp.IsActive))
        {
            if (rolePermission.IsWildcard && !string.IsNullOrEmpty(rolePermission.PermissionPattern))
            {
                permissions.Add(rolePermission.PermissionPattern);
            }
            else if (rolePermission.Permission != null)
            {
                permissions.Add(rolePermission.Permission.Code);
            }
        }

        // Add inherited permissions from parent role
        if (role.InheritPermissions && !string.IsNullOrEmpty(role.ParentRoleId))
        {
            await CollectPermissionsRecursivelyAsync(role.ParentRoleId, permissions, visitedRoles, cancellationToken);
        }
    }

    private async Task CollectChildRolesAsync(
        string roleId,
        List<ApplicationRole> childRoles,
        HashSet<string> visitedRoles,
        bool recursive,
        CancellationToken cancellationToken)
    {
        if (visitedRoles.Contains(roleId))
            return;

        visitedRoles.Add(roleId);

        var directChildren = await _context.Roles
            .Where(r => r.ParentRoleId == roleId && r.IsActive)
            .ToListAsync(cancellationToken);

        childRoles.AddRange(directChildren);

        if (recursive)
        {
            foreach (var child in directChildren)
            {
                await CollectChildRolesAsync(child.Id, childRoles, visitedRoles, recursive, cancellationToken);
            }
        }
    }

    private async Task UpdateHierarchyInfoRecursively(
        ApplicationRole role,
        List<ApplicationRole> allRoles,
        int level,
        string path,
        CancellationToken cancellationToken)
    {
        role.HierarchyLevel = level;
        role.HierarchyPath = path;
        role.LastModifiedAt = DateTime.UtcNow;

        var children = allRoles.Where(r => r.ParentRoleId == role.Id).ToList();
        foreach (var child in children)
        {
            await UpdateHierarchyInfoRecursively(child, allRoles, level + 1, $"{path}/{child.Name}", cancellationToken);
        }
    }

    private async Task RebuildHierarchyForRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null)
            return;

        // Find root of this subtree
        var rootRole = role;
        while (!string.IsNullOrEmpty(rootRole.ParentRoleId))
        {
            var parent = await _context.Roles.FindAsync(new object[] { rootRole.ParentRoleId }, cancellationToken);
            if (parent == null)
                break;
            rootRole = parent;
        }

        // Rebuild from root
        var allRoles = await _context.Roles
            .Where(r => r.GroupId == role.GroupId)
            .ToListAsync(cancellationToken);

        await UpdateHierarchyInfoRecursively(rootRole, allRoles, 0, $"/{rootRole.Name}", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}