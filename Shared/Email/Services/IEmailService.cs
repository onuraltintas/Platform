using EgitimPlatform.Shared.Email.Models;

namespace EgitimPlatform.Shared.Email.Services;

public interface IEmailService
{
    Task<EmailDeliveryResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<BulkEmailResult> SendBulkEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
    
    Task<EmailDeliveryResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task<EmailDeliveryResult> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    
    Task<EmailDeliveryResult> SendTemplateEmailAsync(string to, string templateName, object templateData, string? subject = null, CancellationToken cancellationToken = default);
    Task<EmailDeliveryResult> SendTemplateEmailAsync(IEnumerable<string> to, string templateName, object templateData, string? subject = null, CancellationToken cancellationToken = default);
    
    Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<EmailDeliveryStatistics> GetDeliveryStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object templateData, CancellationToken cancellationToken = default);
    Task<string> RenderTemplateFromContentAsync(string templateContent, object templateData, CancellationToken cancellationToken = default);
    Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);
    Task ClearTemplateCacheAsync(string? templateName = null, CancellationToken cancellationToken = default);
}

public interface IEmailQueueService
{
    Task<string> QueueEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<BulkEmailResult> QueueBulkEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
    Task<EmailDeliveryResult> GetDeliveryStatusAsync(string messageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmailDeliveryResult>> GetPendingEmailsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
}

public interface IEmailValidationService
{
    Task<bool> IsValidEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<EmailValidationResult> ValidateEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmailValidationResult>> ValidateEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);
    Task<bool> IsDomainValidAsync(string domain, CancellationToken cancellationToken = default);
    Task<bool> IsDisposableEmailAsync(string email, CancellationToken cancellationToken = default);
}

public class EmailValidationResult
{
    public string Email { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsDeliverable { get; set; }
    public bool IsDisposable { get; set; }
    public bool IsCatchAll { get; set; }
    public bool HasMxRecord { get; set; }
    public string? Domain { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}