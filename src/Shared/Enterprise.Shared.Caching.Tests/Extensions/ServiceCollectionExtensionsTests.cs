using Enterprise.Shared.Caching.Extensions;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Interceptors;
using Enterprise.Shared.Caching.Models;
using Enterprise.Shared.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Enterprise.Shared.Caching.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CacheSettings:RedisConnection"] = "localhost:6379",
            ["CacheSettings:DefaultTtl"] = "01:00:00",
            ["CacheSettings:EnableL1Cache"] = "true",
            ["CacheSettings:L1CacheSize"] = "100",
            ["CacheSettings:KeyPrefix"] = "test:",
            ["CacheSettings:EnableMetrics"] = "true",
            ["CacheSettings:Serializer"] = "Json"
        });
        
        _configuration = configBuilder.Build();
        
        // Add basic logging for testing
        _services.AddLogging();
    }

    [Fact]
    public void AddSharedCaching_ShouldRegisterAllRequiredServices()
    {
        // Act
        _services.AddSharedCaching(_configuration);

        // Build service provider
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Check that all main services are registered
        serviceProvider.GetService<ICacheService>().Should().NotBeNull();
        serviceProvider.GetService<IAdvancedCacheService>().Should().NotBeNull();
        serviceProvider.GetService<IBulkCacheService>().Should().NotBeNull();
        serviceProvider.GetService<ICacheHealthCheckService>().Should().NotBeNull();
        serviceProvider.GetService<ICacheMetricsService>().Should().NotBeNull();
        serviceProvider.GetService<CacheInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public void AddSharedCaching_ShouldConfigureCacheSettings()
    {
        // Act
        _services.AddSharedCaching(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<CacheAyarlari>>();
        options.Should().NotBeNull();
        options!.Value.RedisConnection.Should().Be("localhost:6379");
        options.Value.EnableL1Cache.Should().BeTrue();
        options.Value.L1CacheSize.Should().Be(100);
        options.Value.KeyPrefix.Should().Be("test:");
    }

    [Fact]
    public void AddSharedCaching_WithCustomOptions_ShouldOverrideDefaults()
    {
        // Act
        _services.AddSharedCaching(_configuration, options =>
        {
            options.L1CacheSize = 200;
            options.KeyPrefix = "custom:";
        });

        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<CacheAyarlari>>();
        options.Should().NotBeNull();
        options!.Value.L1CacheSize.Should().Be(200);
        options.Value.KeyPrefix.Should().Be("custom:");
        // Other values should still come from configuration
        options.Value.RedisConnection.Should().Be("localhost:6379");
    }

    [Fact]
    public void AddSharedCaching_ShouldRegisterSameInstanceForInterfaceAliases()
    {
        // Act
        _services.AddSharedCaching(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - All these interfaces should return the same instance
        var cacheService = serviceProvider.GetService<ICacheService>();
        var advancedService = serviceProvider.GetService<IAdvancedCacheService>();
        var bulkService = serviceProvider.GetService<IBulkCacheService>();
        var healthService = serviceProvider.GetService<ICacheHealthCheckService>();

        cacheService.Should().NotBeNull();
        advancedService.Should().BeSameAs(cacheService);
        bulkService.Should().BeSameAs(cacheService);
        healthService.Should().BeSameAs(cacheService);
    }

    [Fact]
    public void AddMemoryCaching_ShouldRegisterMemoryCacheService()
    {
        // Act
        _services.AddMemoryCaching();
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var cacheService = serviceProvider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<MemoryCacheService>();

        var metricsService = serviceProvider.GetService<ICacheMetricsService>();
        metricsService.Should().NotBeNull();
    }

    [Fact]
    public void AddCacheableService_ShouldCreateProxyWithInterceptor()
    {
        // Arrange
        _services.AddSharedCaching(_configuration);
        _services.AddCacheableService<ITestCacheableService, TestCacheableService>();

        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var service = serviceProvider.GetService<ITestCacheableService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().NotBeOfType<TestCacheableService>(); // Should be a proxy
    }

    [Fact]
    public void AddCacheableClass_ShouldCreateProxyClass()
    {
        // Arrange
        _services.AddSharedCaching(_configuration);
        _services.AddCacheableClass<TestCacheableClass>();

        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var service = serviceProvider.GetService<TestCacheableClass>();

        // Assert
        service.Should().NotBeNull();
        service.GetType().Name.Should().Contain("Proxy"); // Castle proxy naming
    }

    [Fact]
    public void AddSharedCaching_ShouldRegisterHealthChecks()
    {
        // Act
        _services.AddSharedCaching(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var healthCheckService = serviceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddSharedCaching_WithInvalidConfiguration_ShouldThrow()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:RedisConnection"] = "", // Invalid empty connection
            })
            .Build();

        // Act & Assert
        _services.AddSharedCaching(invalidConfig);
        
        // The exception should be thrown when trying to resolve the connection multiplexer
        var serviceProvider = _services.BuildServiceProvider();
        
        Action act = () => serviceProvider.GetRequiredService<IConnectionMultiplexer>();
        act.Should().Throw<InvalidOperationException>();
    }
}

// Test interfaces and classes for proxy testing
public interface ITestCacheableService
{
    Task<string> GetDataAsync(string key);
}

public class TestCacheableService : ITestCacheableService
{
    public async Task<string> GetDataAsync(string key)
    {
        await Task.Delay(10);
        return $"data-for-{key}";
    }
}

public class TestCacheableClass
{
    public virtual string GetData(string key)
    {
        return $"class-data-for-{key}";
    }
}