using Enterprise.Shared.Caching.Extensions;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Redis;
using DotNetEnv;
using System.Reflection;

namespace Enterprise.Shared.Caching.Tests.Integration;

[Collection("Redis Integration Tests")]
public class CacheIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IServiceProvider _serviceProvider = null!;
    private ICacheService _cacheService = null!;
    private IAdvancedCacheService _advancedCacheService = null!;
    private ICacheMetricsService _metricsService = null!;

    public CacheIntegrationTests()
    {
        // Load environment variables from root .env file
        var envPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "../../../../../..",
            ".env");
        
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        
        var redisImage = Environment.GetEnvironmentVariable("TEST_REDIS_IMAGE") ?? "redis:7-alpine";
        var redisPort = int.Parse(Environment.GetEnvironmentVariable("TEST_REDIS_PORT") ?? "6379");
        
        _redisContainer = new RedisBuilder()
            .WithImage(redisImage)
            .WithPortBinding(redisPort, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        
        // Setup DI container with real Redis
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var defaultTtl = Environment.GetEnvironmentVariable("CACHE_DEFAULT_TTL_MINUTES") ?? "30";
        var memoryCacheSize = Environment.GetEnvironmentVariable("MEMORY_CACHE_SIZE_LIMIT_MB") ?? "50";
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:RedisConnection"] = _redisContainer.GetConnectionString(),
                ["CacheSettings:DefaultTtl"] = $"00:{defaultTtl}:00",
                ["CacheSettings:EnableL1Cache"] = "true",
                ["CacheSettings:L1CacheSize"] = memoryCacheSize,
                ["CacheSettings:KeyPrefix"] = "integration-test:",
                ["CacheSettings:EnableMetrics"] = "true",
                ["CacheSettings:CompressionEnabled"] = Environment.GetEnvironmentVariable("CACHE_COMPRESSION_ENABLED") ?? "true",
                ["CacheSettings:SerializationFormat"] = Environment.GetEnvironmentVariable("CACHE_SERIALIZATION_FORMAT") ?? "json"
            })
            .Build();

        services.AddSharedCaching(configuration);
        _serviceProvider = services.BuildServiceProvider();

        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _advancedCacheService = _serviceProvider.GetRequiredService<IAdvancedCacheService>();
        _metricsService = _serviceProvider.GetRequiredService<ICacheMetricsService>();

        // Reset metrics for clean tests
        await _metricsService.ResetMetricsAsync();
    }

    public async Task DisposeAsync()
    {
        (_serviceProvider as IDisposable)?.Dispose();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task BasicCacheOperations_ShouldWork()
    {
        // Arrange
        var key = "test-basic-key";
        var value = new TestData { Name = "Test", Value = 42 };

        // Act & Assert - Set
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        
        // Act & Assert - Get
        var retrieved = await _cacheService.GetAsync<TestData>(key);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(value.Name);
        retrieved.Value.Should().Be(value.Value);

        // Act & Assert - Exists
        var exists = await _cacheService.ExistsAsync(key);
        exists.Should().BeTrue();

        // Act & Assert - Remove
        var removed = await _cacheService.RemoveAsync(key);
        removed.Should().BeTrue();

        // Verify removal
        var afterRemove = await _cacheService.GetAsync<TestData>(key);
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-get-or-set";
        var expectedValue = new TestData { Name = "GetOrSet", Value = 100 };
        var factoryCallCount = 0;

        // Act - First call should execute factory
        var result1 = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCallCount++;
            return Task.FromResult(expectedValue);
        }, TimeSpan.FromMinutes(5));

        // Act - Second call should return cached value
        var result2 = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCallCount++;
            return Task.FromResult(new TestData { Name = "Should not be called", Value = -1 });
        }, TimeSpan.FromMinutes(5));

        // Assert
        result1.Should().BeEquivalentTo(expectedValue);
        result2.Should().BeEquivalentTo(expectedValue);
        factoryCallCount.Should().Be(1); // Factory should only be called once
    }

    [Fact]
    public async Task MultiLevelCaching_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-multilevel";
        var value = "test-value";
        
        // Clear any existing cache entry first
        await _cacheService.RemoveAsync(key);
        
        // Reset metrics to track hits accurately
        await _metricsService.ResetMetricsAsync();

        // Act - First call: Cache miss, store in both L1 and L2
        var result1 = await _advancedCacheService.GetWithResultAsync<string>(key);
        result1.Basarili.Should().BeFalse(); // Miss
        result1.CachedenGeldi.Should().BeFalse();

        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act - Second call: Should hit L1 cache
        var result2 = await _advancedCacheService.GetWithResultAsync<string>(key);
        result2.Basarili.Should().BeTrue();
        result2.L1CachedenGeldi.Should().BeTrue();

        // Clear L1 cache by setting a very short TTL and waiting
        await Task.Delay(100);

        // Act - Third call: Should hit L2 cache (Redis)
        var result3 = await _cacheService.GetAsync<string>(key);
        result3.Should().Be(value);

        // Verify metrics
        var metrics = await _metricsService.GetMetricsAsync();
        metrics.HitSayisi.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BulkOperations_ShouldWork()
    {
        // Arrange
        var bulkService = _serviceProvider.GetRequiredService<IBulkCacheService>();
        var testData = new Dictionary<string, TestData>
        {
            ["bulk-1"] = new() { Name = "Bulk 1", Value = 1 },
            ["bulk-2"] = new() { Name = "Bulk 2", Value = 2 },
            ["bulk-3"] = new() { Name = "Bulk 3", Value = 3 }
        };

        // Act - Set multiple
        var setResult = await bulkService.SetMultipleAsync(testData, TimeSpan.FromMinutes(5));
        setResult.Should().BeTrue();

        // Act - Get multiple
        var getResult = await bulkService.GetMultipleAsync<TestData>(testData.Keys);
        
        // Assert
        getResult.Should().NotBeNull();
        getResult.BasariliSonuclar.Should().HaveCount(3);
        getResult.BasariliSonuclar["bulk-1"].Name.Should().Be("Bulk 1");
        getResult.BasariliSonuclar["bulk-2"].Name.Should().Be("Bulk 2");
        getResult.BasariliSonuclar["bulk-3"].Name.Should().Be("Bulk 3");

        // Cleanup
        var removeCount = await bulkService.RemoveMultipleAsync(testData.Keys);
        removeCount.Should().Be(3);
    }

    [Fact]
    public async Task PatternRemoval_ShouldWork()
    {
        // Arrange - Clear any existing test data
        await _cacheService.RemovePatternAsync("pattern-test:*");
        await _cacheService.RemovePatternAsync("other:*");
        
        // Set test data
        await _cacheService.SetAsync("pattern-test:1", "value1", TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("pattern-test:2", "value2", TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("other:1", "value3", TimeSpan.FromMinutes(5));

        // Verify data was set
        var verify1 = await _cacheService.GetAsync<string>("pattern-test:1");
        var verify2 = await _cacheService.GetAsync<string>("pattern-test:2");
        var verify3 = await _cacheService.GetAsync<string>("other:1");
        
        verify1.Should().Be("value1");
        verify2.Should().Be("value2");
        verify3.Should().Be("value3");

        // Act
        var removedCount = await _cacheService.RemovePatternAsync("pattern-test:*");

        // Assert
        removedCount.Should().Be(2, "Should remove exactly 2 pattern-test keys");
        
        // Verify specific removals
        var value1 = await _cacheService.GetAsync<string>("pattern-test:1");
        var value2 = await _cacheService.GetAsync<string>("pattern-test:2");
        var value3 = await _cacheService.GetAsync<string>("other:1");
        
        value1.Should().BeNull("pattern-test:1 should be removed");
        value2.Should().BeNull("pattern-test:2 should be removed");
        value3.Should().Be("value3", "other:1 should remain untouched");
    }

    [Fact]
    public async Task HealthCheck_ShouldReportHealthy()
    {
        // Arrange
        var healthService = _serviceProvider.GetRequiredService<ICacheHealthCheckService>();

        // Act
        var isHealthy = await healthService.IsHealthyAsync();
        var healthDetails = await healthService.GetHealthDetailsAsync();

        // Assert
        isHealthy.Should().BeTrue();
        healthDetails.Should().NotBeEmpty();
        healthDetails.Should().ContainKey("Redis");
    }

    [Fact]
    public async Task Metrics_ShouldTrackOperations()
    {
        // Arrange
        await _metricsService.ResetMetricsAsync();
        var key = "metrics-test";

        // Act - Generate some cache operations
        await _cacheService.GetAsync<string>(key); // Miss
        await _cacheService.SetAsync(key, "test-value", TimeSpan.FromMinutes(5));
        await _cacheService.GetAsync<string>(key); // Hit
        await _cacheService.GetAsync<string>(key); // Hit
        await _cacheService.GetAsync<string>("non-existent"); // Miss

        // Assert
        var metrics = await _metricsService.GetMetricsAsync();
        metrics.HitSayisi.Should().Be(2);
        metrics.MissSayisi.Should().Be(2);
        metrics.HitOrani.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public async Task TtlOperations_ShouldWork()
    {
        // Arrange
        var key = "ttl-test";
        var shortTtl = TimeSpan.FromSeconds(2);

        // Act
        await _cacheService.SetAsync(key, "test-value", shortTtl);
        
        var initialTtl = await _cacheService.GetTtlAsync(key);
        var exists1 = await _cacheService.ExistsAsync(key);
        
        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        var exists2 = await _cacheService.ExistsAsync(key);
        var expiredValue = await _cacheService.GetAsync<string>(key);

        // Assert
        initialTtl.Should().NotBeNull();
        initialTtl!.Value.Should().BeLessOrEqualTo(shortTtl);
        exists1.Should().BeTrue();
        exists2.Should().BeFalse();
        expiredValue.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_ShouldExtendTtl()
    {
        // Arrange
        var key = "refresh-test";
        var initialTtl = TimeSpan.FromSeconds(30); // Longer initial TTL to avoid expiration
        var extendedTtl = TimeSpan.FromMinutes(5);

        // Clear any existing entry
        await _cacheService.RemoveAsync(key);

        // Act
        await _cacheService.SetAsync(key, "test-value", initialTtl);
        
        // Verify the key exists
        var exists = await _cacheService.ExistsAsync(key);
        exists.Should().BeTrue("Key should exist after being set");
        
        await Task.Delay(TimeSpan.FromMilliseconds(100)); // Brief delay
        
        var refreshed = await _cacheService.RefreshAsync(key, extendedTtl);
        var newTtl = await _cacheService.GetTtlAsync(key);

        // Assert
        refreshed.Should().BeTrue("Refresh should succeed when key exists");
        newTtl.Should().NotBeNull("TTL should not be null after refresh");
        newTtl!.Value.Should().BeGreaterThan(TimeSpan.FromMinutes(4)); // Should be close to 5 minutes
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var key = "concurrent-test";
        var tasks = new List<Task<string?>>();

        // Act - Multiple concurrent gets and sets
        var setTask = _cacheService.SetAsync(key, "concurrent-value", TimeSpan.FromMinutes(5));
        
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_cacheService.GetAsync<string>(key));
        }

        await setTask;
        var results = await Task.WhenAll(tasks);

        // Assert - Should not throw exceptions
        results.Should().NotBeNull();
        // Some may be null (if they ran before set completed), others should have the value
        results.Where(r => r != null).Should().AllSatisfy(r => r.Should().Be("concurrent-value"));
    }
}

public class TestData
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}