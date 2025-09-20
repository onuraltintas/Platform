namespace Enterprise.Shared.Email.Interfaces;

/// <summary>
/// Core email service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a single email message
    /// </summary>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple emails in bulk with batching support
    /// </summary>
    Task<BulkEmailResult> SendBulkAsync(BulkEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template
    /// </summary>
    Task<EmailResult> SendTemplateAsync(string templateName, string to, object templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template with additional options
    /// </summary>
    Task<EmailResult> SendTemplateAsync(string templateName, string to, object templateData, EmailOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules an email for future delivery
    /// </summary>
    Task<EmailResult> ScheduleAsync(EmailMessage message, DateTime scheduledAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a sent email by tracking ID
    /// </summary>
    Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string trackingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an email message before sending
    /// </summary>
    Task<ValidationResult> ValidateEmailAsync(EmailMessage message);

    /// <summary>
    /// Tests email connectivity and configuration
    /// </summary>
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Additional email options for template-based emails
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// Sender email address override
    /// </summary>
    [EmailAddress]
    public string? From { get; set; }

    /// <summary>
    /// Sender display name override
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Reply-to address override
    /// </summary>
    [EmailAddress]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Email priority override
    /// </summary>
    public EmailPriority? Priority { get; set; }

    /// <summary>
    /// Custom headers to add
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Tags to apply to the email
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Attachments to include
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();
}

/// <summary>
/// Email validation result
/// </summary>
public class ValidationResult : Result
{
    /// <summary>
    /// Validation warnings (non-blocking issues)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    protected ValidationResult(bool isSuccess, string error, OperationStatus status = OperationStatus.Success) 
        : base(isSuccess, error, status)
    {
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success(List<string>? warnings = null)
    {
        return new ValidationResult(true, string.Empty)
        {
            Warnings = warnings ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        var primaryError = errorList.FirstOrDefault() ?? "Validation failed";
        var result = new ValidationResult(false, primaryError, OperationStatus.Failed);
        result.Errors.Clear();
        result.Errors.AddRange(errorList);
        return result;
    }
}