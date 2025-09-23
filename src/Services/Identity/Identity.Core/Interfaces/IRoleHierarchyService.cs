using Identity.Core.DTOs;
using Identity.Core.Entities;

namespace Identity.Core.Interfaces;

/// <summary>
/// Role hierarchy management service interface
/// </summary>
public interface IRoleHierarchyService
{
    /// <summary>
    /// Get role hierarchy tree starting from root roles
    /// </summary>
    Task<List<RoleHierarchyNode>> GetRoleHierarchyAsync(Guid? groupId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions for a role including inherited permissions from parent roles
    /// </summary>
    Task<HashSet<string>> GetEffectivePermissionsAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all child roles of a given role (recursive)
    /// </summary>
    Task<List<ApplicationRole>> GetChildRolesAsync(string roleId, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all parent roles of a given role (up to root)
    /// </summary>
    Task<List<ApplicationRole>> GetParentRolesAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set parent role for a role
    /// </summary>
    Task<bool> SetParentRoleAsync(string roleId, string? parentRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move role to a different parent
    /// </summary>
    Task<bool> MoveRoleAsync(string roleId, string? newParentRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if setting parent would create a circular dependency
    /// </summary>
    Task<bool> ValidateHierarchyAsync(string roleId, string parentRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuild hierarchy paths and levels for all roles
    /// </summary>
    Task RebuildHierarchyAsync(Guid? groupId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get roles that user can assign based on their highest role
    /// </summary>
    Task<List<ApplicationRole>> GetAssignableRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a role can manage another role (based on hierarchy)
    /// </summary>
    Task<bool> CanManageRoleAsync(string managerRoleId, string targetRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role inheritance chain (from role to root)
    /// </summary>
    Task<List<RoleInheritanceInfo>> GetInheritanceChainAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get permission conflicts in hierarchy
    /// </summary>
    Task<List<PermissionConflict>> GetPermissionConflictsAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate effective role priority considering hierarchy
    /// </summary>
    Task<int> GetEffectivePriorityAsync(string roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role hierarchy node for tree representation
/// </summary>
public class RoleHierarchyNode
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool InheritPermissions { get; set; }
    public int DirectPermissionCount { get; set; }
    public int InheritedPermissionCount { get; set; }
    public int TotalPermissionCount { get; set; }
    public List<RoleHierarchyNode> Children { get; set; } = new();
}

/// <summary>
/// Role inheritance information
/// </summary>
public class RoleInheritanceInfo
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool InheritPermissions { get; set; }
    public HashSet<string> DirectPermissions { get; set; } = new();
    public HashSet<string> InheritedPermissions { get; set; } = new();
}

/// <summary>
/// Permission conflict information
/// </summary>
public class PermissionConflict
{
    public string PermissionCode { get; set; } = string.Empty;
    public string ConflictType { get; set; } = string.Empty; // "Duplicate", "Override", "Inherited"
    public string SourceRoleId { get; set; } = string.Empty;
    public string SourceRoleName { get; set; } = string.Empty;
    public string ConflictingRoleId { get; set; } = string.Empty;
    public string ConflictingRoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "Low", "Medium", "High"
}