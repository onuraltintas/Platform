using Enterprise.Shared.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Enterprise.Shared.Caching.Tests.Services;

public class CacheMetricsServiceTests
{
    private readonly Mock<ILogger<CacheMetricsService>> _mockLogger;
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CacheMetricsService _metricsService;

    public CacheMetricsServiceTests()
    {
        _mockLogger = new Mock<ILogger<CacheMetricsService>>();
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();

        _mockConnectionMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _metricsService = new CacheMetricsService(_mockLogger.Object, _mockConnectionMultiplexer.Object);
    }

    [Fact]
    public void RecordHit_L1Hit_ShouldIncrementL1HitCount()
    {
        // Act
        _metricsService.RecordHit(l1Hit: true);
        _metricsService.RecordHit(l1Hit: true);

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.HitSayisi.Should().Be(2);
        metrics.L1HitSayisi.Should().Be(2);
        metrics.L2HitSayisi.Should().Be(0);
    }

    [Fact]
    public void RecordHit_L2Hit_ShouldIncrementL2HitCount()
    {
        // Act
        _metricsService.RecordHit(l1Hit: false);
        _metricsService.RecordHit(l1Hit: false);
        _metricsService.RecordHit(l1Hit: false);

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.HitSayisi.Should().Be(3);
        metrics.L1HitSayisi.Should().Be(0);
        metrics.L2HitSayisi.Should().Be(3);
    }

    [Fact]
    public void RecordMiss_ShouldIncrementMissCount()
    {
        // Act
        _metricsService.RecordMiss();
        _metricsService.RecordMiss();
        _metricsService.RecordMiss();

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.MissSayisi.Should().Be(3);
        metrics.HitSayisi.Should().Be(0);
    }

    [Fact]
    public void RecordError_ShouldIncrementErrorCount()
    {
        // Act
        _metricsService.RecordError();
        _metricsService.RecordError();

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.HataSayisi.Should().Be(2);
    }

    [Fact]
    public void RecordOperationTime_ShouldTrackOperationTimes()
    {
        // Arrange
        var operationType = "get";
        var duration1 = TimeSpan.FromMilliseconds(10);
        var duration2 = TimeSpan.FromMilliseconds(20);
        var duration3 = TimeSpan.FromMilliseconds(30);

        // Act
        _metricsService.RecordOperationTime(operationType, duration1);
        _metricsService.RecordOperationTime(operationType, duration2);
        _metricsService.RecordOperationTime(operationType, duration3);

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.OrtalamaGetSuresi.Should().Be(TimeSpan.FromMilliseconds(20)); // Average of 10, 20, 30
    }

    [Fact]
    public void HitOrani_ShouldCalculateCorrectly()
    {
        // Act
        _metricsService.RecordHit(l1Hit: true);  // 1 hit
        _metricsService.RecordHit(l1Hit: false); // 2 hits
        _metricsService.RecordHit(l1Hit: true);  // 3 hits
        _metricsService.RecordMiss();            // 1 miss
        _metricsService.RecordMiss();            // 2 misses

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.HitOrani.Should().BeApproximately(0.6, 0.01); // 3 hits / 5 total = 0.6
    }

    [Fact]
    public void HitOrani_NoOperations_ShouldReturnZero()
    {
        // Act & Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.HitOrani.Should().Be(0);
    }

    [Fact]
    public async Task ResetMetricsAsync_ShouldResetAllCounters()
    {
        // Arrange
        _metricsService.RecordHit(l1Hit: true);
        _metricsService.RecordHit(l1Hit: false);
        _metricsService.RecordMiss();
        _metricsService.RecordError();

        var beforeReset = await _metricsService.GetMetricsAsync();
        beforeReset.HitSayisi.Should().BeGreaterThan(0);
        beforeReset.MissSayisi.Should().BeGreaterThan(0);
        beforeReset.HataSayisi.Should().BeGreaterThan(0);

        // Act
        var resetResult = await _metricsService.ResetMetricsAsync();

        // Assert
        resetResult.Should().BeTrue();
        var afterReset = await _metricsService.GetMetricsAsync();
        afterReset.HitSayisi.Should().Be(0);
        afterReset.MissSayisi.Should().Be(0);
        afterReset.HataSayisi.Should().Be(0);
        afterReset.L1HitSayisi.Should().Be(0);
        afterReset.L2HitSayisi.Should().Be(0);
    }

    [Fact]
    public void RecordOperationTime_MultipleOperationTypes_ShouldTrackSeparately()
    {
        // Arrange
        var getDuration = TimeSpan.FromMilliseconds(10);
        var setDuration = TimeSpan.FromMilliseconds(20);

        // Act
        _metricsService.RecordOperationTime("get", getDuration);
        _metricsService.RecordOperationTime("set", setDuration);

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.OrtalamaGetSuresi.Should().Be(getDuration);
        metrics.OrtalamaSetSuresi.Should().Be(setDuration);
    }

    [Fact]
    public void RecordOperationTime_SameOperationType_ShouldCalculateAverage()
    {
        // Arrange
        var duration1 = TimeSpan.FromMilliseconds(5);
        var duration2 = TimeSpan.FromMilliseconds(15);

        // Act
        _metricsService.RecordOperationTime("get", duration1);
        _metricsService.RecordOperationTime("get", duration2);

        // Assert
        var metrics = _metricsService.GetMetricsAsync().Result;
        metrics.OrtalamaGetSuresi.Should().Be(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task GetMetricsAsync_WithoutRedis_ShouldReturnBasicMetrics()
    {
        // Arrange
        var metricsServiceWithoutRedis = new CacheMetricsService(_mockLogger.Object);
        
        metricsServiceWithoutRedis.RecordHit(l1Hit: true);
        metricsServiceWithoutRedis.RecordMiss();

        // Act
        var metrics = await metricsServiceWithoutRedis.GetMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.HitSayisi.Should().Be(1);
        metrics.MissSayisi.Should().Be(1);
        metrics.Redis.Should().NotBeNull();
        metrics.Redis.Version.Should().Be("Unknown");
    }
}