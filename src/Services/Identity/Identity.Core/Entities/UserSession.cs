namespace Identity.Core.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string SessionId { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; } = true;

    // Security tracking
    public int LoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }

    // Zero Trust scoring
    public double TrustScore { get; set; } = 50.0;
    public string? TrustFactors { get; set; } // JSON
}