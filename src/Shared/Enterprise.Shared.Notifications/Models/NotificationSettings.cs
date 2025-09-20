using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Notifications.Models;

/// <summary>
/// Notification settings configuration
/// </summary>
public class NotificationSettings
{
    public const string SectionName = "NotificationSettings";

    /// <summary>
    /// Email settings
    /// </summary>
    public EmailSettings Email { get; set; } = new();

    /// <summary>
    /// SMS settings
    /// </summary>
    public SmsSettings SMS { get; set; } = new();

    /// <summary>
    /// Push notification settings
    /// </summary>
    public PushSettings Push { get; set; } = new();

    /// <summary>
    /// SignalR settings
    /// </summary>
    public SignalRSettings SignalR { get; set; } = new();

    /// <summary>
    /// Template settings
    /// </summary>
    public TemplateSettings Templates { get; set; } = new();

    /// <summary>
    /// Delivery settings
    /// </summary>
    public DeliverySettings Delivery { get; set; } = new();

    /// <summary>
    /// General settings
    /// </summary>
    public GeneralSettings General { get; set; } = new();
}

/// <summary>
/// Email settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP server address
    /// </summary>
    [Required]
    public string SmtpServer { get; set; } = "localhost";

    /// <summary>
    /// SMTP port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Use SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// From name
    /// </summary>
    [Required]
    public string FromName { get; set; } = "Enterprise Platform";

    /// <summary>
    /// From email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = "noreply@enterprise.com";

    /// <summary>
    /// Reply-to email address
    /// </summary>
    [EmailAddress]
    public string? ReplyToEmail { get; set; }

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable email tracking
    /// </summary>
    public bool EnableTracking { get; set; } = true;
}

/// <summary>
/// SMS settings
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// SMS provider (Twilio, etc.)
    /// </summary>
    public string Provider { get; set; } = "Twilio";

    /// <summary>
    /// Twilio Account SID
    /// </summary>
    public string TwilioAccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token
    /// </summary>
    public string TwilioAuthToken { get; set; } = string.Empty;

    /// <summary>
    /// From phone number
    /// </summary>
    public string TwilioFromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Enable SMS delivery reports
    /// </summary>
    public bool EnableDeliveryReports { get; set; } = true;

    /// <summary>
    /// Message length limit
    /// </summary>
    public int MaxMessageLength { get; set; } = 160;
}

/// <summary>
/// Push notification settings
/// </summary>
public class PushSettings
{
    /// <summary>
    /// Firebase settings
    /// </summary>
    public FirebaseSettings Firebase { get; set; } = new();
}

/// <summary>
/// Firebase settings
/// </summary>
public class FirebaseSettings
{
    /// <summary>
    /// Firebase project ID
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Firebase credentials file path
    /// </summary>
    public string CredentialsPath { get; set; } = string.Empty;

    /// <summary>
    /// Firebase credentials JSON content
    /// </summary>
    public string? CredentialsJson { get; set; }
}

/// <summary>
/// SignalR settings
/// </summary>
public class SignalRSettings
{
    /// <summary>
    /// Hub endpoint path
    /// </summary>
    public string HubEndpoint { get; set; } = "/notificationHub";

    /// <summary>
    /// Enable detailed errors
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Keep alive interval in seconds
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Template settings
/// </summary>
public class TemplateSettings
{
    /// <summary>
    /// Default language
    /// </summary>
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    /// Enable template caching
    /// </summary>
    public bool TemplateCache { get; set; } = true;

    /// <summary>
    /// Template cache duration in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Template directory path
    /// </summary>
    public string TemplateDirectory { get; set; } = "Templates";

    /// <summary>
    /// Supported languages
    /// </summary>
    public string[] SupportedLanguages { get; set; } = { "en-US", "tr-TR" };

    /// <summary>
    /// Template file extensions
    /// </summary>
    public string[] FileExtensions { get; set; } = { ".liquid", ".html" };
}

/// <summary>
/// Delivery settings
/// </summary>
public class DeliverySettings
{
    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delays in minutes
    /// </summary>
    public int[] RetryDelayMinutes { get; set; } = { 1, 5, 15 };

    /// <summary>
    /// Enable delivery tracking
    /// </summary>
    public bool EnableDeliveryTracking { get; set; } = true;

    /// <summary>
    /// Delivery timeout in minutes
    /// </summary>
    public int DeliveryTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Rate limit per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 1000;
}

/// <summary>
/// General settings
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Enable notifications
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default timezone
    /// </summary>
    public string DefaultTimeZone { get; set; } = "UTC";

    /// <summary>
    /// Environment (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Data retention days
    /// </summary>
    public int DataRetentionDays { get; set; } = 90;

    /// <summary>
    /// Enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
}