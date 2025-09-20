using FluentAssertions;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Enterprise.Shared.Validation.Extensions;
using Enterprise.Shared.Validation.Models;
using Enterprise.Shared.Validation.Interfaces;

namespace Enterprise.Shared.Validation.Tests;

[TestFixture]
public class BasicIntegrationTests
{
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddEnterpriseValidation();
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public void ServiceRegistration_ShouldRegisterValidationService()
    {
        var validationService = _serviceProvider.GetService<IValidationService>();
        validationService.Should().NotBeNull();
    }

    [Test]
    public void ValidationSettings_ShouldHaveDefaultValues()
    {
        var validationService = _serviceProvider.GetService<IValidationService>();
        validationService.Should().NotBeNull();
    }
}

[TestFixture]
public class StringValidationTests
{
    [Test]
    [TestCase("test@example.com", true)]
    [TestCase("invalid-email", false)]
    [TestCase("", false)]
    public void IsValidEmail_ShouldValidateCorrectly(string email, bool expected)
    {
        email.IsValidEmail().Should().Be(expected);
    }

    [Test]
    public void IsValidEmail_NullInput_ShouldReturnFalse()
    {
        ((string?)null).IsValidEmail().Should().BeFalse();
    }

    [Test]
    [TestCase("+905551234567", true)]
    [TestCase("05551234567", true)]
    [TestCase("5551234567", true)]
    [TestCase("123456789", false)]
    [TestCase("", false)]
    public void IsValidTurkishPhone_ShouldValidateCorrectly(string phone, bool expected)
    {
        phone.IsValidTurkishPhone().Should().Be(expected);
    }

    [Test]
    public void IsValidTurkishPhone_NullInput_ShouldReturnFalse()
    {
        ((string?)null).IsValidTurkishPhone().Should().BeFalse();
    }

    [Test]
    [TestCase("12345678901", false)]  // Invalid TC number checksum
    [TestCase("1234567890", false)]   // Too short
    [TestCase("00000000000", false)]  // All zeros
    [TestCase("", false)]
    public void IsValidTCNumber_ShouldValidateCorrectly(string tcNumber, bool expected)
    {
        tcNumber.IsValidTCNumber().Should().Be(expected);
    }

    [Test]
    public void IsValidTCNumber_NullInput_ShouldReturnFalse()
    {
        ((string?)null).IsValidTCNumber().Should().BeFalse();
    }

    [Test]
    [TestCase("1234567890", true)]
    [TestCase("123456789", false)]
    [TestCase("", false)]
    public void IsValidTurkishTaxNumber_ShouldValidateCorrectly(string taxNumber, bool expected)
    {
        taxNumber.IsValidTurkishTaxNumber().Should().Be(expected);
    }

    [Test]
    public void IsValidTurkishTaxNumber_NullInput_ShouldReturnFalse()
    {
        ((string?)null).IsValidTurkishTaxNumber().Should().BeFalse();
    }

    [Test]
    [TestCase("TR330006100519786457841326", true)]
    [TestCase("DE89370400440532013000", false)]
    [TestCase("", false)]
    public void IsValidTurkishIban_ShouldValidateCorrectly(string iban, bool expected)
    {
        iban.IsValidTurkishIban().Should().Be(expected);
    }

    [Test]
    public void IsValidTurkishIban_NullInput_ShouldReturnFalse()
    {
        ((string?)null).IsValidTurkishIban().Should().BeFalse();
    }
}

[TestFixture]
public class ResultPatternTests
{
    [Test]
    public void Result_Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Test]
    public void Result_Failure_ShouldCreateFailedResult()
    {
        var errorMessage = "Test hatası";
        var result = Result.Failure(errorMessage);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Test]
    public void ResultT_Success_ShouldCreateSuccessfulResultWithValue()
    {
        var value = "test değeri";
        var result = Result<string>.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Test]
    public void ResultT_Failure_ShouldCreateFailedResult()
    {
        var errorMessage = "Test hatası";
        var result = Result<string>.Failure(errorMessage);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }
}

[TestFixture]
public class ValidationResultTests
{
    [Test]
    public void ValidationResult_Success_ShouldCreateValidResult()
    {
        var result = ValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void ValidationResult_Failed_ShouldCreateInvalidResult()
    {
        var error = new ValidationError("Email", "Geçersiz e-posta", "EMAIL_INVALID");
        var result = ValidationResult.Failed(error);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(error);
    }
}

[TestFixture]
public class StringExtensionBasicTests
{
    [Test]
    public void IsNullOrEmpty_EmptyString_ShouldReturnTrue()
    {
        "".IsNullOrEmpty().Should().BeTrue();
    }

    [Test]
    public void IsNullOrEmpty_ValidString_ShouldReturnFalse()
    {
        "test".IsNullOrEmpty().Should().BeFalse();
    }

    [Test]
    public void HasValue_ValidString_ShouldReturnTrue()
    {
        "test".HasValue().Should().BeTrue();
    }

    [Test]
    public void HasValue_EmptyString_ShouldReturnFalse()
    {
        "".HasValue().Should().BeFalse();
    }

    [Test]
    public void ToSlug_ShouldCreateValidSlug()
    {
        "Test String 123!".ToSlug().Should().Be("test-string-123");
    }

    [Test]
    public void Truncate_LongString_ShouldTruncateWithSuffix()
    {
        "This is a very long string".Truncate(10).Should().Be("This is...");
    }
}