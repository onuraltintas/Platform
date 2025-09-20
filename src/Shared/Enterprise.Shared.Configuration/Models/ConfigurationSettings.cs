using Enterprise.Shared.Common.Enums;

namespace Enterprise.Shared.Configuration.Models;

/// <summary>
/// Configuration settings for the configuration service itself
/// </summary>
public class ConfigurationSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ConfigurationSettings";

    /// <summary>
    /// Configuration provider type (File, AzureKeyVault, Database, etc.)
    /// </summary>
    public ConfigurationProviderType Provider { get; set; } = ConfigurationProviderType.File;

    /// <summary>
    /// Whether to reload configuration when files change
    /// </summary>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// Configuration validation mode
    /// </summary>
    public ValidationMode ValidationMode { get; set; } = ValidationMode.Strict;

    /// <summary>
    /// Cache timeout for configuration values
    /// </summary>
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Encryption key for sensitive configuration values
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Whether to audit configuration changes
    /// </summary>
    public bool AuditChanges { get; set; } = true;

    /// <summary>
    /// Maximum cache size for configuration values
    /// </summary>
    [Range(100, 10000)]
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Feature flag cache timeout
    /// </summary>
    public TimeSpan FeatureFlagCacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Database connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    [Range(5, 300)]
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Whether to enable sensitive data logging (development only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Connection pool size
    /// </summary>
    [Range(1, 100)]
    public int PoolSize { get; set; } = 10;

    /// <summary>
    /// Whether to enable connection pooling
    /// </summary>
    public bool EnablePooling { get; set; } = true;
}

/// <summary>
/// Redis configuration settings
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Redis database index
    /// </summary>
    [Range(0, 15)]
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for all cached items
    /// </summary>
    public string KeyPrefix { get; set; } = "enterprise:";

    /// <summary>
    /// Default expiration time for cached items
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    [Range(1000, 30000)]
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    [Range(1000, 30000)]
    public int SyncTimeout { get; set; } = 5000;
}

/// <summary>
/// RabbitMQ configuration settings
/// </summary>
public class RabbitMQSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ host
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Exchange name
    /// </summary>
    public string ExchangeName { get; set; } = "enterprise.events";

    /// <summary>
    /// Prefetch count for message consumption
    /// </summary>
    [Range(1, 1000)]
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    [Range(1000, 60000)]
    public int TimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// Whether to enable SSL
    /// </summary>
    public bool EnableSsl { get; set; } = false;
}