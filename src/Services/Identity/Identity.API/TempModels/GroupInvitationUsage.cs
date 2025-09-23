using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class GroupInvitationUsage
{
    public Guid Id { get; set; }

    public Guid InvitationId { get; set; }

    public string UserId { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public DateTime UsedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual GroupInvitation Invitation { get; set; } = null!;
}
