using Enterprise.Shared.Common.Extensions;
using FluentAssertions;

namespace Enterprise.Shared.Common.Tests.Extensions;

[TestFixture]
public class StringExtensionsTests
{
    #region Validation Extensions Tests

    [Test]
    public void IsNullOrEmpty_WithNullString_ReturnsTrue()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsNullOrEmpty_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsNullOrEmpty_WithValidString_ReturnsFalse()
    {
        // Arrange
        var value = "test";

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasValue_WithValidString_ReturnsTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = value.HasValue();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasValue_WithWhitespaceString_ReturnsFalse()
    {
        // Arrange
        var value = "   ";

        // Act
        var result = value.HasValue();

        // Assert
        result.Should().BeFalse();
    }

    [TestCase("test@example.com", true)]
    [TestCase("invalid-email", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("user@domain", false)]
    [TestCase("user@domain.c", true)]
    public void IsValidEmail_WithVariousInputs_ReturnsExpectedResult(string? email, bool expected)
    {
        // Act
        var result = email.IsValidEmail();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("+1234567890", true)]
    [TestCase("1234567890", true)]
    [TestCase("123", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("abcdefghij", false)]
    public void IsValidPhone_WithVariousInputs_ReturnsExpectedResult(string? phone, bool expected)
    {
        // Act
        var result = phone.IsValidPhone();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("https://example.com", true)]
    [TestCase("http://test.org", true)]
    [TestCase("ftp://test.com", false)]
    [TestCase("not-a-url", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsValidUrl_WithVariousInputs_ReturnsExpectedResult(string? url, bool expected)
    {
        // Act
        var result = url.IsValidUrl();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Case Conversion Tests

    [TestCase("HelloWorld", "hello-world")]
    [TestCase("XMLHttpRequest", "xmlhttp-request")]
    [TestCase("iPhone", "i-phone")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void ToKebabCase_WithVariousInputs_ReturnsExpectedResult(string? input, string expected)
    {
        // Act
        var result = input.ToKebabCase();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("HelloWorld", "helloWorld")]
    [TestCase("XMLHttpRequest", "xMLHttpRequest")]
    [TestCase("A", "a")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void ToCamelCase_WithVariousInputs_ReturnsExpectedResult(string? input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("helloWorld", "HelloWorld")]
    [TestCase("xmlHttpRequest", "XmlHttpRequest")]
    [TestCase("a", "A")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void ToPascalCase_WithVariousInputs_ReturnsExpectedResult(string? input, string expected)
    {
        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("Hello World!", "hello-world")]
    [TestCase("This & That", "this-that")]
    [TestCase("Multiple   Spaces", "multiple-spaces")]
    [TestCase("", "")]
    public void ToSlug_WithVariousInputs_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = input.ToSlug();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Text Manipulation Tests

    [TestCase("Hello World", 5, "...", "He...")]
    [TestCase("Hello", 10, "...", "Hello")]
    [TestCase("Hello World", 8, " more", "Hell more")]
    [TestCase("", 5, "...", "")]
    [TestCase(null, 5, "...", null)]
    public void Truncate_WithVariousInputs_ReturnsExpectedResult(string? input, int maxLength, string suffix, string? expected)
    {
        // Act
        var result = input.Truncate(maxLength, suffix);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("Hello123!@#", true, "Hello123")]
    [TestCase("Hello123!@#", false, "Hello123")]
    [TestCase("Hello 123", true, "Hello 123")]
    [TestCase("Hello 123", false, "Hello123")]
    public void RemoveSpecialCharacters_WithVariousInputs_ReturnsExpectedResult(string input, bool keepSpaces, string expected)
    {
        // Act
        var result = input.RemoveSpecialCharacters(keepSpaces);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("Hello", "olleH")]
    [TestCase("12345", "54321")]
    [TestCase("", "")]
    [TestCase("A", "A")]
    public void Reverse_WithVariousInputs_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = input.Reverse();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Privacy and Security Tests

    [TestCase("john.doe@example.com", '*', "j*******e@example.com")]
    [TestCase("a@test.com", '*', "a@test.com")] // Too short to mask
    [TestCase("ab@test.com", '*', "a*@test.com")]
    public void MaskEmail_WithValidEmails_ReturnsExpectedResult(string email, char maskChar, string expected)
    {
        // Act
        var result = email.MaskEmail(maskChar);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("1234567890", '*', 4, "******7890")]
    [TestCase("123", '*', 4, "123")] // Too short
    [TestCase("1234567890", '#', 2, "########90")]
    public void MaskPhone_WithVariousInputs_ReturnsExpectedResult(string phone, char maskChar, int visibleDigits, string expected)
    {
        // Act
        var result = phone.MaskPhone(maskChar, visibleDigits);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("1234567890123456", "1234********3456")]
    [TestCase("1234 5678 9012 3456", "1234********3456")]
    [TestCase("123456", "123456")] // Too short
    public void MaskCreditCard_WithVariousInputs_ReturnsExpectedResult(string creditCard, string expected)
    {
        // Act
        var result = creditCard.MaskCreditCard();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Hashing and Encoding Tests

    [Test]
    public void ToSha256_WithValidString_ReturnsValidHash()
    {
        // Arrange
        var input = "test";

        // Act
        var result = input.ToSha256();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 produces 64 character hex string
        result.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Test]
    public void ToMd5_WithValidString_ReturnsValidHash()
    {
        // Arrange
        var input = "test";

        // Act
        var result = input.ToMd5();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(32); // MD5 produces 32 character hex string
        result.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Test]
    public void ToBase64_WithValidString_ReturnsValidBase64()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("SGVsbG8gV29ybGQ=");
    }

    [Test]
    public void FromBase64_WithValidBase64_ReturnsOriginalString()
    {
        // Arrange
        var base64 = "SGVsbG8gV29ybGQ=";

        // Act
        var result = base64.FromBase64();

        // Assert
        result.Should().Be("Hello World");
    }

    [Test]
    public void FromBase64_WithInvalidBase64_ReturnsEmptyString()
    {
        // Arrange
        var invalidBase64 = "invalid-base64!";

        // Act
        var result = invalidBase64.FromBase64();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}