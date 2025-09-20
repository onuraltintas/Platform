using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IEmailNotificationProvider> _emailProviderMock;
    private readonly Mock<ISmsNotificationProvider> _smsProviderMock;
    private readonly Mock<IPushNotificationProvider> _pushProviderMock;
    private readonly Mock<IInAppNotificationProvider> _inAppProviderMock;
    private readonly Mock<IWebhookNotificationProvider> _webhookProviderMock;
    private readonly Mock<ITemplateService> _templateServiceMock;
    private readonly Mock<INotificationPreferencesService> _preferencesServiceMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _emailProviderMock = new Mock<IEmailNotificationProvider>();
        _smsProviderMock = new Mock<ISmsNotificationProvider>();
        _pushProviderMock = new Mock<IPushNotificationProvider>();
        _inAppProviderMock = new Mock<IInAppNotificationProvider>();
        _webhookProviderMock = new Mock<IWebhookNotificationProvider>();
        _templateServiceMock = new Mock<ITemplateService>();
        _preferencesServiceMock = new Mock<INotificationPreferencesService>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();

        _settings = new NotificationSettings
        {
            General = new GeneralSettings { Enabled = true },
            Delivery = new DeliverySettings { BatchSize = 100 }
        };
        _optionsMock.Setup(x => x.Value).Returns(_settings);

        _service = new NotificationService(
            _emailProviderMock.Object,
            _smsProviderMock.Object,
            _pushProviderMock.Object,
            _inAppProviderMock.Object,
            _webhookProviderMock.Object,
            _templateServiceMock.Object,
            _preferencesServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Service_With_Valid_Dependencies()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_EmailProvider_Is_Null()
    {
        // Act & Assert
        var act = () => new NotificationService(
            null!,
            _smsProviderMock.Object,
            _pushProviderMock.Object,
            _inAppProviderMock.Object,
            _webhookProviderMock.Object,
            _templateServiceMock.Object,
            _preferencesServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("emailProvider");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new NotificationService(
            _emailProviderMock.Object,
            _smsProviderMock.Object,
            _pushProviderMock.Object,
            _inAppProviderMock.Object,
            _webhookProviderMock.Object,
            _templateServiceMock.Object,
            _preferencesServiceMock.Object,
            _loggerMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SendAsync_Should_Skip_When_Notifications_Are_Disabled()
    {
        // Arrange
        _settings.General.Enabled = false;
        var request = CreateValidNotificationRequest();

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _smsProviderMock.Verify(x => x.SendAsync(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _pushProviderMock.Verify(x => x.SendAsync(It.IsAny<PushNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _inAppProviderMock.Verify(x => x.SendAsync(It.IsAny<InAppNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookProviderMock.Verify(x => x.SendAsync(It.IsAny<WebhookNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_Should_Skip_When_Notification_Has_Expired()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.ExpiresAt = DateTime.UtcNow.AddMinutes(-10); // Expired

        SetupDefaultPreferences();

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_Should_Skip_Non_Critical_Notifications_During_DND()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Priority = NotificationPriority.Normal;

        var preferences = CreateDefaultPreferences();
        preferences.DoNotDisturb = true;

        _preferencesServiceMock.Setup(x => x.GetUserPreferencesAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesServiceMock.Setup(x => x.IsInQuietHoursAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_Should_Send_Critical_Notifications_During_DND()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Priority = NotificationPriority.Critical;

        var preferences = CreateDefaultPreferences();
        preferences.DoNotDisturb = true;

        _preferencesServiceMock.Setup(x => x.GetUserPreferencesAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesServiceMock.Setup(x => x.IsInQuietHoursAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_Skip_When_All_Channels_Are_Disabled()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        var preferences = CreateDefaultPreferences();
        preferences.EmailEnabled = false;
        preferences.SmsEnabled = false;
        preferences.PushEnabled = false;
        preferences.InAppEnabled = false;
        preferences.WebhookEnabled = false;

        _preferencesServiceMock.Setup(x => x.GetUserPreferencesAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesServiceMock.Setup(x => x.IsInQuietHoursAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_Should_Send_To_Enabled_Channels_Only()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Push };

        var preferences = CreateDefaultPreferences();
        preferences.EmailEnabled = true;
        preferences.SmsEnabled = false; // Disabled
        preferences.PushEnabled = true;

        SetupPreferencesAndTemplate(request, preferences);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsProviderMock.Verify(x => x.SendAsync(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _pushProviderMock.Verify(x => x.SendAsync(It.IsAny<PushNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_Render_Template_When_TemplateKey_Is_Provided()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.TemplateKey = "welcome-template";
        request.Data = new Dictionary<string, object> { { "user", "John" } };

        var preferences = CreateDefaultPreferences();
        var renderedTemplate = new RenderedTemplate
        {
            Subject = "Welcome John!",
            HtmlContent = "<h1>Welcome John!</h1>",
            TextContent = "Welcome John!",
            SmsContent = "Welcome John!",
            PushTitle = "Welcome!",
            PushBody = "Hi John!"
        };

        SetupPreferencesAndTemplate(request, preferences, renderedTemplate);

        // Act
        await _service.SendAsync(request);

        // Assert
        _templateServiceMock.Verify(x => x.RenderAsync(
            "welcome-template",
            request.Data,
            preferences.Language,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_Send_Email_Notification_With_Rendered_Content()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { NotificationChannel.Email };

        var preferences = CreateDefaultPreferences();
        var renderedTemplate = new RenderedTemplate
        {
            Subject = "Test Subject",
            HtmlContent = "<h1>Test HTML</h1>",
            TextContent = "Test Text"
        };

        SetupPreferencesAndTemplate(request, preferences, renderedTemplate);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.Is<EmailNotification>(n => 
            n.Subject == "Test Subject" && 
            n.HtmlContent == "<h1>Test HTML</h1>" && 
            n.TextContent == "Test Text"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_Handle_Provider_Exception_And_Continue()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { NotificationChannel.Email, NotificationChannel.SMS };

        var preferences = CreateDefaultPreferences();
        SetupPreferencesAndTemplate(request, preferences);

        _emailProviderMock.Setup(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email provider error"));

        // Act
        await _service.SendAsync(request);
        
        // Assert
        // Service should handle the exception gracefully and not throw
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsProviderMock.Verify(x => x.SendAsync(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Exception_When_Request_Is_Null()
    {
        // Act & Assert
        var act = async () => await _service.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendBulkAsync_Should_Skip_When_Notifications_Are_Disabled()
    {
        // Arrange
        _settings.General.Enabled = false;
        var request = CreateValidBulkNotificationRequest();

        // Act
        await _service.SendBulkAsync(request);

        // Assert
        _preferencesServiceMock.Verify(x => x.GetUserPreferencesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendBulkAsync_Should_Process_Users_In_Batches()
    {
        // Arrange
        var userIds = Enumerable.Range(1, 250).Select(_ => Guid.NewGuid()).ToArray();
        var request = CreateValidBulkNotificationRequest();
        request.UserIds = userIds;
        request.BatchSize = 100;

        SetupDefaultPreferences();

        // Act
        await _service.SendBulkAsync(request);

        // Assert
        // Should be called 250 times (once for each user)
        _preferencesServiceMock.Verify(x => x.GetUserPreferencesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(250));
    }

    [Fact]
    public async Task SendBulkAsync_Should_Throw_Exception_When_Request_Is_Null()
    {
        // Act & Assert
        var act = async () => await _service.SendBulkAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ScheduleAsync_Should_Send_Immediately_When_Scheduled_Time_Is_In_Past()
    {
        // Arrange
        var request = CreateValidScheduledNotificationRequest();
        request.ScheduledAt = DateTime.UtcNow.AddMinutes(-10); // In the past

        SetupDefaultPreferences();

        // Act
        await _service.ScheduleAsync(request);

        // Assert
        _preferencesServiceMock.Verify(x => x.GetUserPreferencesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_Should_Schedule_Future_Notification()
    {
        // Arrange
        var request = CreateValidScheduledNotificationRequest();
        request.ScheduledAt = DateTime.UtcNow.AddMinutes(1); // In the future

        // Act
        await _service.ScheduleAsync(request);

        // Assert - Should not execute immediately
        _preferencesServiceMock.Verify(x => x.GetUserPreferencesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleAsync_Should_Throw_Exception_When_Request_Is_Null()
    {
        // Act & Assert
        var act = async () => await _service.ScheduleAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetStatusAsync_Should_Return_Sent_Status()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = await _service.GetStatusAsync(notificationId);

        // Assert
        result.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task GetHistoryAsync_Should_Return_Empty_Collection()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetHistoryAsync(userId, 1, 20);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_Return_Statistics_With_Generated_Date()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetStatisticsAsync(fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelAsync_Should_Complete_Successfully()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.CancelAsync(notificationId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RetryAsync_Should_Complete_Successfully()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.RetryAsync(notificationId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_Should_Filter_Channels_By_Type_Specific_Preferences()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { NotificationChannel.Email, NotificationChannel.SMS };
        request.Type = NotificationType.Welcome;

        var preferences = CreateDefaultPreferences();
        preferences.EmailEnabled = true;
        preferences.SmsEnabled = true;

        // Add type-specific preferences that disable SMS for Welcome notifications
        preferences.TypePreferences[NotificationType.Welcome] = new NotificationTypePreference
        {
            EmailEnabled = true,
            SmsEnabled = false, // Disabled for Welcome type
            PushEnabled = true,
            InAppEnabled = true,
            WebhookEnabled = true
        };

        SetupPreferencesAndTemplate(request, preferences);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsProviderMock.Verify(x => x.SendAsync(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(NotificationChannel.SMS)]
    [InlineData(NotificationChannel.Push)]
    [InlineData(NotificationChannel.InApp)]
    [InlineData(NotificationChannel.Webhook)]
    public async Task SendAsync_Should_Send_To_Specific_Channel_Correctly(NotificationChannel channel)
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { channel };

        var preferences = CreateDefaultPreferences();
        SetupPreferencesAndTemplate(request, preferences);

        // Act
        await _service.SendAsync(request);

        // Assert
        switch (channel)
        {
            case NotificationChannel.SMS:
                _smsProviderMock.Verify(x => x.SendAsync(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case NotificationChannel.Push:
                _pushProviderMock.Verify(x => x.SendAsync(It.IsAny<PushNotification>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case NotificationChannel.InApp:
                _inAppProviderMock.Verify(x => x.SendAsync(It.IsAny<InAppNotification>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case NotificationChannel.Webhook:
                _webhookProviderMock.Verify(x => x.SendAsync(It.IsAny<WebhookNotification>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
        }
    }

    [Fact]
    public async Task SendAsync_Should_Use_Custom_Message_When_No_Template()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        request.Channels = new[] { NotificationChannel.Email };
        request.TemplateKey = ""; // No template
        request.CustomMessage = "Custom notification message";

        var preferences = CreateDefaultPreferences();
        SetupPreferencesAndTemplate(request, preferences, template: null);

        // Act
        await _service.SendAsync(request);

        // Assert
        _emailProviderMock.Verify(x => x.SendAsync(It.Is<EmailNotification>(n => 
            n.TextContent == "Custom notification message"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private NotificationRequest CreateValidNotificationRequest()
    {
        return new NotificationRequest
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = NotificationType.Welcome,
            Channels = new[] { NotificationChannel.Email },
            TemplateKey = "welcome-template",
            Data = new Dictionary<string, object>(),
            Priority = NotificationPriority.Normal,
            Subject = "Test Subject",
            CustomMessage = "Test Message"
        };
    }

    private BulkNotificationRequest CreateValidBulkNotificationRequest()
    {
        return new BulkNotificationRequest
        {
            UserIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            Type = NotificationType.Welcome,
            Channels = new[] { NotificationChannel.Email },
            TemplateKey = "welcome-template",
            Data = new Dictionary<string, object>(),
            Priority = NotificationPriority.Normal,
            BatchSize = 100
        };
    }

    private ScheduledNotificationRequest CreateValidScheduledNotificationRequest()
    {
        return new ScheduledNotificationRequest
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = NotificationType.Welcome,
            Channels = new[] { NotificationChannel.Email },
            TemplateKey = "welcome-template",
            Data = new Dictionary<string, object>(),
            Priority = NotificationPriority.Normal,
            ScheduledAt = DateTime.UtcNow.AddMinutes(5)
        };
    }

    private UserNotificationPreferences CreateDefaultPreferences()
    {
        return new UserNotificationPreferences
        {
            UserId = Guid.NewGuid(),
            EmailEnabled = true,
            SmsEnabled = true,
            PushEnabled = true,
            InAppEnabled = true,
            WebhookEnabled = true,
            DoNotDisturb = false,
            Language = "en-US",
            TypePreferences = new Dictionary<NotificationType, NotificationTypePreference>()
        };
    }

    private void SetupDefaultPreferences()
    {
        var preferences = CreateDefaultPreferences();
        _preferencesServiceMock.Setup(x => x.GetUserPreferencesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesServiceMock.Setup(x => x.IsInQuietHoursAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupPreferencesAndTemplate(NotificationRequest request, UserNotificationPreferences preferences, RenderedTemplate? template = null)
    {
        _preferencesServiceMock.Setup(x => x.GetUserPreferencesAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesServiceMock.Setup(x => x.IsInQuietHoursAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        if (!string.IsNullOrEmpty(request.TemplateKey) && template != null)
        {
            _templateServiceMock.Setup(x => x.RenderAsync(
                request.TemplateKey,
                request.Data,
                preferences.Language,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
        }
    }
}