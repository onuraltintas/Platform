namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private Mock<ILogger<TokenService>> _logger = null!;
    private SecuritySettings _settings = null!;
    private IMemoryCache _cache = null!;
    private TokenService _tokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<TokenService>>();
        _settings = new SecuritySettings
        {
            JwtSecretKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("This-is-a-super-secret-key-for-jwt-signing-that-is-long-enough")),
            JwtIssuer = "TestIssuer",
            JwtAudience = "TestAudience",
            JwtAccessTokenExpirationMinutes = 15,
            JwtClockSkewMinutes = 5,
            RefreshTokenExpirationDays = 30
        };

        var options = Options.Create(_settings);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _tokenService = new TokenService(_logger.Object, options, _cache);
    }

    [TearDown]
    public void TearDown()
    {
        _cache?.Dispose();
    }

    #region Access Token Generation Tests

    [Test]
    public void GenerateAccessToken_WithValidClaims_ReturnsValidToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, "User")
        };

        // Act
        var token = _tokenService.GenerateAccessToken(claims);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Verify it's a valid JWT format (3 parts separated by dots)
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Test]
    public void GenerateAccessToken_WithCustomExpiration_UsesCustomExpiration()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var customExpiration = 30;

        // Act
        var token = _tokenService.GenerateAccessToken(claims, customExpiration);
        var expiration = _tokenService.GetTokenExpiration(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiration.Should().NotBeNull();
        expiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(customExpiration), TimeSpan.FromMinutes(1));
    }

    [Test]
    public void GenerateAccessToken_WithNullClaims_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenService.GenerateAccessToken(null!));
    }

    #endregion

    #region Refresh Token Generation Tests

    [Test]
    public void GenerateRefreshToken_ReturnsValidBase64Token()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(refreshToken); // Should not throw
    }

    [Test]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    #endregion

    #region Token Validation Tests

    [Test]
    public void ValidateToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var token = _tokenService.GenerateAccessToken(claims);

        // Act
        var principal = _tokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user123");
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be("user@example.com");
    }

    [Test]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _tokenService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _tokenService.ValidateToken(""));
        Assert.Throws<ArgumentException>(() => _tokenService.ValidateToken(" "));
    }

    [Test]
    public void ValidateToken_WithInvalidSignature_ReturnsNull()
    {
        // Arrange
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var principal = _tokenService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    #endregion

    #region Token Claims Tests

    [Test]
    public void GetClaimsFromToken_WithValidToken_ReturnsClaims()
    {
        // Arrange
        var expectedClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var token = _tokenService.GenerateAccessToken(expectedClaims);

        // Act
        var claims = _tokenService.GetClaimsFromToken(token).ToList();

        // Assert
        claims.Should().NotBeEmpty();
        claims.Should().Contain(c => (c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid") && c.Value == "user123");
        claims.Should().Contain(c => (c.Type == ClaimTypes.Email || c.Type == "email") && c.Value == "user@example.com");
        claims.Should().Contain(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
    }

    [Test]
    public void GetClaimsFromToken_WithInvalidToken_ReturnsEmpty()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var claims = _tokenService.GetClaimsFromToken(invalidToken);

        // Assert
        claims.Should().BeEmpty();
    }

    [Test]
    public void GetClaimsFromToken_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _tokenService.GetClaimsFromToken(""));
    }

    #endregion

    #region Token Expiration Tests

    [Test]
    public void GetTokenExpiration_WithValidToken_ReturnsExpirationTime()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var token = _tokenService.GenerateAccessToken(claims, 30);

        // Act
        var expiration = _tokenService.GetTokenExpiration(token);

        // Assert
        expiration.Should().NotBeNull();
        expiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Test]
    public void GetTokenExpiration_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var expiration = _tokenService.GetTokenExpiration(invalidToken);

        // Assert
        expiration.Should().BeNull();
    }

    #endregion

    #region Refresh Token Tests

    [Test]
    public async Task RefreshAccessTokenAsync_WithValidTokens_ReturnsNewAccessToken()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Act
        var newAccessToken = await _tokenService.RefreshAccessTokenAsync(accessToken, refreshToken);

        // Assert
        newAccessToken.Should().NotBeNullOrEmpty();
        newAccessToken.Should().NotBe(accessToken);
    }

    [Test]
    public async Task RefreshAccessTokenAsync_WithRevokedRefreshToken_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Revoke the refresh token
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        // Act
        var newAccessToken = await _tokenService.RefreshAccessTokenAsync(accessToken, refreshToken);

        // Assert
        newAccessToken.Should().BeNull();
    }

    [Test]
    public async Task RefreshAccessTokenAsync_WithEmptyTokens_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _tokenService.RefreshAccessTokenAsync("", "refreshToken"));
        
        Assert.ThrowsAsync<ArgumentException>(() => 
            _tokenService.RefreshAccessTokenAsync("accessToken", ""));
    }

    #endregion

    #region Token Revocation Tests

    [Test]
    public async Task RevokeRefreshTokenAsync_WithValidToken_RevokesToken()
    {
        // Arrange
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Act
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        var isRevoked = await _tokenService.IsTokenRevokedAsync(refreshToken);

        // Assert
        isRevoked.Should().BeTrue();
    }

    [Test]
    public async Task IsTokenRevokedAsync_WithNonRevokedToken_ReturnsFalse()
    {
        // Arrange
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Act
        var isRevoked = await _tokenService.IsTokenRevokedAsync(refreshToken);

        // Assert
        isRevoked.Should().BeFalse();
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _tokenService.RevokeRefreshTokenAsync(""));
    }

    [Test]
    public async Task IsTokenRevokedAsync_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _tokenService.IsTokenRevokedAsync(""));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void GenerateAccessToken_WithLargeClaims_HandlesCorrectly()
    {
        // Arrange
        var largeClaims = new List<Claim>();
        for (int i = 0; i < 50; i++)
        {
            largeClaims.Add(new Claim($"claim{i}", $"value{i}"));
        }

        // Act
        var token = _tokenService.GenerateAccessToken(largeClaims);
        var retrievedClaims = _tokenService.GetClaimsFromToken(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        retrievedClaims.Should().HaveCountGreaterOrEqualTo(50);
    }

    [Test]
    public void GenerateAccessToken_WithUnicodeClaims_HandlesCorrectly()
    {
        // Arrange
        var unicodeClaims = new[]
        {
            new Claim("name", "José María García"),
            new Claim("city", "北京市"),
            new Claim("description", "🌟🚀💻🔐")
        };

        // Act
        var token = _tokenService.GenerateAccessToken(unicodeClaims);
        var retrievedClaims = _tokenService.GetClaimsFromToken(token).ToList();

        // Assert
        token.Should().NotBeNullOrEmpty();
        retrievedClaims.Should().Contain(c => c.Value == "José María García");
        retrievedClaims.Should().Contain(c => c.Value == "北京市");
        retrievedClaims.Should().Contain(c => c.Value == "🌟🚀💻🔐");
    }

    #endregion
}