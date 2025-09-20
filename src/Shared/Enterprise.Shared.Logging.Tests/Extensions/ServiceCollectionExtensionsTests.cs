using Microsoft.Extensions.Configuration;

namespace Enterprise.Shared.Logging.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEnterpriseLogging_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("LoggingSettings:EnablePerformanceLogging", "true"),
                new KeyValuePair<string, string?>("LoggingSettings:SlowQueryThresholdMs", "2000")
            })
            .Build();

        // Act
        services.AddEnterpriseLogging(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<ICorrelationContextAccessor>().Should().NotBeNull();
        serviceProvider.GetService<IEnterpriseLoggerFactory>().Should().NotBeNull();
        serviceProvider.GetService<IEnterpriseLogger<ServiceCollectionExtensionsTests>>().Should().NotBeNull();
    }

    [Fact]
    public void AddEnterpriseLogging_WithAction_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        services.AddEnterpriseLogging(settings =>
        {
            settings.EnablePerformanceLogging = false;
            settings.SlowQueryThresholdMs = 5000;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<ICorrelationContextAccessor>().Should().NotBeNull();
        serviceProvider.GetService<IEnterpriseLoggerFactory>().Should().NotBeNull();
        
        var options = serviceProvider.GetService<IOptions<LoggingSettings>>();
        options.Should().NotBeNull();
        options!.Value.EnablePerformanceLogging.Should().BeFalse();
        options.Value.SlowQueryThresholdMs.Should().Be(5000);
    }

    [Fact]
    public void AddEnterpriseLogging_ShouldRegisterLoggingInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddEnterpriseLogging(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<LoggingInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public void AddEnterpriseLogging_ShouldRegisterHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddEnterpriseLogging(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void CreateEnterpriseLogger_ShouldReturnLogger()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Serilog:MinimumLevel:Default", "Information"),
                new KeyValuePair<string, string?>("LoggingSettings:ServiceName", "TestService")
            })
            .Build();

        // Act
        var logger = ServiceCollectionExtensions.CreateEnterpriseLogger(configuration);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void CreateEnterpriseLogger_WithServices_ShouldReturnLoggerWithEnrichers()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Serilog:MinimumLevel:Default", "Information"),
                new KeyValuePair<string, string?>("LoggingSettings:ServiceName", "TestService"),
                new KeyValuePair<string, string?>("LoggingSettings:EnableCorrelationId", "true")
            })
            .Build();

        services.AddEnterpriseLogging(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var logger = ServiceCollectionExtensions.CreateEnterpriseLogger(configuration, serviceProvider);

        // Assert
        logger.Should().NotBeNull();
    }
}