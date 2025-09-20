namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class HashingServiceTests
{
    private Mock<ILogger<HashingService>> _logger = null!;
    private SecuritySettings _settings = null!;
    private HashingService _hashingService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<HashingService>>();
        _settings = new SecuritySettings
        {
            BCryptWorkFactor = 10 // Use lower work factor for faster tests
        };

        var options = Options.Create(_settings);
        _hashingService = new HashingService(_logger.Object, options);
    }

    #region Password Hashing Tests

    [Test]
    public void HashPassword_WithValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var result = _hashingService.HashPassword(password);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe(password);
        result.Should().StartWith("$2a$"); // BCrypt format
    }

    [Test]
    public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = _hashingService.HashPassword(password);
        var hash2 = _hashingService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // Each hash should be unique due to salt
    }

    [Test]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hash = _hashingService.HashPassword(password);

        // Act
        var result = _hashingService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _hashingService.HashPassword(password);

        // Act
        var result = _hashingService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HashPassword_WithEmptyPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _hashingService.HashPassword(""));
        Assert.Throws<ArgumentException>(() => _hashingService.HashPassword(" "));
    }

    [Test]
    public void VerifyPassword_WithEmptyInputs_ThrowsArgumentException()
    {
        // Arrange
        var hash = _hashingService.HashPassword("password");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _hashingService.VerifyPassword("", hash));
        Assert.Throws<ArgumentException>(() => _hashingService.VerifyPassword("password", ""));
    }

    [Test]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Act
        var result = _hashingService.VerifyPassword("password", "invalid-hash");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SHA256 Tests

    [Test]
    public void ComputeSha256_WithValidInput_ReturnsCorrectHash()
    {
        // Arrange
        var input = "Hello, World!";
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f"; // Known SHA256 hash

        // Act
        var result = _hashingService.ComputeSha256(input);

        // Assert
        result.Should().Be(expectedHash);
    }

    [Test]
    public void ComputeSha256_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "Consistent input";

        // Act
        var hash1 = _hashingService.ComputeSha256(input);
        var hash2 = _hashingService.ComputeSha256(input);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void ComputeSha256_WithEmptyInput_ReturnsEmptyStringHash()
    {
        // Arrange
        var input = "";
        var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // SHA256 of empty string

        // Act
        var result = _hashingService.ComputeSha256(input);

        // Assert
        result.Should().Be(expectedHash);
    }

    [Test]
    public void ComputeSha256_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _hashingService.ComputeSha256(null!));
    }

    #endregion

    #region SHA512 Tests

    [Test]
    public void ComputeSha512_WithValidInput_ReturnsCorrectHash()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = _hashingService.ComputeSha512(input);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveLength(128); // SHA512 produces 128 hex characters
    }

    [Test]
    public void ComputeSha512_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "Consistent input";

        // Act
        var hash1 = _hashingService.ComputeSha512(input);
        var hash2 = _hashingService.ComputeSha512(input);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void ComputeSha512_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _hashingService.ComputeSha512(null!));
    }

    #endregion

    #region HMAC-SHA256 Tests

    [Test]
    public void ComputeHmacSha256_WithValidInputs_ReturnsValidHash()
    {
        // Arrange
        var input = "Hello, World!";
        var key = "secret-key";

        // Act
        var result = _hashingService.ComputeHmacSha256(input, key);

        // Assert
        result.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(result); // Should not throw
    }

    [Test]
    public void ComputeHmacSha256_WithSameInputs_ReturnsSameHash()
    {
        // Arrange
        var input = "Consistent input";
        var key = "consistent-key";

        // Act
        var hash1 = _hashingService.ComputeHmacSha256(input, key);
        var hash2 = _hashingService.ComputeHmacSha256(input, key);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void ComputeHmacSha256_WithDifferentKeys_ReturnsDifferentHashes()
    {
        // Arrange
        var input = "Same input";
        var key1 = "key1";
        var key2 = "key2";

        // Act
        var hash1 = _hashingService.ComputeHmacSha256(input, key1);
        var hash2 = _hashingService.ComputeHmacSha256(input, key2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void ComputeHmacSha256_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _hashingService.ComputeHmacSha256(null!, "key"));
    }

    [Test]
    public void ComputeHmacSha256_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _hashingService.ComputeHmacSha256("input", ""));
    }

    #endregion

    #region Salt Generation Tests

    [Test]
    public void GenerateSalt_WithDefaultSize_ReturnsCorrectSize()
    {
        // Act
        var salt = _hashingService.GenerateSalt();

        // Assert
        salt.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(salt).Should().HaveCount(32); // Default size
    }

    [Test]
    public void GenerateSalt_WithCustomSize_ReturnsCorrectSize()
    {
        // Arrange
        var size = 16;

        // Act
        var salt = _hashingService.GenerateSalt(size);

        // Assert
        salt.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(salt).Should().HaveCount(size);
    }

    [Test]
    public void GenerateSalt_GeneratesUniqueSalts()
    {
        // Act
        var salt1 = _hashingService.GenerateSalt();
        var salt2 = _hashingService.GenerateSalt();

        // Assert
        salt1.Should().NotBe(salt2);
    }

    [Test]
    public void GenerateSalt_WithInvalidSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _hashingService.GenerateSalt(0));
        Assert.Throws<ArgumentException>(() => _hashingService.GenerateSalt(-1));
    }

    #endregion

    #region Performance Tests

    [Test]
    public void HashPassword_WithHighWorkFactor_CompletesWithinReasonableTime()
    {
        // Arrange
        var highWorkFactorSettings = new SecuritySettings { BCryptWorkFactor = 12 };
        var highWorkFactorOptions = Options.Create(highWorkFactorSettings);
        var highWorkFactorService = new HashingService(_logger.Object, highWorkFactorOptions);
        var password = "TestPassword123!";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var hash = highWorkFactorService.HashPassword(password);
        stopwatch.Stop();

        // Assert
        hash.Should().NotBeNullOrEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    #endregion
}