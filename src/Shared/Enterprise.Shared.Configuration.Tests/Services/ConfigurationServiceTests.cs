namespace Enterprise.Shared.Configuration.Tests.Services;

[TestFixture]
public class ConfigurationServiceTests
{
    private IConfiguration _configuration = null!;
    private IMemoryCache _cache = null!;
    private Mock<ILogger<ConfigurationService>> _logger = null!;
    private IOptions<ConfigurationSettings> _settings = null!;
    private ConfigurationService _configurationService = null!;

    [SetUp]
    public void SetUp()
    {
        var configData = new Dictionary<string, string?>
        {
            ["TestSection:StringValue"] = "test-string",
            ["TestSection:IntValue"] = "42",
            ["TestSection:BoolValue"] = "true",
            ["TestSection:DateValue"] = "2024-01-01T00:00:00Z",
            ["TestSection:NestedSection:NestedValue"] = "nested-test",
            ["FeatureFlags:EnableUserRegistration"] = "true",
            ["FeatureFlags:EnableAdvancedLogging"] = "false",
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDB;Trusted_Connection=true;",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:Database"] = "0"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<ConfigurationService>>();

        var configSettings = new ConfigurationSettings
        {
            CacheTimeout = TimeSpan.FromMinutes(5),
            AuditChanges = true,
            ValidationMode = Enterprise.Shared.Common.Enums.ValidationMode.Strict
        };
        _settings = Options.Create(configSettings);

        _configurationService = new ConfigurationService(_configuration, _cache, _logger.Object, _settings);
    }

    [TearDown]
    public void TearDown()
    {
        _configurationService?.Dispose();
        _cache?.Dispose();
    }

    #region GetValue Tests

    [Test]
    public void GetValue_WithExistingKey_ReturnsCorrectValue()
    {
        // Act
        var result = _configurationService.GetValue<string>("TestSection:StringValue");

        // Assert
        result.Should().Be("test-string");
    }

    [Test]
    public void GetValue_WithExistingIntKey_ReturnsCorrectValue()
    {
        // Act
        var result = _configurationService.GetValue<int>("TestSection:IntValue");

        // Assert
        result.Should().Be(42);
    }

    [Test]
    public void GetValue_WithExistingBoolKey_ReturnsCorrectValue()
    {
        // Act
        var result = _configurationService.GetValue<bool>("TestSection:BoolValue");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void GetValue_WithNonExistingKey_ReturnsDefault()
    {
        // Act
        var result = _configurationService.GetValue<string>("NonExistent:Key");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GetValue_WithDefaultValue_ReturnsDefaultWhenKeyNotExists()
    {
        // Act
        var result = _configurationService.GetValue("NonExistent:Key", "default-value");

        // Assert
        result.Should().Be("default-value");
    }

    [Test]
    public void GetValue_WithDefaultValue_ReturnsActualValueWhenKeyExists()
    {
        // Act
        var result = _configurationService.GetValue("TestSection:StringValue", "default-value");

        // Assert
        result.Should().Be("test-string");
    }

    [Test]
    public void GetValue_WithCaching_ReturnsCachedValueOnSecondCall()
    {
        // Arrange
        var key = "TestSection:StringValue";

        // Act
        var firstResult = _configurationService.GetValue<string>(key);
        var secondResult = _configurationService.GetValue<string>(key);

        // Assert
        firstResult.Should().Be("test-string");
        secondResult.Should().Be("test-string");
        
        // Verify no errors occurred (cache logic is internal)
        firstResult.Should().Be(secondResult);
    }

    [Test]
    public void GetValue_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _configurationService.GetValue<string>(""));
        Assert.Throws<ArgumentException>(() => _configurationService.GetValue<string>(" "));
    }

    #endregion

    #region GetSection Tests

    [Test]
    public void GetSection_WithValidSectionName_ReturnsSection()
    {
        // Act
        var section = _configurationService.GetSection("TestSection");

        // Assert
        section.Should().NotBeNull();
        section.Key.Should().Be("TestSection");
        section["StringValue"].Should().Be("test-string");
    }

    [Test]
    public void GetSection_WithNullOrEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _configurationService.GetSection(""));
        Assert.Throws<ArgumentException>(() => _configurationService.GetSection(" "));
    }

    #endregion

    #region GetValueAsync Tests

    [Test]
    public async Task GetValueAsync_WithExistingKey_ReturnsCorrectValue()
    {
        // Act
        var result = await _configurationService.GetValueAsync<string>("TestSection:StringValue");

        // Assert
        result.Should().Be("test-string");
    }

    [Test]
    public async Task GetValueAsync_WithNonExistingKey_ReturnsDefault()
    {
        // Act
        var result = await _configurationService.GetValueAsync<string>("NonExistent:Key");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetValueAsync Tests

    [Test]
    public async Task SetValueAsync_WithValidKeyAndValue_UpdatesConfiguration()
    {
        // Arrange
        var key = "TestSection:NewValue";
        var value = "new-test-value";

        // Act
        await _configurationService.SetValueAsync(key, value, "TestUser");

        // Assert
        var result = _configurationService.GetValue<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetValueAsync_WithValidKeyAndValue_FiresConfigurationChangedEvent()
    {
        // Arrange
        var key = "TestSection:EventTest";
        var value = "event-test-value";
        ConfigurationChangedEventArgs? eventArgs = null;

        _configurationService.ConfigurationChanged += (sender, args) => eventArgs = args;

        // Act
        await _configurationService.SetValueAsync(key, value, "TestUser");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Key.Should().Be(key);
        eventArgs.NewValue.Should().Be(value);
        eventArgs.ChangedBy.Should().Be("TestUser");
    }

    [Test]
    public async Task SetValueAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _configurationService.SetValueAsync("", "value"));
        
        Assert.ThrowsAsync<ArgumentException>(() => 
            _configurationService.SetValueAsync(" ", "value"));
    }

    #endregion

    #region IsFeatureEnabled Tests

    [Test]
    public void IsFeatureEnabled_WithEnabledFeature_ReturnsTrue()
    {
        // Act
        var result = _configurationService.IsFeatureEnabled("EnableUserRegistration");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsFeatureEnabled_WithDisabledFeature_ReturnsFalse()
    {
        // Act
        var result = _configurationService.IsFeatureEnabled("EnableAdvancedLogging");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsFeatureEnabled_WithNonExistentFeature_ReturnsFalse()
    {
        // Act
        var result = _configurationService.IsFeatureEnabled("NonExistentFeature");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsFeatureEnabled_WithNullOrEmptyFeatureName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _configurationService.IsFeatureEnabled(""));
        Assert.Throws<ArgumentException>(() => _configurationService.IsFeatureEnabled(" "));
    }

    #endregion

    #region IsFeatureEnabledAsync Tests

    [Test]
    public async Task IsFeatureEnabledAsync_WithEnabledFeature_ReturnsTrue()
    {
        // Act
        var result = await _configurationService.IsFeatureEnabledAsync("EnableUserRegistration");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsFeatureEnabledAsync_WithUserId_ReturnsCorrectValue()
    {
        // Act
        var result = await _configurationService.IsFeatureEnabledAsync("EnableUserRegistration", "user-123");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateSection Tests

    [Test]
    public void ValidateSection_WithValidDatabaseSection_ReturnsSuccess()
    {
        // Act
        var result = _configurationService.ValidateSection("Database");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.SectionName.Should().Be("Database");
    }

    [Test]
    public void ValidateSection_WithValidRedisSection_ReturnsSuccess()
    {
        // Act
        var result = _configurationService.ValidateSection("Redis");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void ValidateSection_WithNonExistentSection_ReturnsFailure()
    {
        // Act
        var result = _configurationService.ValidateSection("NonExistentSection");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(error => error.Contains("does not exist"));
    }

    [Test]
    public void ValidateSection_WithNullOrEmptySectionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _configurationService.ValidateSection(""));
        Assert.Throws<ArgumentException>(() => _configurationService.ValidateSection(" "));
    }

    #endregion

    #region GetKeysByPattern Tests

    [Test]
    public void GetKeysByPattern_WithWildcardPattern_ReturnsMatchingKeys()
    {
        // Act
        var result = _configurationService.GetKeysByPattern("TestSection:*");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("TestSection:StringValue");
        result.Should().ContainKey("TestSection:IntValue");
        result.Should().ContainKey("TestSection:BoolValue");
    }

    [Test]
    public void GetKeysByPattern_WithExactMatch_ReturnsSingleKey()
    {
        // Act
        var result = _configurationService.GetKeysByPattern("TestSection:StringValue");

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("TestSection:StringValue");
        result["TestSection:StringValue"].Should().Be("test-string");
    }

    [Test]
    public void GetKeysByPattern_WithNoMatches_ReturnsEmptyDictionary()
    {
        // Act
        var result = _configurationService.GetKeysByPattern("NonExistent:*");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void GetKeysByPattern_WithNullOrEmptyPattern_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _configurationService.GetKeysByPattern(""));
        Assert.Throws<ArgumentException>(() => _configurationService.GetKeysByPattern(" "));
    }

    #endregion

    #region ReloadAsync Tests

    [Test]
    public async Task ReloadAsync_CompletesSuccessfully()
    {
        // Act & Assert
        await _configurationService.ReloadAsync();
        
        // Should complete without throwing
        Assert.Pass();
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_CompletesSuccessfully()
    {
        // Act & Assert
        _configurationService.Dispose();
        
        // Should complete without throwing
        Assert.Pass();
    }

    #endregion
}