using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string Resource { get; set; } = null!;

    public string Action { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public int Type { get; set; }

    public int Priority { get; set; }

    public Guid? ParentId { get; set; }

    public string Path { get; set; } = null!;

    public int Level { get; set; }

    public bool IsWildcard { get; set; }

    public string? WildcardPattern { get; set; }

    public bool InheritsFromParent { get; set; }

    public bool IsImplicit { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public virtual ICollection<Permission> InverseParent { get; set; } = new List<Permission>();

    public virtual Permission? Parent { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual Service Service { get; set; } = null!;
}
