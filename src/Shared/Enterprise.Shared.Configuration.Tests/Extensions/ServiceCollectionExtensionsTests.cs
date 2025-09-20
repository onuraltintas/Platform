using Microsoft.Extensions.Hosting;

namespace Enterprise.Shared.Configuration.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _services = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        _services.AddLogging();

        var configData = new Dictionary<string, string?>
        {
            ["ConfigurationSettings:CacheTimeout"] = "00:05:00",
            ["ConfigurationSettings:AuditChanges"] = "true",
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDB;Trusted_Connection=true;",
            ["Database:CommandTimeout"] = "30",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:Database"] = "0",
            ["RabbitMQ:Host"] = "localhost",
            ["RabbitMQ:Username"] = "test",
            ["RabbitMQ:Password"] = "test",
            ["FeatureFlags:TestFeature"] = "true"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        // Register IConfiguration in the service collection for DI tests
        _services.AddSingleton(_configuration);
    }

    #region AddSharedConfiguration Tests

    [Test]
    public void AddSharedConfiguration_WithValidConfiguration_RegistersServices()
    {
        // Act
        _services.AddSharedConfiguration(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IConfigurationService>().Should().NotBeNull();
        provider.GetService<IFeatureFlagService>().Should().NotBeNull();
        provider.GetService<IConfigurationChangeTracker>().Should().NotBeNull();
        provider.GetService<IUserContextService>().Should().NotBeNull();
        provider.GetService<IMemoryCache>().Should().NotBeNull();
    }

    [Test]
    public void AddSharedConfiguration_WithValidConfiguration_RegistersStronglyTypedSettings()
    {
        // Act
        _services.AddSharedConfiguration(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        var databaseOptions = provider.GetService<IOptions<DatabaseSettings>>();
        var redisOptions = provider.GetService<IOptions<RedisSettings>>();
        var rabbitMqOptions = provider.GetService<IOptions<RabbitMQSettings>>();
        var configOptions = provider.GetService<IOptions<ConfigurationSettings>>();

        databaseOptions.Should().NotBeNull();
        redisOptions.Should().NotBeNull();
        rabbitMqOptions.Should().NotBeNull();
        configOptions.Should().NotBeNull();
    }

    [Test]
    public void AddSharedConfiguration_WithValidConfiguration_RegistersValidators()
    {
        // Act
        _services.AddSharedConfiguration(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        var databaseValidator = provider.GetServices<IValidateOptions<DatabaseSettings>>().FirstOrDefault();
        var redisValidator = provider.GetServices<IValidateOptions<RedisSettings>>().FirstOrDefault();
        var rabbitMqValidator = provider.GetServices<IValidateOptions<RabbitMQSettings>>().FirstOrDefault();
        var configValidator = provider.GetServices<IValidateOptions<ConfigurationSettings>>().FirstOrDefault();

        databaseValidator.Should().NotBeNull();
        redisValidator.Should().NotBeNull();
        rabbitMqValidator.Should().NotBeNull();
        configValidator.Should().NotBeNull();
    }

    [Test]
    public void AddSharedConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddSharedConfiguration(null!, _configuration));
    }

    [Test]
    public void AddSharedConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _services.AddSharedConfiguration(null!));
    }

    #endregion

    #region AddSharedConfiguration with Options Tests

    [Test]
    public void AddSharedConfiguration_WithOptions_RegistersServicesBasedOnOptions()
    {
        // Act
        _services.AddSharedConfiguration(_configuration, options =>
        {
            options.EnableConfigurationService = true;
            options.EnableFeatureFlags = true;
            options.EnableChangeTracking = false;
            options.EnableValidation = true;
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IConfigurationService>().Should().NotBeNull();
        provider.GetService<IFeatureFlagService>().Should().NotBeNull();
        provider.GetService<IConfigurationChangeTracker>().Should().BeNull(); // Not registered when EnableChangeTracking = false
    }

    [Test]
    public void AddSharedConfiguration_WithCustomUserContextService_RegistersCustomService()
    {
        // Act
        _services.AddSharedConfiguration(_configuration, options =>
        {
            options.UserContextServiceType = typeof(TestUserContextService);
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        var userContextService = provider.GetService<IUserContextService>();
        userContextService.Should().NotBeNull();
        userContextService.Should().BeOfType<TestUserContextService>();
    }

    [Test]
    public void AddSharedConfiguration_WithDisabledFeatures_DoesNotRegisterDisabledServices()
    {
        // Act
        _services.AddSharedConfiguration(_configuration, options =>
        {
            options.EnableConfigurationService = false;
            options.EnableFeatureFlags = false;
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IConfigurationService>().Should().BeNull();
        provider.GetService<IFeatureFlagService>().Should().BeNull();
    }

    [Test]
    public void AddSharedConfiguration_WithOptionsAndNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSharedConfiguration(null!, _configuration, _ => { }));
    }

    [Test]
    public void AddSharedConfiguration_WithOptionsAndNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _services.AddSharedConfiguration(null!, _ => { }));
    }

    [Test]
    public void AddSharedConfiguration_WithOptionsAndNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _services.AddSharedConfiguration(_configuration, null!));
    }

    #endregion

    #region AddFeatureFlags Tests

    [Test]
    public void AddFeatureFlags_WithValidConfiguration_RegistersMinimalServices()
    {
        // Act
        _services.AddFeatureFlags(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IFeatureFlagService>().Should().NotBeNull();
        provider.GetService<IUserContextService>().Should().NotBeNull();
        provider.GetService<IMemoryCache>().Should().NotBeNull();
        provider.GetService<IOptions<ConfigurationSettings>>().Should().NotBeNull();
    }

    [Test]
    public void AddFeatureFlags_WithValidConfiguration_DoesNotRegisterFullConfigurationService()
    {
        // Act
        _services.AddFeatureFlags(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IConfigurationService>().Should().BeNull();
        provider.GetService<IConfigurationChangeTracker>().Should().BeNull();
    }

    [Test]
    public void AddFeatureFlags_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddFeatureFlags(null!, _configuration));
    }

    [Test]
    public void AddFeatureFlags_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _services.AddFeatureFlags(null!));
    }

    #endregion

    #region ValidateConfigurationOnStartup Tests

    [Test]
    public void ValidateConfigurationOnStartup_RegistersValidationOptions()
    {
        // Arrange
        _services.AddSharedConfiguration(_configuration);

        // Act
        _services.ValidateConfigurationOnStartup();
        var provider = _services.BuildServiceProvider();

        // Assert
        // This is difficult to test directly, but we can verify the service provider builds without errors
        provider.Should().NotBeNull();
        
        // Verify that options are configured
        var databaseOptions = provider.GetService<IOptions<DatabaseSettings>>();
        var redisOptions = provider.GetService<IOptions<RedisSettings>>();
        var rabbitMqOptions = provider.GetService<IOptions<RabbitMQSettings>>();
        var configOptions = provider.GetService<IOptions<ConfigurationSettings>>();

        databaseOptions.Should().NotBeNull();
        redisOptions.Should().NotBeNull();
        rabbitMqOptions.Should().NotBeNull();
        configOptions.Should().NotBeNull();
    }

    [Test]
    public void ValidateConfigurationOnStartup_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.ValidateConfigurationOnStartup(null!));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AddSharedConfiguration_FullIntegration_ServicesWorkTogether()
    {
        // Arrange
        _services.AddSharedConfiguration(_configuration);
        var provider = _services.BuildServiceProvider();

        // Act
        var configService = provider.GetRequiredService<IConfigurationService>();
        var featureFlagService = provider.GetRequiredService<IFeatureFlagService>();
        var changeTracker = provider.GetRequiredService<IConfigurationChangeTracker>();

        // Assert
        configService.Should().NotBeNull();
        featureFlagService.Should().NotBeNull();
        changeTracker.Should().NotBeNull();

        // Test basic functionality
        var testValue = configService.GetValue<string>("Database:ConnectionString");
        testValue.Should().NotBeNull();

        var featureResult = featureFlagService.IsEnabled("TestFeature");
        featureResult.Should().BeTrue();
    }

    [Test]
    public async Task AddSharedConfiguration_FullIntegration_ServicesInteractCorrectly()
    {
        // Arrange
        _services.AddSharedConfiguration(_configuration);
        var provider = _services.BuildServiceProvider();

        var configService = provider.GetRequiredService<IConfigurationService>();
        var changeTracker = provider.GetRequiredService<IConfigurationChangeTracker>();

        var eventFired = false;
        configService.ConfigurationChanged += (sender, args) => eventFired = true;

        // Act
        await configService.SetValueAsync("TestKey", "TestValue", "TestUser");

        // Assert
        eventFired.Should().BeTrue();
        
        var changes = await changeTracker.GetChangeHistoryAsync("TestKey");
        changes.Should().HaveCount(1);
        changes.First().NewValue.Should().Be("TestValue");
    }

    #endregion

    #region Helper Classes

    public class TestUserContextService : IUserContextService
    {
        public string? GetCurrentUserId() => "test-user-123";
        public IEnumerable<string> GetCurrentUserRoles() => new[] { "Test" };
        public bool HasRole(string role) => role == "Test";
        public Dictionary<string, string> GetUserClaims() => new();
        public string? GetClaimValue(string claimType) => null;
        public bool IsAuthenticated() => true;
    }

    #endregion
}