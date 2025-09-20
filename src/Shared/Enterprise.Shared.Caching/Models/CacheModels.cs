using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Caching.Models;

/// <summary>
/// Cache ayarları
/// </summary>
public class CacheAyarlari
{
    public const string ConfigSection = "CacheSettings";

    /// <summary>
    /// Redis bağlantı string'i
    /// </summary>
    [Required(ErrorMessage = "Redis bağlantısı gereklidir")]
    public string RedisConnection { get; set; } = "localhost:6379";

    /// <summary>
    /// Varsayılan TTL süresi
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// L1 cache (Memory) etkin mi
    /// </summary>
    public bool EnableL1Cache { get; set; } = true;

    /// <summary>
    /// L1 cache boyutu (MB)
    /// </summary>
    [Range(1, 1024, ErrorMessage = "L1 cache boyutu 1-1024 MB arasında olmalıdır")]
    public int L1CacheSize { get; set; } = 100;

    /// <summary>
    /// L1 cache TTL süresi
    /// </summary>
    public TimeSpan L1CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache key prefix'i
    /// </summary>
    public string KeyPrefix { get; set; } = "enterprise:";

    /// <summary>
    /// Metrics etkin mi
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Serializer türü
    /// </summary>
    public SerializerTuru Serializer { get; set; } = SerializerTuru.Json;

    /// <summary>
    /// Redis bağlantı havuzu boyutu
    /// </summary>
    [Range(1, 100, ErrorMessage = "Bağlantı havuzu boyutu 1-100 arasında olmalıdır")]
    public int ConnectionPoolSize { get; set; } = 10;

    /// <summary>
    /// Retry policy ayarları
    /// </summary>
    public RetryAyarlari Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker ayarları
    /// </summary>
    public CircuitBreakerAyarlari CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Health check ayarları
    /// </summary>
    public HealthCheckAyarlari HealthCheck { get; set; } = new();
}

/// <summary>
/// Serializer türleri
/// </summary>
public enum SerializerTuru
{
    Json,
    MessagePack,
    ProtoBuf
}

/// <summary>
/// Retry policy ayarları
/// </summary>
public class RetryAyarlari
{
    /// <summary>
    /// Retry etkin mi
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum retry sayısı
    /// </summary>
    [Range(1, 10, ErrorMessage = "Retry sayısı 1-10 arasında olmalıdır")]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay süresi
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum delay süresi
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Circuit breaker ayarları
/// </summary>
public class CircuitBreakerAyarlari
{
    /// <summary>
    /// Circuit breaker etkin mi
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Failure threshold
    /// </summary>
    [Range(1, 100, ErrorMessage = "Failure threshold 1-100 arasında olmalıdır")]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Recovery timeout
    /// </summary>
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sampling duration
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Health check ayarları
/// </summary>
public class HealthCheckAyarlari
{
    /// <summary>
    /// Health check etkin mi
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check interval
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout süresi
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Cache entry bilgisi
/// </summary>
public class CacheBilgisi
{
    /// <summary>
    /// Cache key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Cache'de var mı
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// TTL süresi (kalan)
    /// </summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>
    /// Boyut (byte)
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// L1 cache'de var mı
    /// </summary>
    public bool InL1Cache { get; set; }

    /// <summary>
    /// L2 cache'de var mı (Redis)
    /// </summary>
    public bool InL2Cache { get; set; }

    /// <summary>
    /// Oluşturulma tarihi (Türkiye saati)
    /// </summary>
    public DateTime? OlusturulmaTarihi { get; set; }

    /// <summary>
    /// Son erişim tarihi (Türkiye saati)
    /// </summary>
    public DateTime? SonErisimTarihi { get; set; }

    /// <summary>
    /// Erişim sayısı
    /// </summary>
    public long ErisimSayisi { get; set; }
}

/// <summary>
/// Cache metrikleri
/// </summary>
public class CacheMetrikleri
{
    /// <summary>
    /// Hit sayısı
    /// </summary>
    public long HitSayisi { get; set; }

    /// <summary>
    /// Miss sayısı
    /// </summary>
    public long MissSayisi { get; set; }

    /// <summary>
    /// Hit oranı
    /// </summary>
    public double HitOrani => HitSayisi + MissSayisi > 0 ? HitSayisi / (double)(HitSayisi + MissSayisi) : 0;

    /// <summary>
    /// L1 hit sayısı
    /// </summary>
    public long L1HitSayisi { get; set; }

    /// <summary>
    /// L2 hit sayısı
    /// </summary>
    public long L2HitSayisi { get; set; }

    /// <summary>
    /// Ortalama get süresi
    /// </summary>
    public TimeSpan OrtalamaGetSuresi { get; set; }

    /// <summary>
    /// Ortalama set süresi
    /// </summary>
    public TimeSpan OrtalamaSetSuresi { get; set; }

    /// <summary>
    /// Toplam cache boyutu (byte)
    /// </summary>
    public long ToplamBoyut { get; set; }

    /// <summary>
    /// Toplam key sayısı
    /// </summary>
    public long ToplamKeySayisi { get; set; }

    /// <summary>
    /// Hata sayısı
    /// </summary>
    public long HataSayisi { get; set; }

    /// <summary>
    /// Son sıfırlama tarihi (Türkiye saati)
    /// </summary>
    public DateTime SonSifirlamaTarihi { get; set; }

    /// <summary>
    /// Bellek kullanımı (MB)
    /// </summary>
    public double BellekKullanimiMB { get; set; }

    /// <summary>
    /// Redis bilgileri
    /// </summary>
    public RedisBilgileri Redis { get; set; } = new();
}

/// <summary>
/// Redis bilgileri
/// </summary>
public class RedisBilgileri
{
    /// <summary>
    /// Redis server versiyonu
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Bağlı client sayısı
    /// </summary>
    public int BagliClientSayisi { get; set; }

    /// <summary>
    /// Kullanılan bellek (byte)
    /// </summary>
    public long KullanılanBellek { get; set; }

    /// <summary>
    /// Peak bellek kullanımı (byte)
    /// </summary>
    public long PeakBellekKullanimi { get; set; }

    /// <summary>
    /// Uptime (saniye)
    /// </summary>
    public long UptimeSaniye { get; set; }

    /// <summary>
    /// İşlenen komut sayısı
    /// </summary>
    public long IslenenKomutSayisi { get; set; }

    /// <summary>
    /// Saniye başına komut sayısı
    /// </summary>
    public double SaniyeBasiKomutSayisi { get; set; }
}

/// <summary>
/// Cache operasyon sonucu
/// </summary>
public class CacheOperasyonSonucu<T>
{
    /// <summary>
    /// Başarılı mı
    /// </summary>
    public bool Basarili { get; set; }

    /// <summary>
    /// Değer
    /// </summary>
    public T? Deger { get; set; }

    /// <summary>
    /// Cache'den mi geldi
    /// </summary>
    public bool CachedenGeldi { get; set; }

    /// <summary>
    /// L1 cache'den mi geldi
    /// </summary>
    public bool L1CachedenGeldi { get; set; }

    /// <summary>
    /// L2 cache'den mi geldi
    /// </summary>
    public bool L2CachedenGeldi { get; set; }

    /// <summary>
    /// İşlem süresi
    /// </summary>
    public TimeSpan IslemSuresi { get; set; }

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? HataMesaji { get; set; }

    /// <summary>
    /// Başarılı sonuç oluşturur
    /// </summary>
    public static CacheOperasyonSonucu<T> BasariliSonuc(T deger, bool cachedenGeldi, bool l1CachedenGeldi, bool l2CachedenGeldi, TimeSpan islemSuresi)
    {
        return new CacheOperasyonSonucu<T>
        {
            Basarili = cachedenGeldi, // Basarili should match whether data came from cache
            Deger = deger,
            CachedenGeldi = cachedenGeldi,
            L1CachedenGeldi = l1CachedenGeldi,
            L2CachedenGeldi = l2CachedenGeldi,
            IslemSuresi = islemSuresi
        };
    }

    /// <summary>
    /// Başarısız sonuç oluşturur
    /// </summary>
    public static CacheOperasyonSonucu<T> BasarisizSonuc(string hataMesaji, TimeSpan islemSuresi)
    {
        return new CacheOperasyonSonucu<T>
        {
            Basarili = false,
            HataMesaji = hataMesaji,
            IslemSuresi = islemSuresi
        };
    }
}

/// <summary>
/// Bulk cache operasyon sonucu
/// </summary>
public class BulkCacheOperasyonSonucu<T>
{
    /// <summary>
    /// Başarılı sonuçlar
    /// </summary>
    public Dictionary<string, T> BasariliSonuclar { get; set; } = new();

    /// <summary>
    /// Başarısız key'ler
    /// </summary>
    public List<string> BasarisizKeyler { get; set; } = new();

    /// <summary>
    /// Toplam işlem süresi
    /// </summary>
    public TimeSpan ToplamIslemSuresi { get; set; }

    /// <summary>
    /// Cache hit sayısı
    /// </summary>
    public int HitSayisi { get; set; }

    /// <summary>
    /// Cache miss sayısı
    /// </summary>
    public int MissSayisi { get; set; }

    /// <summary>
    /// Hit oranı
    /// </summary>
    public double HitOrani => HitSayisi + MissSayisi > 0 ? HitSayisi / (double)(HitSayisi + MissSayisi) : 0;
}