using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class RolePermission
{
    public string RoleId { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public DateTime GrantedAt { get; set; }

    public string? GrantedBy { get; set; }

    public Guid? GroupId { get; set; }

    public string? Conditions { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public virtual Group? Group { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
