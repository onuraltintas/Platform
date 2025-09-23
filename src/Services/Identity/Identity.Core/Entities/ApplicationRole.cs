using Microsoft.AspNetCore.Identity;

namespace Identity.Core.Entities;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }

    // Group/Tenant Support
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }

    // Role Hierarchy Support
    public string? ParentRoleId { get; set; }
    public virtual ApplicationRole? ParentRole { get; set; }
    public virtual ICollection<ApplicationRole> ChildRoles { get; set; } = new List<ApplicationRole>();
    public int HierarchyLevel { get; set; } = 0;
    public string HierarchyPath { get; set; } = string.Empty; // e.g., "/SuperAdmin/Admin/Manager"
    public bool InheritPermissions { get; set; } = true;
    public int Priority { get; set; } = 100; // Higher number = higher priority

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}