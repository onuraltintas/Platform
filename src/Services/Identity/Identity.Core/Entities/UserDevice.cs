namespace Identity.Core.Entities;

public class UserDevice
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? OperatingSystem { get; set; }
    public string? Browser { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string? LastIpAddress { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; } = true;
    
    // User Association
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    // Push Notifications
    public string? PushToken { get; set; }
    public DateTime? PushTokenUpdatedAt { get; set; }
}