namespace Enterprise.Shared.Email.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IFluentEmail> _fluentEmailMock;
    private readonly Mock<IEmailTemplateService> _templateServiceMock;
    private readonly Mock<IOptions<EmailConfiguration>> _configurationMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailConfiguration _configuration;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _fluentEmailMock = new Mock<IFluentEmail>();
        _templateServiceMock = new Mock<IEmailTemplateService>();
        _configurationMock = new Mock<IOptions<EmailConfiguration>>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        _configuration = new EmailConfiguration
        {
            DefaultSender = new EmailSenderOptions
            {
                Email = "test@example.com",
                Name = "Test Sender"
            }
        };

        _configurationMock.Setup(x => x.Value).Returns(_configuration);

        _emailService = new EmailService(
            _fluentEmailMock.Object,
            _templateServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenEmailSentSuccessfully()
    {
        // Arrange
        var message = EmailMessage.Create("recipient@example.com", "Test Subject", "Test Body");
        var sendResponse = new FluentEmail.Core.Models.SendResponse
        {
            MessageId = "msg-123"
            // Successful will be true because ErrorMessages is empty
        };

        SetupFluentEmailMock(sendResponse);

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TrackingId.Should().Be(message.TrackingId);
        result.MessageId.Should().Be("msg-123");
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenEmailSendingFails()
    {
        // Arrange
        var message = EmailMessage.Create("recipient@example.com", "Test Subject", "Test Body");
        var sendResponse = new FluentEmail.Core.Models.SendResponse
        {
            ErrorMessages = new List<string> { "SMTP error occurred" }
            // Successful will be false because ErrorMessages has items
        };

        SetupFluentEmailMock(sendResponse);

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("SMTP error occurred");
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenMessageIsNull()
    {
        // Act
        var result = await _emailService.SendAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email message cannot be null");
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "", // Invalid email
            Subject = "Test Subject",
            Body = "Test Body"
        };

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Recipient email address is required");
    }

    [Fact]
    public async Task SendTemplateAsync_ShouldReturnSuccess_WhenTemplateRendersAndSendsSuccessfully()
    {
        // Arrange
        const string templateName = "welcome-template";
        const string to = "recipient@example.com";
        var templateData = new { Name = "John", CompanyName = "Test Corp" };

        var renderResult = TemplateRenderResult.Success(
            "Welcome John!", 
            "Welcome to Test Corp, John!", 
            true);

        var sendResponse = new FluentEmail.Core.Models.SendResponse
        {
            MessageId = "msg-123"
            // Successful will be true because ErrorMessages is empty
        };

        _templateServiceMock
            .Setup(x => x.RenderTemplateAsync(templateName, templateData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderResult);

        SetupFluentEmailMock(sendResponse);

        // Act
        var result = await _emailService.SendTemplateAsync(templateName, to, templateData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("msg-123");
    }

    [Fact]
    public async Task SendTemplateAsync_ShouldReturnFailure_WhenTemplateRenderingFails()
    {
        // Arrange
        const string templateName = "invalid-template";
        const string to = "recipient@example.com";
        var templateData = new { Name = "John" };

        var renderResult = TemplateRenderResult.Failure("Template not found");

        _templateServiceMock
            .Setup(x => x.RenderTemplateAsync(templateName, templateData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderResult);

        // Act
        var result = await _emailService.SendTemplateAsync(templateName, to, templateData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Template rendering failed: Template not found");
    }

    [Fact]
    public async Task SendBulkAsync_ShouldProcessAllRecipients()
    {
        // Arrange
        var request = new BulkEmailRequest
        {
            Subject = "Bulk Email",
            Body = "This is a bulk email",
            Recipients = new List<BulkEmailRecipient>
            {
                new() { Email = "user1@example.com" },
                new() { Email = "user2@example.com" },
                new() { Email = "user3@example.com" }
            },
            BatchSize = 2,
            MaxConcurrency = 1
        };

        var sendResponse = new FluentEmail.Core.Models.SendResponse
        {
            MessageId = "msg-123"
            // Successful will be true because ErrorMessages is empty
        };

        SetupFluentEmailMock(sendResponse);

        // Act
        var result = await _emailService.SendBulkAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.BulkId.Should().Be(request.BulkId);
        result.TotalEmails.Should().Be(3);
        result.Results.Should().HaveCount(3);
        result.Results.Should().OnlyContain(r => r.IsSuccess);
    }

    [Fact]
    public async Task SendBulkAsync_ShouldReturnFailure_WhenRequestIsNull()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _emailService.SendBulkAsync(null!));

        // Assert
        exception.ParamName.Should().Be("request");
    }

    [Fact]
    public async Task ScheduleAsync_ShouldReturnQueued_WhenScheduledForFuture()
    {
        // Arrange
        var message = EmailMessage.Create("recipient@example.com", "Scheduled Email", "This is scheduled");
        var scheduledTime = DateTime.UtcNow.AddHours(1);

        // Act
        var result = await _emailService.ScheduleAsync(message, scheduledTime);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Queued);
        result.TrackingId.Should().Be(message.TrackingId);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldSendImmediately_WhenScheduledForPast()
    {
        // Arrange
        var message = EmailMessage.Create("recipient@example.com", "Immediate Email", "This is immediate");
        var scheduledTime = DateTime.UtcNow.AddMinutes(-10);

        var sendResponse = new FluentEmail.Core.Models.SendResponse
        {
            MessageId = "msg-123"
            // Successful will be true because ErrorMessages is empty
        };

        SetupFluentEmailMock(sendResponse);

        // Act
        var result = await _emailService.ScheduleAsync(message, scheduledTime);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        result.MessageId.Should().Be("msg-123");
    }

    [Fact]
    public async Task ValidateEmailAsync_ShouldReturnSuccess_ForValidEmail()
    {
        // Arrange
        var message = EmailMessage.Create("valid@example.com", "Valid Subject", "Valid Body");

        // Act
        var result = await _emailService.ValidateEmailAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateEmailAsync_ShouldReturnFailure_ForInvalidEmail()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "invalid-email",
            Subject = "",
            Body = ""
        };

        // Act
        var result = await _emailService.ValidateEmailAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Invalid recipient email address"));
    }

    [Fact]
    public async Task ValidateEmailAsync_ShouldReturnWarnings_ForSpamIndicators()
    {
        // Arrange
        var message = EmailMessage.Create("valid@example.com", "URGENT!!!", "This is URGENT content");

        // Act
        var result = await _emailService.ValidateEmailAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Warnings.Should().Contain("Email content may trigger spam filters");
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnSuccess_WhenConfigurationIsValid()
    {
        // Act
        var result = await _emailService.TestConnectionAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetDeliveryStatusAsync_ShouldReturnSent()
    {
        // Arrange
        const string trackingId = "track-123";

        // Act
        var status = await _emailService.GetDeliveryStatusAsync(trackingId);

        // Assert
        status.Should().Be(EmailDeliveryStatus.Sent);
    }

    private void SetupFluentEmailMock(FluentEmail.Core.Models.SendResponse response)
    {
        var fluentEmailInstance = new Mock<IFluentEmail>();
        
        fluentEmailInstance.Setup(x => x.To(It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.Subject(It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.SetFrom(It.IsAny<string>(), It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.ReplyTo(It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.CC(It.IsAny<string>(), It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.BCC(It.IsAny<string>(), It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.Header(It.IsAny<string>(), It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        fluentEmailInstance.Setup(x => x.Attach(It.IsAny<FluentEmail.Core.Models.Attachment>()))
            .Returns(fluentEmailInstance.Object);
        
        fluentEmailInstance.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(fluentEmailInstance.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(fluentEmailInstance.Object);
    }
}