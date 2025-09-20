using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Notifications.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NotificationOptions>? configureOptions = null)
    {
        var options = new NotificationOptions();
        configureOptions?.Invoke(options);

        // Configure settings from configuration section
        var configSection = configuration.GetSection(NotificationSettings.SectionName);
        services.Configure<NotificationSettings>(configSection.Bind);

        // Register core services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ITemplateService, TemplateService>();

        // Register providers based on options
        RegisterProviders(services, options);

        // Add background services if enabled
        if (options.EnableBackgroundServices)
        {
            services.AddHostedService<NotificationBackgroundService>();
        }

        // Logging is already available from DI container

        return services;
    }

    public static IServiceCollection AddInMemoryNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddNotifications(configuration, options =>
        {
            options.UseInMemoryProviders = true;
            options.EnableBackgroundServices = false;
        });
    }

    public static IServiceCollection AddProductionNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddNotifications(configuration, options =>
        {
            options.UseInMemoryProviders = false;
            options.EnableBackgroundServices = true;
        });
    }

    private static void RegisterProviders(IServiceCollection services, NotificationOptions options)
    {
        if (options.UseInMemoryProviders)
        {
            // Register in-memory providers for testing/development
            services.AddSingleton<IEmailNotificationProvider, InMemoryEmailProvider>();
            services.AddSingleton<ISmsNotificationProvider, InMemorySmsProvider>();
            services.AddSingleton<IPushNotificationProvider, InMemoryPushProvider>();
            services.AddSingleton<IInAppNotificationProvider, InMemoryInAppProvider>();
            services.AddSingleton<IWebhookNotificationProvider, InMemoryWebhookProvider>();
            services.AddSingleton<INotificationPreferencesService, InMemoryNotificationPreferencesService>();
        }
        else
        {
            // Register production providers
            // These would be implemented separately for production use
            
            // Email providers (could be SMTP, SendGrid, etc.)
            // services.AddSingleton<IEmailNotificationProvider, SmtpEmailProvider>();
            // services.AddSingleton<IEmailNotificationProvider, SendGridEmailProvider>();
            
            // SMS providers (could be Twilio, AWS SNS, etc.)
            // services.AddSingleton<ISmsNotificationProvider, TwilioSmsProvider>();
            
            // Push providers (could be FCM, APNS, etc.)
            // services.AddSingleton<IPushNotificationProvider, FirebaseCloudMessagingProvider>();
            
            // For now, use in-memory providers as fallback
            services.AddSingleton<IEmailNotificationProvider, InMemoryEmailProvider>();
            services.AddSingleton<ISmsNotificationProvider, InMemorySmsProvider>();
            services.AddSingleton<IPushNotificationProvider, InMemoryPushProvider>();
            services.AddSingleton<IInAppNotificationProvider, InMemoryInAppProvider>();
            services.AddSingleton<IWebhookNotificationProvider, InMemoryWebhookProvider>();
            services.AddSingleton<INotificationPreferencesService, InMemoryNotificationPreferencesService>();
        }
    }
}

public class NotificationOptions
{
    public bool UseInMemoryProviders { get; set; } = true;
    public bool EnableBackgroundServices { get; set; } = false;
    public bool EnableSignalR { get; set; } = false;
    public bool EnableMetrics { get; set; } = false;
}

internal class NotificationBackgroundService : BackgroundService
{
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NotificationBackgroundService(
        ILogger<NotificationBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification background service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessScheduledNotifications(stoppingToken);
                await CleanupExpiredNotifications(stoppingToken);
                
                // Wait before next iteration
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Notification background service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification background service");
        }
    }

    private async Task ProcessScheduledNotifications(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        // This would process scheduled notifications from a queue or database
        // For now, it's just a placeholder
        
        _logger.LogDebug("Processing scheduled notifications");
    }

    private async Task CleanupExpiredNotifications(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var inAppProvider = scope.ServiceProvider.GetService<IInAppNotificationProvider>();
        
        if (inAppProvider is InMemoryInAppProvider memoryProvider)
        {
            await memoryProvider.ClearExpiredNotificationsAsync(cancellationToken);
        }
        
        _logger.LogDebug("Cleaned up expired notifications");
    }
}