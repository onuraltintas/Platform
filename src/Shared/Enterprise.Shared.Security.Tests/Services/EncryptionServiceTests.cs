using Microsoft.AspNetCore.DataProtection;

namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class EncryptionServiceTests
{
    private Mock<ILogger<EncryptionService>> _logger = null!;
    private SecuritySettings _settings = null!;
    private EncryptionService _encryptionService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<EncryptionService>>();
        _settings = new SecuritySettings
        {
            EncryptionKey = Convert.ToBase64String(new byte[32]), // 32 bytes for AES-256
            EncryptionIV = Convert.ToBase64String(new byte[16]),  // 16 bytes for AES IV
            UseDataProtectionApi = false
        };

        var options = Options.Create(_settings);
        _encryptionService = new EncryptionService(_logger.Object, options);
    }

    [TearDown]
    public void TearDown()
    {
        // EncryptionService does not implement IDisposable
    }

    #region Encrypt/Decrypt Tests

    [Test]
    public void Encrypt_WithValidInput_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var result = _encryptionService.Encrypt(plainText);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe(plainText);
        Convert.FromBase64String(result); // Should not throw
    }

    [Test]
    public void Decrypt_WithValidInput_ReturnsOriginalString()
    {
        // Arrange
        var plainText = "Hello, World!";
        var encrypted = _encryptionService.Encrypt(plainText);

        // Act
        var result = _encryptionService.Decrypt(encrypted);

        // Assert
        result.Should().Be(plainText);
    }

    [Test]
    public void EncryptDecrypt_RoundTrip_PreservesOriginalData()
    {
        // Arrange
        var originalData = "This is a test message with special characters: åäö!@#$%^&*()";

        // Act
        var encrypted = _encryptionService.Encrypt(originalData);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalData);
    }

    [Test]
    public void Encrypt_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _encryptionService.Encrypt(""));
        Assert.Throws<ArgumentException>(() => _encryptionService.Encrypt(" "));
    }

    [Test]
    public void Decrypt_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _encryptionService.Decrypt(""));
        Assert.Throws<ArgumentException>(() => _encryptionService.Decrypt(" "));
    }

    [Test]
    public void Decrypt_WithInvalidCipherText_ThrowsSecurityException()
    {
        // Act & Assert
        Assert.Throws<SecurityException>(() => _encryptionService.Decrypt("invalid-cipher-text"));
    }

    #endregion

    #region Custom Key Tests

    [Test]
    public void EncryptWithCustomKey_WithValidInputs_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "Secret message";
        var customKey = _encryptionService.GenerateKey();

        // Act
        var result = _encryptionService.Encrypt(plainText, customKey);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe(plainText);
    }

    [Test]
    public void DecryptWithCustomKey_WithValidInputs_ReturnsOriginalString()
    {
        // Arrange
        var plainText = "Secret message";
        var customKey = _encryptionService.GenerateKey();
        var encrypted = _encryptionService.Encrypt(plainText, customKey);

        // Act
        var result = _encryptionService.Decrypt(encrypted, customKey);

        // Assert
        result.Should().Be(plainText);
    }

    [Test]
    public void EncryptWithCustomKey_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _encryptionService.Encrypt("test", ""));
    }

    [Test]
    public void DecryptWithCustomKey_WithWrongKey_ThrowsSecurityException()
    {
        // Arrange
        var plainText = "Secret message";
        var key1 = _encryptionService.GenerateKey();
        var key2 = _encryptionService.GenerateKey();
        var encrypted = _encryptionService.Encrypt(plainText, key1);

        // Act & Assert
        Assert.Throws<SecurityException>(() => _encryptionService.Decrypt(encrypted, key2));
    }

    #endregion

    #region Key Generation Tests

    [Test]
    public void GenerateKey_ReturnsValidBase64String()
    {
        // Act
        var key = _encryptionService.GenerateKey();

        // Assert
        key.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(key).Should().HaveCount(32); // 256-bit key
    }

    [Test]
    public void GenerateKey_GeneratesUniqueKeys()
    {
        // Act
        var key1 = _encryptionService.GenerateKey();
        var key2 = _encryptionService.GenerateKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    [Test]
    public void GenerateIV_ReturnsValidBase64String()
    {
        // Act
        var iv = _encryptionService.GenerateIV();

        // Assert
        iv.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(iv).Should().HaveCount(16); // 128-bit IV
    }

    [Test]
    public void GenerateIV_GeneratesUniqueIVs()
    {
        // Act
        var iv1 = _encryptionService.GenerateIV();
        var iv2 = _encryptionService.GenerateIV();

        // Assert
        iv1.Should().NotBe(iv2);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Encrypt_WithLongString_WorksCorrectly()
    {
        // Arrange
        var longString = new string('A', 10000);

        // Act
        var encrypted = _encryptionService.Encrypt(longString);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(longString);
    }

    [Test]
    public void Encrypt_WithUnicodeCharacters_WorksCorrectly()
    {
        // Arrange
        var unicodeString = "🌟🚀💻🔐 Unicode test: 你好世界 العالم مرحبا";

        // Act
        var encrypted = _encryptionService.Encrypt(unicodeString);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(unicodeString);
    }

    #endregion
}