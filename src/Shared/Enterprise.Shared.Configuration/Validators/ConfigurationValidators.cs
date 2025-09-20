using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Validators;

/// <summary>
/// Validator for database configuration settings
/// </summary>
public sealed class DatabaseSettingsValidator : IValidateOptions<DatabaseSettings>
{
    private readonly ILogger<DatabaseSettingsValidator> _logger;

    public DatabaseSettingsValidator(ILogger<DatabaseSettingsValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, DatabaseSettings options)
    {
        var failures = new List<string>();

        try
        {
            // Connection string validation
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                failures.Add("Database connection string is required");
            }
            else
            {
                // Basic connection string format validation
                if (!options.ConnectionString.Contains("Server=") && !options.ConnectionString.Contains("Data Source="))
                {
                    failures.Add("Database connection string must contain Server or Data Source");
                }

                if (!options.ConnectionString.Contains("Database=") && !options.ConnectionString.Contains("Initial Catalog="))
                {
                    failures.Add("Database connection string must contain Database or Initial Catalog");
                }
            }

            // Command timeout validation
            if (options.CommandTimeout < 5 || options.CommandTimeout > 300)
            {
                failures.Add("Command timeout must be between 5 and 300 seconds");
            }

            // Max retry count validation
            if (options.MaxRetryCount < 0 || options.MaxRetryCount > 10)
            {
                failures.Add("Max retry count must be between 0 and 10");
            }

            // Pool size validation
            if (options.PoolSize < 1 || options.PoolSize > 100)
            {
                failures.Add("Pool size must be between 1 and 100");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database settings validation");
            failures.Add($"Validation error: {ex.Message}");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Redis configuration settings
/// </summary>
public sealed class RedisSettingsValidator : IValidateOptions<RedisSettings>
{
    private readonly ILogger<RedisSettingsValidator> _logger;

    public RedisSettingsValidator(ILogger<RedisSettingsValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, RedisSettings options)
    {
        var failures = new List<string>();

        try
        {
            // Connection string validation
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                failures.Add("Redis connection string is required");
            }
            else
            {
                // Basic connection string format validation
                if (!options.ConnectionString.Contains(':') && !options.ConnectionString.Contains(','))
                {
                    // Could be a hostname only, which is valid
                }
            }

            // Database index validation
            if (options.Database < 0 || options.Database > 15)
            {
                failures.Add("Redis database index must be between 0 and 15");
            }

            // Default expiration validation
            if (options.DefaultExpiration <= TimeSpan.Zero)
            {
                failures.Add("Default expiration must be greater than zero");
            }

            if (options.DefaultExpiration > TimeSpan.FromDays(30))
            {
                failures.Add("Default expiration should not exceed 30 days");
            }

            // Timeout validations
            if (options.ConnectTimeout < 1000 || options.ConnectTimeout > 30000)
            {
                failures.Add("Connect timeout must be between 1000 and 30000 milliseconds");
            }

            if (options.SyncTimeout < 1000 || options.SyncTimeout > 30000)
            {
                failures.Add("Sync timeout must be between 1000 and 30000 milliseconds");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Redis settings validation");
            failures.Add($"Validation error: {ex.Message}");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for RabbitMQ configuration settings
/// </summary>
public sealed class RabbitMQSettingsValidator : IValidateOptions<RabbitMQSettings>
{
    private readonly ILogger<RabbitMQSettingsValidator> _logger;

    public RabbitMQSettingsValidator(ILogger<RabbitMQSettingsValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, RabbitMQSettings options)
    {
        var failures = new List<string>();

        try
        {
            // Host validation
            if (string.IsNullOrWhiteSpace(options.Host))
            {
                failures.Add("RabbitMQ host is required");
            }

            // Port validation
            if (options.Port < 1 || options.Port > 65535)
            {
                failures.Add("RabbitMQ port must be between 1 and 65535");
            }

            // Username validation
            if (string.IsNullOrWhiteSpace(options.Username))
            {
                failures.Add("RabbitMQ username is required");
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(options.Password))
            {
                failures.Add("RabbitMQ password is required");
            }

            // Virtual host validation (can be empty, but not null)
            if (options.VirtualHost is null)
            {
                failures.Add("RabbitMQ virtual host cannot be null (use empty string for default)");
            }

            // Prefetch count validation
            if (options.PrefetchCount < 1 || options.PrefetchCount > 1000)
            {
                failures.Add("RabbitMQ prefetch count must be between 1 and 1000");
            }

            // Timeout validation
            if (options.TimeoutMilliseconds < 1000 || options.TimeoutMilliseconds > 60000)
            {
                failures.Add("RabbitMQ timeout must be between 1000 and 60000 milliseconds");
            }

            // Exchange name validation (can be empty for default exchange)
            if (options.ExchangeName is null)
            {
                failures.Add("RabbitMQ exchange name cannot be null (use empty string for default exchange)");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RabbitMQ settings validation");
            failures.Add($"Validation error: {ex.Message}");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for configuration settings
/// </summary>
public sealed class ConfigurationSettingsValidator : IValidateOptions<ConfigurationSettings>
{
    private readonly ILogger<ConfigurationSettingsValidator> _logger;

    public ConfigurationSettingsValidator(ILogger<ConfigurationSettingsValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ConfigurationSettings options)
    {
        var failures = new List<string>();

        try
        {
            // Cache timeout validation
            if (options.CacheTimeout < TimeSpan.Zero)
            {
                failures.Add("Cache timeout cannot be negative");
            }

            if (options.CacheTimeout > TimeSpan.FromHours(24))
            {
                failures.Add("Cache timeout should not exceed 24 hours");
            }

            // Feature flag cache timeout validation
            if (options.FeatureFlagCacheTimeout < TimeSpan.Zero)
            {
                failures.Add("Feature flag cache timeout cannot be negative");
            }

            if (options.FeatureFlagCacheTimeout > TimeSpan.FromHours(1))
            {
                failures.Add("Feature flag cache timeout should not exceed 1 hour");
            }

            // Max cache size validation
            if (options.MaxCacheSize < 100 || options.MaxCacheSize > 10000)
            {
                failures.Add("Max cache size must be between 100 and 10000");
            }

            // Encryption key validation (if provided)
            if (!string.IsNullOrEmpty(options.EncryptionKey))
            {
                if (options.EncryptionKey.Length < 16)
                {
                    failures.Add("Encryption key must be at least 16 characters long");
                }

                if (options.EncryptionKey.Length > 256)
                {
                    failures.Add("Encryption key should not exceed 256 characters");
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration settings validation");
            failures.Add($"Validation error: {ex.Message}");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}