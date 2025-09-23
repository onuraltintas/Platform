using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserPreference
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public bool MarketingEmailsConsent { get; set; }

    public bool DataProcessingConsent { get; set; }

    public DateTime? ConsentGivenAt { get; set; }

    public bool EmailNotifications { get; set; }

    public bool PushNotifications { get; set; }

    public bool SmsNotifications { get; set; }

    public string ProfileVisibility { get; set; } = null!;

    public bool ShowOnlineStatus { get; set; }

    public string Theme { get; set; } = null!;

    public string DateFormat { get; set; } = null!;

    public string TimeFormat { get; set; } = null!;

    public string? CustomPreferences { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
