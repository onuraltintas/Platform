using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Configuration;

namespace Enterprise.Shared.Notifications.Configuration;

public static class NotificationConfiguration
{
    public static NotificationSettings CreateDefaultSettings()
    {
        return new NotificationSettings
        {
            General = new GeneralSettings
            {
                Enabled = true,
                DefaultTimeZone = "UTC",
                Environment = "Development"
            },
            Email = new EmailSettings
            {
                SmtpServer = "localhost",
                SmtpPort = 587,
                UseSsl = true,
                Username = "",
                Password = "",
                FromEmail = "noreply@example.com",
                FromName = "System Notifications"
            },
            SMS = new SmsSettings
            {
                Provider = "InMemory",
                MaxMessageLength = 160
            },
            Push = new PushSettings
            {
                Firebase = new FirebaseSettings
                {
                    ProjectId = "test-project"
                }
            },
            Templates = new TemplateSettings
            {
                TemplateCache = true,
                CacheDurationMinutes = 60,
                DefaultLanguage = "en-US"
            },
            Delivery = new DeliverySettings
            {
                BatchSize = 100,
                MaxRetryAttempts = 3,
                RateLimitPerMinute = 1000
            }
        };
    }

    public static IConfigurationBuilder AddNotificationDefaults(this IConfigurationBuilder builder)
    {
        // Simply return the builder without adding in-memory configuration
        // In production, this would be configured through appsettings.json
        return builder;
    }

    private static Dictionary<string, string?> ConvertToConfigurationDictionary(NotificationSettings settings)
    {
        var dict = new Dictionary<string, string?>();
        var prefix = NotificationSettings.SectionName;

        // General settings
        dict[$"{prefix}:General:Enabled"] = settings.General.Enabled.ToString();
        dict[$"{prefix}:General:DefaultTimeZone"] = settings.General.DefaultTimeZone;
        dict[$"{prefix}:General:Environment"] = settings.General.Environment;

        // Email settings
        dict[$"{prefix}:Email:SmtpServer"] = settings.Email.SmtpServer;
        dict[$"{prefix}:Email:SmtpPort"] = settings.Email.SmtpPort.ToString();
        dict[$"{prefix}:Email:UseSsl"] = settings.Email.UseSsl.ToString();
        dict[$"{prefix}:Email:FromEmail"] = settings.Email.FromEmail;
        dict[$"{prefix}:Email:FromName"] = settings.Email.FromName;

        // SMS settings
        dict[$"{prefix}:SMS:Provider"] = settings.SMS.Provider;
        dict[$"{prefix}:SMS:MaxMessageLength"] = settings.SMS.MaxMessageLength.ToString();

        // Push settings
        dict[$"{prefix}:Push:Firebase:ProjectId"] = settings.Push.Firebase.ProjectId;

        // Templates settings
        dict[$"{prefix}:Templates:TemplateCache"] = settings.Templates.TemplateCache.ToString();
        dict[$"{prefix}:Templates:CacheDurationMinutes"] = settings.Templates.CacheDurationMinutes.ToString();
        dict[$"{prefix}:Templates:DefaultLanguage"] = settings.Templates.DefaultLanguage;

        // Delivery settings
        dict[$"{prefix}:Delivery:BatchSize"] = settings.Delivery.BatchSize.ToString();
        dict[$"{prefix}:Delivery:MaxRetryAttempts"] = settings.Delivery.MaxRetryAttempts.ToString();
        dict[$"{prefix}:Delivery:RateLimitPerMinute"] = settings.Delivery.RateLimitPerMinute.ToString();

        return dict;
    }
}

public static class NotificationConfigurationValidation
{
    public static void ValidateConfiguration(NotificationSettings settings)
    {
        var errors = new List<string>();

        // Validate general settings
        if (string.IsNullOrWhiteSpace(settings.General.DefaultTimeZone))
        {
            errors.Add("General.DefaultTimeZone cannot be empty");
        }

        // Validate email settings
        if (string.IsNullOrWhiteSpace(settings.Email.SmtpServer))
        {
            errors.Add("Email.SmtpServer is required");
        }

        if (settings.Email.SmtpPort <= 0 || settings.Email.SmtpPort > 65535)
        {
            errors.Add("Email.SmtpPort must be between 1 and 65535");
        }

        if (string.IsNullOrWhiteSpace(settings.Email.FromEmail))
        {
            errors.Add("Email.FromEmail is required");
        }

        // Validate SMS settings
        if (settings.SMS.MaxMessageLength <= 0)
        {
            errors.Add("SMS.MaxMessageLength must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(settings.SMS.Provider))
        {
            errors.Add("SMS.Provider is required");
        }

        // Validate delivery settings
        if (settings.Delivery.BatchSize <= 0)
        {
            errors.Add("Delivery.BatchSize must be greater than 0");
        }

        if (settings.Delivery.MaxRetryAttempts < 0)
        {
            errors.Add("Delivery.MaxRetryAttempts cannot be negative");
        }

        if (settings.Delivery.RateLimitPerMinute <= 0)
        {
            errors.Add("Delivery.RateLimitPerMinute must be greater than 0");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Notification configuration validation failed: {string.Join(", ", errors)}");
        }
    }
}