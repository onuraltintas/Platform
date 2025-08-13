namespace EgitimPlatform.Services.UserService.Models.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? SocialMediaLinks { get; set; }
    public string? Preferences { get; set; }
}

public class CreateUserProfileRequest
{
    public Guid UserId { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? SocialMediaLinks { get; set; }
    public string? Preferences { get; set; }
}

public class UpdateUserProfileRequest
{
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? SocialMediaLinks { get; set; }
    public string? Preferences { get; set; }
}

public class UserSettingsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "tr";
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool SMSNotifications { get; set; } = false;
    public string? Preferences { get; set; }
}

public class UpdateUserSettingsRequest
{
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public string? TimeZone { get; set; }
    public bool? EmailNotifications { get; set; }
    public bool? PushNotifications { get; set; }
    public bool? SMSNotifications { get; set; }
    public string? Preferences { get; set; }
}

