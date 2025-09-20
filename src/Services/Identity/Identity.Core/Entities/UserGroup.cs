namespace Identity.Core.Entities;

public class UserGroup
{
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    public Guid GroupId { get; set; }
    public virtual Group Group { get; set; } = null!;
    
    public DateTime JoinedAt { get; set; }
    public string? InvitedBy { get; set; }
    public UserGroupRole Role { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
}

public enum UserGroupRole
{
    Member = 1,
    Moderator = 2,
    Admin = 3,
    Owner = 4
}