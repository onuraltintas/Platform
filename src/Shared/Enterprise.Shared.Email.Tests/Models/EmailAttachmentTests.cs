namespace Enterprise.Shared.Email.Tests.Models;

public class EmailAttachmentTests
{
    [Fact]
    public void FromBytes_ShouldCreateAttachment_WithValidParameters()
    {
        // Arrange
        const string fileName = "test.txt";
        var content = Encoding.UTF8.GetBytes("Hello World");
        const string contentType = "text/plain";

        // Act
        var attachment = EmailAttachment.FromBytes(fileName, content, contentType);

        // Assert
        attachment.Should().NotBeNull();
        attachment.FileName.Should().Be(fileName);
        attachment.Content.Should().BeEquivalentTo(content);
        attachment.ContentType.Should().Be(contentType);
        attachment.Size.Should().Be(content.Length);
        attachment.IsInline.Should().BeFalse();
        attachment.ContentId.Should().BeNull();
    }

    [Fact]
    public void FromBytes_ShouldCreateAttachment_WithAutoDetectedContentType()
    {
        // Arrange
        const string fileName = "test.pdf";
        var content = Encoding.UTF8.GetBytes("PDF content");

        // Act
        var attachment = EmailAttachment.FromBytes(fileName, content);

        // Assert
        attachment.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task FromFileAsync_ShouldCreateAttachment_FromExistingFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        const string content = "Test file content";
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var attachment = await EmailAttachment.FromFileAsync(tempFile);

            // Assert
            attachment.Should().NotBeNull();
            attachment.FileName.Should().Be(Path.GetFileName(tempFile));
            Encoding.UTF8.GetString(attachment.Content).Should().Be(content);
            attachment.ContentType.Should().Be("application/octet-stream"); // Default for unknown extensions
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Inline_ShouldCreateInlineAttachment()
    {
        // Arrange
        const string fileName = "image.png";
        var content = new byte[] { 1, 2, 3, 4 };
        const string contentId = "image1";
        const string contentType = "image/png";

        // Act
        var attachment = EmailAttachment.Inline(fileName, content, contentId, contentType);

        // Assert
        attachment.Should().NotBeNull();
        attachment.FileName.Should().Be(fileName);
        attachment.Content.Should().BeEquivalentTo(content);
        attachment.ContentType.Should().Be(contentType);
        attachment.ContentId.Should().Be(contentId);
        attachment.IsInline.Should().BeTrue();
    }

    [Fact]
    public void Inline_ShouldCreateInlineAttachment_WithAutoDetectedContentType()
    {
        // Arrange
        const string fileName = "image.jpg";
        var content = new byte[] { 1, 2, 3, 4 };
        const string contentId = "image1";

        // Act
        var attachment = EmailAttachment.Inline(fileName, content, contentId);

        // Assert
        attachment.ContentType.Should().Be("image/jpeg");
    }

    [Theory]
    [InlineData(".txt", "text/plain")]
    [InlineData(".html", "text/html")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".unknown", "application/octet-stream")]
    public void GetContentType_ShouldReturnCorrectMimeType(string extension, string expectedMimeType)
    {
        // Arrange
        var fileName = $"test{extension}";
        var content = new byte[] { 1, 2, 3 };

        // Act
        var attachment = EmailAttachment.FromBytes(fileName, content);

        // Assert
        attachment.ContentType.Should().Be(expectedMimeType);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidAttachment()
    {
        // Arrange
        var attachment = EmailAttachment.FromBytes("test.txt", Encoding.UTF8.GetBytes("content"));

        // Act
        var isValid = attachment.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenFileNameIsEmpty()
    {
        // Arrange
        var attachment = new EmailAttachment
        {
            FileName = "",
            Content = new byte[] { 1, 2, 3 }
        };

        // Act
        var isValid = attachment.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("File name is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenContentIsNull()
    {
        // Arrange
        var attachment = new EmailAttachment
        {
            FileName = "test.txt",
            Content = null!
        };

        // Act
        var isValid = attachment.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("File content is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenContentIsEmpty()
    {
        // Arrange
        var attachment = new EmailAttachment
        {
            FileName = "test.txt",
            Content = Array.Empty<byte>()
        };

        // Act
        var isValid = attachment.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("File content is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var largeContent = new byte[26 * 1024 * 1024]; // 26MB
        var attachment = new EmailAttachment
        {
            FileName = "large.txt",
            Content = largeContent
        };

        // Act
        var isValid = attachment.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("File size exceeds the maximum limit of 25MB");
    }

    [Fact]
    public void Size_ShouldReturnContentLength()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var attachment = new EmailAttachment
        {
            Content = content
        };

        // Act
        var size = attachment.Size;

        // Assert
        size.Should().Be(content.Length);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var attachment = new EmailAttachment();

        // Assert
        attachment.FileName.Should().BeEmpty();
        attachment.ContentType.Should().Be("application/octet-stream");
        attachment.Content.Should().BeEquivalentTo(Array.Empty<byte>());
        attachment.ContentId.Should().BeNull();
        attachment.IsInline.Should().BeFalse();
    }
}