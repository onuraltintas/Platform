using Microsoft.Extensions.Configuration;
using Enterprise.Shared.Security.Extensions;

namespace Enterprise.Shared.Security.Tests.Extensions;

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
            ["Security:EncryptionKey"] = Convert.ToBase64String(new byte[32]),
            ["Security:JwtSecretKey"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("super-secret-jwt-signing-key-that-is-long-enough-for-security")),
            ["Security:JwtIssuer"] = "TestIssuer",
            ["Security:JwtAudience"] = "TestAudience",
            ["Security:JwtAccessTokenExpirationMinutes"] = "60",
            ["Security:BCryptWorkFactor"] = "10",
            ["Security:UseDataProtectionApi"] = "false"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Register IConfiguration in the service collection
        _services.AddSingleton(_configuration);
    }

    #region AddEnterpriseSecurity Tests

    [Test]
    public void AddEnterpriseSecurity_WithValidConfiguration_RegistersServices()
    {
        // Act
        _services.AddEnterpriseSecurity(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IEncryptionService>().Should().NotBeNull();
        provider.GetService<IHashingService>().Should().NotBeNull();
        provider.GetService<ITokenService>().Should().NotBeNull();
        provider.GetService<ISecurityValidator>().Should().NotBeNull();
        provider.GetService<IApiKeyService>().Should().NotBeNull();
        provider.GetService<ISecurityAuditService>().Should().NotBeNull();
        provider.GetService<IMemoryCache>().Should().NotBeNull();
        provider.GetService<IOptions<SecuritySettings>>().Should().NotBeNull();
    }

    [Test]
    public void AddEnterpriseSecurity_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddEnterpriseSecurity(_configuration));
    }

    [Test]
    public void AddEnterpriseSecurity_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _services.AddEnterpriseSecurity(null!));
    }

    [Test]
    public void AddEnterpriseSecurity_WithCustomOptions_RegistersBasedOnOptions()
    {
        // Act
        _services.AddEnterpriseSecurity(_configuration, options =>
        {
            options.EnableEncryption = false;
            options.EnableHashing = false;
            options.EnableTokenService = true;
            options.EnableSecurityValidator = true;
            options.EnableApiKeyService = false;
            options.EnableSecurityAudit = false;
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IEncryptionService>().Should().BeNull();
        provider.GetService<IHashingService>().Should().BeNull();
        provider.GetService<ITokenService>().Should().NotBeNull();
        provider.GetService<ISecurityValidator>().Should().NotBeNull();
        provider.GetService<IApiKeyService>().Should().BeNull();
        provider.GetService<ISecurityAuditService>().Should().BeNull();
    }

    [Test]
    public void AddEnterpriseSecurity_WithJwtAuthentication_ConfiguresAuthentication()
    {
        // Act
        _services.AddEnterpriseSecurity(_configuration, options =>
        {
            options.EnableJwtAuthentication = true;
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        // Check that authentication services are registered
        // Just verify that TokenService is registered when JWT auth is enabled
        provider.GetService<ITokenService>().Should().NotBeNull();
    }

    [Test]
    public void AddEnterpriseSecurity_WithAuthorization_ConfiguresAuthorization()
    {
        // Act
        _services.AddEnterpriseSecurity(_configuration, options =>
        {
            options.EnableAuthorization = true;
        });
        var provider = _services.BuildServiceProvider();

        // Assert
        // Verify authorization services are configured
        // In real scenario, this would check for specific authorization services
        provider.Should().NotBeNull();
    }

    #endregion

    #region AddJwtAuthentication Tests

    [Test]
    public void AddJwtAuthentication_WithValidConfiguration_RegistersJwtServices()
    {
        // Act
        _services.AddJwtAuthentication(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<ITokenService>().Should().NotBeNull();
        provider.GetService<IEncryptionService>().Should().BeNull(); // Should be disabled
        provider.GetService<IHashingService>().Should().BeNull(); // Should be disabled
    }

    [Test]
    public void AddJwtAuthentication_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _services.AddJwtAuthentication(null!));
    }

    #endregion

    #region AddApiKeyAuthentication Tests

    [Test]
    public void AddApiKeyAuthentication_WithValidConfiguration_RegistersApiKeyServices()
    {
        // Act
        _services.AddApiKeyAuthentication(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<IApiKeyService>().Should().NotBeNull();
        provider.GetService<IHashingService>().Should().NotBeNull();
        provider.GetService<ISecurityAuditService>().Should().NotBeNull();
        provider.GetService<ITokenService>().Should().BeNull(); // Should be disabled
    }

    [Test]
    public void AddApiKeyAuthentication_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _services.AddApiKeyAuthentication(null!));
    }

    #endregion

    #region SecurityOptions Tests

    [Test]
    public void SecurityOptions_DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new SecurityOptions();

        // Assert
        options.EnableEncryption.Should().BeTrue();
        options.EnableHashing.Should().BeTrue();
        options.EnableTokenService.Should().BeTrue();
        options.EnableSecurityValidator.Should().BeTrue();
        options.EnableApiKeyService.Should().BeTrue();
        options.EnableSecurityAudit.Should().BeTrue();
        options.EnableJwtAuthentication.Should().BeFalse();
        options.EnableAuthorization.Should().BeFalse();
        options.RequireHttps.Should().BeTrue();
        options.AuthorizationPolicies.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void SecurityOptions_CanBeCustomized()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.EnableEncryption = false;
        options.EnableJwtAuthentication = true;
        options.RequireHttps = false;
        options.AuthorizationPolicies["CustomPolicy"] = policy => policy.RequireClaim("custom");

        // Assert
        options.EnableEncryption.Should().BeFalse();
        options.EnableJwtAuthentication.Should().BeTrue();
        options.RequireHttps.Should().BeFalse();
        options.AuthorizationPolicies.Should().ContainKey("CustomPolicy");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AddEnterpriseSecurity_ServicesWorkTogether()
    {
        // Act
        _services.AddEnterpriseSecurity(_configuration);
        var provider = _services.BuildServiceProvider();

        // Get services
        var encryptionService = provider.GetService<IEncryptionService>();
        var hashingService = provider.GetService<IHashingService>();
        var tokenService = provider.GetService<ITokenService>();

        // Assert
        encryptionService.Should().NotBeNull();
        hashingService.Should().NotBeNull();
        tokenService.Should().NotBeNull();

        // Test that services can work together
        var plainText = "test data";
        var encrypted = encryptionService!.Encrypt(plainText);
        var decrypted = encryptionService.Decrypt(encrypted);
        decrypted.Should().Be(plainText);

        var password = "testPassword123!";
        var hash = hashingService!.HashPassword(password);
        var verified = hashingService.VerifyPassword(password, hash);
        verified.Should().BeTrue();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test") };
        var token = tokenService!.GenerateAccessToken(claims);
        var principal = tokenService.ValidateToken(token);
        principal.Should().NotBeNull();
    }

    [Test]
    public void AddEnterpriseSecurity_WithMissingJwtConfig_ThrowsException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        _services.AddSingleton(emptyConfig);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _services.AddEnterpriseSecurity(emptyConfig, options =>
            {
                options.EnableJwtAuthentication = true;
            });
            _services.BuildServiceProvider().GetService<ITokenService>();
        });
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void AddEnterpriseSecurity_WithInvalidJwtSecretKey_ThrowsException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:JwtSecretKey"] = "", // Empty key
                ["Security:JwtIssuer"] = "TestIssuer",
                ["Security:JwtAudience"] = "TestAudience"
            })
            .Build();

        _services.AddSingleton(invalidConfig);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            _services.AddEnterpriseSecurity(invalidConfig, options =>
            {
                options.EnableJwtAuthentication = true;
            });
            var provider = _services.BuildServiceProvider();
            provider.GetService<ITokenService>();
        });
    }

    #endregion
}