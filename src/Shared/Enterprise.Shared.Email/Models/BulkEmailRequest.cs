namespace Enterprise.Shared.Email.Models;

/// <summary>
/// Represents a bulk email sending request
/// </summary>
public class BulkEmailRequest
{
    /// <summary>
    /// Template to use for all emails
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Common subject for all emails (if not using template)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Common body for all emails (if not using template)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Indicates if the body/template contains HTML
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// List of recipients with their personalized data
    /// </summary>
    [Required]
    public List<BulkEmailRecipient> Recipients { get; set; } = new();

    /// <summary>
    /// Common sender email (optional)
    /// </summary>
    [EmailAddress]
    public string? From { get; set; }

    /// <summary>
    /// Common sender display name (optional)
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Common reply-to address (optional)
    /// </summary>
    [EmailAddress]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Priority for all emails
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Scheduled delivery time (optional)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Tags to apply to all emails
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Common metadata for all emails
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Maximum number of concurrent email sends
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Delay between batch sends (in milliseconds)
    /// </summary>
    [Range(0, 60000)]
    public int DelayBetweenBatches { get; set; } = 1000;

    /// <summary>
    /// Batch size for sending emails
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Unique identifier for this bulk operation
    /// </summary>
    public string BulkId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Validates the bulk email request
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (Recipients == null || Recipients.Count == 0)
        {
            errors.Add("At least one recipient is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TemplateName) && 
            (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Body)))
        {
            errors.Add("Either template name or subject and body must be provided");
        }

        // Validate recipients
        for (int i = 0; i < Recipients.Count; i++)
        {
            if (!Recipients[i].IsValid(out var recipientErrors))
            {
                errors.AddRange(recipientErrors.Select(e => $"Recipient {i + 1}: {e}"));
            }
        }

        return errors.Count == 0;
    }
}

/// <summary>
/// Represents a recipient in a bulk email operation
/// </summary>
public class BulkEmailRecipient
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Personalized data for template substitution
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Recipient-specific subject (overrides common subject)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Recipient-specific metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Unique tracking ID for this recipient
    /// </summary>
    public string TrackingId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Validates the recipient
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add("Email address is required");
        }
        else if (!IsValidEmail(Email))
        {
            errors.Add("Invalid email address format");
        }

        return errors.Count == 0;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents the result of a bulk email operation
/// </summary>
public class BulkEmailResult : Result
{
    /// <summary>
    /// Unique identifier for this bulk operation
    /// </summary>
    public string BulkId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of emails to send
    /// </summary>
    public int TotalEmails { get; set; }

    /// <summary>
    /// Number of successfully sent emails
    /// </summary>
    public int SuccessfulSends { get; set; }

    /// <summary>
    /// Number of failed email sends
    /// </summary>
    public int FailedSends { get; set; }

    /// <summary>
    /// Individual email results
    /// </summary>
    public List<EmailResult> Results { get; set; } = new();

    /// <summary>
    /// When the bulk operation started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the bulk operation completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total time taken for the operation
    /// </summary>
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalEmails > 0 ? (double)SuccessfulSends / TotalEmails * 100 : 0;

    public BulkEmailResult() : base(true, string.Empty) { }

    protected BulkEmailResult(bool isSuccess, string error, OperationStatus status = OperationStatus.Success) 
        : base(isSuccess, error, status)
    {
    }

    /// <summary>
    /// Creates a successful bulk email result
    /// </summary>
    public static BulkEmailResult Success(string bulkId, List<EmailResult> results)
    {
        var successful = results.Count(r => r.IsSuccess);
        var failed = results.Count - successful;

        return new BulkEmailResult(failed == 0, failed > 0 ? $"{failed} emails failed" : string.Empty)
        {
            BulkId = bulkId,
            TotalEmails = results.Count,
            SuccessfulSends = successful,
            FailedSends = failed,
            Results = results,
            CompletedAt = DateTime.UtcNow
        };
    }
}