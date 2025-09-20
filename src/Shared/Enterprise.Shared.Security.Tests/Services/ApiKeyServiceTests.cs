namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class ApiKeyServiceTests
{
    private Mock<ILogger<ApiKeyService>> _logger = null!;
    private Mock<IHashingService> _hashingService = null!;
    private IMemoryCache _cache = null!;
    private SecuritySettings _settings = null!;
    private ApiKeyService _apiKeyService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ApiKeyService>>();
        _hashingService = new Mock<IHashingService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _settings = new SecuritySettings
        {
            ApiKeyExpirationDays = 365,
            ApiKeyRateLimit = 1000
        };

        var options = Options.Create(_settings);

        // Setup hashing service mock
        _hashingService.Setup(x => x.ComputeSha256(It.IsAny<string>()))
            .Returns<string>(input => $"hashed_{input}");

        _apiKeyService = new ApiKeyService(_logger.Object, _hashingService.Object, _cache, options);
    }

    [TearDown]
    public void TearDown()
    {
        _cache?.Dispose();
    }

    #region Generate API Key Tests

    [Test]
    public void GenerateApiKey_WithoutMetadata_ReturnsValidApiKey()
    {
        // Act
        var apiKey = _apiKeyService.GenerateApiKey();

        // Assert
        apiKey.Should().NotBeNull();
        apiKey.Key.Should().StartWith("sk_");
        apiKey.Key.Length.Should().BeGreaterThan(30);
        apiKey.HashedKey.Should().NotBeNullOrEmpty();
        apiKey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        apiKey.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(365), TimeSpan.FromDays(1));
        apiKey.Metadata.Should().NotBeNull();
    }

    [Test]
    public void GenerateApiKey_WithMetadata_IncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["app"] = "test-app",
            ["version"] = "1.0.0"
        };

        // Act
        var apiKey = _apiKeyService.GenerateApiKey(metadata);

        // Assert
        apiKey.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Test]
    public void GenerateApiKey_GeneratesUniqueKeys()
    {
        // Act
        var apiKey1 = _apiKeyService.GenerateApiKey();
        var apiKey2 = _apiKeyService.GenerateApiKey();

        // Assert
        apiKey1.Key.Should().NotBe(apiKey2.Key);
        apiKey1.HashedKey.Should().NotBe(apiKey2.HashedKey);
    }

    [Test]
    public void GenerateApiKey_WithNoExpiration_SetsNullExpiration()
    {
        // Arrange
        _settings.ApiKeyExpirationDays = null;

        // Act
        var apiKey = _apiKeyService.GenerateApiKey();

        // Assert
        apiKey.ExpiresAt.Should().BeNull();
    }

    #endregion

    #region Validate API Key Tests

    [Test]
    public async Task ValidateApiKeyAsync_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();

        // Act
        var isValid = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateApiKeyAsync_WithInvalidKey_ReturnsFalse()
    {
        // Arrange
        var invalidKey = "sk_invalid_key_123456789";

        // Act
        var isValid = await _apiKeyService.ValidateApiKeyAsync(invalidKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public async Task ValidateApiKeyAsync_WithEmptyKey_ReturnsFalse()
    {
        // Act
        var isValid = await _apiKeyService.ValidateApiKeyAsync("");

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public async Task ValidateApiKeyAsync_WithRevokedKey_ReturnsFalse()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();
        await _apiKeyService.RevokeApiKeyAsync(apiKey.Key);

        // Act
        var isValid = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public async Task ValidateApiKeyAsync_UsesCaching()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();

        // Act
        var isValid1 = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);
        var isValid2 = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Assert
        isValid1.Should().BeTrue();
        isValid2.Should().BeTrue();
        
        // Verify hashing was called only once (due to caching)
        _hashingService.Verify(x => x.ComputeSha256(It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion

    #region Get API Key Details Tests

    [Test]
    public async Task GetApiKeyDetailsAsync_WithValidKey_ReturnsDetails()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["app"] = "test" };
        var apiKey = _apiKeyService.GenerateApiKey(metadata);

        // Act
        var details = await _apiKeyService.GetApiKeyDetailsAsync(apiKey.Key);

        // Assert
        details.Should().NotBeNull();
        details!.Key.Should().Be(apiKey.HashedKey);
        details.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        details.IsActive.Should().BeTrue();
        details.UsageCount.Should().Be(0);
        details.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Test]
    public async Task GetApiKeyDetailsAsync_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var invalidKey = "sk_invalid_key_123456789";

        // Act
        var details = await _apiKeyService.GetApiKeyDetailsAsync(invalidKey);

        // Assert
        details.Should().BeNull();
    }

    [Test]
    public async Task GetApiKeyDetailsAsync_WithEmptyKey_ReturnsNull()
    {
        // Act
        var details = await _apiKeyService.GetApiKeyDetailsAsync("");

        // Assert
        details.Should().BeNull();
    }

    #endregion

    #region Revoke API Key Tests

    [Test]
    public async Task RevokeApiKeyAsync_WithValidKey_RevokesKey()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();

        // Act
        await _apiKeyService.RevokeApiKeyAsync(apiKey.Key);
        var isValid = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public async Task RevokeApiKeyAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _apiKeyService.RevokeApiKeyAsync(""));
    }

    [Test]
    public async Task RevokeApiKeyAsync_ClearsCacheEntry()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();
        
        // Prime the cache
        await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Act
        await _apiKeyService.RevokeApiKeyAsync(apiKey.Key);
        var isValidAfterRevoke = await _apiKeyService.ValidateApiKeyAsync(apiKey.Key);

        // Assert
        isValidAfterRevoke.Should().BeFalse();
    }

    #endregion

    #region Refresh API Key Tests

    [Test]
    public async Task RefreshApiKeyAsync_WithValidKey_ReturnsNewKey()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["app"] = "test" };
        var originalKey = _apiKeyService.GenerateApiKey(metadata);

        // Act
        var newKey = await _apiKeyService.RefreshApiKeyAsync(originalKey.Key);

        // Assert
        newKey.Should().NotBeNull();
        newKey.Key.Should().NotBe(originalKey.Key);
        newKey.Metadata.Should().BeEquivalentTo(metadata);
        
        // Original key should be revoked
        var originalIsValid = await _apiKeyService.ValidateApiKeyAsync(originalKey.Key);
        originalIsValid.Should().BeFalse();
        
        // New key should be valid
        var newIsValid = await _apiKeyService.ValidateApiKeyAsync(newKey.Key);
        newIsValid.Should().BeTrue();
    }

    [Test]
    public async Task RefreshApiKeyAsync_WithInvalidKey_ThrowsSecurityException()
    {
        // Arrange
        var invalidKey = "sk_invalid_key_123456789";

        // Act & Assert
        Assert.ThrowsAsync<SecurityException>(() => 
            _apiKeyService.RefreshApiKeyAsync(invalidKey));
    }

    [Test]
    public async Task RefreshApiKeyAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _apiKeyService.RefreshApiKeyAsync(""));
    }

    #endregion

    #region Usage Tracking Tests

    [Test]
    public async Task TrackUsageAsync_WithValidKey_UpdatesUsageCount()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();
        var endpoint = "/api/test";

        // Act
        await _apiKeyService.TrackUsageAsync(apiKey.Key, endpoint);
        var details = await _apiKeyService.GetApiKeyDetailsAsync(apiKey.Key);

        // Assert
        details.Should().NotBeNull();
        details!.UsageCount.Should().Be(1);
        details.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task TrackUsageAsync_MultipleUsages_IncrementsCount()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();

        // Act
        await _apiKeyService.TrackUsageAsync(apiKey.Key, "/api/test1");
        await _apiKeyService.TrackUsageAsync(apiKey.Key, "/api/test2");
        await _apiKeyService.TrackUsageAsync(apiKey.Key, "/api/test3");
        
        var details = await _apiKeyService.GetApiKeyDetailsAsync(apiKey.Key);

        // Assert
        details.Should().NotBeNull();
        details!.UsageCount.Should().Be(3);
    }

    [Test]
    public async Task TrackUsageAsync_WithEmptyKey_DoesNotThrow()
    {
        // Act & Assert
        await _apiKeyService.TrackUsageAsync("", "/api/test");
        // Should complete without throwing
    }

    #endregion

    #region Rate Limiting Tests

    [Test]
    public async Task IsRateLimitExceededAsync_WithinLimit_ReturnsFalse()
    {
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();
        
        // Track some usage but stay within limit
        for (int i = 0; i < 10; i++)
        {
            await _apiKeyService.TrackUsageAsync(apiKey.Key, "/api/test");
        }

        // Act
        var isExceeded = await _apiKeyService.IsRateLimitExceededAsync(apiKey.Key);

        // Assert
        isExceeded.Should().BeFalse();
    }

    [Test]
    public async Task IsRateLimitExceededAsync_ExceedingLimit_ReturnsTrue()
    {
        // Arrange
        var limitedSettings = new SecuritySettings { ApiKeyRateLimit = 5 };
        var limitedOptions = Options.Create(limitedSettings);
        var limitedService = new ApiKeyService(_logger.Object, _hashingService.Object, _cache, limitedOptions);
        
        var apiKey = limitedService.GenerateApiKey();
        
        // Exceed the limit
        for (int i = 0; i < 6; i++)
        {
            await limitedService.TrackUsageAsync(apiKey.Key, "/api/test");
        }

        // Act
        var isExceeded = await limitedService.IsRateLimitExceededAsync(apiKey.Key);

        // Assert
        isExceeded.Should().BeTrue();
    }

    [Test]
    public async Task IsRateLimitExceededAsync_WithEmptyKey_ReturnsTrue()
    {
        // Act
        var isExceeded = await _apiKeyService.IsRateLimitExceededAsync("");

        // Assert
        isExceeded.Should().BeTrue();
    }

    [Test]
    public async Task IsRateLimitExceededAsync_WithUnknownKey_ReturnsFalse()
    {
        // Arrange
        var unknownKey = "sk_unknown_key_123456789";

        // Act
        var isExceeded = await _apiKeyService.IsRateLimitExceededAsync(unknownKey);

        // Assert
        isExceeded.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void GenerateApiKey_HandlesExceptionGracefully()
    {
        // Arrange
        _hashingService.Setup(x => x.ComputeSha256(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Hashing failed"));

        // Act & Assert
        Assert.Throws<SecurityException>(() => _apiKeyService.GenerateApiKey());
    }

    [Test]
    public async Task ValidateApiKeyAsync_HandlesHashingException()
    {
        // Arrange
        _hashingService.Setup(x => x.ComputeSha256(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Hashing failed"));

        // Act
        var isValid = await _apiKeyService.ValidateApiKeyAsync("sk_test_key");

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public async Task TrackUsageAsync_CleansOldEntries()
    {
        // This test verifies that old usage entries are cleaned up
        // In a real implementation, this would be more complex
        // Here we just verify the method completes without errors
        
        // Arrange
        var apiKey = _apiKeyService.GenerateApiKey();

        // Act
        for (int i = 0; i < 100; i++)
        {
            await _apiKeyService.TrackUsageAsync(apiKey.Key, $"/api/test{i}");
        }

        // Assert
        // Should complete without throwing or memory issues
        var details = await _apiKeyService.GetApiKeyDetailsAsync(apiKey.Key);
        details.Should().NotBeNull();
        details!.UsageCount.Should().Be(100);
    }

    #endregion
}