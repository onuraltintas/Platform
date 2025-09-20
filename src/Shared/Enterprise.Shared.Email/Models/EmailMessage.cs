namespace Enterprise.Shared.Email.Models;

/// <summary>
/// Represents an email message with all necessary properties for sending
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Primary recipient email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Carbon copy recipients
    /// </summary>
    public string[]? Cc { get; set; }

    /// <summary>
    /// Blind carbon copy recipients
    /// </summary>
    public string[]? Bcc { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [StringLength(998)] // RFC 5322 limit
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the body contains HTML content
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// Sender email address (optional, uses default if not provided)
    /// </summary>
    [EmailAddress]
    public string? From { get; set; }

    /// <summary>
    /// Sender display name
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Reply-to email address
    /// </summary>
    [EmailAddress]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Custom email headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Email priority level
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Scheduled delivery time (optional for future sending)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Unique tracking identifier for this email
    /// </summary>
    public string TrackingId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Additional metadata for the email
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Email attachments
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Template data for template-based emails
    /// </summary>
    public object? TemplateData { get; set; }

    /// <summary>
    /// Tags for email categorization and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// When the email was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a simple email message
    /// </summary>
    public static EmailMessage Create(string to, string subject, string body, bool isHtml = true)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        };
    }

    /// <summary>
    /// Adds a tag to the email
    /// </summary>
    public EmailMessage AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
        return this;
    }

    /// <summary>
    /// Adds metadata to the email
    /// </summary>
    public EmailMessage AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a header to the email
    /// </summary>
    public EmailMessage AddHeader(string name, string value)
    {
        Headers[name] = value;
        return this;
    }

    /// <summary>
    /// Adds an attachment to the email
    /// </summary>
    public EmailMessage AddAttachment(EmailAttachment attachment)
    {
        Attachments.Add(attachment);
        return this;
    }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority
    /// </summary>
    High = 3,

    /// <summary>
    /// Urgent priority
    /// </summary>
    Urgent = 4
}