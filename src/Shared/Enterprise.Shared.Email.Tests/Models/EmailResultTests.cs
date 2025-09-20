namespace Enterprise.Shared.Email.Tests.Models;

public class EmailResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string trackingId = "tracking-123";
        const string messageId = "message-456";

        // Act
        var result = EmailResult.Success(trackingId, messageId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TrackingId.Should().Be(trackingId);
        result.MessageId.Should().Be(messageId);
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Errors.Should().BeEmpty();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Success_ShouldCreateSuccessfulResult_WithoutMessageId()
    {
        // Arrange
        const string trackingId = "tracking-123";

        // Act
        var result = EmailResult.Success(trackingId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TrackingId.Should().Be(trackingId);
        result.MessageId.Should().BeNull();
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult_WithMessage()
    {
        // Arrange
        const string message = "Email sending failed";
        const string trackingId = "tracking-123";

        // Act
        var result = EmailResult.Failure(message, trackingId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(message);
        result.TrackingId.Should().Be(trackingId);
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
        result.Errors.Should().ContainSingle(message);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult_WithoutTrackingId()
    {
        // Arrange
        const string message = "Email sending failed";

        // Act
        var result = EmailResult.Failure(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(message);
        result.TrackingId.Should().BeNull();
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult_WithMultipleErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        const string trackingId = "tracking-123";

        // Act
        var result = EmailResult.Failure(errors, trackingId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error 1");
        result.TrackingId.Should().Be(trackingId);
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult_WithEmptyErrors()
    {
        // Arrange
        var errors = Array.Empty<string>();
        const string trackingId = "tracking-123";

        // Act
        var result = EmailResult.Failure(errors, trackingId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Operation failed");
        result.TrackingId.Should().Be(trackingId);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Queued_ShouldCreateQueuedResult()
    {
        // Arrange
        const string trackingId = "tracking-123";

        // Act
        var result = EmailResult.Queued(trackingId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TrackingId.Should().Be(trackingId);
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Queued);
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new EmailResult();

        // Assert
        result.TrackingId.Should().BeNull();
        result.MessageId.Should().BeNull();
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        result.RetryCount.Should().Be(0);
        result.ProviderData.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(EmailDeliveryStatus.Sent)]
    [InlineData(EmailDeliveryStatus.Queued)]
    [InlineData(EmailDeliveryStatus.Delivered)]
    [InlineData(EmailDeliveryStatus.Failed)]
    [InlineData(EmailDeliveryStatus.Bounced)]
    [InlineData(EmailDeliveryStatus.Spam)]
    [InlineData(EmailDeliveryStatus.Unsubscribed)]
    public void DeliveryStatus_ShouldAcceptAllValidStatuses(EmailDeliveryStatus status)
    {
        // Arrange & Act
        var result = new EmailResult
        {
            DeliveryStatus = status
        };

        // Assert
        result.DeliveryStatus.Should().Be(status);
    }
}