namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Configuration options for the audit system
/// </summary>
public class AuditConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Auditing";

    /// <summary>
    /// Whether auditing is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default service name to use in audit events
    /// </summary>
    [StringLength(100)]
    public string DefaultServiceName { get; set; } = "Unknown";

    /// <summary>
    /// Default environment to use in audit events
    /// </summary>
    [StringLength(50)]
    public string DefaultEnvironment { get; set; } = "Development";

    /// <summary>
    /// Whether to automatically enrich events with HTTP context
    /// </summary>
    public bool EnrichWithHttpContext { get; set; } = true;

    /// <summary>
    /// Whether to automatically enrich events with user context
    /// </summary>
    public bool EnrichWithUserContext { get; set; } = true;

    /// <summary>
    /// Minimum severity level for logging events
    /// </summary>
    public AuditSeverity MinimumSeverity { get; set; } = AuditSeverity.Information;

    /// <summary>
    /// Categories of events to exclude from auditing
    /// </summary>
    public List<AuditEventCategory> ExcludedCategories { get; set; } = new();

    /// <summary>
    /// Actions to exclude from auditing (case-insensitive)
    /// </summary>
    public List<string> ExcludedActions { get; set; } = new();

    /// <summary>
    /// Resources to exclude from auditing (case-insensitive)
    /// </summary>
    public List<string> ExcludedResources { get; set; } = new();

    /// <summary>
    /// IP addresses to exclude from auditing
    /// </summary>
    public List<string> ExcludedIpAddresses { get; set; } = new();

    /// <summary>
    /// User agents to exclude from auditing (contains match)
    /// </summary>
    public List<string> ExcludedUserAgents { get; set; } = new();

    /// <summary>
    /// Maximum size of metadata in bytes (0 = no limit)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxMetadataSize { get; set; } = 65536; // 64KB

    /// <summary>
    /// Maximum size of details field in characters
    /// </summary>
    [Range(0, 5000)]
    public int MaxDetailsLength { get; set; } = 2000;

    /// <summary>
    /// Whether to enable batch processing
    /// </summary>
    public bool EnableBatchProcessing { get; set; } = true;

    /// <summary>
    /// Maximum batch size for bulk operations
    /// </summary>
    [Range(1, 1000)]
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Batch flush interval in seconds
    /// </summary>
    [Range(1, 3600)]
    public int BatchFlushIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Data retention settings
    /// </summary>
    public AuditRetentionSettings Retention { get; set; } = new();

    /// <summary>
    /// Security-specific settings
    /// </summary>
    public AuditSecuritySettings Security { get; set; } = new();

    /// <summary>
    /// Performance settings
    /// </summary>
    public AuditPerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Storage settings
    /// </summary>
    public AuditStorageSettings Storage { get; set; } = new();

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DefaultServiceName))
        {
            errors.Add("Default service name is required");
        }

        if (string.IsNullOrWhiteSpace(DefaultEnvironment))
        {
            errors.Add("Default environment is required");
        }

        if (MaxMetadataSize < 0)
        {
            errors.Add("Max metadata size cannot be negative");
        }

        if (MaxDetailsLength < 0)
        {
            errors.Add("Max details length cannot be negative");
        }

        if (MaxBatchSize <= 0)
        {
            errors.Add("Max batch size must be greater than 0");
        }

        if (BatchFlushIntervalSeconds <= 0)
        {
            errors.Add("Batch flush interval must be greater than 0");
        }

        var retentionValidation = Retention.Validate();
        if (!retentionValidation.IsSuccess)
        {
            errors.Add($"Retention settings: {retentionValidation.Error}");
        }

        var securityValidation = Security.Validate();
        if (!securityValidation.IsSuccess)
        {
            errors.Add($"Security settings: {securityValidation.Error}");
        }

        var performanceValidation = Performance.Validate();
        if (!performanceValidation.IsSuccess)
        {
            errors.Add($"Performance settings: {performanceValidation.Error}");
        }

        var storageValidation = Storage.Validate();
        if (!storageValidation.IsSuccess)
        {
            errors.Add($"Storage settings: {storageValidation.Error}");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }

    /// <summary>
    /// Checks if an event should be audited based on configuration
    /// </summary>
    public bool ShouldAuditEvent(AuditEvent auditEvent)
    {
        if (!Enabled)
        {
            return false;
        }

        if (auditEvent.Severity < MinimumSeverity)
        {
            return false;
        }

        if (ExcludedCategories.Contains(auditEvent.Category))
        {
            return false;
        }

        if (ExcludedActions.Any(action => 
            string.Equals(action, auditEvent.Action, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (ExcludedResources.Any(resource => 
            string.Equals(resource, auditEvent.Resource, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(auditEvent.IpAddress) && 
            ExcludedIpAddresses.Contains(auditEvent.IpAddress))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(auditEvent.UserAgent) && 
            ExcludedUserAgents.Any(userAgent => 
                auditEvent.UserAgent.Contains(userAgent, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Data retention settings for audit events
/// </summary>
public class AuditRetentionSettings
{
    /// <summary>
    /// Default retention period in days
    /// </summary>
    [Range(1, 3650)] // 1 day to 10 years
    public int DefaultRetentionDays { get; set; } = 365; // 1 year

    /// <summary>
    /// Retention periods by category (in days)
    /// </summary>
    public Dictionary<AuditEventCategory, int> CategoryRetention { get; set; } = new()
    {
        { AuditEventCategory.Security, 2555 }, // 7 years for security events
        { AuditEventCategory.Compliance, 2555 }, // 7 years for compliance events
        { AuditEventCategory.Administration, 1095 }, // 3 years for admin events
        { AuditEventCategory.DataAccess, 730 }, // 2 years for data access
        { AuditEventCategory.Application, 365 } // 1 year for application events
    };

    /// <summary>
    /// Whether to enable automatic purging
    /// </summary>
    public bool EnableAutoPurge { get; set; } = true;

    /// <summary>
    /// Auto-purge schedule (cron expression)
    /// </summary>
    [StringLength(100)]
    public string AutoPurgeSchedule { get; set; } = "0 2 * * *"; // Daily at 2 AM

    /// <summary>
    /// Whether to archive events before purging
    /// </summary>
    public bool ArchiveBeforePurge { get; set; } = false;

    /// <summary>
    /// Archive location (file path, connection string, etc.)
    /// </summary>
    [StringLength(500)]
    public string? ArchiveLocation { get; set; }

    /// <summary>
    /// Validates the retention settings
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (DefaultRetentionDays <= 0)
        {
            errors.Add("Default retention days must be greater than 0");
        }

        if (CategoryRetention.Values.Any(days => days <= 0))
        {
            errors.Add("All category retention periods must be greater than 0");
        }

        if (EnableAutoPurge && string.IsNullOrWhiteSpace(AutoPurgeSchedule))
        {
            errors.Add("Auto-purge schedule is required when auto-purge is enabled");
        }

        if (ArchiveBeforePurge && string.IsNullOrWhiteSpace(ArchiveLocation))
        {
            errors.Add("Archive location is required when archive before purge is enabled");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }
}

/// <summary>
/// Security-specific audit settings
/// </summary>
public class AuditSecuritySettings
{
    /// <summary>
    /// Whether to enable security event alerting
    /// </summary>
    public bool EnableAlerting { get; set; } = true;

    /// <summary>
    /// Minimum risk score for generating alerts
    /// </summary>
    [Range(0, 100)]
    public int AlertRiskThreshold { get; set; } = 75;

    /// <summary>
    /// Whether to encrypt sensitive data in audit events
    /// </summary>
    public bool EncryptSensitiveData { get; set; } = true;

    /// <summary>
    /// Fields to encrypt
    /// </summary>
    public List<string> EncryptedFields { get; set; } = new()
    {
        "Email", "PhoneNumber", "SSN", "CreditCard"
    };

    /// <summary>
    /// Whether to mask IP addresses (for GDPR compliance)
    /// </summary>
    public bool MaskIpAddresses { get; set; } = false;

    /// <summary>
    /// Whether to track failed login attempts
    /// </summary>
    public bool TrackFailedLogins { get; set; } = true;

    /// <summary>
    /// Maximum failed login attempts before alerting
    /// </summary>
    [Range(1, 100)]
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Time window for tracking failed logins (in minutes)
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int FailedLoginWindowMinutes { get; set; } = 60;

    /// <summary>
    /// Validates the security settings
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (AlertRiskThreshold < 0 || AlertRiskThreshold > 100)
        {
            errors.Add("Alert risk threshold must be between 0 and 100");
        }

        if (MaxFailedLoginAttempts <= 0)
        {
            errors.Add("Max failed login attempts must be greater than 0");
        }

        if (FailedLoginWindowMinutes <= 0)
        {
            errors.Add("Failed login window must be greater than 0");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }
}

/// <summary>
/// Performance settings for audit processing
/// </summary>
public class AuditPerformanceSettings
{
    /// <summary>
    /// Whether to use asynchronous processing
    /// </summary>
    public bool UseAsyncProcessing { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent operations
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentOperations { get; set; } = 10;

    /// <summary>
    /// Timeout for audit operations in seconds
    /// </summary>
    [Range(1, 300)]
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable in-memory caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    [Range(1, 1440)]
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum cache size (number of items)
    /// </summary>
    [Range(100, 100000)]
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Validates the performance settings
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrentOperations <= 0)
        {
            errors.Add("Max concurrent operations must be greater than 0");
        }

        if (OperationTimeoutSeconds <= 0)
        {
            errors.Add("Operation timeout must be greater than 0");
        }

        if (CacheExpirationMinutes <= 0)
        {
            errors.Add("Cache expiration must be greater than 0");
        }

        if (MaxCacheSize <= 0)
        {
            errors.Add("Max cache size must be greater than 0");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }
}

/// <summary>
/// Storage settings for audit events
/// </summary>
public class AuditStorageSettings
{
    /// <summary>
    /// Storage provider type
    /// </summary>
    public AuditStorageType StorageType { get; set; } = AuditStorageType.Database;

    /// <summary>
    /// Connection string or configuration for the storage provider
    /// </summary>
    [StringLength(1000)]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Table/collection name for storing audit events
    /// </summary>
    [StringLength(100)]
    public string TableName { get; set; } = "AuditEvents";

    /// <summary>
    /// Whether to use partitioning (for large datasets)
    /// </summary>
    public bool UsePartitioning { get; set; } = false;

    /// <summary>
    /// Partitioning strategy
    /// </summary>
    public AuditPartitioningStrategy PartitioningStrategy { get; set; } = AuditPartitioningStrategy.ByMonth;

    /// <summary>
    /// Whether to enable compression
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Validates the storage settings
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(TableName))
        {
            errors.Add("Table name is required");
        }

        if (StorageType == AuditStorageType.Database && string.IsNullOrWhiteSpace(ConnectionString))
        {
            errors.Add("Connection string is required for database storage");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }
}

/// <summary>
/// Types of audit storage providers
/// </summary>
public enum AuditStorageType
{
    /// <summary>
    /// Database storage (SQL Server, PostgreSQL, etc.)
    /// </summary>
    Database = 0,

    /// <summary>
    /// File-based storage
    /// </summary>
    File = 1,

    /// <summary>
    /// In-memory storage (for testing)
    /// </summary>
    Memory = 2,

    /// <summary>
    /// Elasticsearch storage
    /// </summary>
    Elasticsearch = 3,

    /// <summary>
    /// MongoDB storage
    /// </summary>
    MongoDB = 4,

    /// <summary>
    /// Azure Table Storage
    /// </summary>
    AzureTable = 5,

    /// <summary>
    /// Custom storage provider
    /// </summary>
    Custom = 99
}

/// <summary>
/// Partitioning strategies for audit data
/// </summary>
public enum AuditPartitioningStrategy
{
    /// <summary>
    /// No partitioning
    /// </summary>
    None = 0,

    /// <summary>
    /// Partition by day
    /// </summary>
    ByDay = 1,

    /// <summary>
    /// Partition by week
    /// </summary>
    ByWeek = 2,

    /// <summary>
    /// Partition by month
    /// </summary>
    ByMonth = 3,

    /// <summary>
    /// Partition by year
    /// </summary>
    ByYear = 4,

    /// <summary>
    /// Partition by category
    /// </summary>
    ByCategory = 5
}