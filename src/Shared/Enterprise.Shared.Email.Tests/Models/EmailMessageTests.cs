namespace Enterprise.Shared.Email.Tests.Models;

public class EmailMessageTests
{
    [Fact]
    public void Create_ShouldCreateEmailMessage_WithValidParameters()
    {
        // Arrange
        const string to = "test@example.com";
        const string subject = "Test Subject";
        const string body = "Test Body";
        const bool isHtml = true;

        // Act
        var result = EmailMessage.Create(to, subject, body, isHtml);

        // Assert
        result.Should().NotBeNull();
        result.To.Should().Be(to);
        result.Subject.Should().Be(subject);
        result.Body.Should().Be(body);
        result.IsHtml.Should().Be(isHtml);
        result.TrackingId.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldCreateEmailMessage_WithDefaultHtmlTrue()
    {
        // Arrange
        const string to = "test@example.com";
        const string subject = "Test Subject";
        const string body = "Test Body";

        // Act
        var result = EmailMessage.Create(to, subject, body);

        // Assert
        result.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void AddTag_ShouldAddTag_WhenTagIsValid()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        const string tag = "marketing";

        // Act
        var result = message.AddTag(tag);

        // Assert
        result.Should().BeSameAs(message);
        message.Tags.Should().Contain(tag);
    }

    [Fact]
    public void AddTag_ShouldNotAddTag_WhenTagIsNull()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");

        // Act
        var result = message.AddTag(null!);

        // Assert
        result.Should().BeSameAs(message);
        message.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_ShouldNotAddTag_WhenTagIsWhitespace()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");

        // Act
        var result = message.AddTag("   ");

        // Assert
        result.Should().BeSameAs(message);
        message.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_ShouldNotAddDuplicateTag()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        const string tag = "marketing";

        // Act
        message.AddTag(tag);
        message.AddTag(tag);

        // Assert
        message.Tags.Should().ContainSingle(tag);
    }

    [Fact]
    public void AddMetadata_ShouldAddMetadata()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        const string key = "userId";
        const string value = "123";

        // Act
        var result = message.AddMetadata(key, value);

        // Assert
        result.Should().BeSameAs(message);
        message.Metadata.Should().ContainKey(key);
        message.Metadata[key].Should().Be(value);
    }

    [Fact]
    public void AddMetadata_ShouldReplaceExistingMetadata()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        const string key = "userId";
        const string oldValue = "123";
        const string newValue = "456";

        // Act
        message.AddMetadata(key, oldValue);
        message.AddMetadata(key, newValue);

        // Assert
        message.Metadata[key].Should().Be(newValue);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        const string name = "X-Custom-Header";
        const string value = "custom-value";

        // Act
        var result = message.AddHeader(name, value);

        // Assert
        result.Should().BeSameAs(message);
        message.Headers.Should().ContainKey(name);
        message.Headers[name].Should().Be(value);
    }

    [Fact]
    public void AddAttachment_ShouldAddAttachment()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Subject", "Body");
        var attachment = EmailAttachment.FromBytes("test.txt", Encoding.UTF8.GetBytes("test content"));

        // Act
        var result = message.AddAttachment(attachment);

        // Assert
        result.Should().BeSameAs(message);
        message.Attachments.Should().Contain(attachment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ShouldInitializeWithDefaults_WhenCreatedWithNew(string? emptyString)
    {
        // Arrange
        _ = emptyString; // Acknowledge parameter usage
        
        // Act
        var message = new EmailMessage();

        // Assert
        message.To.Should().BeEmpty();
        message.Subject.Should().BeEmpty();
        message.Body.Should().BeEmpty();
        message.IsHtml.Should().BeTrue();
        message.Priority.Should().Be(EmailPriority.Normal);
        message.TrackingId.Should().NotBeNullOrEmpty();
        message.Tags.Should().NotBeNull().And.BeEmpty();
        message.Metadata.Should().NotBeNull().And.BeEmpty();
        message.Headers.Should().NotBeNull().And.BeEmpty();
        message.Attachments.Should().NotBeNull().And.BeEmpty();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}