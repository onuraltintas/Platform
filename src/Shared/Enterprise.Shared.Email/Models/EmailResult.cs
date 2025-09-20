namespace Enterprise.Shared.Email.Models;

/// <summary>
/// Represents the result of an email sending operation
/// </summary>
public class EmailResult : Result
{
    /// <summary>
    /// Unique tracking identifier for the email
    /// </summary>
    public string? TrackingId { get; set; }

    /// <summary>
    /// Message ID assigned by the email server
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// When the email was sent
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Provider-specific response data
    /// </summary>
    public Dictionary<string, object> ProviderData { get; set; } = new();

    /// <summary>
    /// Delivery status information
    /// </summary>
    public EmailDeliveryStatus DeliveryStatus { get; set; } = EmailDeliveryStatus.Sent;

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }

    public EmailResult() : base(true, string.Empty) { }

    protected EmailResult(bool isSuccess, string error, OperationStatus status = OperationStatus.Success) 
        : base(isSuccess, error, status)
    {
    }

    /// <summary>
    /// Creates a successful email result
    /// </summary>
    public static EmailResult Success(string trackingId, string? messageId = null)
    {
        return new EmailResult(true, string.Empty)
        {
            TrackingId = trackingId,
            MessageId = messageId,
            SentAt = DateTime.UtcNow,
            DeliveryStatus = EmailDeliveryStatus.Sent
        };
    }

    /// <summary>
    /// Creates a failed email result
    /// </summary>
    public static EmailResult Failure(string message, string? trackingId = null)
    {
        return new EmailResult(false, message, OperationStatus.Failed)
        {
            TrackingId = trackingId,
            DeliveryStatus = EmailDeliveryStatus.Failed
        };
    }

    /// <summary>
    /// Creates a failed email result with multiple errors
    /// </summary>
    public static EmailResult Failure(IEnumerable<string> errors, string? trackingId = null)
    {
        var errorList = errors.ToList();
        var primaryError = errorList.FirstOrDefault() ?? "Operation failed";
        
        var result = new EmailResult(false, primaryError, OperationStatus.Failed)
        {
            TrackingId = trackingId,
            DeliveryStatus = EmailDeliveryStatus.Failed
        };
        result.Errors.Clear();
        result.Errors.AddRange(errorList);
        return result;
    }

    /// <summary>
    /// Creates a queued email result
    /// </summary>
    public static EmailResult Queued(string trackingId)
    {
        return new EmailResult(true, string.Empty)
        {
            TrackingId = trackingId,
            DeliveryStatus = EmailDeliveryStatus.Queued,
            SentAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Email delivery status enumeration
/// </summary>
public enum EmailDeliveryStatus
{
    /// <summary>
    /// Email has been sent to the mail server
    /// </summary>
    Sent,

    /// <summary>
    /// Email has been queued for later delivery
    /// </summary>
    Queued,

    /// <summary>
    /// Email delivery failed
    /// </summary>
    Failed,

    /// <summary>
    /// Email was delivered successfully
    /// </summary>
    Delivered,

    /// <summary>
    /// Email bounced back
    /// </summary>
    Bounced,

    /// <summary>
    /// Email was marked as spam
    /// </summary>
    Spam,

    /// <summary>
    /// Recipient unsubscribed
    /// </summary>
    Unsubscribed
}