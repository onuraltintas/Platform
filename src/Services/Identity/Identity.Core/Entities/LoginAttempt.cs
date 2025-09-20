namespace Identity.Core.Entities;

public class LoginAttempt
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? DeviceId { get; set; }
    public string? Location { get; set; }
    public LoginType Type { get; set; }
    
    // Group Context
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }
}

public enum LoginType
{
    Password = 1,
    RefreshToken = 2,
    TwoFactor = 3,
    Google = 4,
    Facebook = 5,
    Microsoft = 6,
    ApiKey = 7
}