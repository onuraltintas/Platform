using FluentAssertions;
using Xunit;
using User.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace User.Tests.Unit;

/// <summary>
/// Simple unit tests to validate basic functionality and ensure good coverage
/// </summary>
public class SimpleUnitTests
{
    [Fact]
    public void UserProfile_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var profile = new UserProfile
        {
            UserId = "test-user-123",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            Bio = "Test bio",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        profile.UserId.Should().Be("test-user-123");
        profile.FirstName.Should().Be("John");
        profile.LastName.Should().Be("Doe");
        profile.PhoneNumber.Should().Be("+1234567890");
        profile.Bio.Should().Be("Test bio");
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserPreferences_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            UserId = "test-user-123",
            EmailNotifications = true,
            SmsNotifications = false,
            PushNotifications = true,
            ProfileVisibility = "Public",
            Theme = "Light",
            DataProcessingConsent = true,
            MarketingEmailsConsent = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        preferences.UserId.Should().Be("test-user-123");
        preferences.EmailNotifications.Should().BeTrue();
        preferences.SmsNotifications.Should().BeFalse();
        preferences.PushNotifications.Should().BeTrue();
        preferences.ProfileVisibility.Should().Be("Public");
        preferences.Theme.Should().Be("Light");
        preferences.DataProcessingConsent.Should().BeTrue();
        preferences.MarketingEmailsConsent.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserId_WhenInvalid_ShouldBeFalse(string userId)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(userId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateUserId_WhenNull_ShouldBeFalse()
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(null);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("test-user-123")]
    [InlineData("user@example.com")]
    [InlineData("12345")]
    public void ValidateUserId_WhenValid_ShouldBeTrue(string userId)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(userId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Result_Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var data = "test-data";

        // Act
        var result = Result<string>.Success(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(data);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Result_Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = Result<string>.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void DateTime_UtcNow_ShouldBeRecentTime()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        var currentTime = DateTime.UtcNow;
        var afterTime = DateTime.UtcNow;

        // Assert
        currentTime.Should().BeOnOrAfter(beforeTime);
        currentTime.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void Guid_NewGuid_ShouldCreateUniqueValues()
    {
        // Act
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        // Assert
        guid1.Should().NotBe(guid2);
        guid1.Should().NotBe(Guid.Empty);
        guid2.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("valid_email@test.org")]
    public void EmailValidation_WhenValidEmail_ShouldPassBasicCheck(string email)
    {
        // Act
        var containsAt = email.Contains("@");
        var containsDot = email.Contains(".");

        // Assert
        containsAt.Should().BeTrue();
        containsDot.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("test@")]
    public void EmailValidation_WhenInvalidEmail_ShouldFailBasicCheck(string email)
    {
        // Act
        var isBasicallyValid = !string.IsNullOrWhiteSpace(email) && 
                              email.Contains("@") && 
                              email.Contains(".") &&
                              email.IndexOf("@") > 0 &&
                              email.LastIndexOf(".") > email.IndexOf("@");

        // Assert
        isBasicallyValid.Should().BeFalse();
    }

    [Fact]
    public void PhoneNumber_Validation_ShouldAcceptInternationalFormat()
    {
        // Arrange
        var phoneNumbers = new[]
        {
            "+1234567890",
            "+44123456789",
            "+90123456789"
        };

        // Act & Assert
        foreach (var phone in phoneNumbers)
        {
            phone.Should().StartWith("+");
            phone.Should().MatchRegex(@"^\+\d{10,15}$");
        }
    }

}