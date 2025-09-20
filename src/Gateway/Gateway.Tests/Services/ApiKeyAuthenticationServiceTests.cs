using Gateway.Core.Services;
using Gateway.Core.Configuration;
using Gateway.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gateway.Tests.Services;

public class ApiKeyAuthenticationServiceTests
{
    private readonly Mock<ILogger<ApiKeyAuthenticationService>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly ApiKeyAuthenticationService _service;
    private readonly GatewayOptions _gatewayOptions;

    public ApiKeyAuthenticationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApiKeyAuthenticationService>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions
        {
            Security = new SecurityOptions
            {
                EnableApiKeyAuthentication = true
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
        _service = new ApiKeyAuthenticationService(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidDefaultKey_ShouldReturnSuccess()
    {
        // Arrange
        var defaultApiKey = "gw_dev_admin_key_12345678901234567890";

        // Act
        var result = await _service.ValidateApiKeyAsync(defaultApiKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("API", result.Value.FirstName);
        Assert.Equal("Key", result.Value.LastName);
        Assert.Contains("api-client", result.Value.Roles);
        Assert.Contains("admin", result.Value.Permissions);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidKey_ShouldReturnFailure()
    {
        // Arrange
        var invalidApiKey = "invalid-api-key";

        // Act
        var result = await _service.ValidateApiKeyAsync(invalidApiKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid API key", result.Error);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithEmptyKey_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("API key is required", result.Error);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Test API Key";
        var permissions = new List<string> { "read", "write" };

        // Act
        var result = await _service.CreateApiKeyAsync(name, permissions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(permissions, result.Value.Permissions);
        Assert.True(result.Value.IsActive);
        Assert.NotNull(result.Value.PlainKey);
        Assert.StartsWith("gw_", result.Value.PlainKey);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithEmptyName_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CreateApiKeyAsync("", new List<string>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("name is required", result.Error);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithExpiryDate_ShouldCreateExpirableKey()
    {
        // Arrange
        var name = "Expirable Key";
        var permissions = new List<string> { "read" };
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var result = await _service.CreateApiKeyAsync(name, permissions, expiresAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expiresAt.Date, result.Value.ExpiresAt?.Date);
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithValidKey_ShouldReturnSuccess()
    {
        // Arrange
        var createResult = await _service.CreateApiKeyAsync("Test Key", new List<string> { "read" });
        var apiKey = createResult.Value!.PlainKey!;

        // Act
        var result = await _service.RevokeApiKeyAsync(apiKey);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify key is inactive
        var validateResult = await _service.ValidateApiKeyAsync(apiKey);
        Assert.False(validateResult.IsSuccess);
        Assert.Contains("inactive", validateResult.Error.ToLower());
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithInvalidKey_ShouldReturnFailure()
    {
        // Act
        var result = await _service.RevokeApiKeyAsync("invalid-key");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task GetApiKeysAsync_ShouldReturnAllKeys()
    {
        // Arrange
        await _service.CreateApiKeyAsync("Key 1", new List<string> { "read" });
        await _service.CreateApiKeyAsync("Key 2", new List<string> { "write" });

        // Act
        var result = await _service.GetApiKeysAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var keys = result.Value.ToList();
        Assert.True(keys.Count >= 3); // Default key + 2 created keys
        
        // Ensure plain keys are not returned in list
        Assert.All(keys, key => Assert.Null(key.PlainKey));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithCreatedKey_ShouldTrackUsage()
    {
        // Arrange
        var createResult = await _service.CreateApiKeyAsync("Usage Test", new List<string> { "read" });
        var apiKey = createResult.Value!.PlainKey!;

        // Act
        var result1 = await _service.ValidateApiKeyAsync(apiKey);
        var result2 = await _service.ValidateApiKeyAsync(apiKey);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        
        // Verify usage is tracked (this is a basic check, actual implementation may vary)
        var keysResult = await _service.GetApiKeysAsync();
        var usageTestKey = keysResult.Value!.FirstOrDefault(k => k.Name == "Usage Test");
        Assert.NotNull(usageTestKey);
        Assert.True(usageTestKey.UsageCount > 0);
        Assert.NotNull(usageTestKey.LastUsedAt);
    }

    [Theory]
    [InlineData("gw_test_key_123")]
    [InlineData("gw_prod_key_456")]
    [InlineData("gw_staging_key_789")]
    public async Task ValidateApiKeyAsync_WithDifferentKeyFormats_ShouldHandleCorrectly(string keyFormat)
    {
        // Arrange - Create a key with specific format (this is a conceptual test)
        var createResult = await _service.CreateApiKeyAsync($"Test {keyFormat}", new List<string> { "read" });
        var actualKey = createResult.Value!.PlainKey!;

        // Act
        var result = await _service.ValidateApiKeyAsync(actualKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithExpiredKey_ShouldReturnFailure()
    {
        // Arrange
        var name = "Expired Key";
        var permissions = new List<string> { "read" };
        var pastDate = DateTime.UtcNow.AddDays(-1); // Already expired

        var createResult = await _service.CreateApiKeyAsync(name, permissions, pastDate);
        var apiKey = createResult.Value!.PlainKey!;

        // Act
        var result = await _service.ValidateApiKeyAsync(apiKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("expired", result.Error.ToLower());
    }
}