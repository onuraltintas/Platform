namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class SecurityValidatorTests
{
    private Mock<ILogger<SecurityValidator>> _logger = null!;
    private SecuritySettings _settings = null!;
    private SecurityValidator _securityValidator = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<SecurityValidator>>();
        _settings = new SecuritySettings
        {
            PasswordMinLength = 8,
            PasswordRequireUppercase = true,
            PasswordRequireLowercase = true,
            PasswordRequireDigit = true,
            PasswordRequireSpecialChar = true
        };

        var options = Options.Create(_settings);
        _securityValidator = new SecurityValidator(_logger.Object, options);
    }

    #region Password Validation Tests

    [Test]
    public void ValidatePassword_WithStrongPassword_ReturnsValidResult()
    {
        // Arrange
        var password = "MyStr0ng!Password";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Strength.Should().BeOneOf(PasswordStrength.Strong, PasswordStrength.VeryStrong);
        result.Score.Should().BeGreaterThan(50);
    }

    [Test]
    public void ValidatePassword_WithWeakPassword_ReturnsInvalidResult()
    {
        // Arrange
        var password = "weak";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Strength.Should().Be(PasswordStrength.VeryWeak);
    }

    [Test]
    public void ValidatePassword_WithEmptyPassword_ReturnsInvalidResult()
    {
        // Act
        var result = _securityValidator.ValidatePassword("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password is required");
        result.Strength.Should().Be(PasswordStrength.VeryWeak);
    }

    [Test]
    public void ValidatePassword_WithShortPassword_ReturnsLengthError()
    {
        // Arrange
        var password = "Sh0rt!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain($"Password must be at least {_settings.PasswordMinLength} characters long");
    }

    [Test]
    public void ValidatePassword_WithoutUppercase_ReturnsUppercaseError()
    {
        // Arrange
        var password = "lowercase123!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password must contain at least one uppercase letter");
    }

    [Test]
    public void ValidatePassword_WithoutLowercase_ReturnsLowercaseError()
    {
        // Arrange
        var password = "UPPERCASE123!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password must contain at least one lowercase letter");
    }

    [Test]
    public void ValidatePassword_WithoutDigit_ReturnsDigitError()
    {
        // Arrange
        var password = "NoDigitsHere!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password must contain at least one digit");
    }

    [Test]
    public void ValidatePassword_WithoutSpecialChar_ReturnsSpecialCharError()
    {
        // Arrange
        var password = "NoSpecialChars123";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password must contain at least one special character");
    }

    [Test]
    public void ValidatePassword_WithCommonPattern_LowersScore()
    {
        // Arrange
        var password = "Password123!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.Suggestions.Should().Contain("Avoid common patterns like '123', 'abc', or 'qwerty'");
    }

    [Test]
    public void ValidatePassword_WithRepeatedCharacters_LowersScore()
    {
        // Arrange
        var password = "Passsword123!";

        // Act
        var result = _securityValidator.ValidatePassword(password);

        // Assert
        result.Suggestions.Should().Contain("Avoid excessive repeated characters");
    }

    #endregion

    #region SQL Injection Tests

    [Test]
    public void ContainsSqlInjectionPattern_WithSqlKeywords_ReturnsTrue()
    {
        // Arrange
        var inputs = new[]
        {
            "'; DROP TABLE users; --",
            "admin' OR '1'='1",
            "UNION SELECT * FROM passwords",
            "'; INSERT INTO users VALUES ('hacker', 'pass'); --"
        };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = _securityValidator.ContainsSqlInjectionPattern(input);
            result.Should().BeTrue($"Input '{input}' should be detected as SQL injection");
        }
    }

    [Test]
    public void ContainsSqlInjectionPattern_WithSafeInput_ReturnsFalse()
    {
        // Arrange
        var inputs = new[]
        {
            "John Doe",
            "user@example.com",
            "Safe search query",
            "Product name with numbers 123"
        };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = _securityValidator.ContainsSqlInjectionPattern(input);
            result.Should().BeFalse($"Input '{input}' should be safe");
        }
    }

    [Test]
    public void ContainsSqlInjectionPattern_WithEmptyInput_ReturnsFalse()
    {
        // Act
        var result = _securityValidator.ContainsSqlInjectionPattern("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region XSS Tests

    [Test]
    public void ContainsXssPattern_WithXssAttacks_ReturnsTrue()
    {
        // Arrange
        var inputs = new[]
        {
            "<script>alert('XSS')</script>",
            "<img src='x' onerror='alert(1)'>",
            "javascript:alert('XSS')",
            "<svg onload=alert('XSS')>",
            "<iframe src='javascript:alert(1)'></iframe>"
        };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = _securityValidator.ContainsXssPattern(input);
            result.Should().BeTrue($"Input '{input}' should be detected as XSS");
        }
    }

    [Test]
    public void ContainsXssPattern_WithSafeInput_ReturnsFalse()
    {
        // Arrange
        var inputs = new[]
        {
            "Normal text content",
            "Email: user@example.com",
            "Price: $19.99",
            "Description with <em>emphasis</em>"
        };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = _securityValidator.ContainsXssPattern(input);
            result.Should().BeFalse($"Input '{input}' should be safe");
        }
    }

    [Test]
    public void SanitizeForXss_WithDangerousInput_ReturnsSanitizedOutput()
    {
        // Arrange
        var input = "<script>alert('XSS')</script>";

        // Act
        var result = _securityValidator.SanitizeForXss(input);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
    }

    [Test]
    public void SanitizeForXss_WithQuotes_EscapesQuotes()
    {
        // Arrange
        var input = "Text with 'single' and \"double\" quotes";

        // Act
        var result = _securityValidator.SanitizeForXss(input);

        // Assert
        result.Should().Contain("&#39;"); // Escaped single quote
        result.Should().Contain("&quot;"); // Escaped double quote
    }

    #endregion

    #region Email Validation Tests

    [Test]
    public void IsValidEmail_WithValidEmails_ReturnsTrue()
    {
        // Arrange
        var validEmails = new[]
        {
            "user@example.com",
            "test.email@domain.co.uk",
            "user+tag@example.org",
            "user123@test-domain.com"
        };

        // Act & Assert
        foreach (var email in validEmails)
        {
            var result = _securityValidator.IsValidEmail(email);
            result.Should().BeTrue($"Email '{email}' should be valid");
        }
    }

    [Test]
    public void IsValidEmail_WithInvalidEmails_ReturnsFalse()
    {
        // Arrange
        var invalidEmails = new[]
        {
            "invalid-email",
            "@example.com",
            "user@",
            "user..double.dot@example.com",
            "user@.com"
        };

        // Act & Assert
        foreach (var email in invalidEmails)
        {
            var result = _securityValidator.IsValidEmail(email);
            result.Should().BeFalse($"Email '{email}' should be invalid");
        }
    }

    [Test]
    public void IsValidEmail_WithEmptyInput_ReturnsFalse()
    {
        // Act
        var result = _securityValidator.IsValidEmail("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region URL Validation Tests

    [Test]
    public void IsValidUrl_WithValidUrls_ReturnsTrue()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://www.example.com",
            "http://test.com/path",
            "https://subdomain.example.org/path?query=value",
            "http://localhost:8080"
        };

        // Act & Assert
        foreach (var url in validUrls)
        {
            var result = _securityValidator.IsValidUrl(url);
            result.Should().BeTrue($"URL '{url}' should be valid");
        }
    }

    [Test]
    public void IsValidUrl_WithInvalidUrls_ReturnsFalse()
    {
        // Arrange
        var invalidUrls = new[]
        {
            "not-a-url",
            "ftp://example.com", // Not http/https
            "https://",
            "http://.com"
        };

        // Act & Assert
        foreach (var url in invalidUrls)
        {
            var result = _securityValidator.IsValidUrl(url);
            result.Should().BeFalse($"URL '{url}' should be invalid");
        }
    }

    #endregion

    #region File Type Validation Tests

    [Test]
    public void IsAllowedFileType_WithAllowedExtensions_ReturnsTrue()
    {
        // Arrange
        var allowedExtensions = new[] { ".jpg", ".png", ".gif" };

        // Act & Assert
        _securityValidator.IsAllowedFileType("image.jpg", allowedExtensions).Should().BeTrue();
        _securityValidator.IsAllowedFileType("photo.PNG", allowedExtensions).Should().BeTrue(); // Case insensitive
        _securityValidator.IsAllowedFileType("animation.gif", allowedExtensions).Should().BeTrue();
    }

    [Test]
    public void IsAllowedFileType_WithDisallowedExtensions_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { ".jpg", ".png", ".gif" };

        // Act & Assert
        _securityValidator.IsAllowedFileType("document.pdf", allowedExtensions).Should().BeFalse();
        _securityValidator.IsAllowedFileType("script.exe", allowedExtensions).Should().BeFalse();
        _securityValidator.IsAllowedFileType("archive.zip", allowedExtensions).Should().BeFalse();
    }

    [Test]
    public void IsAllowedFileType_WithNoExtensions_ReturnsTrue()
    {
        // Arrange
        var noRestrictions = Array.Empty<string>();

        // Act
        var result = _securityValidator.IsAllowedFileType("anyfile.xyz", noRestrictions);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsAllowedFileType_WithoutExtension_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { ".jpg", ".png" };

        // Act
        var result = _securityValidator.IsAllowedFileType("filename", allowedExtensions);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region API Key Validation Tests

    [Test]
    public void IsValidApiKey_WithValidApiKey_ReturnsTrue()
    {
        // Arrange
        var validApiKey = "sk_1234567890abcdef1234567890abcdef12345678";

        // Act
        var result = _securityValidator.IsValidApiKey(validApiKey);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidApiKey_WithShortApiKey_ReturnsFalse()
    {
        // Arrange
        var shortApiKey = "sk_123456";

        // Act
        var result = _securityValidator.IsValidApiKey(shortApiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidApiKey_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var invalidApiKey = "sk_1234567890abcdef!@#$%^&*()1234567890abcdef";

        // Act
        var result = _securityValidator.IsValidApiKey(invalidApiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidApiKey_WithEmptyInput_ReturnsFalse()
    {
        // Act
        var result = _securityValidator.IsValidApiKey("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}