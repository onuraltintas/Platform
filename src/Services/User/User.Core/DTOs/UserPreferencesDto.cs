namespace User.Core.DTOs;

/// <summary>
/// User preferences data transfer object
/// </summary>
public class UserPreferencesDto
{
    /// <summary>
    /// Marketing emails consent
    /// </summary>
    public bool MarketingEmailsConsent { get; set; }

    /// <summary>
    /// Data processing consent for GDPR
    /// </summary>
    public bool DataProcessingConsent { get; set; }

    /// <summary>
    /// Marketing consent for GDPR
    /// </summary>
    public bool MarketingConsent { get; set; }

    /// <summary>
    /// Analytics consent for GDPR  
    /// </summary>
    public bool AnalyticsConsent { get; set; }

    /// <summary>
    /// Two-factor authentication enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// When consent was given
    /// </summary>
    public DateTime? ConsentGivenAt { get; set; }

    /// <summary>
    /// Email notifications enabled
    /// </summary>
    public bool EmailNotifications { get; set; }

    /// <summary>
    /// Push notifications enabled
    /// </summary>
    public bool PushNotifications { get; set; }

    /// <summary>
    /// SMS notifications enabled
    /// </summary>
    public bool SmsNotifications { get; set; }

    /// <summary>
    /// Profile visibility
    /// </summary>
    public string ProfileVisibility { get; set; } = string.Empty;

    /// <summary>
    /// Show online status
    /// </summary>
    public bool ShowOnlineStatus { get; set; }

    /// <summary>
    /// Theme preference
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// Date format preference
    /// </summary>
    public string DateFormat { get; set; } = string.Empty;

    /// <summary>
    /// Time format preference
    /// </summary>
    public string TimeFormat { get; set; } = string.Empty;

    /// <summary>
    /// Custom preferences
    /// </summary>
    public Dictionary<string, object>? CustomPreferences { get; set; }
}