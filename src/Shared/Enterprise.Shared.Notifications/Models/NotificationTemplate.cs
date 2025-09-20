using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Notifications.Models;

/// <summary>
/// Notification template
/// </summary>
public class NotificationTemplate
{
    /// <summary>
    /// Template ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Template key
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Language code
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Template name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Subject template
    /// </summary>
    [Required]
    [StringLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// HTML template
    /// </summary>
    public string HtmlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Text template
    /// </summary>
    [Required]
    public string TextTemplate { get; set; } = string.Empty;

    /// <summary>
    /// SMS template (if different from text)
    /// </summary>
    public string? SmsTemplate { get; set; }

    /// <summary>
    /// Push notification title template
    /// </summary>
    [StringLength(200)]
    public string? PushTitleTemplate { get; set; }

    /// <summary>
    /// Push notification body template
    /// </summary>
    [StringLength(500)]
    public string? PushBodyTemplate { get; set; }

    /// <summary>
    /// Template category
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// Template version
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Is default for the key
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Required data fields
    /// </summary>
    public List<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Optional data fields
    /// </summary>
    public List<string> OptionalFields { get; set; } = new();

    /// <summary>
    /// Sample data for testing
    /// </summary>
    public Dictionary<string, object> SampleData { get; set; } = new();

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Created by user ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated by user ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Preview data
    /// </summary>
    public TemplatePreview? Preview { get; set; }
}

/// <summary>
/// Rendered template
/// </summary>
public class RenderedTemplate
{
    /// <summary>
    /// Rendered subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Rendered text content
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// Rendered SMS content
    /// </summary>
    public string? SmsContent { get; set; }

    /// <summary>
    /// Rendered push title
    /// </summary>
    public string? PushTitle { get; set; }

    /// <summary>
    /// Rendered push body
    /// </summary>
    public string? PushBody { get; set; }

    /// <summary>
    /// Rendering timestamp
    /// </summary>
    public DateTime RenderedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template key used
    /// </summary>
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// Language used
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Data used for rendering
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Rendering errors (if any)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings during rendering
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Template preview
/// </summary>
public class TemplatePreview
{
    /// <summary>
    /// Subject preview
    /// </summary>
    public string SubjectPreview { get; set; } = string.Empty;

    /// <summary>
    /// HTML preview
    /// </summary>
    public string HtmlPreview { get; set; } = string.Empty;

    /// <summary>
    /// Text preview
    /// </summary>
    public string TextPreview { get; set; } = string.Empty;

    /// <summary>
    /// SMS preview
    /// </summary>
    public string? SmsPreview { get; set; }

    /// <summary>
    /// Push preview
    /// </summary>
    public string? PushPreview { get; set; }

    /// <summary>
    /// Preview generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Template validation result
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Is valid
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Missing required fields
    /// </summary>
    public List<string> MissingFields { get; set; } = new();

    /// <summary>
    /// Unused fields
    /// </summary>
    public List<string> UnusedFields { get; set; } = new();

    /// <summary>
    /// Validation performed at
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validation summary
    /// </summary>
    public string Summary => IsValid ? "Template is valid" : $"{Errors.Count} error(s), {Warnings.Count} warning(s)";
}

/// <summary>
/// User notification preferences
/// </summary>
public class UserNotificationPreferences
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email enabled
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// SMS enabled
    /// </summary>
    public bool SmsEnabled { get; set; } = true;

    /// <summary>
    /// Push enabled
    /// </summary>
    public bool PushEnabled { get; set; } = true;

    /// <summary>
    /// In-app enabled
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Webhook enabled
    /// </summary>
    public bool WebhookEnabled { get; set; } = false;

    /// <summary>
    /// Preferred language
    /// </summary>
    [StringLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Timezone
    /// </summary>
    [StringLength(50)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Notification type preferences
    /// </summary>
    public Dictionary<NotificationType, NotificationTypePreference> TypePreferences { get; set; } = new();

    /// <summary>
    /// Quiet hours start
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// Quiet hours end
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    /// <summary>
    /// Do not disturb
    /// </summary>
    public bool DoNotDisturb { get; set; } = false;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification type preference
/// </summary>
public class NotificationTypePreference
{
    /// <summary>
    /// Email enabled for this type
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// SMS enabled for this type
    /// </summary>
    public bool SmsEnabled { get; set; } = true;

    /// <summary>
    /// Push enabled for this type
    /// </summary>
    public bool PushEnabled { get; set; } = true;

    /// <summary>
    /// In-app enabled for this type
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Webhook enabled for this type
    /// </summary>
    public bool WebhookEnabled { get; set; } = false;
}