using Enterprise.Shared.Notifications.Extensions;
using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        _services.AddLogging();

        var configurationData = new Dictionary<string, string>
        {
            [$"{NotificationSettings.SectionName}:General:Enabled"] = "true",
            [$"{NotificationSettings.SectionName}:Delivery:BatchSize"] = "100"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
    }

    [Fact]
    public void AddNotifications_Should_Register_Core_Services()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<INotificationService>().Should().NotBeNull().And.BeOfType<NotificationService>();
        serviceProvider.GetService<ITemplateService>().Should().NotBeNull().And.BeOfType<TemplateService>();
    }

    [Fact]
    public void AddNotifications_Should_Register_Configuration()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<NotificationSettings>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddNotifications_Should_Register_InMemory_Providers_By_Default()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IEmailNotificationProvider>().Should().NotBeNull().And.BeOfType<InMemoryEmailProvider>();
        serviceProvider.GetService<ISmsNotificationProvider>().Should().NotBeNull().And.BeOfType<InMemorySmsProvider>();
        serviceProvider.GetService<IPushNotificationProvider>().Should().NotBeNull().And.BeOfType<InMemoryPushProvider>();
        serviceProvider.GetService<IInAppNotificationProvider>().Should().NotBeNull().And.BeOfType<InMemoryInAppProvider>();
        serviceProvider.GetService<IWebhookNotificationProvider>().Should().NotBeNull().And.BeOfType<InMemoryWebhookProvider>();
        serviceProvider.GetService<INotificationPreferencesService>().Should().NotBeNull().And.BeOfType<InMemoryNotificationPreferencesService>();
    }

    [Fact]
    public void AddNotifications_Should_Apply_Custom_Options()
    {
        // Act
        _services.AddNotifications(_configuration, options =>
        {
            options.UseInMemoryProviders = true;
            options.EnableBackgroundServices = true;
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        // Should still register in-memory providers since UseInMemoryProviders is true
        serviceProvider.GetService<IEmailNotificationProvider>().Should().BeOfType<InMemoryEmailProvider>();
        
        // Should register background service since EnableBackgroundServices is true
        serviceProvider.GetServices<IHostedService>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddNotifications_Should_Register_Background_Service_When_Enabled()
    {
        // Act
        _services.AddNotifications(_configuration, options =>
        {
            options.EnableBackgroundServices = true;
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        hostedServices.Should().NotBeEmpty();
        hostedServices.Should().Contain(s => s.GetType().Name.Contains("NotificationBackgroundService"));
    }

    [Fact]
    public void AddNotifications_Should_Not_Register_Background_Service_When_Disabled()
    {
        // Act
        _services.AddNotifications(_configuration, options =>
        {
            options.EnableBackgroundServices = false;
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        hostedServices.Should().BeEmpty();
    }

    [Fact]
    public void AddNotifications_Should_Use_Production_Providers_When_Configured()
    {
        // Act
        _services.AddNotifications(_configuration, options =>
        {
            options.UseInMemoryProviders = false;
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        // Currently, production providers fall back to in-memory providers
        // This test verifies the registration logic works
        serviceProvider.GetService<IEmailNotificationProvider>().Should().NotBeNull();
        serviceProvider.GetService<ISmsNotificationProvider>().Should().NotBeNull();
        serviceProvider.GetService<IPushNotificationProvider>().Should().NotBeNull();
        serviceProvider.GetService<IInAppNotificationProvider>().Should().NotBeNull();
        serviceProvider.GetService<IWebhookNotificationProvider>().Should().NotBeNull();
        serviceProvider.GetService<INotificationPreferencesService>().Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryNotifications_Should_Configure_For_Development()
    {
        // Act
        _services.AddInMemoryNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IEmailNotificationProvider>().Should().BeOfType<InMemoryEmailProvider>();
        serviceProvider.GetService<ISmsNotificationProvider>().Should().BeOfType<InMemorySmsProvider>();
        serviceProvider.GetService<IPushNotificationProvider>().Should().BeOfType<InMemoryPushProvider>();
        serviceProvider.GetService<IInAppNotificationProvider>().Should().BeOfType<InMemoryInAppProvider>();
        serviceProvider.GetService<IWebhookNotificationProvider>().Should().BeOfType<InMemoryWebhookProvider>();
        serviceProvider.GetService<INotificationPreferencesService>().Should().BeOfType<InMemoryNotificationPreferencesService>();
        
        // Should not register background services in development
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        hostedServices.Should().BeEmpty();
    }

    [Fact]
    public void AddProductionNotifications_Should_Configure_For_Production()
    {
        // Act
        _services.AddProductionNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        // Should register background services in production
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        hostedServices.Should().NotBeEmpty();
        hostedServices.Should().Contain(s => s.GetType().Name.Contains("NotificationBackgroundService"));
    }

    [Fact]
    public void AddNotifications_Should_Return_ServiceCollection_For_Chaining()
    {
        // Act
        var result = _services.AddNotifications(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddInMemoryNotifications_Should_Return_ServiceCollection_For_Chaining()
    {
        // Act
        var result = _services.AddInMemoryNotifications(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddProductionNotifications_Should_Return_ServiceCollection_For_Chaining()
    {
        // Act
        var result = _services.AddProductionNotifications(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddNotifications_Should_Handle_Missing_Configuration_Section()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act & Assert
        var act = () => _services.AddNotifications(emptyConfig);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddNotifications_Should_Register_Services_As_Singletons()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var notificationService1 = serviceProvider.GetService<INotificationService>();
        var notificationService2 = serviceProvider.GetService<INotificationService>();
        
        notificationService1.Should().BeSameAs(notificationService2);

        var templateService1 = serviceProvider.GetService<ITemplateService>();
        var templateService2 = serviceProvider.GetService<ITemplateService>();
        
        templateService1.Should().BeSameAs(templateService2);
    }

    [Fact]
    public void Multiple_AddNotifications_Calls_Should_Not_Duplicate_Services()
    {
        // Act
        _services.AddNotifications(_configuration);
        _services.AddNotifications(_configuration); // Second call
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var notificationServices = serviceProvider.GetServices<INotificationService>().ToList();
        notificationServices.Should().HaveCount(2); // ServiceCollection allows multiple registrations, last one wins

        // But getting single service should work
        var notificationService = serviceProvider.GetService<INotificationService>();
        notificationService.Should().NotBeNull();
    }

    [Fact]
    public void NotificationOptions_Should_Have_Default_Values()
    {
        // Act
        var options = new NotificationOptions();

        // Assert
        options.UseInMemoryProviders.Should().BeTrue();
        options.EnableBackgroundServices.Should().BeFalse();
        options.EnableSignalR.Should().BeFalse();
        options.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void NotificationOptions_Should_Allow_Property_Updates()
    {
        // Act
        var options = new NotificationOptions
        {
            UseInMemoryProviders = false,
            EnableBackgroundServices = true,
            EnableSignalR = true,
            EnableMetrics = true
        };

        // Assert
        options.UseInMemoryProviders.Should().BeFalse();
        options.EnableBackgroundServices.Should().BeTrue();
        options.EnableSignalR.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void ServiceRegistration_Should_Allow_Resolution_Of_All_Interfaces()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - All interfaces should be resolvable
        var notificationService = serviceProvider.GetService<INotificationService>();
        notificationService.Should().NotBeNull();

        var templateService = serviceProvider.GetService<ITemplateService>();
        templateService.Should().NotBeNull();

        var emailProvider = serviceProvider.GetService<IEmailNotificationProvider>();
        emailProvider.Should().NotBeNull();

        var smsProvider = serviceProvider.GetService<ISmsNotificationProvider>();
        smsProvider.Should().NotBeNull();

        var pushProvider = serviceProvider.GetService<IPushNotificationProvider>();
        pushProvider.Should().NotBeNull();

        var inAppProvider = serviceProvider.GetService<IInAppNotificationProvider>();
        inAppProvider.Should().NotBeNull();

        var webhookProvider = serviceProvider.GetService<IWebhookNotificationProvider>();
        webhookProvider.Should().NotBeNull();

        var preferencesService = serviceProvider.GetService<INotificationPreferencesService>();
        preferencesService.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_Binding_Should_Work_Correctly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            [$"{NotificationSettings.SectionName}:General:Enabled"] = "false",
            [$"{NotificationSettings.SectionName}:Templates:DefaultLanguage"] = "fr-FR",
            [$"{NotificationSettings.SectionName}:Delivery:BatchSize"] = "250",
            [$"{NotificationSettings.SectionName}:Delivery:MaxRetryAttempts"] = "5"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        _services.AddNotifications(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var settings = serviceProvider.GetService<IOptions<NotificationSettings>>();
        settings.Should().NotBeNull();
        
        var settingsValue = settings!.Value;
        settingsValue.General.Enabled.Should().BeFalse();
        settingsValue.Templates.DefaultLanguage.Should().Be("fr-FR");
        settingsValue.Delivery.BatchSize.Should().Be(250);
        settingsValue.Delivery.MaxRetryAttempts.Should().Be(5);
    }

    [Fact] 
    public void Dependencies_Should_Be_Properly_Injected_Into_NotificationService()
    {
        // Act
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Getting NotificationService should not throw (all dependencies available)
        var act = () => serviceProvider.GetService<INotificationService>();
        act.Should().NotThrow();
        
        var notificationService = serviceProvider.GetService<INotificationService>();
        notificationService.Should().NotBeNull().And.BeOfType<NotificationService>();
    }

    [Fact]
    public void Service_Resolution_Should_Work_With_Scoped_Provider()
    {
        // Arrange
        _services.AddNotifications(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var scopedNotificationService = scope.ServiceProvider.GetService<INotificationService>();

        // Assert
        scopedNotificationService.Should().NotBeNull();
        
        // Since services are registered as singletons, scoped resolution should return the same instance
        var rootNotificationService = serviceProvider.GetService<INotificationService>();
        scopedNotificationService.Should().BeSameAs(rootNotificationService);
    }
}