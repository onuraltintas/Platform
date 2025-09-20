namespace Identity.Core.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    
    // Service Association
    public Guid ServiceId { get; set; }
    public virtual Service Service { get; set; } = null!;
    
    // Classification
    public PermissionType Type { get; set; }
    public int Priority { get; set; } = 0;
    
    // Hierarchy Support
    public Guid? ParentId { get; set; }
    public virtual Permission? Parent { get; set; }
    public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();
    public string Path { get; set; } = string.Empty; // Hierarchical path like "/admin/users/read"
    public int Level { get; set; } = 0; // Depth in hierarchy

    // Wildcard Support
    public bool IsWildcard { get; set; } = false;
    public string? WildcardPattern { get; set; } // Pattern like "users.*" or "admin.**"

    // Inheritance
    public bool InheritsFromParent { get; set; } = true;
    public bool IsImplicit { get; set; } = false; // Auto-generated from wildcards

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public enum PermissionType
{
    Read = 1,
    Write = 2,
    Delete = 3,
    Execute = 4,
    Admin = 5,
    Custom = 99
}