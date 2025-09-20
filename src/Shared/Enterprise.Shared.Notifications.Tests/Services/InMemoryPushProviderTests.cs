using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemoryPushProviderTests
{
    private readonly Mock<ILogger<InMemoryPushProvider>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly InMemoryPushProvider _provider;

    public InMemoryPushProviderTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryPushProvider>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();
        _settings = new NotificationSettings();
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _provider = new InMemoryPushProvider(_loggerMock.Object, _optionsMock.Object);
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
        var act = () => new InMemoryPushProvider(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemoryPushProvider(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SendAsync_Should_Send_Push_Notification_Successfully()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notification = new PushNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test Title",
            Body = "Test message",
            DeviceTokens = new List<string> { "valid-device-token" },
            Data = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var sentPushes = InMemoryPushProvider.GetSentPushNotifications();
        sentPushes.Should().ContainSingle();
        sentPushes.First().Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Exception_When_Notification_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendBulkAsync_Should_Send_Multiple_Push_Notifications_Successfully()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notifications = new[]
        {
            new PushNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Test Title 1",
                Body = "Test message 1",
                DeviceTokens = new List<string> { "token1" }
            },
            new PushNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Test Title 2",
                Body = "Test message 2",
                DeviceTokens = new List<string> { "token2" }
            }
        };

        // Act
        await _provider.SendBulkAsync(notifications);

        // Assert
        var sentPushes = InMemoryPushProvider.GetSentPushNotifications();
        sentPushes.Should().HaveCount(2);
        sentPushes.Should().BeEquivalentTo(notifications);
    }

    [Fact]
    public async Task SendBulkAsync_Should_Throw_Exception_When_Notifications_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendBulkAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendToTopicAsync_Should_Send_Push_Notification_To_Topic_Successfully()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var topic = "test-topic";
        var notification = new PushNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Topic Message",
            Body = "Test topic message"
        };

        // Act
        await _provider.SendToTopicAsync(topic, notification);

        // Assert
        var sentPushes = InMemoryPushProvider.GetSentPushNotifications();
        sentPushes.Should().ContainSingle();
        var sentPush = sentPushes.First();
        sentPush.Topic.Should().Be(topic);
        sentPush.Title.Should().Be("Topic Message");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SendToTopicAsync_Should_Throw_Exception_When_Topic_Is_Invalid(string topic)
    {
        // Arrange
        var notification = new PushNotification
        {
            NotificationId = Guid.NewGuid(),
            Title = "Test",
            Body = "Test"
        };

        // Act & Assert
        var act = async () => await _provider.SendToTopicAsync(topic, notification);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendToTopicAsync_Should_Throw_Exception_When_Notification_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendToTopicAsync("topic", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Delivered_When_Notification_Exists()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notification = new PushNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test",
            Body = "Test"
        };
        await _provider.SendAsync(notification);

        // Act
        var result = await _provider.VerifyDeliveryAsync(notification.NotificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Unknown_When_Notification_Does_Not_Exist()
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
    public async Task SubscribeToTopicAsync_Should_Add_Tokens_To_Topic()
    {
        // Arrange
        var tokens = new[] { "token1", "token2", "token3" };
        var topic = "test-topic";

        // Act
        await _provider.SubscribeToTopicAsync(tokens, topic);

        // Assert
        var subscriptions = InMemoryPushProvider.GetTopicSubscriptions();
        subscriptions.Should().ContainKey(topic);
        subscriptions[topic].Should().BeEquivalentTo(tokens);
    }

    [Fact]
    public async Task SubscribeToTopicAsync_Should_Not_Add_Duplicate_Tokens()
    {
        // Arrange
        var tokens1 = new[] { "token1", "token2" };
        var tokens2 = new[] { "token2", "token3" };
        var topic = "test-topic";

        // Act
        await _provider.SubscribeToTopicAsync(tokens1, topic);
        await _provider.SubscribeToTopicAsync(tokens2, topic);

        // Assert
        var subscriptions = InMemoryPushProvider.GetTopicSubscriptions();
        subscriptions[topic].Should().BeEquivalentTo(new[] { "token1", "token2", "token3" });
    }

    [Fact]
    public async Task SubscribeToTopicAsync_Should_Throw_Exception_When_Tokens_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SubscribeToTopicAsync(null!, "topic");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SubscribeToTopicAsync_Should_Throw_Exception_When_Topic_Is_Invalid(string topic)
    {
        // Act & Assert
        var act = async () => await _provider.SubscribeToTopicAsync(new[] { "token" }, topic);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UnsubscribeFromTopicAsync_Should_Remove_Tokens_From_Topic()
    {
        // Arrange
        var tokens = new[] { "token1", "token2", "token3" };
        var tokensToRemove = new[] { "token1", "token3" };
        var topic = "test-topic";

        await _provider.SubscribeToTopicAsync(tokens, topic);

        // Act
        await _provider.UnsubscribeFromTopicAsync(tokensToRemove, topic);

        // Assert
        var subscriptions = InMemoryPushProvider.GetTopicSubscriptions();
        subscriptions[topic].Should().BeEquivalentTo(new[] { "token2" });
    }

    [Fact]
    public async Task UnsubscribeFromTopicAsync_Should_Handle_Non_Existent_Topic()
    {
        // Arrange
        var tokens = new[] { "token1", "token2" };
        var topic = "non-existent-topic";

        // Act & Assert
        var act = async () => await _provider.UnsubscribeFromTopicAsync(tokens, topic);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateTokensAsync_Should_Return_Valid_Tokens()
    {
        // Arrange
        var tokens = new[] 
        { 
            "valid-long-token-123456", 
            "another-valid-token-789", 
            "short", // Invalid - too short
            "", // Invalid - empty
            "   ", // Invalid - whitespace
            "valid-token-abc" // Valid
        };

        // Act
        var result = await _provider.ValidateTokensAsync(tokens);

        // Assert
        var validTokens = result.ToList();
        validTokens.Should().HaveCount(3);
        validTokens.Should().Contain("valid-long-token-123456");
        validTokens.Should().Contain("another-valid-token-789");
        validTokens.Should().Contain("valid-token-abc");
        validTokens.Should().NotContain("short");
        validTokens.Should().NotContain("");
        validTokens.Should().NotContain("   ");
    }

    [Fact]
    public async Task ValidateTokensAsync_Should_Throw_Exception_When_Tokens_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.ValidateTokensAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GetSentPushNotifications_Should_Return_All_Sent_Notifications()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notification1 = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test 1", Body = "Body 1" };
        var notification2 = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test 2", Body = "Body 2" };

        // Act
        _provider.SendAsync(notification1).Wait();
        _provider.SendAsync(notification2).Wait();
        var result = InMemoryPushProvider.GetSentPushNotifications();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.NotificationId == notification1.NotificationId);
        result.Should().Contain(p => p.NotificationId == notification2.NotificationId);
    }

    [Fact]
    public void GetSentPushNotification_Should_Return_Specific_Notification_When_Exists()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notification = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test", Body = "Body" };
        _provider.SendAsync(notification).Wait();

        // Act
        var result = InMemoryPushProvider.GetSentPushNotification(notification.NotificationId);

        // Assert
        result.Should().NotBeNull();
        result!.NotificationId.Should().Be(notification.NotificationId);
        result.Title.Should().Be("Test");
    }

    [Fact]
    public void GetSentPushNotification_Should_Return_Null_When_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = InMemoryPushProvider.GetSentPushNotification(notificationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearSentPushNotifications_Should_Remove_All_Notifications()
    {
        // Arrange
        var notification = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test", Body = "Body" };
        _provider.SendAsync(notification).Wait();
        InMemoryPushProvider.GetSentPushNotifications().Should().NotBeEmpty();

        // Act
        InMemoryPushProvider.ClearSentPushNotifications();

        // Assert
        InMemoryPushProvider.GetSentPushNotifications().Should().BeEmpty();
        InMemoryPushProvider.GetSentPushNotificationsCount().Should().Be(0);
    }

    [Fact]
    public void GetSentPushNotificationsCount_Should_Return_Correct_Count()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var notification1 = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test1", Body = "Body1" };
        var notification2 = new PushNotification { NotificationId = Guid.NewGuid(), Title = "Test2", Body = "Body2" };

        // Act & Assert
        InMemoryPushProvider.GetSentPushNotificationsCount().Should().Be(0);

        _provider.SendAsync(notification1).Wait();
        InMemoryPushProvider.GetSentPushNotificationsCount().Should().Be(1);

        _provider.SendAsync(notification2).Wait();
        InMemoryPushProvider.GetSentPushNotificationsCount().Should().Be(2);
    }

    [Fact]
    public void GetTopicSubscriptions_Should_Return_All_Topic_Subscriptions()
    {
        // Arrange
        InMemoryPushProvider.ClearSentPushNotifications();
        var tokens1 = new[] { "token1", "token2" };
        var tokens2 = new[] { "token3", "token4" };
        var topic1 = "topic1";
        var topic2 = "topic2";

        // Act
        _provider.SubscribeToTopicAsync(tokens1, topic1).Wait();
        _provider.SubscribeToTopicAsync(tokens2, topic2).Wait();
        var subscriptions = InMemoryPushProvider.GetTopicSubscriptions();

        // Assert
        subscriptions.Should().HaveCount(2);
        subscriptions.Should().ContainKey(topic1);
        subscriptions.Should().ContainKey(topic2);
        subscriptions[topic1].Should().BeEquivalentTo(tokens1);
        subscriptions[topic2].Should().BeEquivalentTo(tokens2);
    }
}