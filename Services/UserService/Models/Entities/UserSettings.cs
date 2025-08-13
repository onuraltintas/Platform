namespace EgitimPlatform.Services.UserService.Models.Entities;

public class UserSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "tr";
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool SMSNotifications { get; set; } = false;
    public string? Preferences { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

