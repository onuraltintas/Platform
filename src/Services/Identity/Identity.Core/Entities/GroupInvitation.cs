namespace Identity.Core.Entities;

public class GroupInvitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public virtual Group Group { get; set; } = null!;

    // Invitation Details
    public string InvitationCode { get; set; } = string.Empty;
    public UserGroupRole Role { get; set; } = UserGroupRole.Member;
    public DateTime ExpiresAt { get; set; }

    // Usage Limits
    public int? MaxUses { get; set; } // null = unlimited
    public int UsedCount { get; set; } = 0;

    // Creator Info
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }

    // Additional Settings
    public string? Note { get; set; } // Internal note about this invitation
    public string? AllowedEmails { get; set; } // JSON array of specific emails allowed
    public string? AllowedDomains { get; set; } // JSON array of email domains allowed

    // Navigation Properties
    public virtual ICollection<GroupInvitationUsage> Usages { get; set; } = new List<GroupInvitationUsage>();
}

public class GroupInvitationUsage
{
    public Guid Id { get; set; }
    public Guid InvitationId { get; set; }
    public virtual GroupInvitation Invitation { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime UsedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}