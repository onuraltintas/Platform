using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemorySmsProviderTests
{
    private readonly Mock<ILogger<InMemorySmsProvider>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly InMemorySmsProvider _provider;

    public InMemorySmsProviderTests()
    {
        _loggerMock = new Mock<ILogger<InMemorySmsProvider>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();
        _settings = new NotificationSettings
        {
            SMS = new SmsSettings
            {
                MaxMessageLength = 160
            }
        };
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _provider = new InMemorySmsProvider(_loggerMock.Object, _optionsMock.Object);
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
        var act = () => new InMemorySmsProvider(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemorySmsProvider(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SendAsync_Should_Send_Sms_Successfully_With_Valid_Phone_Number()
    {
        // Arrange
        InMemorySmsProvider.ClearSentSms();
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test message"
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var sentSms = InMemorySmsProvider.GetSentSms();
        sentSms.Should().ContainSingle();
        sentSms.First().Should().BeEquivalentTo(notification);

        var deliveryStatus = InMemorySmsProvider.GetDeliveryStatus(notification.NotificationId);
        deliveryStatus.Should().Be(DeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_Should_Mark_As_Failed_With_Invalid_Phone_Number()
    {
        // Arrange
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToPhoneNumber = "invalid-number",
            Message = "Test message"
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var deliveryStatus = InMemorySmsProvider.GetDeliveryStatus(notification.NotificationId);
        deliveryStatus.Should().Be(DeliveryStatus.Failed);
    }

    [Fact]
    public async Task SendAsync_Should_Send_Sms_Even_When_Message_Exceeds_Max_Length()
    {
        // Arrange
        var longMessage = new string('a', 200); // Exceeds 160 character limit
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = longMessage
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var sentSms = InMemorySmsProvider.GetSentSms();
        sentSms.Should().ContainSingle();
        sentSms.First().Message.Should().Be(longMessage);

        var deliveryStatus = InMemorySmsProvider.GetDeliveryStatus(notification.NotificationId);
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
    public async Task SendBulkAsync_Should_Send_Multiple_Sms_Successfully()
    {
        // Arrange
        InMemorySmsProvider.ClearSentSms();
        var notifications = new[]
        {
            new SmsNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ToPhoneNumber = "+1234567890",
                Message = "Test message 1"
            },
            new SmsNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ToPhoneNumber = "+0987654321",
                Message = "Test message 2"
            }
        };

        // Act
        await _provider.SendBulkAsync(notifications);

        // Assert
        var sentSms = InMemorySmsProvider.GetSentSms();
        sentSms.Should().HaveCount(2);
        sentSms.Should().BeEquivalentTo(notifications);
    }

    [Fact]
    public async Task SendBulkAsync_Should_Throw_Exception_When_Notifications_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendBulkAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Delivered_When_Sms_Exists()
    {
        // Arrange
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test message"
        };
        await _provider.SendAsync(notification);

        // Act
        var result = await _provider.VerifyDeliveryAsync(notification.NotificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Unknown_When_Sms_Does_Not_Exist()
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
    public async Task GetDeliveryReportsAsync_Should_Return_Reports_For_Valid_Message_Ids()
    {
        // Arrange
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test message"
        };
        await _provider.SendAsync(notification);

        var allReports = InMemorySmsProvider.GetAllDeliveryReports().ToList();
        var messageIds = allReports.Select(r => r.MessageId).ToList();

        // Act
        var result = await _provider.GetDeliveryReportsAsync(messageIds);

        // Assert
        result.Should().HaveCount(1);
        result.First().PhoneNumber.Should().Be(notification.ToPhoneNumber);
        result.First().Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public async Task GetDeliveryReportsAsync_Should_Return_Empty_For_Invalid_Message_Ids()
    {
        // Arrange
        var messageIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        // Act
        var result = await _provider.GetDeliveryReportsAsync(messageIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeliveryReportsAsync_Should_Throw_Exception_When_MessageIds_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.GetDeliveryReportsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidatePhoneNumberAsync_Should_Return_Valid_For_Proper_Format()
    {
        // Arrange
        var phoneNumber = "+1234567890";

        // Act
        var result = await _provider.ValidatePhoneNumberAsync(phoneNumber);

        // Assert
        result.IsValid.Should().BeTrue();
        result.FormattedNumber.Should().Be(phoneNumber);
        result.CountryCode.Should().Be("US");
        result.CountryName.Should().Be("United States");
        result.PhoneType.Should().Be("Mobile");
    }

    [Fact]
    public async Task ValidatePhoneNumberAsync_Should_Return_Invalid_For_Improper_Format()
    {
        // Arrange
        var phoneNumber = "invalid-phone";

        // Act
        var result = await _provider.ValidatePhoneNumberAsync(phoneNumber);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FormattedNumber.Should().Be(phoneNumber);
        result.CountryCode.Should().Be("Unknown");
        result.CountryName.Should().Be("Unknown");
    }

    [Fact]
    public async Task ValidatePhoneNumberAsync_Should_Throw_Exception_When_PhoneNumber_Is_Null_Or_Empty()
    {
        // Act & Assert
        var act1 = async () => await _provider.ValidatePhoneNumberAsync(null!);
        await act1.Should().ThrowAsync<ArgumentException>();

        var act2 = async () => await _provider.ValidatePhoneNumberAsync("");
        await act2.Should().ThrowAsync<ArgumentException>();

        var act3 = async () => await _provider.ValidatePhoneNumberAsync("   ");
        await act3.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetSentSms_Should_Return_All_Sent_Messages()
    {
        // Arrange
        InMemorySmsProvider.ClearSentSms();
        var notification1 = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test 1"
        };
        var notification2 = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            ToPhoneNumber = "+0987654321",
            Message = "Test 2"
        };

        // Act
        _provider.SendAsync(notification1).Wait();
        _provider.SendAsync(notification2).Wait();
        var result = InMemorySmsProvider.GetSentSms();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.NotificationId == notification1.NotificationId);
        result.Should().Contain(s => s.NotificationId == notification2.NotificationId);
    }

    [Fact]
    public void GetSentSms_By_Id_Should_Return_Specific_Message_When_Exists()
    {
        // Arrange
        InMemorySmsProvider.ClearSentSms();
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test"
        };
        _provider.SendAsync(notification).Wait();

        // Act
        var result = InMemorySmsProvider.GetSentSms(notification.NotificationId);

        // Assert
        result.Should().NotBeNull();
        result!.NotificationId.Should().Be(notification.NotificationId);
        result.ToPhoneNumber.Should().Be(notification.ToPhoneNumber);
        result.Message.Should().Be(notification.Message);
    }

    [Fact]
    public void GetSentSms_By_Id_Should_Return_Null_When_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = InMemorySmsProvider.GetSentSms(notificationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearSentSms_Should_Remove_All_Messages_And_Related_Data()
    {
        // Arrange
        var notification = new SmsNotification
        {
            NotificationId = Guid.NewGuid(),
            ToPhoneNumber = "+1234567890",
            Message = "Test"
        };
        _provider.SendAsync(notification).Wait();
        InMemorySmsProvider.GetSentSms().Should().NotBeEmpty();

        // Act
        InMemorySmsProvider.ClearSentSms();

        // Assert
        InMemorySmsProvider.GetSentSms().Should().BeEmpty();
        InMemorySmsProvider.GetSentSmsCount().Should().Be(0);
        InMemorySmsProvider.GetDeliveryStatus(notification.NotificationId).Should().BeNull();
        InMemorySmsProvider.GetAllDeliveryReports().Should().BeEmpty();
    }

    [Fact]
    public void GetSentSmsCount_Should_Return_Correct_Count()
    {
        // Arrange
        InMemorySmsProvider.ClearSentSms();
        var notification1 = new SmsNotification { NotificationId = Guid.NewGuid(), ToPhoneNumber = "+1111111111", Message = "Test1" };
        var notification2 = new SmsNotification { NotificationId = Guid.NewGuid(), ToPhoneNumber = "+2222222222", Message = "Test2" };

        // Act & Assert
        InMemorySmsProvider.GetSentSmsCount().Should().Be(0);

        _provider.SendAsync(notification1).Wait();
        InMemorySmsProvider.GetSentSmsCount().Should().Be(1);

        _provider.SendAsync(notification2).Wait();
        InMemorySmsProvider.GetSentSmsCount().Should().Be(2);
    }
}