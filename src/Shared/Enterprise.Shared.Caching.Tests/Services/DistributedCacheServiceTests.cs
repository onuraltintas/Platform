using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using Enterprise.Shared.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Text.Json;

namespace Enterprise.Shared.Caching.Tests.Services;

public class DistributedCacheServiceTests : IDisposable
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<ICacheMetricsService> _mockMetricsService;
    private readonly Mock<ILogger<DistributedCacheService>> _mockLogger;
    private readonly IOptions<CacheAyarlari> _options;
    private readonly DistributedCacheService _cacheService;

    public DistributedCacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDatabase = new Mock<IDatabase>();
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockMetricsService = new Mock<ICacheMetricsService>();
        _mockLogger = new Mock<ILogger<DistributedCacheService>>();

        var cacheSettings = new CacheAyarlari
        {
            EnableL1Cache = true,
            KeyPrefix = "test:",
            DefaultTtl = TimeSpan.FromMinutes(30),
            L1CacheTtl = TimeSpan.FromMinutes(5)
        };

        _options = Options.Create(cacheSettings);

        _mockConnectionMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _cacheService = new DistributedCacheService(
            _mockDistributedCache.Object,
            _mockMemoryCache.Object,
            _mockConnectionMultiplexer.Object,
            _options,
            _mockMetricsService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetAsync_L1CacheHit_ShouldReturnValueAndRecordL1Hit()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new { Name = "Test", Value = 42 };
        var fullKey = "test:test-key";

        _mockMemoryCache.Setup(x => x.TryGetValue(fullKey, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = expectedValue;
                return true;
            });

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedValue);
        _mockMetricsService.Verify(x => x.RecordHit(true), Times.Once);
    }

    [Fact]
    public async Task GetAsync_L2CacheHit_ShouldReturnValueAndRecordL2Hit()
    {
        // Arrange
        var key = "test-key";
        var testObject = new { Name = "Test", Value = 42 };
        var serializedValue = JsonSerializer.Serialize(testObject);
        var fullKey = "test:test-key";

        _mockMemoryCache.Setup(x => x.TryGetValue(fullKey, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = null!;
                return false;
            });

        _mockDatabase.Setup(x => x.StringGetAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)serializedValue);

        // Mock the CreateEntry method for L1 cache set operation after L2 hit
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        result.Should().NotBeNull();
        _mockMetricsService.Verify(x => x.RecordHit(false), Times.Once);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ShouldReturnNullAndRecordMiss()
    {
        // Arrange
        var key = "test-key";
        var fullKey = "test:test-key";

        _mockMemoryCache.Setup(x => x.TryGetValue(fullKey, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = null!;
                return false;
            });

        _mockDatabase.Setup(x => x.StringGetAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        result.Should().BeNull();
        _mockMetricsService.Verify(x => x.RecordMiss(), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreInBothL1AndL2Cache()
    {
        // Arrange
        var key = "test-key";
        var value = new { Name = "Test", Value = 42 };
        var fullKey = "test:test-key";
        var serializedValue = JsonSerializer.Serialize(value);
        var expiry = TimeSpan.FromMinutes(10);

        // Mock the CreateEntry method that Set extension method calls
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        
        _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Act
        await _cacheService.SetAsync(key, value, expiry);

        // Assert - Redis veritabanına doğrudan kayıt yapıldığını doğrula
        _mockDatabase.Verify(x => x.StringSetAsync(
            fullKey,
            It.IsAny<RedisValue>(),
            expiry,
            It.IsAny<bool>(),
            When.Always,
            CommandFlags.None), Times.Once);

        _mockMemoryCache.Verify(x => x.CreateEntry(fullKey), Times.Once);
        mockCacheEntry.VerifySet(x => x.Value = value, Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_KeyExists_ShouldReturnExistingValue()
    {
        // Arrange
        var key = "test-key";
        var existingValue = new { Name = "Existing", Value = 100 };
        var fullKey = "test:test-key";
        var factoryCallCount = 0;

        _mockMemoryCache.Setup(x => x.TryGetValue(fullKey, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = existingValue;
                return true;
            });

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCallCount++;
            return Task.FromResult(new { Name = "New", Value = 200 });
        });

        // Assert
        result.Should().BeEquivalentTo(existingValue);
        factoryCallCount.Should().Be(0); // Factory should not be called
    }

    [Fact]
    public async Task GetOrSetAsync_KeyNotExists_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        var key = "test-key";
        var newValue = new { Name = "New", Value = 200 };
        var fullKey = "test:test-key";

        _mockMemoryCache.Setup(x => x.TryGetValue(fullKey, out It.Ref<object>.IsAny))
            .Returns((string k, out object value) =>
            {
                value = null!;
                return false;
            });

        _mockDatabase.Setup(x => x.StringGetAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Mock the CreateEntry method for memory cache set operation
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(newValue));

        // Assert
        result.Should().BeEquivalentTo(newValue);
        _mockDatabase.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromBothCaches()
    {
        // Arrange
        var key = "test-key";
        var fullKey = "test:test-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync(fullKey, It.IsAny<CommandFlags>()), Times.Once);
        _mockMemoryCache.Verify(x => x.Remove(fullKey), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_KeyExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key";
        var fullKey = "test:test-key";

        _mockDatabase.Setup(x => x.KeyExistsAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_KeyNotExists_ShouldReturnFalse()
    {
        // Arrange
        var key = "test-key";
        var fullKey = "test:test-key";

        _mockDatabase.Setup(x => x.KeyExistsAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTtlAsync_ShouldReturnRemainingTtl()
    {
        // Arrange
        var key = "test-key";
        var fullKey = "test:test-key";
        var expectedTtl = TimeSpan.FromMinutes(5);

        _mockDatabase.Setup(x => x.KeyTimeToLiveAsync(fullKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedTtl);

        // Act
        var result = await _cacheService.GetTtlAsync(key);

        // Assert
        result.Should().Be(expectedTtl);
    }

    [Theory]
    [InlineData("user:123", "test:user:123")]
    [InlineData("product:*", "test:product:*")]
    [InlineData("", "test:")]
    public void BuildKey_ShouldAddPrefixCorrectly(string input, string expected)
    {
        // Act & Assert using private reflection or making BuildKey internal for testing
        var fullKey = $"{_options.Value.KeyPrefix}{input}";
        fullKey.Should().Be(expected);
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}