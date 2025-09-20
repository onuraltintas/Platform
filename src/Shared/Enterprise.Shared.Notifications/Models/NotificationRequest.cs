using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Notifications.Models;

/// <summary>
/// Validation attribute to ensure Guid is not empty
/// </summary>
public class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is Guid guid)
        {
            return guid != Guid.Empty;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} cannot be empty.";
    }
}

/// <summary>
/// Notification request model
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Unique notification identifier
    /// </summary>
    public Guid NotificationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Target user ID
    /// </summary>
    [Required]
    [NotEmptyGuid]
    public Guid UserId { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Delivery channels
    /// </summary>
    [Required]
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Template key for rendering
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// Template data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Scheduled delivery time
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Custom message (overrides template)
    /// </summary>
    [StringLength(1000)]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Custom subject (overrides template)
    /// </summary>
    [StringLength(200)]
    public string? Subject { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Bulk notification request
/// </summary>
public class BulkNotificationRequest
{
    /// <summary>
    /// Target user IDs
    /// </summary>
    [Required]
    public Guid[] UserIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Notification type
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Delivery channels
    /// </summary>
    [Required]
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Template key for rendering
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// Template data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Scheduled notification request
/// </summary>
public class ScheduledNotificationRequest : NotificationRequest
{
    /// <summary>
    /// Scheduled delivery time (required for scheduled notifications)
    /// </summary>
    [Required]
    public new DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Timezone for scheduling
    /// </summary>
    [StringLength(50)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Recurrence pattern (optional)
    /// </summary>
    public RecurrencePattern? Recurrence { get; set; }
}

/// <summary>
/// Recurrence pattern for scheduled notifications
/// </summary>
public class RecurrencePattern
{
    /// <summary>
    /// Recurrence type
    /// </summary>
    public RecurrenceType Type { get; set; }

    /// <summary>
    /// Interval between occurrences
    /// </summary>
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Days of week (for weekly recurrence)
    /// </summary>
    public DayOfWeek[]? DaysOfWeek { get; set; }

    /// <summary>
    /// Day of month (for monthly recurrence)
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// End date for recurrence
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum occurrences
    /// </summary>
    public int? MaxOccurrences { get; set; }
}

/// <summary>
/// Recurrence types
/// </summary>
public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}