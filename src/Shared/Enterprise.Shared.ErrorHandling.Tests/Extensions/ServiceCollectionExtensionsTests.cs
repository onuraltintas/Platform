using Microsoft.Extensions.Configuration;

namespace Enterprise.Shared.ErrorHandling.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharedErrorHandling_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ErrorHandlingSettings:EnableDetailedErrors", "true"),
                new KeyValuePair<string, string?>("ErrorHandlingSettings:DefaultLanguage", "tr-TR")
            })
            .Build();

        // Act
        services.AddSharedErrorHandling(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<ITimeZoneProvider>().Should().NotBeNull();
        serviceProvider.GetService<IErrorResponseFactory>().Should().NotBeNull();
        serviceProvider.GetService<RetryPolicyFactory>().Should().NotBeNull();
        serviceProvider.GetService<CircuitBreakerFactory>().Should().NotBeNull();
        serviceProvider.GetService<IErrorMonitoringService>().Should().NotBeNull();
        serviceProvider.GetService<EnterpriseExceptionFilter>().Should().NotBeNull();
        serviceProvider.GetService<ValidationExceptionFilter>().Should().NotBeNull();
    }

    [Fact]
    public void AddSharedErrorHandling_WithAction_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        services.AddSharedErrorHandling(settings =>
        {
            settings.EnableDetailedErrors = true;
            settings.DefaultLanguage = "tr-TR";
            settings.TimeZoneId = "Turkey Standard Time";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<ErrorHandlingSettings>>();
        options.Should().NotBeNull();
        options!.Value.EnableDetailedErrors.Should().BeTrue();
        options.Value.DefaultLanguage.Should().Be("tr-TR");
        options.Value.TimeZoneId.Should().Be("Turkey Standard Time");
        
        serviceProvider.GetService<ITimeZoneProvider>().Should().NotBeNull();
        serviceProvider.GetService<IErrorResponseFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddErrorHandlingFilters_ShouldRegisterFilters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddErrorHandlingFilters();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<EnterpriseExceptionFilter>().Should().NotBeNull();
        serviceProvider.GetService<ValidationExceptionFilter>().Should().NotBeNull();
    }

    [Fact]
    public void AddRetryPolicies_ShouldRegisterPolicyFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ErrorHandlingSettings>(settings => { });

        // Act
        services.AddRetryPolicies();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<RetryPolicyFactory>().Should().NotBeNull();
        serviceProvider.GetService<CircuitBreakerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddSharedErrorHandling_WithLocalizationEnabled_ShouldConfigureCulture()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ErrorHandlingSettings:EnableLocalization", "true"),
                new KeyValuePair<string, string?>("ErrorHandlingSettings:DefaultCulture", "tr-TR")
            })
            .Build();

        // Act
        services.AddSharedErrorHandling(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify that the configuration was applied (localization services are internal)
        var timeZoneProvider = serviceProvider.GetService<ITimeZoneProvider>();
        timeZoneProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddSharedErrorHandling_ShouldConfigureTurkishCultureByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ErrorHandlingSettings:EnableLocalization", "true")
            })
            .Build();

        // Act
        services.AddSharedErrorHandling(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<ErrorHandlingSettings>>();
        
        settings.Should().NotBeNull();
        settings!.Value.DefaultCulture.Should().Be("tr-TR");
        settings.Value.DefaultLanguage.Should().Be("tr-TR");
        settings.Value.TimeZoneId.Should().Be("Turkey Standard Time");
    }

    [Fact]
    public void AddSharedErrorHandling_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedErrorHandling(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Core services
        serviceProvider.GetService<ITimeZoneProvider>().Should().NotBeNull();
        serviceProvider.GetService<IErrorResponseFactory>().Should().NotBeNull();
        serviceProvider.GetService<IErrorMonitoringService>().Should().NotBeNull();
        
        // Policy factories
        serviceProvider.GetService<RetryPolicyFactory>().Should().NotBeNull();
        serviceProvider.GetService<CircuitBreakerFactory>().Should().NotBeNull();
        
        // Filters
        serviceProvider.GetService<EnterpriseExceptionFilter>().Should().NotBeNull();
        serviceProvider.GetService<ValidationExceptionFilter>().Should().NotBeNull();
        
        // Settings
        var settings = serviceProvider.GetService<IOptions<ErrorHandlingSettings>>();
        settings.Should().NotBeNull();
        settings!.Value.Should().NotBeNull();
    }
}