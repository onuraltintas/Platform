namespace Enterprise.Shared.Email.Configuration;

/// <summary>
/// Email service configuration options
/// </summary>
public class EmailConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "EmailService";

    /// <summary>
    /// Default sender configuration
    /// </summary>
    public EmailSenderOptions DefaultSender { get; set; } = new();

    /// <summary>
    /// SMTP server configuration
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>
    /// Template service configuration
    /// </summary>
    public TemplateOptions Templates { get; set; } = new();

    /// <summary>
    /// Bulk email processing configuration
    /// </summary>
    public BulkProcessingOptions BulkProcessing { get; set; } = new();

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Default sender configuration
/// </summary>
public class EmailSenderOptions
{
    /// <summary>
    /// Default sender email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Default sender display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default reply-to address
    /// </summary>
    [EmailAddress]
    public string? ReplyTo { get; set; }
}

/// <summary>
/// SMTP server configuration
/// </summary>
public class SmtpOptions
{
    /// <summary>
    /// SMTP server hostname
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Use default credentials
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = false;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Maximum number of concurrent connections
    /// </summary>
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;
}

/// <summary>
/// Template service configuration
/// </summary>
public class TemplateOptions
{
    /// <summary>
    /// Template storage provider type
    /// </summary>
    public TemplateProvider Provider { get; set; } = TemplateProvider.FileSystem;

    /// <summary>
    /// Template directory path (for file system provider)
    /// </summary>
    public string DirectoryPath { get; set; } = "Templates/Email";

    /// <summary>
    /// Template file extension
    /// </summary>
    public string FileExtension { get; set; } = ".liquid";

    /// <summary>
    /// Enable template caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration in minutes
    /// </summary>
    [Range(1, 1440)]
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Watch for template file changes
    /// </summary>
    public bool WatchFileChanges { get; set; } = true;
}

/// <summary>
/// Template provider types
/// </summary>
public enum TemplateProvider
{
    FileSystem,
    Database,
    Memory
}

/// <summary>
/// Bulk processing configuration
/// </summary>
public class BulkProcessingOptions
{
    /// <summary>
    /// Default batch size for bulk operations
    /// </summary>
    [Range(1, 1000)]
    public int DefaultBatchSize { get; set; } = 50;

    /// <summary>
    /// Default maximum concurrency
    /// </summary>
    [Range(1, 100)]
    public int DefaultMaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Default delay between batches in milliseconds
    /// </summary>
    [Range(0, 60000)]
    public int DefaultDelayBetweenBatchesMs { get; set; } = 1000;

    /// <summary>
    /// Maximum allowed batch size
    /// </summary>
    [Range(1, 10000)]
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum allowed concurrency
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrency { get; set; } = 50;
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Enable retry mechanism
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    [Range(100, 60000)]
    public int DelayMs { get; set; } = 1000;

    /// <summary>
    /// Use exponential backoff for retry delays
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Maximum delay for exponential backoff in milliseconds
    /// </summary>
    [Range(1000, 300000)]
    public int MaxDelayMs { get; set; } = 30000;
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum emails per minute
    /// </summary>
    [Range(1, 10000)]
    public int EmailsPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum emails per hour
    /// </summary>
    [Range(1, 100000)]
    public int EmailsPerHour { get; set; } = 1000;

    /// <summary>
    /// Maximum emails per day
    /// </summary>
    [Range(1, 1000000)]
    public int EmailsPerDay { get; set; } = 10000;
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Log successful email sends
    /// </summary>
    public bool LogSuccessfulSends { get; set; } = true;

    /// <summary>
    /// Log failed email sends
    /// </summary>
    public bool LogFailedSends { get; set; } = true;

    /// <summary>
    /// Log email content (for debugging)
    /// </summary>
    public bool LogEmailContent { get; set; } = false;

    /// <summary>
    /// Log template rendering
    /// </summary>
    public bool LogTemplateRendering { get; set; } = false;

    /// <summary>
    /// Log performance metrics
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = true;
}