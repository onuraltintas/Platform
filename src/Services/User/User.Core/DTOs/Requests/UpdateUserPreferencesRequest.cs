namespace User.Core.DTOs.Requests;

/// <summary>
/// Request to update user preferences
/// </summary>
public class UpdateUserPreferencesRequest
{
    /// <summary>
    /// Marketing emails consent
    /// </summary>
    public bool? MarketingEmailsConsent { get; set; }

    /// <summary>
    /// Email notifications enabled
    /// </summary>
    public bool? EmailNotifications { get; set; }

    /// <summary>
    /// Push notifications enabled
    /// </summary>
    public bool? PushNotifications { get; set; }

    /// <summary>
    /// SMS notifications enabled
    /// </summary>
    public bool? SmsNotifications { get; set; }

    /// <summary>
    /// Profile visibility
    /// </summary>
    public string? ProfileVisibility { get; set; }

    /// <summary>
    /// Show online status
    /// </summary>
    public bool? ShowOnlineStatus { get; set; }

    /// <summary>
    /// Theme preference
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Date format preference
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Time format preference
    /// </summary>
    public string? TimeFormat { get; set; }

    /// <summary>
    /// Custom preferences to update
    /// </summary>
    public Dictionary<string, object>? CustomPreferences { get; set; }
}