namespace EgitimPlatform.Shared.Email.Models;

public class EmailDeliveryResult
{
    public string MessageId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.Pending;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<EmailRecipientResult> RecipientResults { get; set; } = new();
    public EmailProviderInfo? ProviderInfo { get; set; }
    
    public static EmailDeliveryResult Success(string messageId, DateTime? deliveredAt = null)
    {
        return new EmailDeliveryResult
        {
            MessageId = messageId,
            IsSuccess = true,
            Status = EmailDeliveryStatus.Delivered,
            DeliveredAt = deliveredAt ?? DateTime.UtcNow
        };
    }
    
    public static EmailDeliveryResult Failure(string messageId, string errorMessage, Exception? exception = null)
    {
        return new EmailDeliveryResult
        {
            MessageId = messageId,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Status = EmailDeliveryStatus.Failed
        };
    }
    
    public static EmailDeliveryResult Pending(string messageId)
    {
        return new EmailDeliveryResult
        {
            MessageId = messageId,
            IsSuccess = false,
            Status = EmailDeliveryStatus.Pending
        };
    }
    
    public static EmailDeliveryResult Queued(string messageId)
    {
        return new EmailDeliveryResult
        {
            MessageId = messageId,
            IsSuccess = false,
            Status = EmailDeliveryStatus.Queued
        };
    }
}

public class EmailRecipientResult
{
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public string? TrackingId { get; set; }
}

public class EmailProviderInfo
{
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public Dictionary<string, object> ProviderData { get; set; } = new();
    public DateTime? ProviderTimestamp { get; set; }
}

public enum EmailDeliveryStatus
{
    Pending = 0,
    Queued = 1,
    Sending = 2,
    Delivered = 3,
    Failed = 4,
    Bounced = 5,
    Rejected = 6,
    Spam = 7,
    Unsubscribed = 8,
    Deferred = 9
}

public class BulkEmailResult
{
    public int TotalEmails { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public List<EmailDeliveryResult> Results { get; set; } = new();
    public TimeSpan TotalProcessingTime { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public double SuccessRate => TotalEmails > 0 ? (double)SuccessfulDeliveries / TotalEmails * 100 : 0;
    public bool IsCompleted => CompletedAt.HasValue;
}

public class EmailDeliveryStatistics
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPending { get; set; }
    public int TotalBounced { get; set; }
    public int TotalSpam { get; set; }
    public int TotalUnsubscribed { get; set; }
    
    public Dictionary<string, int> DeliveryByProvider { get; set; } = new();
    public Dictionary<string, int> DeliveryByCategory { get; set; } = new();
    public Dictionary<DateTime, int> DeliveryByDate { get; set; } = new();
    
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
    public double BounceRate => TotalSent > 0 ? (double)TotalBounced / TotalSent * 100 : 0;
    public double SpamRate => TotalSent > 0 ? (double)TotalSpam / TotalSent * 100 : 0;
}