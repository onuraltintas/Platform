namespace Enterprise.Shared.Email.Tests.Models;

public class BulkEmailRequestTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidBulkRequest()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "test1@example.com" },
                new() { Email = "test2@example.com" }
            }
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidTemplateRequest()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            TemplateName = "welcome-template",
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "test1@example.com" },
                new() { Email = "test2@example.com" }
            }
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNoRecipients()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Recipients = new List<BulkEmailRecipient>()
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("At least one recipient is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenRecipientsIsNull()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Recipients = null!
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("At least one recipient is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMissingTemplateAndContent()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "test@example.com" }
            }
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Either template name or subject and body must be provided");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMissingSubjectWithBody()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Body = "Test Body",
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "test@example.com" }
            }
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Either template name or subject and body must be provided");
    }

    [Fact]
    public void IsValid_ShouldValidateRecipients()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "valid@example.com" },
                new() { Email = "invalid-email" },
                new() { Email = "" }
            }
        };

        // Act
        var isValid = request.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Recipient 2"));
        errors.Should().Contain(e => e.Contains("Recipient 3"));
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new BulkEmailRequest();

        // Assert
        request.IsHtml.Should().BeTrue();
        request.Priority.Should().Be(EmailPriority.Normal);
        request.MaxConcurrency.Should().Be(10);
        request.DelayBetweenBatches.Should().Be(1000);
        request.BatchSize.Should().Be(50);
        request.BulkId.Should().NotBeNullOrEmpty();
        request.Recipients.Should().NotBeNull().And.BeEmpty();
        request.Tags.Should().NotBeNull().And.BeEmpty();
        request.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void MaxConcurrency_ShouldAcceptValidValues(int maxConcurrency)
    {
        // Arrange & Act
        var request = new BulkEmailRequest
        {
            MaxConcurrency = maxConcurrency
        };

        // Assert
        request.MaxConcurrency.Should().Be(maxConcurrency);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void BatchSize_ShouldAcceptValidValues(int batchSize)
    {
        // Arrange & Act
        var request = new BulkEmailRequest
        {
            BatchSize = batchSize
        };

        // Assert
        request.BatchSize.Should().Be(batchSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(60000)]
    public void DelayBetweenBatches_ShouldAcceptValidValues(int delay)
    {
        // Arrange & Act
        var request = new BulkEmailRequest
        {
            DelayBetweenBatches = delay
        };

        // Assert
        request.DelayBetweenBatches.Should().Be(delay);
    }
}

public class BulkEmailRecipientTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidRecipient()
    {
        // Arrange
        var recipient = new BulkEmailRecipient
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var isValid = recipient.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenEmailIsEmpty()
    {
        // Arrange
        var recipient = new BulkEmailRecipient
        {
            Email = "",
            Name = "Test User"
        };

        // Act
        var isValid = recipient.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Email address is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenEmailIsInvalid()
    {
        // Arrange
        var recipient = new BulkEmailRecipient
        {
            Email = "invalid-email",
            Name = "Test User"
        };

        // Act
        var isValid = recipient.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Invalid email address format");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var recipient = new BulkEmailRecipient();

        // Assert
        recipient.Email.Should().BeEmpty();
        recipient.Name.Should().BeNull();
        recipient.TrackingId.Should().NotBeNullOrEmpty();
        recipient.Data.Should().NotBeNull().And.BeEmpty();
        recipient.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.com")]
    [InlineData("test+tag@example.org")]
    public void Email_ShouldAcceptValidEmailAddresses(string email)
    {
        // Arrange & Act
        var recipient = new BulkEmailRecipient
        {
            Email = email
        };

        // Assert
        recipient.Email.Should().Be(email);
    }
}

public class BulkEmailResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string bulkId = "bulk-123";
        var results = new List<EmailResult>
        {
            EmailResult.Success("track-1"),
            EmailResult.Success("track-2"),
            EmailResult.Failure("Error", "track-3")
        };

        // Act
        var bulkResult = BulkEmailResult.Success(bulkId, results);

        // Assert
        bulkResult.Should().NotBeNull();
        bulkResult.BulkId.Should().Be(bulkId);
        bulkResult.TotalEmails.Should().Be(3);
        bulkResult.SuccessfulSends.Should().Be(2);
        bulkResult.FailedSends.Should().Be(1);
        bulkResult.Results.Should().BeEquivalentTo(results);
        bulkResult.IsSuccess.Should().BeFalse(); // Because there are failed sends
        bulkResult.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Success_ShouldCreateSuccessfulResult_WhenAllEmailsSucceed()
    {
        // Arrange
        const string bulkId = "bulk-123";
        var results = new List<EmailResult>
        {
            EmailResult.Success("track-1"),
            EmailResult.Success("track-2")
        };

        // Act
        var bulkResult = BulkEmailResult.Success(bulkId, results);

        // Assert
        bulkResult.IsSuccess.Should().BeTrue();
        bulkResult.SuccessfulSends.Should().Be(2);
        bulkResult.FailedSends.Should().Be(0);
    }

    [Fact]
    public void SuccessRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var results = new List<EmailResult>
        {
            EmailResult.Success("track-1"),
            EmailResult.Success("track-2"),
            EmailResult.Failure("Error", "track-3"),
            EmailResult.Failure("Error", "track-4")
        };
        var bulkResult = BulkEmailResult.Success("bulk-123", results);

        // Act
        var successRate = bulkResult.SuccessRate;

        // Assert
        successRate.Should().Be(50.0);
    }

    [Fact]
    public void SuccessRate_ShouldReturnZero_WhenNoEmails()
    {
        // Arrange
        var bulkResult = new BulkEmailResult
        {
            TotalEmails = 0,
            SuccessfulSends = 0
        };

        // Act
        var successRate = bulkResult.SuccessRate;

        // Assert
        successRate.Should().Be(0.0);
    }

    [Fact]
    public void Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var endTime = DateTime.UtcNow;
        var bulkResult = new BulkEmailResult
        {
            StartedAt = startTime,
            CompletedAt = endTime
        };

        // Act
        var duration = bulkResult.Duration;

        // Assert
        duration.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Duration_ShouldReturnNull_WhenNotCompleted()
    {
        // Arrange
        var bulkResult = new BulkEmailResult
        {
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = null
        };

        // Act
        var duration = bulkResult.Duration;

        // Assert
        duration.Should().BeNull();
    }
}