using Enterprise.Shared.Caching.Extensions;
using Enterprise.Shared.Caching.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Redis;
using DotNetEnv;
using System.Reflection;

namespace Enterprise.Shared.Caching.Tests.Integration;

public class CacheDebugTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IServiceProvider _serviceProvider = null!;
    private ICacheService _cacheService = null!;
    private IAdvancedCacheService _advancedCacheService = null!;

    public CacheDebugTest()
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
        
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:RedisConnection"] = _redisContainer.GetConnectionString(),
                ["CacheSettings:DefaultTtl"] = "00:30:00",
                ["CacheSettings:EnableL1Cache"] = "true",
                ["CacheSettings:L1CacheSize"] = "50",
                ["CacheSettings:KeyPrefix"] = "debug-test:",
                ["CacheSettings:EnableMetrics"] = "true"
            })
            .Build();

        services.AddSharedCaching(configuration);
        _serviceProvider = services.BuildServiceProvider();

        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _advancedCacheService = _serviceProvider.GetRequiredService<IAdvancedCacheService>();
    }

    public async Task DisposeAsync()
    {
        (_serviceProvider as IDisposable)?.Dispose();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task Debug_PatternRemovalIssue()
    {
        // Clear any existing data
        await _advancedCacheService.FlushAsync();

        // Set test data
        await _cacheService.SetAsync("pattern-test:1", "value1", TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("pattern-test:2", "value2", TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("other:1", "value3", TimeSpan.FromMinutes(5));

        // Wait a bit for data to be written
        await Task.Delay(100);

        // Check if data exists by trying to get it
        var verify1 = await _cacheService.GetAsync<string>("pattern-test:1");
        var verify2 = await _cacheService.GetAsync<string>("pattern-test:2");
        var verify3 = await _cacheService.GetAsync<string>("other:1");
        Console.WriteLine($"Get verification: p1={verify1}, p2={verify2}, o1={verify3}");

        // Get all keys to see what's actually in Redis
        var allKeys = await _advancedCacheService.GetKeysAsync("*", 1000);
        Console.WriteLine($"All keys in Redis: [{string.Join(", ", allKeys)}]");

        // Get pattern-test keys specifically
        var patternKeys = await _advancedCacheService.GetKeysAsync("pattern-test:*", 1000);
        Console.WriteLine($"Pattern keys found: [{string.Join(", ", patternKeys)}]");

        // Try to remove by pattern
        var removedCount = await _cacheService.RemovePatternAsync("pattern-test:*");
        Console.WriteLine($"RemovePatternAsync returned: {removedCount}");

        // Get all keys after removal
        var allKeysAfter = await _advancedCacheService.GetKeysAsync("*", 1000);
        Console.WriteLine($"All keys after removal: [{string.Join(", ", allKeysAfter)}]");

        // Assert
        removedCount.Should().Be(2);
    }

    [Fact]
    public async Task Debug_TtlIssue()
    {
        // Clear any existing data
        await _advancedCacheService.FlushAsync();

        // Set test data with short TTL
        var key = "ttl-debug-key";
        var shortTtl = TimeSpan.FromSeconds(2);
        await _cacheService.SetAsync(key, "test-value", shortTtl);

        Console.WriteLine($"Set key '{key}' with TTL {shortTtl}");

        // Check if key exists
        var exists = await _cacheService.ExistsAsync(key);
        Console.WriteLine($"Key exists: {exists}");

        // Try to get TTL
        var ttl = await _cacheService.GetTtlAsync(key);
        Console.WriteLine($"TTL retrieved: {ttl}");

        // Wait and check again
        await Task.Delay(TimeSpan.FromSeconds(3));
        var existsAfter = await _cacheService.ExistsAsync(key);
        Console.WriteLine($"Key exists after expiration: {existsAfter}");

        // Assertions
        exists.Should().BeTrue();
        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeLessOrEqualTo(shortTtl);
        existsAfter.Should().BeFalse();
    }
}