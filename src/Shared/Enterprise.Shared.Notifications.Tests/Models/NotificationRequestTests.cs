using Enterprise.Shared.Notifications.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Models;

public class NotificationRequestTests
{
    [Fact]
    public void NotificationRequest_Should_Initialize_With_Default_Values()
    {
        // Act
        var request = new NotificationRequest();

        // Assert
        request.NotificationId.Should().NotBe(Guid.Empty);
        request.UserId.Should().Be(Guid.Empty);
        request.Type.Should().Be(NotificationType.Welcome);
        request.Channels.Should().BeEmpty();
        request.TemplateKey.Should().BeEmpty();
        request.Data.Should().BeEmpty();
        request.Priority.Should().Be(NotificationPriority.Normal);
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationRequest_Should_Set_Properties_Correctly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var channels = new[] { NotificationChannel.Email, NotificationChannel.SMS };
        var data = new Dictionary<string, object> { { "key", "value" } };
        var metadata = new Dictionary<string, object> { { "source", "test" } };

        // Act
        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            UserId = userId,
            Type = NotificationType.SecurityAlert,
            Channels = channels,
            TemplateKey = "security-alert",
            Data = data,
            Priority = NotificationPriority.High,
            Subject = "Security Alert",
            CustomMessage = "Test message",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CorrelationId = "test-correlation",
            Metadata = metadata
        };

        // Assert
        request.NotificationId.Should().Be(notificationId);
        request.UserId.Should().Be(userId);
        request.Type.Should().Be(NotificationType.SecurityAlert);
        request.Channels.Should().BeEquivalentTo(channels);
        request.TemplateKey.Should().Be("security-alert");
        request.Data.Should().BeEquivalentTo(data);
        request.Priority.Should().Be(NotificationPriority.High);
        request.Subject.Should().Be("Security Alert");
        request.CustomMessage.Should().Be("Test message");
        request.ExpiresAt.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation");
        request.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    public void NotificationRequest_Should_Fail_Validation_For_Invalid_TemplateKey_Length(string templateKey)
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = Guid.NewGuid(),
            TemplateKey = templateKey,
            Channels = new[] { NotificationChannel.Email }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(NotificationRequest.TemplateKey));
    }

    [Fact]
    public void NotificationRequest_Should_Fail_Validation_For_Empty_UserId()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = Guid.Empty,
            TemplateKey = "test-template",
            Channels = new[] { NotificationChannel.Email }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(NotificationRequest.UserId));
    }

    [Fact]
    public void NotificationRequest_Should_Pass_Validation_For_Valid_Data()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = Guid.NewGuid(),
            TemplateKey = "valid-template",
            Channels = new[] { NotificationChannel.Email },
            Type = NotificationType.Welcome
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}