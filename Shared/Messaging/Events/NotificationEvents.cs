namespace EgitimPlatform.Shared.Messaging.Events;

public class EmailNotificationRequestedEvent : IntegrationEvent
{
    public EmailNotificationRequestedEvent(
        string recipient, 
        string subject, 
        string body, 
        string templateName, 
        Dictionary<string, object> templateData)
    {
        Recipient = recipient;
        Subject = subject;
        Body = body;
        TemplateName = templateName;
        TemplateData = templateData;
        Priority = NotificationPriority.Normal;
    }

    public string Recipient { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public string TemplateName { get; private set; }
    public Dictionary<string, object> TemplateData { get; private set; }
    public NotificationPriority Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public List<string> Attachments { get; set; } = new();
}

public class PushNotificationRequestedEvent : IntegrationEvent
{
    public PushNotificationRequestedEvent(
        string userId, 
        string title, 
        string message, 
        string type)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        Priority = NotificationPriority.Normal;
    }

    public string UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string Type { get; private set; }
    public NotificationPriority Priority { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? ActionUrl { get; set; }
    public string? IconUrl { get; set; }
}

public class SmsNotificationRequestedEvent : IntegrationEvent
{
    public SmsNotificationRequestedEvent(string phoneNumber, string message)
    {
        PhoneNumber = phoneNumber;
        Message = message;
        Priority = NotificationPriority.Normal;
    }

    public string PhoneNumber { get; private set; }
    public string Message { get; private set; }
    public NotificationPriority Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class NotificationSentEvent : IntegrationEvent
{
    public NotificationSentEvent(
        string notificationId, 
        string type, 
        string recipient, 
        bool success, 
        string? errorMessage = null)
    {
        NotificationId = notificationId;
        Type = type;
        Recipient = recipient;
        Success = success;
        ErrorMessage = errorMessage;
    }

    public string NotificationId { get; private set; }
    public string Type { get; private set; }
    public string Recipient { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}