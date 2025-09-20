using Identity.Core.Entities;

namespace Identity.Core.DTOs;

public class GroupInvitationDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string InvitationCode { get; set; } = string.Empty;
    public string InvitationLink { get; set; } = string.Empty; // Full URL for joining
    public UserGroupRole Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int? RemainingUses => MaxUses.HasValue ? MaxUses.Value - UsedCount : null;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
    public List<string>? AllowedEmails { get; set; }
    public List<string>? AllowedDomains { get; set; }
}

public class CreateGroupInvitationRequest
{
    public UserGroupRole Role { get; set; } = UserGroupRole.Member;
    public int ExpirationHours { get; set; } = 24; // Default 24 hours
    public int? MaxUses { get; set; } // null = unlimited
    public string? Note { get; set; }
    public List<string>? AllowedEmails { get; set; } // Specific emails that can use this invite
    public List<string>? AllowedDomains { get; set; } // Email domains that can use this invite (e.g., "@company.com")
}

public class GroupInvitationUsageDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime UsedAt { get; set; }
    public string? IpAddress { get; set; }
}