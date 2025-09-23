using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserGroup
{
    public string UserId { get; set; } = null!;

    public Guid GroupId { get; set; }

    public DateTime JoinedAt { get; set; }

    public string? InvitedBy { get; set; }

    public int Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public string? SuspensionReason { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
