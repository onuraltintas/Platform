using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemoryEmailProviderTests
{
    private readonly Mock<ILogger<InMemoryEmailProvider>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly InMemoryEmailProvider _provider;

    public InMemoryEmailProviderTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryEmailProvider>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();
        _settings = new NotificationSettings();
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _provider = new InMemoryEmailProvider(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Provider_With_Valid_Dependencies()
    {
        // Act & Assert
        _provider.ProviderName.Should().Be("InMemory");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemoryEmailProvider(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemoryEmailProvider(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SendAsync_Should_Send_Email_Successfully()
    {
        // Arrange
        var notification = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToEmail = "test@example.com",
            Subject = "Test Subject",
            HtmlContent = "<p>Test content</p>",
            TextContent = "Test content"
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var sentEmails = InMemoryEmailProvider.GetSentEmails();
        sentEmails.Should().ContainSingle();
        sentEmails.First().Should().BeEquivalentTo(notification);

        var deliveryStatus = InMemoryEmailProvider.GetDeliveryStatus(notification.NotificationId);
        deliveryStatus.Should().Be(DeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Exception_When_Notification_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendBulkAsync_Should_Send_Multiple_Emails_Successfully()
    {
        // Arrange
        InMemoryEmailProvider.ClearSentEmails();
        var notifications = new[]
        {
            new EmailNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ToEmail = "test1@example.com",
                Subject = "Test Subject 1",
                TextContent = "Test content 1"
            },
            new EmailNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ToEmail = "test2@example.com",
                Subject = "Test Subject 2",
                TextContent = "Test content 2"
            }
        };

        // Act
        await _provider.SendBulkAsync(notifications);

        // Assert
        var sentEmails = InMemoryEmailProvider.GetSentEmails();
        sentEmails.Should().HaveCount(2);
        sentEmails.Should().BeEquivalentTo(notifications);
    }

    [Fact]
    public async Task SendBulkAsync_Should_Throw_Exception_When_Notifications_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendBulkAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Delivered_When_Email_Exists()
    {
        // Arrange
        var notification = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToEmail = "test@example.com",
            Subject = "Test Subject",
            TextContent = "Test content"
        };
        await _provider.SendAsync(notification);

        // Act
        var result = await _provider.VerifyDeliveryAsync(notification.NotificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Unknown_When_Email_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = await _provider.VerifyDeliveryAsync(notificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Unknown);
    }

    [Fact]
    public async Task IsHealthyAsync_Should_Return_True()
    {
        // Act
        var result = await _provider.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetBounceListAsync_Should_Return_Empty_List()
    {
        // Act
        var result = await _provider.GetBounceListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSentEmails_Should_Return_All_Sent_Emails()
    {
        // Arrange
        InMemoryEmailProvider.ClearSentEmails(); // Clear any previous emails
        var notification1 = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            ToEmail = "test1@example.com",
            Subject = "Test 1"
        };
        var notification2 = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            ToEmail = "test2@example.com",
            Subject = "Test 2"
        };

        // Act
        _provider.SendAsync(notification1).Wait();
        _provider.SendAsync(notification2).Wait();
        var result = InMemoryEmailProvider.GetSentEmails();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.NotificationId == notification1.NotificationId);
        result.Should().Contain(e => e.NotificationId == notification2.NotificationId);
    }

    [Fact]
    public void GetSentEmail_Should_Return_Specific_Email_When_Exists()
    {
        // Arrange
        InMemoryEmailProvider.ClearSentEmails();
        var notification = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            ToEmail = "test@example.com",
            Subject = "Test"
        };
        _provider.SendAsync(notification).Wait();

        // Act
        var result = InMemoryEmailProvider.GetSentEmail(notification.NotificationId);

        // Assert
        result.Should().NotBeNull();
        result!.NotificationId.Should().Be(notification.NotificationId);
        result.ToEmail.Should().Be(notification.ToEmail);
    }

    [Fact]
    public void GetSentEmail_Should_Return_Null_When_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = InMemoryEmailProvider.GetSentEmail(notificationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearSentEmails_Should_Remove_All_Emails()
    {
        // Arrange
        var notification = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            ToEmail = "test@example.com"
        };
        _provider.SendAsync(notification).Wait();
        InMemoryEmailProvider.GetSentEmails().Should().NotBeEmpty();

        // Act
        InMemoryEmailProvider.ClearSentEmails();

        // Assert
        InMemoryEmailProvider.GetSentEmails().Should().BeEmpty();
        InMemoryEmailProvider.GetSentEmailsCount().Should().Be(0);
    }

    [Fact]
    public void GetSentEmailsCount_Should_Return_Correct_Count()
    {
        // Arrange
        InMemoryEmailProvider.ClearSentEmails();
        var notification1 = new EmailNotification { NotificationId = Guid.NewGuid(), ToEmail = "test1@example.com" };
        var notification2 = new EmailNotification { NotificationId = Guid.NewGuid(), ToEmail = "test2@example.com" };

        // Act & Assert
        InMemoryEmailProvider.GetSentEmailsCount().Should().Be(0);

        _provider.SendAsync(notification1).Wait();
        InMemoryEmailProvider.GetSentEmailsCount().Should().Be(1);

        _provider.SendAsync(notification2).Wait();
        InMemoryEmailProvider.GetSentEmailsCount().Should().Be(2);
    }

    [Fact]
    public void GetDeliveryStatus_Should_Return_Status_When_Exists()
    {
        // Arrange
        InMemoryEmailProvider.ClearSentEmails();
        var notification = new EmailNotification
        {
            NotificationId = Guid.NewGuid(),
            ToEmail = "test@example.com"
        };
        _provider.SendAsync(notification).Wait();

        // Act
        var result = InMemoryEmailProvider.GetDeliveryStatus(notification.NotificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Sent);
    }

    [Fact]
    public void GetDeliveryStatus_Should_Return_Null_When_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = InMemoryEmailProvider.GetDeliveryStatus(notificationId);

        // Assert
        result.Should().BeNull();
    }
}