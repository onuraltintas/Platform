using Enterprise.Shared.Configuration.Validators;

namespace Enterprise.Shared.Configuration.Tests.Validators;

[TestFixture]
public class ConfigurationValidatorsTests
{
    #region DatabaseSettingsValidator Tests

    [TestFixture]
    public class DatabaseSettingsValidatorTests
    {
        private Mock<ILogger<DatabaseSettingsValidator>> _logger = null!;
        private DatabaseSettingsValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<DatabaseSettingsValidator>>();
            _validator = new DatabaseSettingsValidator(_logger.Object);
        }

        [Test]
        public void Validate_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;",
                CommandTimeout = 30,
                MaxRetryCount = 3,
                PoolSize = 10,
                EnablePooling = true,
                EnableSensitiveDataLogging = false
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }

        [Test]
        public void Validate_WithEmptyConnectionString_ReturnsFailure()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "",
                CommandTimeout = 30
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Database connection string is required");
        }

        [Test]
        public void Validate_WithInvalidConnectionString_ReturnsFailure()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "invalid-connection-string",
                CommandTimeout = 30
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Database connection string must contain Server or Data Source");
        }

        [Test]
        public void Validate_WithInvalidCommandTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=TestDB;",
                CommandTimeout = 3 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Command timeout must be between 5 and 300 seconds");
        }

        [Test]
        public void Validate_WithInvalidMaxRetryCount_ReturnsFailure()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=TestDB;",
                CommandTimeout = 30,
                MaxRetryCount = 15 // Too high
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Max retry count must be between 0 and 10");
        }

        [Test]
        public void Validate_WithInvalidPoolSize_ReturnsFailure()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=TestDB;",
                CommandTimeout = 30,
                PoolSize = 0 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Pool size must be between 1 and 100");
        }
    }

    #endregion

    #region RedisSettingsValidator Tests

    [TestFixture]
    public class RedisSettingsValidatorTests
    {
        private Mock<ILogger<RedisSettingsValidator>> _logger = null!;
        private RedisSettingsValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<RedisSettingsValidator>>();
            _validator = new RedisSettingsValidator(_logger.Object);
        }

        [Test]
        public void Validate_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                Database = 0,
                KeyPrefix = "test:",
                DefaultExpiration = TimeSpan.FromHours(1),
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }

        [Test]
        public void Validate_WithEmptyConnectionString_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "",
                Database = 0
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Redis connection string is required");
        }

        [Test]
        public void Validate_WithInvalidDatabase_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                Database = 16 // Too high
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Redis database index must be between 0 and 15");
        }

        [Test]
        public void Validate_WithInvalidDefaultExpiration_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                Database = 0,
                DefaultExpiration = TimeSpan.Zero // Invalid
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Default expiration must be greater than zero");
        }

        [Test]
        public void Validate_WithTooLongDefaultExpiration_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                Database = 0,
                DefaultExpiration = TimeSpan.FromDays(31) // Too long
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Default expiration should not exceed 30 days");
        }

        [Test]
        public void Validate_WithInvalidConnectTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                ConnectTimeout = 500 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Connect timeout must be between 1000 and 30000 milliseconds");
        }

        [Test]
        public void Validate_WithInvalidSyncTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new RedisSettings
            {
                ConnectionString = "localhost:6379",
                SyncTimeout = 35000 // Too high
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Sync timeout must be between 1000 and 30000 milliseconds");
        }
    }

    #endregion

    #region RabbitMQSettingsValidator Tests

    [TestFixture]
    public class RabbitMQSettingsValidatorTests
    {
        private Mock<ILogger<RabbitMQSettingsValidator>> _logger = null!;
        private RabbitMQSettingsValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<RabbitMQSettingsValidator>>();
            _validator = new RabbitMQSettingsValidator(_logger.Object);
        }

        [Test]
        public void Validate_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Port = 5672,
                Username = "testuser",
                Password = "testpass",
                VirtualHost = "/test",
                ExchangeName = "test.events",
                PrefetchCount = 10,
                TimeoutMilliseconds = 30000,
                EnableSsl = false
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }

        [Test]
        public void Validate_WithEmptyHost_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "",
                Username = "test",
                Password = "test"
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ host is required");
        }

        [Test]
        public void Validate_WithInvalidPort_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Port = 0, // Invalid
                Username = "test",
                Password = "test"
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ port must be between 1 and 65535");
        }

        [Test]
        public void Validate_WithEmptyUsername_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Port = 5672,
                Username = "",
                Password = "test"
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ username is required");
        }

        [Test]
        public void Validate_WithEmptyPassword_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Port = 5672,
                Username = "test",
                Password = ""
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ password is required");
        }

        [Test]
        public void Validate_WithNullVirtualHost_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Username = "test",
                Password = "test",
                VirtualHost = null! // Null not allowed
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ virtual host cannot be null (use empty string for default)");
        }

        [Test]
        public void Validate_WithInvalidPrefetchCount_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Username = "test",
                Password = "test",
                PrefetchCount = 0 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ prefetch count must be between 1 and 1000");
        }

        [Test]
        public void Validate_WithInvalidTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Username = "test",
                Password = "test",
                TimeoutMilliseconds = 500 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ timeout must be between 1000 and 60000 milliseconds");
        }

        [Test]
        public void Validate_WithNullExchangeName_ReturnsFailure()
        {
            // Arrange
            var settings = new RabbitMQSettings
            {
                Host = "localhost",
                Username = "test",
                Password = "test",
                ExchangeName = null! // Null not allowed
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("RabbitMQ exchange name cannot be null (use empty string for default exchange)");
        }
    }

    #endregion

    #region ConfigurationSettingsValidator Tests

    [TestFixture]
    public class ConfigurationSettingsValidatorTests
    {
        private Mock<ILogger<ConfigurationSettingsValidator>> _logger = null!;
        private ConfigurationSettingsValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<ConfigurationSettingsValidator>>();
            _validator = new ConfigurationSettingsValidator(_logger.Object);
        }

        [Test]
        public void Validate_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                CacheTimeout = TimeSpan.FromMinutes(5),
                FeatureFlagCacheTimeout = TimeSpan.FromMinutes(2),
                MaxCacheSize = 500,
                EncryptionKey = "valid-encryption-key-123",
                AuditChanges = true
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }

        [Test]
        public void Validate_WithNegativeCacheTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                CacheTimeout = TimeSpan.FromMinutes(-1) // Negative
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Cache timeout cannot be negative");
        }

        [Test]
        public void Validate_WithTooLongCacheTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                CacheTimeout = TimeSpan.FromHours(25) // Too long
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Cache timeout should not exceed 24 hours");
        }

        [Test]
        public void Validate_WithNegativeFeatureFlagCacheTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                FeatureFlagCacheTimeout = TimeSpan.FromMinutes(-1) // Negative
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Feature flag cache timeout cannot be negative");
        }

        [Test]
        public void Validate_WithTooLongFeatureFlagCacheTimeout_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                FeatureFlagCacheTimeout = TimeSpan.FromHours(2) // Too long
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Feature flag cache timeout should not exceed 1 hour");
        }

        [Test]
        public void Validate_WithInvalidMaxCacheSize_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                MaxCacheSize = 50 // Too low
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Max cache size must be between 100 and 10000");
        }

        [Test]
        public void Validate_WithTooShortEncryptionKey_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                EncryptionKey = "short" // Too short
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Encryption key must be at least 16 characters long");
        }

        [Test]
        public void Validate_WithTooLongEncryptionKey_ReturnsFailure()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                EncryptionKey = new string('x', 257) // Too long
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().Contain("Encryption key should not exceed 256 characters");
        }

        [Test]
        public void Validate_WithEmptyEncryptionKey_ReturnsSuccess()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                EncryptionKey = "" // Empty is allowed
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }

        [Test]
        public void Validate_WithNullEncryptionKey_ReturnsSuccess()
        {
            // Arrange
            var settings = new ConfigurationSettings
            {
                EncryptionKey = null // Null is allowed
            };

            // Act
            var result = _validator.Validate(null, settings);

            // Assert
            result.Should().Be(ValidateOptionsResult.Success);
        }
    }

    #endregion
}