using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// User preferences and settings
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>
    /// User ID reference
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to user profile
    /// </summary>
    public virtual UserProfile UserProfile { get; set; } = null!;

    /// <summary>
    /// Marketing emails consent
    /// </summary>
    public bool MarketingEmailsConsent { get; set; }

    /// <summary>
    /// Data processing consent for GDPR
    /// </summary>
    public bool DataProcessingConsent { get; set; } = true;

    /// <summary>
    /// When consent was given
    /// </summary>
    public DateTime? ConsentGivenAt { get; set; }

    /// <summary>
    /// Email notifications enabled
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Push notifications enabled
    /// </summary>
    public bool PushNotifications { get; set; } = true;

    /// <summary>
    /// SMS notifications enabled
    /// </summary>
    public bool SmsNotifications { get; set; } = false;

    /// <summary>
    /// Profile visibility (public, private, friends)
    /// </summary>
    public string ProfileVisibility { get; set; } = "public";

    /// <summary>
    /// Show online status
    /// </summary>
    public bool ShowOnlineStatus { get; set; } = true;

    /// <summary>
    /// Theme preference (light, dark, system)
    /// </summary>
    public string Theme { get; set; } = "system";

    /// <summary>
    /// Date format preference
    /// </summary>
    public string DateFormat { get; set; } = "MM/dd/yyyy";

    /// <summary>
    /// Time format preference (12h, 24h)
    /// </summary>
    public string TimeFormat { get; set; } = "12h";

    /// <summary>
    /// Additional custom preferences as JSON
    /// </summary>
    public string? CustomPreferences { get; set; }
}