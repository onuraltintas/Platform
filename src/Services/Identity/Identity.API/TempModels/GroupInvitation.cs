using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class GroupInvitation
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public string InvitationCode { get; set; } = null!;

    public int Role { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int? MaxUses { get; set; }

    public int UsedCount { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedBy { get; set; }

    public string? Note { get; set; }

    public string? AllowedEmails { get; set; }

    public string? AllowedDomains { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual ICollection<GroupInvitationUsage> GroupInvitationUsages { get; set; } = new List<GroupInvitationUsage>();
}
