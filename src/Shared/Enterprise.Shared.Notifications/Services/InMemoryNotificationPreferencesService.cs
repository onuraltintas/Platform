using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Notifications.Services;

public class InMemoryNotificationPreferencesService : INotificationPreferencesService
{
    private readonly ILogger<InMemoryNotificationPreferencesService> _logger;
    private readonly NotificationSettings _settings;
    private readonly ConcurrentDictionary<Guid, UserNotificationPreferences> _preferences = new();

    public InMemoryNotificationPreferencesService(
        ILogger<InMemoryNotificationPreferencesService> logger,
        IOptions<NotificationSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task<UserNotificationPreferences> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting preferences for user {UserId}", userId);

        var preferences = _preferences.GetOrAdd(userId, _ => CreateDefaultPreferences(userId));
        return Task.FromResult(preferences);
    }

    public Task UpdateUserPreferencesAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        _logger.LogInformation("Updating preferences for user {UserId}", preferences.UserId);

        preferences.UpdatedAt = DateTime.UtcNow;
        _preferences.AddOrUpdate(preferences.UserId, preferences, (_, _) => preferences);

        return Task.CompletedTask;
    }

    public Task ResetToDefaultAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting preferences to default for user {UserId}", userId);

        var defaultPreferences = CreateDefaultPreferences(userId);
        _preferences.AddOrUpdate(userId, defaultPreferences, (_, _) => defaultPreferences);

        return Task.CompletedTask;
    }

    public async Task<bool> IsOptedOutAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        // Check global channel preference
        var channelEnabled = channel switch
        {
            NotificationChannel.Email => preferences.EmailEnabled,
            NotificationChannel.SMS => preferences.SmsEnabled,
            NotificationChannel.Push => preferences.PushEnabled,
            NotificationChannel.InApp => preferences.InAppEnabled,
            NotificationChannel.Webhook => preferences.WebhookEnabled,
            _ => false
        };

        if (!channelEnabled)
            return true;

        // Check type-specific preference
        if (preferences.TypePreferences.TryGetValue(notificationType, out var typePreference))
        {
            var typeChannelEnabled = channel switch
            {
                NotificationChannel.Email => typePreference.EmailEnabled,
                NotificationChannel.SMS => typePreference.SmsEnabled,
                NotificationChannel.Push => typePreference.PushEnabled,
                NotificationChannel.InApp => typePreference.InAppEnabled,
                NotificationChannel.Webhook => typePreference.WebhookEnabled,
                _ => false
            };

            return !typeChannelEnabled;
        }

        return false;
    }

    public async Task<bool> IsInQuietHoursAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        if (!preferences.QuietHoursStart.HasValue || !preferences.QuietHoursEnd.HasValue)
            return false;

        var now = DateTime.Now.TimeOfDay;
        var start = preferences.QuietHoursStart.Value;
        var end = preferences.QuietHoursEnd.Value;

        // Handle quiet hours that span midnight
        if (start <= end)
        {
            return now >= start && now <= end;
        }
        else
        {
            return now >= start || now <= end;
        }
    }

    public async Task<string> GetUserLanguageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
        return preferences.Language;
    }

    public async Task<string> GetUserTimezoneAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
        return preferences.TimeZone;
    }

    public async Task<IEnumerable<NotificationChannel>> GetEnabledChannelsAsync(Guid userId, NotificationType notificationType, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
        var enabledChannels = new List<NotificationChannel>();

        var allChannels = Enum.GetValues<NotificationChannel>();

        foreach (var channel in allChannels)
        {
            var isOptedOut = await IsOptedOutAsync(userId, notificationType, channel, cancellationToken);
            if (!isOptedOut)
            {
                enabledChannels.Add(channel);
            }
        }

        return enabledChannels;
    }

    public async Task OptOutAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opting out user {UserId} from {NotificationType} via {Channel}", 
            userId, notificationType, channel);

        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        if (!preferences.TypePreferences.ContainsKey(notificationType))
        {
            preferences.TypePreferences[notificationType] = CreateDefaultTypePreferences();
        }

        var typePreference = preferences.TypePreferences[notificationType];
        switch (channel)
        {
            case NotificationChannel.Email:
                typePreference.EmailEnabled = false;
                break;
            case NotificationChannel.SMS:
                typePreference.SmsEnabled = false;
                break;
            case NotificationChannel.Push:
                typePreference.PushEnabled = false;
                break;
            case NotificationChannel.InApp:
                typePreference.InAppEnabled = false;
                break;
            case NotificationChannel.Webhook:
                typePreference.WebhookEnabled = false;
                break;
        }

        await UpdateUserPreferencesAsync(preferences, cancellationToken);
    }

    public async Task OptInAsync(Guid userId, NotificationType notificationType, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opting in user {UserId} to {NotificationType} via {Channel}", 
            userId, notificationType, channel);

        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        if (!preferences.TypePreferences.ContainsKey(notificationType))
        {
            preferences.TypePreferences[notificationType] = CreateDefaultTypePreferences();
        }

        // Enable global channel if not already enabled
        switch (channel)
        {
            case NotificationChannel.Email:
                if (!preferences.EmailEnabled) preferences.EmailEnabled = true;
                break;
            case NotificationChannel.SMS:
                if (!preferences.SmsEnabled) preferences.SmsEnabled = true;
                break;
            case NotificationChannel.Push:
                if (!preferences.PushEnabled) preferences.PushEnabled = true;
                break;
            case NotificationChannel.InApp:
                if (!preferences.InAppEnabled) preferences.InAppEnabled = true;
                break;
            case NotificationChannel.Webhook:
                if (!preferences.WebhookEnabled) preferences.WebhookEnabled = true;
                break;
        }

        var typePreference = preferences.TypePreferences[notificationType];
        switch (channel)
        {
            case NotificationChannel.Email:
                typePreference.EmailEnabled = true;
                break;
            case NotificationChannel.SMS:
                typePreference.SmsEnabled = true;
                break;
            case NotificationChannel.Push:
                typePreference.PushEnabled = true;
                break;
            case NotificationChannel.InApp:
                typePreference.InAppEnabled = true;
                break;
            case NotificationChannel.Webhook:
                typePreference.WebhookEnabled = true;
                break;
        }

        await UpdateUserPreferencesAsync(preferences, cancellationToken);
    }

    public async Task SetDoNotDisturbAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting DND mode for user {UserId} to {Enabled}", userId, enabled);

        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
        preferences.DoNotDisturb = enabled;
        await UpdateUserPreferencesAsync(preferences, cancellationToken);
    }

    public async Task SetQuietHoursAsync(Guid userId, TimeSpan? startTime, TimeSpan? endTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting quiet hours for user {UserId}: {StartTime} - {EndTime}", 
            userId, startTime, endTime);

        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
        preferences.QuietHoursStart = startTime;
        preferences.QuietHoursEnd = endTime;
        await UpdateUserPreferencesAsync(preferences, cancellationToken);
    }

    public async Task ImportPreferencesAsync(Guid userId, Dictionary<string, object> source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        _logger.LogInformation("Importing preferences for user {UserId}", userId);

        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        // Import basic preferences
        if (source.TryGetValue("language", out var language) && language is string lang)
        {
            preferences.Language = lang;
        }

        if (source.TryGetValue("timezone", out var timezone) && timezone is string tz)
        {
            preferences.TimeZone = tz;
        }

        if (source.TryGetValue("doNotDisturb", out var dnd) && dnd is bool dndValue)
        {
            preferences.DoNotDisturb = dndValue;
        }

        // Import channel preferences
        if (source.TryGetValue("emailEnabled", out var email) && email is bool emailEnabled)
        {
            preferences.EmailEnabled = emailEnabled;
        }

        if (source.TryGetValue("smsEnabled", out var sms) && sms is bool smsEnabled)
        {
            preferences.SmsEnabled = smsEnabled;
        }

        if (source.TryGetValue("pushEnabled", out var push) && push is bool pushEnabled)
        {
            preferences.PushEnabled = pushEnabled;
        }

        if (source.TryGetValue("inAppEnabled", out var inApp) && inApp is bool inAppEnabled)
        {
            preferences.InAppEnabled = inAppEnabled;
        }

        await UpdateUserPreferencesAsync(preferences, cancellationToken);
    }

    public async Task<Dictionary<string, object>> ExportPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await GetUserPreferencesAsync(userId, cancellationToken);

        var exported = new Dictionary<string, object>
        {
            ["userId"] = preferences.UserId,
            ["language"] = preferences.Language,
            ["timezone"] = preferences.TimeZone,
            ["doNotDisturb"] = preferences.DoNotDisturb,
            ["emailEnabled"] = preferences.EmailEnabled,
            ["smsEnabled"] = preferences.SmsEnabled,
            ["pushEnabled"] = preferences.PushEnabled,
            ["inAppEnabled"] = preferences.InAppEnabled,
            ["webhookEnabled"] = preferences.WebhookEnabled,
            ["quietHoursStart"] = preferences.QuietHoursStart?.ToString() ?? string.Empty,
            ["quietHoursEnd"] = preferences.QuietHoursEnd?.ToString() ?? string.Empty,
            ["typePreferences"] = preferences.TypePreferences.ToDictionary(
                kv => kv.Key.ToString(),
                kv => new Dictionary<string, object>
                {
                    ["emailEnabled"] = kv.Value.EmailEnabled,
                    ["smsEnabled"] = kv.Value.SmsEnabled,
                    ["pushEnabled"] = kv.Value.PushEnabled,
                    ["inAppEnabled"] = kv.Value.InAppEnabled,
                    ["webhookEnabled"] = kv.Value.WebhookEnabled
                })
        };

        return exported;
    }

    public Task<PreferenceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allPreferences = _preferences.Values.ToList();

        var statistics = new PreferenceStatistics
        {
            TotalUsers = allPreferences.Count,
            ByLanguage = allPreferences.GroupBy(p => p.Language).ToDictionary(g => g.Key, g => g.Count()),
            ByTimezone = allPreferences.GroupBy(p => p.TimeZone).ToDictionary(g => g.Key, g => g.Count()),
            OptOutRates = CalculateChannelOptOutRates(allPreferences),
            TypeOptOutRates = CalculateTypeOptOutRates(allPreferences),
            DoNotDisturbUsers = allPreferences.Count(p => p.DoNotDisturb),
            QuietHoursUsers = allPreferences.Count(p => p.QuietHoursStart.HasValue && p.QuietHoursEnd.HasValue),
            GeneratedAt = DateTime.UtcNow
        };

        return Task.FromResult(statistics);
    }

    public Task BulkUpdatePreferencesAsync(IEnumerable<UserNotificationPreferences> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        var updatesList = updates.ToList();
        _logger.LogInformation("Bulk updating preferences for {Count} users", updatesList.Count);

        foreach (var preferences in updatesList)
        {
            preferences.UpdatedAt = DateTime.UtcNow;
            _preferences.AddOrUpdate(preferences.UserId, preferences, (_, _) => preferences);
        }

        return Task.CompletedTask;
    }

    private UserNotificationPreferences CreateDefaultPreferences(Guid userId)
    {
        return new UserNotificationPreferences
        {
            UserId = userId,
            Language = "en-US",
            TimeZone = "UTC",
            EmailEnabled = true,
            SmsEnabled = true,
            PushEnabled = true,
            InAppEnabled = true,
            WebhookEnabled = false,
            DoNotDisturb = false,
            QuietHoursStart = null,
            QuietHoursEnd = null,
            TypePreferences = new Dictionary<NotificationType, NotificationTypePreference>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private NotificationTypePreference CreateDefaultTypePreferences()
    {
        return new NotificationTypePreference
        {
            EmailEnabled = true,
            SmsEnabled = true,
            PushEnabled = true,
            InAppEnabled = true,
            WebhookEnabled = false
        };
    }

    private Dictionary<NotificationChannel, double> CalculateChannelOptOutRates(List<UserNotificationPreferences> preferences)
    {
        if (!preferences.Any())
            return new Dictionary<NotificationChannel, double>();

        var total = preferences.Count;
        return new Dictionary<NotificationChannel, double>
        {
            [NotificationChannel.Email] = preferences.Count(p => !p.EmailEnabled) / (double)total * 100,
            [NotificationChannel.SMS] = preferences.Count(p => !p.SmsEnabled) / (double)total * 100,
            [NotificationChannel.Push] = preferences.Count(p => !p.PushEnabled) / (double)total * 100,
            [NotificationChannel.InApp] = preferences.Count(p => !p.InAppEnabled) / (double)total * 100,
            [NotificationChannel.Webhook] = preferences.Count(p => !p.WebhookEnabled) / (double)total * 100
        };
    }

    private Dictionary<NotificationType, double> CalculateTypeOptOutRates(List<UserNotificationPreferences> preferences)
    {
        if (!preferences.Any())
            return new Dictionary<NotificationType, double>();

        var typeOptOutRates = new Dictionary<NotificationType, double>();
        var allTypes = Enum.GetValues<NotificationType>();

        foreach (var type in allTypes)
        {
            var usersWithTypePrefs = preferences.Where(p => p.TypePreferences.ContainsKey(type)).ToList();
            if (usersWithTypePrefs.Any())
            {
                var totalOptedOut = usersWithTypePrefs.Count(p => 
                    !p.TypePreferences[type].EmailEnabled &&
                    !p.TypePreferences[type].SmsEnabled &&
                    !p.TypePreferences[type].PushEnabled &&
                    !p.TypePreferences[type].InAppEnabled);

                typeOptOutRates[type] = totalOptedOut / (double)usersWithTypePrefs.Count * 100;
            }
            else
            {
                typeOptOutRates[type] = 0;
            }
        }

        return typeOptOutRates;
    }

    public static IEnumerable<UserNotificationPreferences> GetAllPreferences()
    {
        // This would be used for testing
        return new List<UserNotificationPreferences>();
    }

    public static void ClearAllPreferences()
    {
        // This would be used for testing cleanup
    }
}