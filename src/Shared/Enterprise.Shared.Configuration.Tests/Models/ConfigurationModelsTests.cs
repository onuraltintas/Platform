namespace Enterprise.Shared.Configuration.Tests.Models;

[TestFixture]
public class ConfigurationModelsTests
{
    #region ConfigurationChangeRecord Tests

    [TestFixture]
    public class ConfigurationChangeRecordTests
    {
        [Test]
        public void ConfigurationChangeRecord_DefaultValues_AreSetCorrectly()
        {
            // Act
            var record = new ConfigurationChangeRecord();

            // Assert
            record.Id.Should().NotBeEmpty();
            record.Key.Should().Be(string.Empty);
            record.OldValue.Should().BeNull();
            record.NewValue.Should().BeNull();
            record.ChangedBy.Should().Be("System");
            record.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            record.Reason.Should().BeNull();
            record.Environment.Should().BeNull();
            record.Source.Should().BeNull();
        }

        [Test]
        public void ConfigurationChangeRecord_WithCustomValues_SetsPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var key = "TestKey";
            var oldValue = "OldValue";
            var newValue = "NewValue";
            var changedBy = "TestUser";
            var changedAt = DateTime.UtcNow.AddMinutes(-5);
            var reason = "Test reason";
            var environment = "Development";
            var source = "API";

            // Act
            var record = new ConfigurationChangeRecord
            {
                Id = id,
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = changedBy,
                ChangedAt = changedAt,
                Reason = reason,
                Environment = environment,
                Source = source
            };

            // Assert
            record.Id.Should().Be(id);
            record.Key.Should().Be(key);
            record.OldValue.Should().Be(oldValue);
            record.NewValue.Should().Be(newValue);
            record.ChangedBy.Should().Be(changedBy);
            record.ChangedAt.Should().Be(changedAt);
            record.Reason.Should().Be(reason);
            record.Environment.Should().Be(environment);
            record.Source.Should().Be(source);
        }
    }

    #endregion

    #region ConfigurationChangedEventArgs Tests

    [TestFixture]
    public class ConfigurationChangedEventArgsTests
    {
        [Test]
        public void ConfigurationChangedEventArgs_DefaultValues_AreSetCorrectly()
        {
            // Act
            var args = new ConfigurationChangedEventArgs();

            // Assert
            args.Key.Should().Be(string.Empty);
            args.OldValue.Should().BeNull();
            args.NewValue.Should().BeNull();
            args.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            args.ChangedBy.Should().BeNull();
        }

        [Test]
        public void ConfigurationChangedEventArgs_WithCustomValues_SetsPropertiesCorrectly()
        {
            // Arrange
            var key = "TestKey";
            var oldValue = "OldValue";
            var newValue = "NewValue";
            var changedAt = DateTime.UtcNow.AddMinutes(-5);
            var changedBy = "TestUser";

            // Act
            var args = new ConfigurationChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = changedAt,
                ChangedBy = changedBy
            };

            // Assert
            args.Key.Should().Be(key);
            args.OldValue.Should().Be(oldValue);
            args.NewValue.Should().Be(newValue);
            args.ChangedAt.Should().Be(changedAt);
            args.ChangedBy.Should().Be(changedBy);
        }
    }

    #endregion

    #region FeatureFlagResult Tests

    [TestFixture]
    public class FeatureFlagResultTests
    {
        [Test]
        public void FeatureFlagResult_DefaultValues_AreSetCorrectly()
        {
            // Act
            var result = new FeatureFlagResult();

            // Assert
            result.Name.Should().Be(string.Empty);
            result.IsEnabled.Should().BeFalse();
            result.UserId.Should().BeNull();
            result.EvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Source.Should().Be("Configuration");
            result.Metadata.Should().NotBeNull().And.BeEmpty();
        }

        [Test]
        public void FeatureFlagResult_WithCustomValues_SetsPropertiesCorrectly()
        {
            // Arrange
            var name = "TestFeature";
            var isEnabled = true;
            var userId = "test-user-123";
            var evaluatedAt = DateTime.UtcNow.AddMinutes(-5);
            var source = "Database";
            var metadata = new Dictionary<string, object> { ["test"] = "value" };

            // Act
            var result = new FeatureFlagResult
            {
                Name = name,
                IsEnabled = isEnabled,
                UserId = userId,
                EvaluatedAt = evaluatedAt,
                Source = source,
                Metadata = metadata
            };

            // Assert
            result.Name.Should().Be(name);
            result.IsEnabled.Should().Be(isEnabled);
            result.UserId.Should().Be(userId);
            result.EvaluatedAt.Should().Be(evaluatedAt);
            result.Source.Should().Be(source);
            result.Metadata.Should().BeEquivalentTo(metadata);
        }
    }

    #endregion

    #region ConfigurationValidationResult Tests

    [TestFixture]
    public class ConfigurationValidationResultTests
    {
        [Test]
        public void ConfigurationValidationResult_DefaultValues_AreSetCorrectly()
        {
            // Act
            var result = new ConfigurationValidationResult();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeNull().And.BeEmpty();
            result.Warnings.Should().NotBeNull().And.BeEmpty();
            result.SectionName.Should().BeNull();
            result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ConfigurationValidationResult_Success_CreatesValidResult()
        {
            // Arrange
            var sectionName = "TestSection";

            // Act
            var result = ConfigurationValidationResult.Success(sectionName);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
            result.SectionName.Should().Be(sectionName);
        }

        [Test]
        public void ConfigurationValidationResult_Failure_CreatesInvalidResult()
        {
            // Arrange
            var errors = new[] { "Error 1", "Error 2" };
            var sectionName = "TestSection";

            // Act
            var result = ConfigurationValidationResult.Failure(errors, sectionName);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().BeEquivalentTo(errors);
            result.Warnings.Should().BeEmpty();
            result.SectionName.Should().Be(sectionName);
        }

        [Test]
        public void ConfigurationValidationResult_WithWarnings_CreatesValidResultWithWarnings()
        {
            // Arrange
            var warnings = new[] { "Warning 1", "Warning 2" };
            var sectionName = "TestSection";

            // Act
            var result = ConfigurationValidationResult.WithWarnings(warnings, sectionName);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().BeEquivalentTo(warnings);
            result.SectionName.Should().Be(sectionName);
        }
    }

    #endregion

    #region Configuration Settings Tests

    [TestFixture]
    public class ConfigurationSettingsTests
    {
        [Test]
        public void ConfigurationSettings_DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new ConfigurationSettings();

            // Assert
            settings.Provider.Should().Be(Enterprise.Shared.Common.Enums.ConfigurationProviderType.File);
            settings.ReloadOnChange.Should().BeTrue();
            settings.ValidationMode.Should().Be(Enterprise.Shared.Common.Enums.ValidationMode.Strict);
            settings.CacheTimeout.Should().Be(TimeSpan.FromMinutes(5));
            settings.EncryptionKey.Should().BeNull();
            settings.AuditChanges.Should().BeTrue();
            settings.MaxCacheSize.Should().Be(1000);
            settings.FeatureFlagCacheTimeout.Should().Be(TimeSpan.FromMinutes(5));
        }

        [Test]
        public void ConfigurationSettings_SectionName_IsCorrect()
        {
            // Assert
            ConfigurationSettings.SectionName.Should().Be("ConfigurationSettings");
        }
    }

    #endregion

    #region Database Settings Tests

    [TestFixture]
    public class DatabaseSettingsTests
    {
        [Test]
        public void DatabaseSettings_DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new DatabaseSettings();

            // Assert
            settings.ConnectionString.Should().Be(string.Empty);
            settings.CommandTimeout.Should().Be(30);
            settings.EnableSensitiveDataLogging.Should().BeFalse();
            settings.MaxRetryCount.Should().Be(3);
            settings.PoolSize.Should().Be(10);
            settings.EnablePooling.Should().BeTrue();
        }

        [Test]
        public void DatabaseSettings_SectionName_IsCorrect()
        {
            // Assert
            DatabaseSettings.SectionName.Should().Be("Database");
        }
    }

    #endregion

    #region Redis Settings Tests

    [TestFixture]
    public class RedisSettingsTests
    {
        [Test]
        public void RedisSettings_DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new RedisSettings();

            // Assert
            settings.ConnectionString.Should().Be(string.Empty);
            settings.Database.Should().Be(0);
            settings.KeyPrefix.Should().Be("enterprise:");
            settings.DefaultExpiration.Should().Be(TimeSpan.FromHours(1));
            settings.ConnectTimeout.Should().Be(5000);
            settings.SyncTimeout.Should().Be(5000);
        }

        [Test]
        public void RedisSettings_SectionName_IsCorrect()
        {
            // Assert
            RedisSettings.SectionName.Should().Be("Redis");
        }
    }

    #endregion

    #region RabbitMQ Settings Tests

    [TestFixture]
    public class RabbitMQSettingsTests
    {
        [Test]
        public void RabbitMQSettings_DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new RabbitMQSettings();

            // Assert
            settings.Host.Should().Be(string.Empty);
            settings.Port.Should().Be(5672);
            settings.Username.Should().Be(string.Empty);
            settings.Password.Should().Be(string.Empty);
            settings.VirtualHost.Should().Be("/");
            settings.ExchangeName.Should().Be("enterprise.events");
            settings.PrefetchCount.Should().Be(10);
            settings.TimeoutMilliseconds.Should().Be(30000);
            settings.EnableSsl.Should().BeFalse();
        }

        [Test]
        public void RabbitMQSettings_SectionName_IsCorrect()
        {
            // Assert
            RabbitMQSettings.SectionName.Should().Be("RabbitMQ");
        }
    }

    #endregion
}