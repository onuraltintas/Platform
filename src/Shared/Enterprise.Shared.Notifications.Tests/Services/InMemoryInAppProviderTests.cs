using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemoryInAppProviderTests
{
    private readonly Mock<ILogger<InMemoryInAppProvider>> _loggerMock;
    private readonly InMemoryInAppProvider _provider;

    public InMemoryInAppProviderTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryInAppProvider>>();
        _provider = new InMemoryInAppProvider(_loggerMock.Object);
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
        var act = () => new InMemoryInAppProvider(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task SendAsync_Should_Send_InApp_Notification_Successfully()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Test Title",
            Content = "Test message",
            Type = InAppNotificationType.Info,
            ActionUrl = "/dashboard",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _provider.SendAsync(notification);

        // Assert
        var allNotifications = InMemoryInAppProvider.GetAllNotifications();
        allNotifications.Should().ContainSingle();
        allNotifications.First().Should().BeEquivalentTo(notification);

        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        userNotifications.Should().ContainSingle();
        userNotifications.First().Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Exception_When_Notification_Is_Null()
    {
        // Act & Assert
        var act = async () => await _provider.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_Should_Limit_User_Notifications_To_100()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notifications = new List<InAppNotification>();

        // Create 105 notifications
        for (int i = 0; i < 105; i++)
        {
            notifications.Add(new InAppNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = $"Test Title {i}",
                Content = $"Test message {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        foreach (var notification in notifications)
        {
            await _provider.SendAsync(notification);
        }

        // Assert
        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        userNotifications.Should().HaveCount(100);
        
        // Should keep the latest 100 (oldest 5 should be removed)
        userNotifications.Should().NotContain(n => n.Title == "Test Title 0");
        userNotifications.Should().NotContain(n => n.Title == "Test Title 1");
        userNotifications.Should().NotContain(n => n.Title == "Test Title 2");
        userNotifications.Should().NotContain(n => n.Title == "Test Title 3");
        userNotifications.Should().NotContain(n => n.Title == "Test Title 4");
    }

    [Fact]
    public async Task VerifyDeliveryAsync_Should_Return_Delivered_When_Notification_Exists()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test",
            Content = "Test"
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
    public async Task MarkAsReadAsync_Should_Mark_Notification_As_Read()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Test",
            Content = "Test",
            IsRead = false
        };
        await _provider.SendAsync(notification);

        // Act
        await _provider.MarkAsReadAsync(notification.NotificationId, userId);

        // Assert
        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        var updatedNotification = userNotifications.First();
        updatedNotification.IsRead.Should().BeTrue();
        updatedNotification.ReadAt.Should().NotBeNull();
        updatedNotification.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MarkAsReadAsync_Should_Not_Mark_Notification_From_Different_User()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId1,
            Title = "Test",
            Content = "Test",
            IsRead = false
        };
        await _provider.SendAsync(notification);

        // Act
        await _provider.MarkAsReadAsync(notification.NotificationId, userId2);

        // Assert
        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId1);
        var unchangedNotification = userNotifications.First();
        unchangedNotification.IsRead.Should().BeFalse();
        unchangedNotification.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_Should_Mark_All_User_Notifications_As_Read()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notifications = new[]
        {
            new InAppNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = "Test 1",
                Content = "Test 1",
                IsRead = false
            },
            new InAppNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = "Test 2",
                Content = "Test 2",
                IsRead = false
            },
            new InAppNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = "Test 3",
                Content = "Test 3",
                IsRead = true // Already read
            }
        };

        foreach (var notification in notifications)
        {
            await _provider.SendAsync(notification);
        }

        // Act
        await _provider.MarkAllAsReadAsync(userId);

        // Assert
        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        userNotifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
        userNotifications.Where(n => n.Title != "Test 3").Should().AllSatisfy(n => 
            n.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task GetUnreadCountAsync_Should_Return_Correct_Count_Of_Unread_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var readNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Read",
            Content = "Read",
            IsRead = true
        };
        var unreadNotification1 = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Unread 1",
            Content = "Unread 1",
            IsRead = false
        };
        var unreadNotification2 = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Unread 2",
            Content = "Unread 2",
            IsRead = false
        };

        await _provider.SendAsync(readNotification);
        await _provider.SendAsync(unreadNotification1);
        await _provider.SendAsync(unreadNotification2);

        // Act
        var result = await _provider.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_Should_Exclude_Expired_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var expiredNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Expired",
            Content = "Expired",
            IsRead = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Expired
        };
        var validNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Valid",
            Content = "Valid",
            IsRead = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Not expired
        };

        await _provider.SendAsync(expiredNotification);
        await _provider.SendAsync(validNotification);

        // Act
        var result = await _provider.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetUnreadCountAsync_Should_Return_Zero_When_User_Has_No_Notifications()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _provider.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetNotificationsAsync_Should_Return_Paginated_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notifications = new List<InAppNotification>();

        for (int i = 0; i < 25; i++)
        {
            notifications.Add(new InAppNotification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = $"Test {i}",
                Content = $"Body {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        foreach (var notification in notifications)
        {
            await _provider.SendAsync(notification);
        }

        // Act
        var result = await _provider.GetNotificationsAsync(userId, page: 1, pageSize: 10);

        // Assert
        result.Should().HaveCount(10);
        // Should be ordered by CreatedAt descending (most recent first)
        var resultList = result.ToList();
        resultList.First().Title.Should().Be("Test 0"); // Most recent
        resultList.Last().Title.Should().Be("Test 9");
    }

    [Fact]
    public async Task GetNotificationsAsync_Should_Return_Only_Unread_When_UnreadOnly_Is_True()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var readNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Read",
            Content = "Read",
            IsRead = true
        };
        var unreadNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Unread",
            Content = "Unread",
            IsRead = false
        };

        await _provider.SendAsync(readNotification);
        await _provider.SendAsync(unreadNotification);

        // Act
        var result = await _provider.GetNotificationsAsync(userId, unreadOnly: true);

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Unread");
    }

    [Fact]
    public async Task GetNotificationsAsync_Should_Exclude_Expired_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var expiredNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Expired",
            Content = "Expired",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var validNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Valid",
            Content = "Valid",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await _provider.SendAsync(expiredNotification);
        await _provider.SendAsync(validNotification);

        // Act
        var result = await _provider.GetNotificationsAsync(userId);

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Valid");
    }

    [Fact]
    public async Task GetNotificationsAsync_Should_Return_Empty_When_User_Has_No_Notifications()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _provider.GetNotificationsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteNotificationAsync_Should_Delete_User_Notification()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Test",
            Content = "Test"
        };
        await _provider.SendAsync(notification);

        // Act
        await _provider.DeleteNotificationAsync(notification.NotificationId, userId);

        // Assert
        var allNotifications = InMemoryInAppProvider.GetAllNotifications();
        allNotifications.Should().BeEmpty();

        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        userNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteNotificationAsync_Should_Not_Delete_Other_User_Notification()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var notification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId1,
            Title = "Test",
            Content = "Test"
        };
        await _provider.SendAsync(notification);

        // Act
        await _provider.DeleteNotificationAsync(notification.NotificationId, userId2);

        // Assert
        var allNotifications = InMemoryInAppProvider.GetAllNotifications();
        allNotifications.Should().ContainSingle();

        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId1);
        userNotifications.Should().ContainSingle();
    }

    [Fact]
    public async Task ClearExpiredNotificationsAsync_Should_Remove_Expired_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var expiredNotification1 = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Expired 1",
            Content = "Expired 1",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var expiredNotification2 = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Expired 2",
            Content = "Expired 2",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var validNotification = new InAppNotification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Valid",
            Content = "Valid",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await _provider.SendAsync(expiredNotification1);
        await _provider.SendAsync(expiredNotification2);
        await _provider.SendAsync(validNotification);

        // Act
        await _provider.ClearExpiredNotificationsAsync();

        // Assert
        var allNotifications = InMemoryInAppProvider.GetAllNotifications();
        allNotifications.Should().ContainSingle();
        allNotifications.First().Title.Should().Be("Valid");

        var userNotifications = InMemoryInAppProvider.GetUserNotifications(userId);
        userNotifications.Should().ContainSingle();
        userNotifications.First().Title.Should().Be("Valid");
    }

    [Fact]
    public void GetAllNotifications_Should_Return_All_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var notification1 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId1, Title = "Test 1", Content = "Body 1" };
        var notification2 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId2, Title = "Test 2", Content = "Body 2" };

        // Act
        _provider.SendAsync(notification1).Wait();
        _provider.SendAsync(notification2).Wait();
        var result = InMemoryInAppProvider.GetAllNotifications();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.NotificationId == notification1.NotificationId);
        result.Should().Contain(n => n.NotificationId == notification2.NotificationId);
    }

    [Fact]
    public void GetUserNotifications_Should_Return_Only_User_Notifications()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var notification1 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId1, Title = "Test 1", Content = "Body 1" };
        var notification2 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId2, Title = "Test 2", Content = "Body 2" };

        // Act
        _provider.SendAsync(notification1).Wait();
        _provider.SendAsync(notification2).Wait();
        var result = InMemoryInAppProvider.GetUserNotifications(userId1);

        // Assert
        result.Should().ContainSingle();
        result.First().NotificationId.Should().Be(notification1.NotificationId);
    }

    [Fact]
    public void ClearAllNotifications_Should_Remove_All_Data()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId, Title = "Test", Content = "Body" };
        _provider.SendAsync(notification).Wait();

        // Act
        InMemoryInAppProvider.ClearAllNotifications();

        // Assert
        InMemoryInAppProvider.GetAllNotifications().Should().BeEmpty();
        InMemoryInAppProvider.GetUserNotifications(userId).Should().BeEmpty();
        InMemoryInAppProvider.GetTotalNotificationsCount().Should().Be(0);
    }

    [Fact]
    public void GetTotalNotificationsCount_Should_Return_Correct_Count()
    {
        // Arrange
        InMemoryInAppProvider.ClearAllNotifications();
        var userId = Guid.NewGuid();
        var notification1 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId, Title = "Test 1", Content = "Body 1" };
        var notification2 = new InAppNotification { NotificationId = Guid.NewGuid(), UserId = userId, Title = "Test 2", Content = "Body 2" };

        // Act & Assert
        InMemoryInAppProvider.GetTotalNotificationsCount().Should().Be(0);

        _provider.SendAsync(notification1).Wait();
        InMemoryInAppProvider.GetTotalNotificationsCount().Should().Be(1);

        _provider.SendAsync(notification2).Wait();
        InMemoryInAppProvider.GetTotalNotificationsCount().Should().Be(2);
    }
}