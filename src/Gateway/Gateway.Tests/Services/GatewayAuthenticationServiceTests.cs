using Gateway.Core.Services;
using Gateway.Core.Configuration;
using Gateway.Core.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Gateway.Tests.Services;

public class GatewayAuthenticationServiceTests
{
    private readonly Mock<ILogger<GatewayAuthenticationService>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly GatewayAuthenticationService _service;
    private readonly GatewayOptions _gatewayOptions;

    public GatewayAuthenticationServiceTests()
    {
        _mockLogger = new Mock<ILogger<GatewayAuthenticationService>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions
        {
            Security = new SecurityOptions
            {
                JwtSecret = "development-secret-key-minimum-256-bits-for-hs256-algorithm",
                JwtIssuer = "https://localhost:5000",
                JwtAudience = "gateway-api"
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
        _service = new GatewayAuthenticationService(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var token = GenerateValidJwtToken();

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("test-user-123", result.Value.UserId);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.Contains("user", result.Value.Roles);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var token = GenerateExpiredJwtToken();

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("expired", result.Error.ToLower());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidSignature_ShouldReturnFailure()
    {
        // Arrange
        var token = GenerateTokenWithInvalidSignature();

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("signature", result.Error.ToLower());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ValidateTokenAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("required", result.Error.ToLower());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithBearerPrefix_ShouldStripPrefixAndValidate()
    {
        // Arrange
        var token = "Bearer " + GenerateValidJwtToken();

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("test-user-123", result.Value.UserId);
    }

    [Fact]
    public async Task CheckPermissionAsync_WithValidParameters_ShouldReturnSuccess()
    {
        // Act
        var result = await _service.CheckPermissionAsync("user123", "identity", "read");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CheckPermissionAsync_WithEmptyUserId_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CheckPermissionAsync("", "identity", "read");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("UserId", result.Error);
    }

    [Fact]
    public async Task CheckPermissionAsync_WithEmptyResource_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CheckPermissionAsync("user123", "", "read");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Resource", result.Error);
    }

    [Theory]
    [InlineData("identity", "read", true)]
    [InlineData("users", "write", true)]
    [InlineData("notifications", "send", true)]
    [InlineData("health", "read", true)]
    [InlineData("unknown", "read", false)]
    [InlineData("identity", "admin", false)]
    public async Task CheckPermissionAsync_WithDifferentResourceActions_ShouldReturnExpectedResult(
        string resource, string action, bool expectedResult)
    {
        // Act
        var result = await _service.CheckPermissionAsync("user123", resource, action);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

    private string GenerateValidJwtToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_gatewayOptions.Security.JwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
                new Claim("sub", "test-user-123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.GivenName, "Test"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Role, "user"),
                new Claim("permission", "read"),
                new Claim("group_id", Guid.NewGuid().ToString()),
                new Claim("group_name", "Test Group")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _gatewayOptions.Security.JwtIssuer,
            Audience = _gatewayOptions.Security.JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateExpiredJwtToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_gatewayOptions.Security.JwtSecret);
        var expiredTime = DateTime.UtcNow.AddMinutes(-10); // Expired 10 minutes ago
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
                new Claim(ClaimTypes.Email, "test@example.com")
            }),
            NotBefore = expiredTime.AddMinutes(-60), // NotBefore must be before Expires
            Expires = expiredTime,
            Issuer = _gatewayOptions.Security.JwtIssuer,
            Audience = _gatewayOptions.Security.JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateTokenWithInvalidSignature()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = Encoding.UTF8.GetBytes("wrong-secret-key-different-from-configuration");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
                new Claim(ClaimTypes.Email, "test@example.com")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _gatewayOptions.Security.JwtIssuer,
            Audience = _gatewayOptions.Security.JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(wrongKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}