using EgitimPlatform.Shared.Email.Configuration;

namespace EgitimPlatform.Shared.Email.Models;

public class EmailMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    
    // From
    public EmailAddress From { get; set; } = new();
    
    // Recipients
    public List<EmailAddress> To { get; set; } = new();
    public List<EmailAddress> Cc { get; set; } = new();
    public List<EmailAddress> Bcc { get; set; } = new();
    public EmailAddress? ReplyTo { get; set; }
    
    // Attachments
    public List<EmailAttachment> Attachments { get; set; } = new();
    
    // Headers
    public Dictionary<string, string> Headers { get; set; } = new();
    
    // Template
    public string? TemplateName { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    
    // Tracking
    public bool TrackOpens { get; set; }
    public bool TrackClicks { get; set; }
    public string? TrackingId { get; set; }
    
    // Metadata
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    
    // Delivery
    public EmailDeliveryOptions? DeliveryOptions { get; set; }
    
    public EmailMessage() { }
    
    public EmailMessage(string to, string subject, string body, bool isHtml = true)
    {
        To.Add(new EmailAddress(to));
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }
    
    public EmailMessage(List<string> to, string subject, string body, bool isHtml = true)
    {
        To.AddRange(to.Select(email => new EmailAddress(email)));
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }
    
    // Fluent methods
    public EmailMessage AddTo(string email, string? name = null)
    {
        To.Add(new EmailAddress(email, name));
        return this;
    }
    
    public EmailMessage AddCc(string email, string? name = null)
    {
        Cc.Add(new EmailAddress(email, name));
        return this;
    }
    
    public EmailMessage AddBcc(string email, string? name = null)
    {
        Bcc.Add(new EmailAddress(email, name));
        return this;
    }
    
    public EmailMessage SetFrom(string email, string? name = null)
    {
        From = new EmailAddress(email, name);
        return this;
    }
    
    public EmailMessage SetReplyTo(string email, string? name = null)
    {
        ReplyTo = new EmailAddress(email, name);
        return this;
    }
    
    public EmailMessage AddAttachment(EmailAttachment attachment)
    {
        Attachments.Add(attachment);
        return this;
    }
    
    public EmailMessage AddAttachment(string fileName, byte[] content, string? contentType = null)
    {
        Attachments.Add(new EmailAttachment(fileName, content, contentType));
        return this;
    }
    
    public EmailMessage AddHeader(string name, string value)
    {
        Headers[name] = value;
        return this;
    }
    
    public EmailMessage SetTemplate(string templateName, object templateData)
    {
        TemplateName = templateName;
        TemplateData = ConvertToTemplateData(templateData);
        return this;
    }
    
    public EmailMessage SetPriority(EmailPriority priority)
    {
        Priority = priority;
        return this;
    }
    
    public EmailMessage SetCategory(string category)
    {
        Category = category;
        return this;
    }
    
    public EmailMessage AddTag(string tag)
    {
        Tags.Add(tag);
        return this;
    }
    
    public EmailMessage AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
    
    public EmailMessage EnableTracking(bool trackOpens = true, bool trackClicks = true)
    {
        TrackOpens = trackOpens;
        TrackClicks = trackClicks;
        TrackingId = Guid.NewGuid().ToString();
        return this;
    }
    
    public EmailMessage ScheduleFor(DateTime scheduledAt)
    {
        ScheduledAt = scheduledAt;
        return this;
    }
    
    private Dictionary<string, object> ConvertToTemplateData(object data)
    {
        if (data is Dictionary<string, object> dict)
        {
            return dict;
        }
        
        var result = new Dictionary<string, object>();
        var properties = data.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(data);
            if (value != null)
            {
                result[property.Name] = value;
            }
        }
        
        return result;
    }
}

public class EmailAddress
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    
    public EmailAddress() { }
    
    public EmailAddress(string email, string? name = null)
    {
        Email = email;
        Name = name;
    }
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? Email : $"{Name} <{Email}>";
    }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string? ContentType { get; set; }
    public string? ContentId { get; set; }
    public bool IsInline { get; set; }
    public long Size => Content.Length;
    
    public EmailAttachment() { }
    
    public EmailAttachment(string fileName, byte[] content, string? contentType = null)
    {
        FileName = fileName;
        Content = content;
        ContentType = contentType ?? GetContentTypeFromFileName(fileName);
    }
    
    public EmailAttachment(string fileName, Stream contentStream, string? contentType = null)
    {
        FileName = fileName;
        using var memoryStream = new MemoryStream();
        contentStream.CopyTo(memoryStream);
        Content = memoryStream.ToArray();
        ContentType = contentType ?? GetContentTypeFromFileName(fileName);
    }
    
    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

public class EmailDeliveryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);
    public DateTime? DeliveryNotBefore { get; set; }
    public DateTime? DeliveryNotAfter { get; set; }
    public bool RequireDeliveryNotification { get; set; }
    public string? DeliveryNotificationUrl { get; set; }
}