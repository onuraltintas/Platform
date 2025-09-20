using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Enterprise.Shared.Notifications.Models;

/// <summary>
/// Base notification model
/// </summary>
public abstract class NotificationBase
{
    /// <summary>
    /// Notification ID
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Email notification model
/// </summary>
public class EmailNotification : NotificationBase
{
    /// <summary>
    /// Email subject
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML content
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Plain text content
    /// </summary>
    [Required]
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// To email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// To name
    /// </summary>
    [StringLength(100)]
    public string? ToName { get; set; }

    /// <summary>
    /// CC recipients
    /// </summary>
    public List<string> CcEmails { get; set; } = new();

    /// <summary>
    /// BCC recipients
    /// </summary>
    public List<string> BccEmails { get; set; } = new();

    /// <summary>
    /// Attachments
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Email priority
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// File name
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File content
    /// </summary>
    [Required]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Is inline attachment
    /// </summary>
    public bool IsInline { get; set; } = false;

    /// <summary>
    /// Content ID for inline attachments
    /// </summary>
    [StringLength(50)]
    public string? ContentId { get; set; }
}

/// <summary>
/// Email priorities
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}

/// <summary>
/// SMS notification model
/// </summary>
public class SmsNotification : NotificationBase
{
    /// <summary>
    /// SMS message
    /// </summary>
    [Required]
    [StringLength(1600)] // Support for long SMS
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// To phone number
    /// </summary>
    [Required]
    [Phone]
    public string ToPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// From phone number
    /// </summary>
    [Phone]
    public string? FromPhoneNumber { get; set; }

    /// <summary>
    /// SMS type
    /// </summary>
    public SmsType Type { get; set; } = SmsType.Transactional;

    /// <summary>
    /// Enable delivery report
    /// </summary>
    public bool EnableDeliveryReport { get; set; } = true;
}

/// <summary>
/// SMS types
/// </summary>
public enum SmsType
{
    Transactional,
    Promotional,
    OTP,
    Alert
}

/// <summary>
/// Push notification model
/// </summary>
public class PushNotification : NotificationBase
{
    /// <summary>
    /// Notification title
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Icon URL
    /// </summary>
    [Url]
    public string? Icon { get; set; }

    /// <summary>
    /// Image URL
    /// </summary>
    [Url]
    public string? Image { get; set; }

    /// <summary>
    /// Click action URL
    /// </summary>
    [Url]
    public string? ClickAction { get; set; }

    /// <summary>
    /// Badge count
    /// </summary>
    public int? Badge { get; set; }

    /// <summary>
    /// Sound file
    /// </summary>
    [StringLength(50)]
    public string? Sound { get; set; } = "default";

    /// <summary>
    /// Custom data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Device tokens
    /// </summary>
    public List<string> DeviceTokens { get; set; } = new();

    /// <summary>
    /// Topic (for topic-based messaging)
    /// </summary>
    [StringLength(100)]
    public string? Topic { get; set; }

    /// <summary>
    /// Time to live in seconds
    /// </summary>
    public int? TimeToLive { get; set; }

    /// <summary>
    /// Collapse key for message grouping
    /// </summary>
    [StringLength(50)]
    public string? CollapseKey { get; set; }
}

/// <summary>
/// In-app notification model
/// </summary>
public class InAppNotification : NotificationBase
{
    /// <summary>
    /// Notification title
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification content
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Notification type
    /// </summary>
    public InAppNotificationType Type { get; set; } = InAppNotificationType.Info;

    /// <summary>
    /// Priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Action URL
    /// </summary>
    [Url]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action text
    /// </summary>
    [StringLength(50)]
    public string? ActionText { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Is read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Read at timestamp
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Expires at
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// In-app notification types
/// </summary>
public enum InAppNotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Announcement
}

/// <summary>
/// Webhook notification model
/// </summary>
public class WebhookNotification : NotificationBase
{
    /// <summary>
    /// Webhook URL
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Payload
    /// </summary>
    [Required]
    public object Payload { get; set; } = new();

    /// <summary>
    /// Content type
    /// </summary>
    [StringLength(100)]
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Secret for signature validation
    /// </summary>
    [StringLength(200)]
    public string? Secret { get; set; }

    /// <summary>
    /// Signature header name
    /// </summary>
    [StringLength(50)]
    public string SignatureHeader { get; set; } = "X-Signature";
}

/// <summary>
/// Notification history
/// </summary>
public class NotificationHistory
{
    /// <summary>
    /// History ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Notification ID
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Channel
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public NotificationStatus Status { get; set; }

    /// <summary>
    /// Delivery status
    /// </summary>
    public DeliveryStatus DeliveryStatus { get; set; }

    /// <summary>
    /// Subject/Title
    /// </summary>
    [StringLength(200)]
    public string? Subject { get; set; }

    /// <summary>
    /// Content preview
    /// </summary>
    [StringLength(500)]
    public string? ContentPreview { get; set; }

    /// <summary>
    /// Template key
    /// </summary>
    [StringLength(100)]
    public string? TemplateKey { get; set; }

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sent at
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Delivered at
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Failed at
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Retry count
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Next retry at
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Metadata
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Serialized metadata for storage
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? MetadataJson
    {
        get => Metadata.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(Metadata) : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(value) ?? new();
                }
                catch
                {
                    Metadata = new();
                }
            }
        }
    }
}