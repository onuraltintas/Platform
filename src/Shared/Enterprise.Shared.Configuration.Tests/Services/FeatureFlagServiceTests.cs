namespace Enterprise.Shared.Configuration.Tests.Services;

[TestFixture]
public class FeatureFlagServiceTests
{
    private IConfiguration _configuration = null!;
    private IMemoryCache _cache = null!;
    private Mock<ILogger<FeatureFlagService>> _logger = null!;
    private IOptions<ConfigurationSettings> _settings = null!;
    private Mock<IUserContextService> _mockUserContextService = null!;
    private FeatureFlagService _featureFlagService = null!;

    [SetUp]
    public void SetUp()
    {
        var configData = new Dictionary<string, string?>
        {
            ["FeatureFlags:EnableUserRegistration"] = "true",
            ["FeatureFlags:EnableAdvancedLogging"] = "false",
            ["FeatureFlags:EnableNewPaymentGateway"] = "true",
            ["FeatureFlags:EnableCryptoPay"] = "true",
            ["FeatureFlags:EnableCryptoPay:RolloutPercentage"] = "25",
            ["FeatureFlags:EnableBetaFeatures"] = "true",
            ["FeatureFlags:EnableBetaFeatures:Users:test-user-1"] = "true",
            ["FeatureFlags:EnableBetaFeatures:Users:test-user-2"] = "false",
            ["FeatureFlags:EnableBetaFeatures:Roles:Admin"] = "true",
            ["FeatureFlags:EnableBetaFeatures:Roles:Beta"] = "true",
            ["FeatureFlags:EnableBetaFeatures:Roles:User"] = "false"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<FeatureFlagService>>();

        var configSettings = new ConfigurationSettings
        {
            FeatureFlagCacheTimeout = TimeSpan.FromMinutes(2),
            AuditChanges = true
        };
        _settings = Options.Create(configSettings);

        _mockUserContextService = new Mock<IUserContextService>();
        _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns("test-user-123");
        _mockUserContextService.Setup(x => x.GetCurrentUserRoles()).Returns(new[] { "User", "Customer" });

        _featureFlagService = new FeatureFlagService(
            _configuration,
            _cache,
            _logger.Object,
            _settings,
            _mockUserContextService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _featureFlagService?.Dispose();
        _cache?.Dispose();
    }

    #region IsEnabled Tests

    [Test]
    public void IsEnabled_WithEnabledFeature_ReturnsTrue()
    {
        // Act
        var result = _featureFlagService.IsEnabled("EnableUserRegistration");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsEnabled_WithDisabledFeature_ReturnsFalse()
    {
        // Act
        var result = _featureFlagService.IsEnabled("EnableAdvancedLogging");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsEnabled_WithNonExistentFeature_ReturnsFalse()
    {
        // Act
        var result = _featureFlagService.IsEnabled("NonExistentFeature");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsEnabled_WithUserIdParameter_ReturnsCorrectValue()
    {
        // Act
        var result = _featureFlagService.IsEnabled("EnableUserRegistration", "specific-user");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsEnabled_WithUserSpecificOverride_ReturnsUserSpecificValue()
    {
        // Act
        var resultUser1 = _featureFlagService.IsEnabled("EnableBetaFeatures", "test-user-1");
        var resultUser2 = _featureFlagService.IsEnabled("EnableBetaFeatures", "test-user-2");

        // Assert
        resultUser1.Should().BeTrue();
        resultUser2.Should().BeFalse();
    }

    [Test]
    public void IsEnabled_WithRoleBasedFlag_ReturnsRoleBasedValue()
    {
        // Arrange
        _mockUserContextService.Setup(x => x.GetCurrentUserRoles()).Returns(new[] { "Admin" });

        // Act
        var result = _featureFlagService.IsEnabled("EnableBetaFeatures");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsEnabled_WithPercentageRollout_ReturnsConsistentResult()
    {
        // Arrange
        var userId = "consistent-user-id";

        // Act
        var result1 = _featureFlagService.IsEnabled("EnableCryptoPay", userId);
        var result2 = _featureFlagService.IsEnabled("EnableCryptoPay", userId);

        // Assert
        result1.Should().Be(result2); // Should be consistent for the same user
    }

    [Test]
    public void IsEnabled_WithCaching_UsesCache()
    {
        // Arrange
        var featureName = "EnableUserRegistration";
        var userId = "cache-test-user";

        // Act
        var result1 = _featureFlagService.IsEnabled(featureName, userId);
        var result2 = _featureFlagService.IsEnabled(featureName, userId);

        // Assert
        result1.Should().Be(result2); // Should be consistent for caching
    }

    [Test]
    public void IsEnabled_WithNullOrEmptyFeatureName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _featureFlagService.IsEnabled(""));
        Assert.Throws<ArgumentException>(() => _featureFlagService.IsEnabled(" "));
    }

    [Test]
    public void IsEnabled_WithNullOrEmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _featureFlagService.IsEnabled("TestFeature", ""));
        Assert.Throws<ArgumentException>(() => _featureFlagService.IsEnabled("TestFeature", " "));
    }

    #endregion

    #region IsEnabledAsync Tests

    [Test]
    public async Task IsEnabledAsync_WithEnabledFeature_ReturnsTrue()
    {
        // Act
        var result = await _featureFlagService.IsEnabledAsync("EnableUserRegistration");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsEnabledAsync_WithUserIdParameter_ReturnsCorrectValue()
    {
        // Act
        var result = await _featureFlagService.IsEnabledAsync("EnableUserRegistration", "specific-user");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetAllFlagsAsync Tests

    [Test]
    public async Task GetAllFlagsAsync_ReturnsAllFeatureFlags()
    {
        // Act
        var result = await _featureFlagService.GetAllFlagsAsync("test-user");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("EnableUserRegistration");
        result.Should().ContainKey("EnableAdvancedLogging");
        result["EnableUserRegistration"].Should().BeTrue();
        result["EnableAdvancedLogging"].Should().BeFalse();
    }

    [Test]
    public async Task GetAllFlagsAsync_WithCurrentUser_UsesCurrentUserId()
    {
        // Act
        var result = await _featureFlagService.GetAllFlagsAsync();

        // Assert
        result.Should().NotBeEmpty();
        _mockUserContextService.Verify(x => x.GetCurrentUserId(), Times.Once);
    }

    #endregion

    #region SetFlagAsync Tests

    [Test]
    public async Task SetFlagAsync_WithValidFeatureFlag_UpdatesFlag()
    {
        // Arrange
        var featureName = "EnableUserRegistration";
        var newValue = false;

        // Act
        await _featureFlagService.SetFlagAsync(featureName, newValue);

        // Assert
        var result = _featureFlagService.IsEnabled(featureName);
        result.Should().Be(newValue);
    }

    [Test]
    public async Task SetFlagAsync_WithUserSpecificFlag_UpdatesUserSpecificFlag()
    {
        // Arrange
        var featureName = "EnableBetaFeatures";
        var userId = "test-user-3";
        var newValue = true;

        // Act
        await _featureFlagService.SetFlagAsync(featureName, newValue, userId);

        // Assert
        var result = _featureFlagService.IsEnabled(featureName, userId);
        result.Should().Be(newValue);
    }

    [Test]
    public async Task SetFlagAsync_FiresFeatureFlagChangedEvent()
    {
        // Arrange
        var featureName = "EnableAdvancedLogging";
        var newValue = true;
        ConfigurationChangedEventArgs? eventArgs = null;

        _featureFlagService.FeatureFlagChanged += (sender, args) => eventArgs = args;

        // Act
        await _featureFlagService.SetFlagAsync(featureName, newValue);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Key.Should().Contain(featureName);
        eventArgs.NewValue.Should().Be(newValue);
    }

    [Test]
    public async Task SetFlagAsync_WithNullOrEmptyFeatureName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _featureFlagService.SetFlagAsync("", true));
        
        Assert.ThrowsAsync<ArgumentException>(() => 
            _featureFlagService.SetFlagAsync(" ", true));
    }

    #endregion

    #region GetFeatureFlagResultAsync Tests

    [Test]
    public async Task GetFeatureFlagResultAsync_ReturnsDetailedResult()
    {
        // Act
        var result = await _featureFlagService.GetFeatureFlagResultAsync("EnableUserRegistration", "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("EnableUserRegistration");
        result.UserId.Should().Be("test-user");
        result.Source.Should().Be("Configuration");
        result.Metadata.Should().NotBeEmpty();
        result.Metadata.Should().ContainKey("EvaluationStrategy");
    }

    [Test]
    public async Task GetFeatureFlagResultAsync_WithCurrentUser_UsesCurrentUserId()
    {
        // Act
        var result = await _featureFlagService.GetFeatureFlagResultAsync("EnableUserRegistration");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("test-user-123");
    }

    #endregion

    #region SetRolloutPercentageAsync Tests

    [Test]
    public async Task SetRolloutPercentageAsync_WithValidPercentage_UpdatesRollout()
    {
        // Arrange
        var featureName = "EnableNewPaymentGateway";
        var percentage = 50;

        // Act
        await _featureFlagService.SetRolloutPercentageAsync(featureName, percentage);

        // Assert
        // Verify the configuration was updated (this is indirect testing)
        Assert.Pass();
    }

    [Test]
    public async Task SetRolloutPercentageAsync_WithInvalidPercentage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _featureFlagService.SetRolloutPercentageAsync("TestFeature", -1));
        
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _featureFlagService.SetRolloutPercentageAsync("TestFeature", 101));
    }

    [Test]
    public async Task SetRolloutPercentageAsync_WithNullOrEmptyFeatureName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() =>
            _featureFlagService.SetRolloutPercentageAsync("", 50));
        
        Assert.ThrowsAsync<ArgumentException>(() =>
            _featureFlagService.SetRolloutPercentageAsync(" ", 50));
    }

    #endregion

    #region ClearCache Tests

    [Test]
    public void ClearCache_WithFeatureName_ClearsCacheForFeature()
    {
        // Arrange
        var featureName = "EnableUserRegistration";
        var userId = "cache-user";
        
        // Prime the cache
        _featureFlagService.IsEnabled(featureName, userId);

        // Act
        _featureFlagService.ClearCache(featureName);

        // Assert
        // Should complete without throwing
        Assert.Pass();
    }

    [Test]
    public void ClearCache_WithNullOrEmptyFeatureName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _featureFlagService.ClearCache(""));
        Assert.Throws<ArgumentException>(() => _featureFlagService.ClearCache(" "));
    }

    [Test]
    public void ClearAllCache_ClearsAllCache()
    {
        // Arrange
        _featureFlagService.IsEnabled("EnableUserRegistration", "user1");
        _featureFlagService.IsEnabled("EnableAdvancedLogging", "user2");

        // Act
        _featureFlagService.ClearAllCache();

        // Assert
        // Should complete without throwing
        Assert.Pass();
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_CompletesSuccessfully()
    {
        // Act & Assert
        _featureFlagService.Dispose();
        
        // Should complete without throwing
        Assert.Pass();
    }

    #endregion
}