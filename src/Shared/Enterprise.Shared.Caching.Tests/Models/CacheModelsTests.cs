using Enterprise.Shared.Caching.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Caching.Tests.Models;

public class CacheModelsTests
{
    [Fact]
    public void CacheAyarlari_DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new CacheAyarlari();

        // Assert
        settings.RedisConnection.Should().Be("localhost:6379");
        settings.DefaultTtl.Should().Be(TimeSpan.FromHours(1));
        settings.EnableL1Cache.Should().BeTrue();
        settings.L1CacheSize.Should().Be(100);
        settings.L1CacheTtl.Should().Be(TimeSpan.FromMinutes(5));
        settings.KeyPrefix.Should().Be("enterprise:");
        settings.EnableMetrics.Should().BeTrue();
        settings.Serializer.Should().Be(SerializerTuru.Json);
        settings.ConnectionPoolSize.Should().Be(10);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(1024, true)]
    [InlineData(1025, false)]
    public void CacheAyarlari_L1CacheSize_ShouldValidateRange(int size, bool isValid)
    {
        // Arrange
        var settings = new CacheAyarlari { L1CacheSize = size };
        var context = new ValidationContext(settings) { MemberName = nameof(CacheAyarlari.L1CacheSize) };
        var results = new List<ValidationResult>();

        // Act
        var actualIsValid = Validator.TryValidateProperty(settings.L1CacheSize, context, results);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void CacheAyarlari_ConnectionPoolSize_ShouldValidateRange(int poolSize, bool isValid)
    {
        // Arrange
        var settings = new CacheAyarlari { ConnectionPoolSize = poolSize };
        var context = new ValidationContext(settings) { MemberName = nameof(CacheAyarlari.ConnectionPoolSize) };
        var results = new List<ValidationResult>();

        // Act
        var actualIsValid = Validator.TryValidateProperty(settings.ConnectionPoolSize, context, results);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Fact]
    public void RetryAyarlari_DefaultValues_ShouldBeSet()
    {
        // Act
        var retrySettings = new RetryAyarlari();

        // Assert
        retrySettings.Enabled.Should().BeTrue();
        retrySettings.MaxRetryCount.Should().Be(3);
        retrySettings.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        retrySettings.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CircuitBreakerAyarlari_DefaultValues_ShouldBeSet()
    {
        // Act
        var circuitBreakerSettings = new CircuitBreakerAyarlari();

        // Assert
        circuitBreakerSettings.Enabled.Should().BeTrue();
        circuitBreakerSettings.FailureThreshold.Should().Be(5);
        circuitBreakerSettings.RecoveryTimeout.Should().Be(TimeSpan.FromSeconds(30));
        circuitBreakerSettings.SamplingDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void HealthCheckAyarlari_DefaultValues_ShouldBeSet()
    {
        // Act
        var healthCheckSettings = new HealthCheckAyarlari();

        // Assert
        healthCheckSettings.Enabled.Should().BeTrue();
        healthCheckSettings.Interval.Should().Be(TimeSpan.FromSeconds(30));
        healthCheckSettings.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CacheMetrikleri_HitOrani_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new CacheMetrikleri
        {
            HitSayisi = 80,
            MissSayisi = 20
        };

        // Act
        var hitRatio = metrics.HitOrani;

        // Assert
        hitRatio.Should().BeApproximately(0.8, 0.01);
    }

    [Fact]
    public void CacheMetrikleri_HitOrani_NoOperations_ShouldReturnZero()
    {
        // Arrange
        var metrics = new CacheMetrikleri
        {
            HitSayisi = 0,
            MissSayisi = 0
        };

        // Act
        var hitRatio = metrics.HitOrani;

        // Assert
        hitRatio.Should().Be(0);
    }

    [Fact]
    public void BulkCacheOperasyonSonucu_HitOrani_ShouldCalculateCorrectly()
    {
        // Arrange
        var bulkResult = new BulkCacheOperasyonSonucu<string>
        {
            HitSayisi = 7,
            MissSayisi = 3
        };

        // Act
        var hitRatio = bulkResult.HitOrani;

        // Assert
        hitRatio.Should().BeApproximately(0.7, 0.01);
    }

    [Fact]
    public void CacheOperasyonSonucu_BasariliSonuc_ShouldCreateSuccessResult()
    {
        // Arrange
        var value = "test-value";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        var result = CacheOperasyonSonucu<string>.BasariliSonuc(
            value, 
            cachedenGeldi: true, 
            l1CachedenGeldi: true, 
            l2CachedenGeldi: false, 
            duration);

        // Assert
        result.Basarili.Should().BeTrue();
        result.Deger.Should().Be(value);
        result.CachedenGeldi.Should().BeTrue();
        result.L1CachedenGeldi.Should().BeTrue();
        result.L2CachedenGeldi.Should().BeFalse();
        result.IslemSuresi.Should().Be(duration);
        result.HataMesaji.Should().BeNull();
    }

    [Fact]
    public void CacheOperasyonSonucu_BasarisizSonuc_ShouldCreateFailureResult()
    {
        // Arrange
        var errorMessage = "Cache operation failed";
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        var result = CacheOperasyonSonucu<string>.BasarisizSonuc(errorMessage, duration);

        // Assert
        result.Basarili.Should().BeFalse();
        result.Deger.Should().BeNull();
        result.CachedenGeldi.Should().BeFalse();
        result.L1CachedenGeldi.Should().BeFalse();
        result.L2CachedenGeldi.Should().BeFalse();
        result.IslemSuresi.Should().Be(duration);
        result.HataMesaji.Should().Be(errorMessage);
    }

    [Fact]
    public void CacheBilgisi_ShouldInitializeWithDefaults()
    {
        // Act
        var cacheInfo = new CacheBilgisi();

        // Assert
        cacheInfo.Key.Should().Be(string.Empty);
        cacheInfo.Exists.Should().BeFalse();
        cacheInfo.Ttl.Should().BeNull();
        cacheInfo.SizeInBytes.Should().Be(0);
        cacheInfo.InL1Cache.Should().BeFalse();
        cacheInfo.InL2Cache.Should().BeFalse();
        cacheInfo.ErisimSayisi.Should().Be(0);
    }

    [Fact]
    public void RedisBilgileri_ShouldInitializeWithDefaults()
    {
        // Act
        var redisInfo = new RedisBilgileri();

        // Assert
        redisInfo.Version.Should().Be(string.Empty);
        redisInfo.BagliClientSayisi.Should().Be(0);
        redisInfo.KullanılanBellek.Should().Be(0);
        redisInfo.PeakBellekKullanimi.Should().Be(0);
        redisInfo.UptimeSaniye.Should().Be(0);
        redisInfo.IslenenKomutSayisi.Should().Be(0);
        redisInfo.SaniyeBasiKomutSayisi.Should().Be(0);
    }

    [Theory]
    [InlineData(SerializerTuru.Json)]
    [InlineData(SerializerTuru.MessagePack)]
    [InlineData(SerializerTuru.ProtoBuf)]
    public void SerializerTuru_ShouldHaveValidValues(SerializerTuru serializer)
    {
        // Act & Assert
        serializer.Should().BeDefined();
        Enum.IsDefined(typeof(SerializerTuru), serializer).Should().BeTrue();
    }
}