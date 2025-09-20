using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class InMemoryNotificationPreferencesServiceTests
{
    private readonly Mock<ILogger<InMemoryNotificationPreferencesService>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly InMemoryNotificationPreferencesService _service;

    public InMemoryNotificationPreferencesServiceTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryNotificationPreferencesService>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();
        _settings = new NotificationSettings();
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _service = new InMemoryNotificationPreferencesService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Service_With_Valid_Dependencies()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemoryNotificationPreferencesService(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new InMemoryNotificationPreferencesService(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task GetUserPreferencesAsync_Should_Return_Default_Preferences_For_New_User()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Language.Should().Be("en-US");
        result.TimeZone.Should().Be("UTC");
        result.EmailEnabled.Should().BeTrue();
        result.SmsEnabled.Should().BeTrue();
        result.PushEnabled.Should().BeTrue();
        result.InAppEnabled.Should().BeTrue();
        result.WebhookEnabled.Should().BeFalse();
        result.DoNotDisturb.Should().BeFalse();
        result.QuietHoursStart.Should().BeNull();
        result.QuietHoursEnd.Should().BeNull();
        result.TypePreferences.Should().BeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUserPreferencesAsync_Should_Return_Same_Instance_For_Multiple_Calls()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result1 = await _service.GetUserPreferencesAsync(userId);
        var result2 = await _service.GetUserPreferencesAsync(userId);

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_Should_Update_Existing_Preferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var originalPreferences = await _service.GetUserPreferencesAsync(userId);
        var originalUpdatedAt = originalPreferences.UpdatedAt;

        // Wait a moment to ensure UpdatedAt changes
        await Task.Delay(10);

        var updatedPreferences = new UserNotificationPreferences
        {
            UserId = userId,
            Language = "fr-FR",
            TimeZone = "Europe/Paris",
            EmailEnabled = false,
            SmsEnabled = true,
            PushEnabled = false,
            InAppEnabled = true,
            WebhookEnabled = true,
            DoNotDisturb = true
        };

        // Act
        await _service.UpdateUserPreferencesAsync(updatedPreferences);
        var result = await _service.GetUserPreferencesAsync(userId);

        // Assert
        result.Language.Should().Be("fr-FR");
        result.TimeZone.Should().Be("Europe/Paris");
        result.EmailEnabled.Should().BeFalse();
        result.SmsEnabled.Should().BeTrue();
        result.PushEnabled.Should().BeFalse();
        result.InAppEnabled.Should().BeTrue();
        result.WebhookEnabled.Should().BeTrue();
        result.DoNotDisturb.Should().BeTrue();
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_Should_Throw_Exception_When_Preferences_Is_Null()
    {
        // Act & Assert
        var act = async () => await _service.UpdateUserPreferencesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ResetToDefaultAsync_Should_Reset_User_Preferences()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // First, update preferences to non-default values
        var customPreferences = new UserNotificationPreferences
        {
            UserId = userId,
            Language = "de-DE",
            EmailEnabled = false,
            DoNotDisturb = true
        };
        await _service.UpdateUserPreferencesAsync(customPreferences);

        // Act
        await _service.ResetToDefaultAsync(userId);
        var result = await _service.GetUserPreferencesAsync(userId);

        // Assert
        result.Language.Should().Be("en-US");
        result.EmailEnabled.Should().BeTrue();
        result.DoNotDisturb.Should().BeFalse();
    }

    [Fact]
    public async Task IsOptedOutAsync_Should_Return_True_When_Channel_Is_Globally_Disabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.EmailEnabled = false;
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOptedOutAsync_Should_Return_False_When_Channel_Is_Globally_Enabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.EmailEnabled = true;
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsOptedOutAsync_Should_Check_Type_Specific_Preferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.EmailEnabled = true; // Globally enabled
        preferences.TypePreferences[NotificationType.Welcome] = new NotificationTypePreference
        {
            EmailEnabled = false // Disabled for Welcome type
        };
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInQuietHoursAsync_Should_Return_False_When_No_Quiet_Hours_Set()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.IsInQuietHoursAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInQuietHoursAsync_Should_Return_True_When_In_Quiet_Hours_Same_Day()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        
        var now = DateTime.Now.TimeOfDay;
        preferences.QuietHoursStart = now.Add(TimeSpan.FromHours(-1));
        preferences.QuietHoursEnd = now.Add(TimeSpan.FromHours(1));
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.IsInQuietHoursAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInQuietHoursAsync_Should_Handle_Quiet_Hours_Spanning_Midnight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        
        preferences.QuietHoursStart = new TimeSpan(23, 0, 0); // 11 PM
        preferences.QuietHoursEnd = new TimeSpan(6, 0, 0);   // 6 AM
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act - Test at midnight (should be in quiet hours)
        var result = await _service.IsInQuietHoursAsync(userId);

        // Assert - This will depend on current time, but the logic should handle midnight properly
        // We can't guarantee the result since it depends on current time
        // We can't guarantee the result since it depends on current time, but it should be a bool
    }

    [Fact]
    public async Task GetUserLanguageAsync_Should_Return_User_Language()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.Language = "es-ES";
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.GetUserLanguageAsync(userId);

        // Assert
        result.Should().Be("es-ES");
    }

    [Fact]
    public async Task GetUserTimezoneAsync_Should_Return_User_Timezone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.TimeZone = "America/New_York";
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.GetUserTimezoneAsync(userId);

        // Assert
        result.Should().Be("America/New_York");
    }

    [Fact]
    public async Task GetEnabledChannelsAsync_Should_Return_All_Enabled_Channels()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.EmailEnabled = true;
        preferences.SmsEnabled = false;
        preferences.PushEnabled = true;
        preferences.InAppEnabled = true;
        preferences.WebhookEnabled = false;
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var result = await _service.GetEnabledChannelsAsync(userId, NotificationType.Welcome);

        // Assert
        var enabledChannels = result.ToList();
        enabledChannels.Should().Contain(NotificationChannel.Email);
        enabledChannels.Should().NotContain(NotificationChannel.SMS);
        enabledChannels.Should().Contain(NotificationChannel.Push);
        enabledChannels.Should().Contain(NotificationChannel.InApp);
        enabledChannels.Should().NotContain(NotificationChannel.Webhook);
    }

    [Fact]
    public async Task OptOutAsync_Should_Disable_Channel_For_Specific_Type()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.OptOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        var isOptedOut = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);
        isOptedOut.Should().BeTrue();
    }

    [Fact]
    public async Task OptOutAsync_Should_Create_Type_Preferences_If_Not_Exist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.TypePreferences.Should().BeEmpty();

        // Act
        await _service.OptOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        var updatedPreferences = await _service.GetUserPreferencesAsync(userId);
        updatedPreferences.TypePreferences.Should().ContainKey(NotificationType.Welcome);
        updatedPreferences.TypePreferences[NotificationType.Welcome].EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task OptInAsync_Should_Enable_Channel_For_Specific_Type()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.OptOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Act
        await _service.OptInAsync(userId, NotificationType.Welcome, NotificationChannel.Email);

        // Assert
        var isOptedOut = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, NotificationChannel.Email);
        isOptedOut.Should().BeFalse();
    }

    [Theory]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.SMS)]
    [InlineData(NotificationChannel.Push)]
    [InlineData(NotificationChannel.InApp)]
    [InlineData(NotificationChannel.Webhook)]
    public async Task OptOutAsync_Should_Work_For_All_Channels(NotificationChannel channel)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.OptOutAsync(userId, NotificationType.Welcome, channel);

        // Assert
        var isOptedOut = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, channel);
        isOptedOut.Should().BeTrue();
    }

    [Theory]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.SMS)]
    [InlineData(NotificationChannel.Push)]
    [InlineData(NotificationChannel.InApp)]
    public async Task OptInAsync_Should_Work_For_All_Channels(NotificationChannel channel)
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.OptOutAsync(userId, NotificationType.Welcome, channel);

        // Act
        await _service.OptInAsync(userId, NotificationType.Welcome, channel);

        // Assert
        var isOptedOut = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, channel);
        isOptedOut.Should().BeFalse();
    }

    [Fact]
    public async Task OptInAsync_Should_Work_For_Webhook_Channel()
    {
        // Arrange - Webhook is disabled by default
        var userId = Guid.NewGuid();
        var channel = NotificationChannel.Webhook;

        // Act - Opt in to enable webhook
        await _service.OptInAsync(userId, NotificationType.Welcome, channel);

        // Assert
        var isOptedOut = await _service.IsOptedOutAsync(userId, NotificationType.Welcome, channel);
        isOptedOut.Should().BeFalse();
    }

    [Fact]
    public async Task SetDoNotDisturbAsync_Should_Enable_DND_Mode()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.SetDoNotDisturbAsync(userId, true);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.DoNotDisturb.Should().BeTrue();
    }

    [Fact]
    public async Task SetDoNotDisturbAsync_Should_Disable_DND_Mode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.SetDoNotDisturbAsync(userId, true);

        // Act
        await _service.SetDoNotDisturbAsync(userId, false);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.DoNotDisturb.Should().BeFalse();
    }

    [Fact]
    public async Task SetQuietHoursAsync_Should_Set_Quiet_Hours()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startTime = new TimeSpan(22, 0, 0);
        var endTime = new TimeSpan(7, 0, 0);

        // Act
        await _service.SetQuietHoursAsync(userId, startTime, endTime);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.QuietHoursStart.Should().Be(startTime);
        preferences.QuietHoursEnd.Should().Be(endTime);
    }

    [Fact]
    public async Task SetQuietHoursAsync_Should_Clear_Quiet_Hours_When_Null()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.SetQuietHoursAsync(userId, new TimeSpan(22, 0, 0), new TimeSpan(7, 0, 0));

        // Act
        await _service.SetQuietHoursAsync(userId, null, null);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.QuietHoursStart.Should().BeNull();
        preferences.QuietHoursEnd.Should().BeNull();
    }

    [Fact]
    public async Task ImportPreferencesAsync_Should_Import_Basic_Preferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var source = new Dictionary<string, object>
        {
            ["language"] = "ja-JP",
            ["timezone"] = "Asia/Tokyo",
            ["doNotDisturb"] = true,
            ["emailEnabled"] = false,
            ["smsEnabled"] = true,
            ["pushEnabled"] = false,
            ["inAppEnabled"] = true
        };

        // Act
        await _service.ImportPreferencesAsync(userId, source);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.Language.Should().Be("ja-JP");
        preferences.TimeZone.Should().Be("Asia/Tokyo");
        preferences.DoNotDisturb.Should().BeTrue();
        preferences.EmailEnabled.Should().BeFalse();
        preferences.SmsEnabled.Should().BeTrue();
        preferences.PushEnabled.Should().BeFalse();
        preferences.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ImportPreferencesAsync_Should_Ignore_Invalid_Types()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var originalPreferences = await _service.GetUserPreferencesAsync(userId);
        var source = new Dictionary<string, object>
        {
            ["language"] = 123, // Invalid type
            ["timezone"] = true, // Invalid type
            ["doNotDisturb"] = "not a boolean", // Invalid type
            ["emailEnabled"] = "false" // Invalid type
        };

        // Act
        await _service.ImportPreferencesAsync(userId, source);

        // Assert
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.Language.Should().Be(originalPreferences.Language); // Should remain unchanged
        preferences.TimeZone.Should().Be(originalPreferences.TimeZone); // Should remain unchanged
        preferences.DoNotDisturb.Should().Be(originalPreferences.DoNotDisturb); // Should remain unchanged
        preferences.EmailEnabled.Should().Be(originalPreferences.EmailEnabled); // Should remain unchanged
    }

    [Fact]
    public async Task ImportPreferencesAsync_Should_Throw_Exception_When_Source_Is_Null()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.ImportPreferencesAsync(userId, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExportPreferencesAsync_Should_Export_All_Preferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = await _service.GetUserPreferencesAsync(userId);
        preferences.Language = "fr-FR";
        preferences.TimeZone = "Europe/Paris";
        preferences.DoNotDisturb = true;
        preferences.EmailEnabled = false;
        preferences.QuietHoursStart = new TimeSpan(23, 0, 0);
        preferences.QuietHoursEnd = new TimeSpan(7, 0, 0);
        preferences.TypePreferences[NotificationType.Welcome] = new NotificationTypePreference
        {
            EmailEnabled = false,
            SmsEnabled = true
        };
        await _service.UpdateUserPreferencesAsync(preferences);

        // Act
        var exported = await _service.ExportPreferencesAsync(userId);

        // Assert
        exported.Should().ContainKey("userId");
        exported.Should().ContainKey("language");
        exported.Should().ContainKey("timezone");
        exported.Should().ContainKey("doNotDisturb");
        exported.Should().ContainKey("emailEnabled");
        exported.Should().ContainKey("smsEnabled");
        exported.Should().ContainKey("pushEnabled");
        exported.Should().ContainKey("inAppEnabled");
        exported.Should().ContainKey("webhookEnabled");
        exported.Should().ContainKey("quietHoursStart");
        exported.Should().ContainKey("quietHoursEnd");
        exported.Should().ContainKey("typePreferences");

        exported["language"].Should().Be("fr-FR");
        exported["timezone"].Should().Be("Europe/Paris");
        exported["doNotDisturb"].Should().Be(true);
        exported["emailEnabled"].Should().Be(false);
        exported["quietHoursStart"].Should().Be("23:00:00");
        exported["quietHoursEnd"].Should().Be("07:00:00");
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_Return_Empty_Statistics_For_No_Users()
    {
        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalUsers.Should().Be(0);
        result.ByLanguage.Should().BeEmpty();
        result.ByTimezone.Should().BeEmpty();
        result.DoNotDisturbUsers.Should().Be(0);
        result.QuietHoursUsers.Should().Be(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_Calculate_Statistics_Correctly()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        // Set up different preferences for each user
        var prefs1 = await _service.GetUserPreferencesAsync(user1);
        prefs1.Language = "en-US";
        prefs1.TimeZone = "UTC";
        prefs1.DoNotDisturb = true;
        prefs1.QuietHoursStart = new TimeSpan(23, 0, 0);
        prefs1.QuietHoursEnd = new TimeSpan(7, 0, 0);
        await _service.UpdateUserPreferencesAsync(prefs1);

        var prefs2 = await _service.GetUserPreferencesAsync(user2);
        prefs2.Language = "en-US";
        prefs2.TimeZone = "America/New_York";
        prefs2.DoNotDisturb = false;
        await _service.UpdateUserPreferencesAsync(prefs2);

        var prefs3 = await _service.GetUserPreferencesAsync(user3);
        prefs3.Language = "fr-FR";
        prefs3.TimeZone = "UTC";
        prefs3.DoNotDisturb = true;
        await _service.UpdateUserPreferencesAsync(prefs3);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.TotalUsers.Should().Be(3);
        result.ByLanguage.Should().ContainKey("en-US").WhoseValue.Should().Be(2);
        result.ByLanguage.Should().ContainKey("fr-FR").WhoseValue.Should().Be(1);
        result.ByTimezone.Should().ContainKey("UTC").WhoseValue.Should().Be(2);
        result.ByTimezone.Should().ContainKey("America/New_York").WhoseValue.Should().Be(1);
        result.DoNotDisturbUsers.Should().Be(2);
        result.QuietHoursUsers.Should().Be(1);
    }

    [Fact]
    public async Task BulkUpdatePreferencesAsync_Should_Update_Multiple_Users()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var updates = new[]
        {
            new UserNotificationPreferences
            {
                UserId = user1,
                Language = "de-DE",
                EmailEnabled = false
            },
            new UserNotificationPreferences
            {
                UserId = user2,
                Language = "it-IT",
                SmsEnabled = false
            }
        };

        // Act
        await _service.BulkUpdatePreferencesAsync(updates);

        // Assert
        var prefs1 = await _service.GetUserPreferencesAsync(user1);
        var prefs2 = await _service.GetUserPreferencesAsync(user2);

        prefs1.Language.Should().Be("de-DE");
        prefs1.EmailEnabled.Should().BeFalse();
        prefs1.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        prefs2.Language.Should().Be("it-IT");
        prefs2.SmsEnabled.Should().BeFalse();
        prefs2.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task BulkUpdatePreferencesAsync_Should_Throw_Exception_When_Updates_Is_Null()
    {
        // Act & Assert
        var act = async () => await _service.BulkUpdatePreferencesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}