using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemoryWebhookProviderTests
{
    private readonly Mock<ILogger<InMemoryWebhookProvider>> _loggerMock;
    private readonly InMemoryWebhookProvider _provider;

    public InMemoryWebhookProviderTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryWebhookProvider>>();
        _provider = new InMemoryWebhookProvider(_loggerMock.Object);
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
        var act = () => new InMemoryWebhookProvider(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task SendAsync_Should_Send_Webhook_Notification_Successfully()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var notification = new WebhookNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Url = "https://api.example.com/webhook",
            Method = "POST",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", "Bearer token123" }
            },
            Payload = new { message = "Test webhook", timestamp = DateTime.UtcNow },
            Secret = "webhook-secret"
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var sentWebhooks = InMemoryWebhookProvider.GetSentWebhooks();
        sentWebhooks.Should().ContainSingle();
        sentWebhooks.First().Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Exception_When_Notification_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Delivered_When_Webhook_Exists()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var notification = new WebhookNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Url = "https://api.example.com/webhook",
            Payload = new { test = "data" }
        };
        await _provider.SendAsync(notification);

        // Act
        var result = await _provider.VerifyDeliveryAsync(notification.NotificationId);

        // Assert
        result.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Unknown_When_Webhook_Does_Not_Exist()
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
    public async Task RegisterWebhookAsync_Should_Register_Webhook_With_Event_Types()
    {
        // Arrange
        var url = "https://api.example.com/webhook";
        var secret = "webhook-secret";
        var eventTypes = new[] { NotificationType.Welcome, NotificationType.OrderConfirmation, NotificationType.PaymentSuccess };

        // Act
        await _provider.RegisterWebhookAsync(url, secret, eventTypes);

        // Assert
        var registeredWebhooks = InMemoryWebhookProvider.GetRegisteredWebhooks();
        registeredWebhooks.Should().ContainKey(url);
        registeredWebhooks[url].Should().BeEquivalentTo(eventTypes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RegisterWebhookAsync_Should_Throw_Exception_When_Url_Is_Invalid(string url)
    {
        // Arrange
        var eventTypes = new[] { NotificationType.Welcome };

        // Act & Assert
        var act = async () => await _provider.RegisterWebhookAsync(url, "secret", eventTypes);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RegisterWebhookAsync_Should_Throw_Exception_When_EventTypes_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.RegisterWebhookAsync("https://example.com", "secret", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterWebhookAsync_Should_Replace_Existing_Registration()
    {
        // Arrange
        var url = "https://api.example.com/webhook";
        var secret = "webhook-secret";
        var initialEventTypes = new[] { NotificationType.Welcome };
        var updatedEventTypes = new[] { NotificationType.Welcome, NotificationType.OrderConfirmation };

        await _provider.RegisterWebhookAsync(url, secret, initialEventTypes);

        // Act
        await _provider.RegisterWebhookAsync(url, secret, updatedEventTypes);

        // Assert
        var registeredWebhooks = InMemoryWebhookProvider.GetRegisteredWebhooks();
        registeredWebhooks[url].Should().BeEquivalentTo(updatedEventTypes);
    }

    [Fact]
    public async Task UnregisterWebhookAsync_Should_Remove_Webhook_Registration()
    {
        // Arrange
        var url = "https://api.example.com/webhook";
        var eventTypes = new[] { NotificationType.Welcome };
        await _provider.RegisterWebhookAsync(url, "secret", eventTypes);

        // Act
        await _provider.UnregisterWebhookAsync(url);

        // Assert
        var registeredWebhooks = InMemoryWebhookProvider.GetRegisteredWebhooks();
        registeredWebhooks.Should().NotContainKey(url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UnregisterWebhookAsync_Should_Throw_Exception_When_Url_Is_Invalid(string url)
    {
        // Act & Assert
        var act = async () => await _provider.UnregisterWebhookAsync(url);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UnregisterWebhookAsync_Should_Handle_Non_Existent_Webhook()
    {
        // Arrange
        var url = "https://api.example.com/webhook";

        // Act & Assert
        var act = async () => await _provider.UnregisterWebhookAsync(url);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TestWebhookAsync_Should_Return_Successful_Test_Result()
    {
        // Arrange
        var url = "https://api.example.com/webhook";

        // Act
        var result = await _provider.TestWebhookAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        result.ResponseHeaders.Should().NotBeEmpty();
        result.ResponseHeaders.Should().ContainKey("Content-Type");
        result.ResponseHeaders.Should().ContainKey("Server");
        result.ResponseHeaders["Content-Type"].Should().Be("application/json");
        result.ResponseHeaders["Server"].Should().Be("InMemory");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task TestWebhookAsync_Should_Throw_Exception_When_Url_Is_Invalid(string url)
    {
        // Act & Assert
        var act = async () => await _provider.TestWebhookAsync(url);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TestWebhookAsync_Should_Return_Varying_Response_Times()
    {
        // Arrange
        var url = "https://api.example.com/webhook";
        var results = new List<Enterprise.Shared.Notifications.Interfaces.WebhookTestResult>();

        // Act - Run multiple tests to check randomization
        for (int i = 0; i < 10; i++)
        {
            var result = await _provider.TestWebhookAsync(url);
            results.Add(result);
        }

        // Assert - Response times should vary (due to randomization)
        var responseTimes = results.Select(r => r.ResponseTimeMs).ToList();
        responseTimes.Should().AllSatisfy(rt => rt.Should().BeInRange(50, 500));
        
        // At least some variation in response times (not all the same)
        responseTimes.Distinct().Should().HaveCountGreaterThan(1, "Response times should vary due to randomization");
    }

    [Fact]
    public void GetSentWebhooks_Should_Return_All_Sent_Webhooks()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var webhook1 = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api1.example.com/webhook",
            Payload = new { test1 = "data1" }
        };
        var webhook2 = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api2.example.com/webhook",
            Payload = new { test2 = "data2" }
        };

        // Act
        _provider.SendAsync(webhook1).Wait();
        _provider.SendAsync(webhook2).Wait();
        var result = InMemoryWebhookProvider.GetSentWebhooks();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(w => w.NotificationId == webhook1.NotificationId);
        result.Should().Contain(w => w.NotificationId == webhook2.NotificationId);
    }

    [Fact]
    public void GetSentWebhook_Should_Return_Specific_Webhook_When_Exists()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var webhook = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api.example.com/webhook",
            Payload = new { test = "data" }
        };
        _provider.SendAsync(webhook).Wait();

        // Act
        var result = InMemoryWebhookProvider.GetSentWebhook(webhook.NotificationId);

        // Assert
        result.Should().NotBeNull();
        result!.NotificationId.Should().Be(webhook.NotificationId);
        result.Url.Should().Be(webhook.Url);
    }

    [Fact]
    public void GetSentWebhook_Should_Return_Null_When_Does_Not_Exist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var result = InMemoryWebhookProvider.GetSentWebhook(notificationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearSentWebhooks_Should_Remove_All_Sent_Webhooks()
    {
        // Arrange
        var webhook = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api.example.com/webhook",
            Payload = new { test = "data" }
        };
        _provider.SendAsync(webhook).Wait();
        InMemoryWebhookProvider.GetSentWebhooks().Should().NotBeEmpty();

        // Act
        InMemoryWebhookProvider.ClearSentWebhooks();

        // Assert
        InMemoryWebhookProvider.GetSentWebhooks().Should().BeEmpty();
        InMemoryWebhookProvider.GetSentWebhooksCount().Should().Be(0);
    }

    [Fact]
    public void GetSentWebhooksCount_Should_Return_Correct_Count()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var webhook1 = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api1.example.com/webhook",
            Payload = new { test1 = "data1" }
        };
        var webhook2 = new WebhookNotification 
        { 
            NotificationId = Guid.NewGuid(), 
            Url = "https://api2.example.com/webhook",
            Payload = new { test2 = "data2" }
        };

        // Act & Assert
        InMemoryWebhookProvider.GetSentWebhooksCount().Should().Be(0);

        _provider.SendAsync(webhook1).Wait();
        InMemoryWebhookProvider.GetSentWebhooksCount().Should().Be(1);

        _provider.SendAsync(webhook2).Wait();
        InMemoryWebhookProvider.GetSentWebhooksCount().Should().Be(2);
    }

    [Fact]
    public void GetRegisteredWebhooks_Should_Return_All_Registered_Webhooks()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var url1 = "https://api1.example.com/webhook";
        var url2 = "https://api2.example.com/webhook";
        var eventTypes1 = new[] { NotificationType.Welcome };
        var eventTypes2 = new[] { NotificationType.OrderConfirmation, NotificationType.PaymentSuccess };

        // Act
        _provider.RegisterWebhookAsync(url1, "secret1", eventTypes1).Wait();
        _provider.RegisterWebhookAsync(url2, "secret2", eventTypes2).Wait();
        var registeredWebhooks = InMemoryWebhookProvider.GetRegisteredWebhooks();

        // Assert
        registeredWebhooks.Should().HaveCount(2);
        registeredWebhooks.Should().ContainKey(url1);
        registeredWebhooks.Should().ContainKey(url2);
        registeredWebhooks[url1].Should().BeEquivalentTo(eventTypes1);
        registeredWebhooks[url2].Should().BeEquivalentTo(eventTypes2);
    }

    [Fact]
    public async Task Multiple_Operations_Should_Work_Together_Correctly()
    {
        // Arrange
        InMemoryWebhookProvider.ClearSentWebhooks();
        var url = "https://api.example.com/webhook";
        var eventTypes = new[] { NotificationType.Welcome, NotificationType.OrderConfirmation };
        var webhook = new WebhookNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Url = url,
            Payload = new { message = "Integration test" }
        };

        // Act & Assert
        // 1. Register webhook
        await _provider.RegisterWebhookAsync(url, "secret", eventTypes);
        var registrations = InMemoryWebhookProvider.GetRegisteredWebhooks();
        registrations.Should().ContainKey(url);

        // 2. Test webhook
        var testResult = await _provider.TestWebhookAsync(url);
        testResult.IsSuccessful.Should().BeTrue();

        // 3. Send webhook
        await _provider.SendAsync(webhook);
        var sentWebhooks = InMemoryWebhookProvider.GetSentWebhooks();
        sentWebhooks.Should().ContainSingle();

        // 4. Verify delivery
        var deliveryStatus = await _provider.VerifyDeliveryAsync(webhook.NotificationId);
        deliveryStatus.Should().Be(DeliveryStatus.Delivered);

        // 5. Unregister webhook
        await _provider.UnregisterWebhookAsync(url);
        registrations = InMemoryWebhookProvider.GetRegisteredWebhooks();
        registrations.Should().NotContainKey(url);
    }
}