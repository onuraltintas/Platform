namespace EgitimPlatform.Shared.Email.Configuration;

public class EmailOptions
{
    public const string SectionName = "Email";

    public SmtpOptions Smtp { get; set; } = new();
    public EmailSettings Settings { get; set; } = new();
    public TemplateOptions Templates { get; set; } = new();
    public AttachmentOptions Attachments { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
    public ThrottlingOptions Throttling { get; set; } = new();
    public DeliveryOptions Delivery { get; set; } = new();
    public QueueOptions Queue { get; set; } = new();
    public ValidationOptions Validation { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool EnableStartTls { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public bool UseDefaultCredentials { get; set; } = false;
    public string? Domain { get; set; }
    public AuthenticationMethod AuthenticationMethod { get; set; } = AuthenticationMethod.Login;
}

public class EmailSettings
{
    public string DefaultFromEmail { get; set; } = "noreply@egitimplatform.com";
    public string DefaultFromName { get; set; } = "Eğitim Platform";
    public string DefaultReplyToEmail { get; set; } = "support@egitimplatform.com";
    public string DefaultReplyToName { get; set; } = "Eğitim Platform Destek";
    public string DefaultSubjectPrefix { get; set; } = "[Eğitim Platform] ";
    public string DefaultEncoding { get; set; } = "UTF-8";
    public EmailPriority DefaultPriority { get; set; } = EmailPriority.Normal;
    public bool TrackOpens { get; set; } = false;
    public bool TrackClicks { get; set; } = false;
    public bool EnableUnsubscribe { get; set; } = true;
    public string UnsubscribeUrl { get; set; } = "https://egitimplatform.com/unsubscribe";
}

public class TemplateOptions
{
    public bool Enabled { get; set; } = true;
    public string TemplatesPath { get; set; } = "EmailTemplates";
    public TemplateEngine Engine { get; set; } = TemplateEngine.Handlebars;
    public string DefaultLayout { get; set; } = "default";
    public bool CacheTemplates { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 60;
    public Dictionary<string, string> GlobalVariables { get; set; } = new();
    public List<string> AllowedTemplates { get; set; } = new();
}

public class AttachmentOptions
{
    public bool AllowAttachments { get; set; } = true;
    public long MaxAttachmentSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxAttachmentsPerEmail { get; set; } = 10;
    public List<string> AllowedFileExtensions { get; set; } = new()
    {
        ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif"
    };
    public List<string> BlockedFileExtensions { get; set; } = new()
    {
        ".exe", ".bat", ".cmd", ".scr", ".pif", ".com", ".dll"
    };
    public bool ScanForViruses { get; set; } = false;
    public string? VirusScannerUrl { get; set; }
}

public class SecurityOptions
{
    public bool RequireTls { get; set; } = true;
    public bool ValidateCertificates { get; set; } = true;
    public bool LogSensitiveData { get; set; } = false;
    public bool EnableDkim { get; set; } = false;
    public string? DkimPrivateKey { get; set; }
    public string? DkimSelector { get; set; }
    public string? DkimDomain { get; set; }
    public bool EnableSpf { get; set; } = false;
    public bool EnableDmarc { get; set; } = false;
    public List<string> AllowedDomains { get; set; } = new();
    public List<string> BlockedDomains { get; set; } = new();
    public bool RequireSSL { get; set; } = true;
    public int MaxAttachmentSizeMB { get; set; } = 10;
}

public class ThrottlingOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxEmailsPerMinute { get; set; } = 100;
    public int MaxEmailsPerHour { get; set; } = 1000;
    public int MaxEmailsPerDay { get; set; } = 10000;
    public int MaxRecipientsPerEmail { get; set; } = 100;
    public int DelayBetweenEmailsMs { get; set; } = 100;
    public bool EnableBulkEmailMode { get; set; } = false;
    public int BulkEmailBatchSize { get; set; } = 50;
}

public class DeliveryOptions
{
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public RetryBackoffType RetryBackoffType { get; set; } = RetryBackoffType.Exponential;
    public bool EnableDeadLetterQueue { get; set; } = true;
    public string? DeadLetterQueuePath { get; set; }
    public bool EnableDeliveryNotifications { get; set; } = false;
    public string? DeliveryNotificationUrl { get; set; }
    public int DeliveryTimeoutMinutes { get; set; } = 5;
}

public enum AuthenticationMethod
{
    None,
    Login,
    Plain,
    CramMd5,
    DigestMd5,
    OAuth2,
    NtlmIntegrated
}

public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum TemplateEngine
{
    None,
    Handlebars,
    Razor,
    Mustache
}

public enum RetryBackoffType
{
    Linear,
    Exponential,
    Fixed
}

public class QueueOptions
{
    public bool Enabled { get; set; } = true;
    public string QueueName { get; set; } = "email-queue";
    public int MaxQueueSize { get; set; } = 10000;
    public int ProcessingBatchSize { get; set; } = 10;
    public int ProcessingDelayMs { get; set; } = 1000;
    public bool EnablePriority { get; set; } = true;
    public int HighPriorityThreshold { get; set; } = 100;
    public int RetentionDays { get; set; } = 7;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 100;
    public int DeliveryResultRetentionDays { get; set; } = 30;
    public int ProcessingIntervalSeconds { get; set; } = 10;
}

public class ValidationOptions
{
    public bool Enabled { get; set; } = true;
    public bool ValidateEmailFormat { get; set; } = true;
    public bool ValidateDomainMx { get; set; } = false;
    public bool ValidateDisposableEmail { get; set; } = false;
    public bool ValidateBlacklist { get; set; } = true;
    public List<string> BlacklistedDomains { get; set; } = new();
    public List<string> WhitelistedDomains { get; set; } = new();
    public int MaxRecipients { get; set; } = 100;
    public int MaxSubjectLength { get; set; } = 255;
    public int MaxBodyLength { get; set; } = 1024 * 1024; // 1MB
    public string? DisposableEmailApiUrl { get; set; }
    public bool ValidateDomains { get; set; } = true;
    public bool BlockDisposableEmails { get; set; } = false;
    public EmailValidationLevel ValidationLevel { get; set; } = EmailValidationLevel.Basic;
    public List<string> BlockedDomains { get; set; } = new();
}

public enum EmailValidationLevel
{
    None = 0,
    Basic = 1,
    Standard = 2,
    Strict = 3
}