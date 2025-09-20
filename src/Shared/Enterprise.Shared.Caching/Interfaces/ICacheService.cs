using Enterprise.Shared.Caching.Models;

namespace Enterprise.Shared.Caching.Interfaces;

/// <summary>
/// Ana cache servisi arayüzü
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Cache'den değer alır
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Cache'deki değer veya null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache'den değer alır, yoksa factory ile oluşturup cache'e kaydeder
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Değer üretici fonksiyon</param>
    /// <param name="expiry">Expiration süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Cache'deki veya yeni oluşturulan değer</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, 
        TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache'e değer kaydeder
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Kaydedilecek değer</param>
    /// <param name="expiry">Expiration süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache'den değer siler
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silme başarılı mı</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pattern'e uyan cache key'lerini siler
    /// </summary>
    /// <param name="pattern">Key pattern'ı (örn: user:*, product:123:*)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silinen key sayısı</returns>
    Task<int> RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache key'inin var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Key var mı</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache key'inin TTL süresini yeniler
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="expiry">Yeni expiration süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yenileme başarılı mı</returns>
    Task<bool> RefreshAsync(string key, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache key'inin kalan TTL süresini alır
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kalan TTL süresi</returns>
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Detaylı cache operasyonları arayüzü
/// </summary>
public interface IAdvancedCacheService : ICacheService
{
    /// <summary>
    /// Detaylı cache operasyon sonucu ile değer alır
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Detaylı operasyon sonucu</returns>
    Task<CacheOperasyonSonucu<T>> GetWithResultAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaylı cache operasyon sonucu ile değer kaydeder
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Kaydedilecek değer</param>
    /// <param name="expiry">Expiration süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Detaylı operasyon sonucu</returns>
    Task<CacheOperasyonSonucu<bool>> SetWithResultAsync<T>(string key, T value, TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache key hakkında bilgi alır
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Cache bilgisi</returns>
    Task<CacheBilgisi> GetCacheInfoAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pattern'e uyan cache key'lerini listeler
    /// </summary>
    /// <param name="pattern">Key pattern'ı</param>
    /// <param name="limit">Maximum sonuç sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Key listesi</returns>
    Task<List<string>> GetKeysAsync(string pattern, int limit = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache'i temizler
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Temizleme başarılı mı</returns>
    Task<bool> FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Bulk cache operasyonları arayüzü
/// </summary>
public interface IBulkCacheService
{
    /// <summary>
    /// Birden fazla key için değer alır
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="keys">Key listesi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Key-value eşlemeleri</returns>
    Task<BulkCacheOperasyonSonucu<T>> GetMultipleAsync<T>(IEnumerable<string> keys, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla key-value çiftini cache'e kaydeder
    /// </summary>
    /// <typeparam name="T">Değer türü</typeparam>
    /// <param name="values">Key-value eşlemeleri</param>
    /// <param name="expiry">Expiration süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kaydetme başarılı mı</returns>
    Task<bool> SetMultipleAsync<T>(IDictionary<string, T> values, TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla key'i cache'den siler
    /// </summary>
    /// <param name="keys">Key listesi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silinen key sayısı</returns>
    Task<int> RemoveMultipleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache metrikleri arayüzü
/// </summary>
public interface ICacheMetricsService
{
    /// <summary>
    /// Cache metriklerini alır
    /// </summary>
    /// <param name="keyPattern">Key pattern'ı (opsiyonel)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Cache metrikleri</returns>
    Task<CacheMetrikleri> GetMetricsAsync(string? keyPattern = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache metriklerini sıfırlar
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Sıfırlama başarılı mı</returns>
    Task<bool> ResetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Hit sayısını artırır
    /// </summary>
    /// <param name="l1Hit">L1 cache hit mi</param>
    void RecordHit(bool l1Hit = false);

    /// <summary>
    /// Miss sayısını artırır
    /// </summary>
    void RecordMiss();

    /// <summary>
    /// İşlem süresini kaydeder
    /// </summary>
    /// <param name="operationType">Operasyon türü (get, set, remove)</param>
    /// <param name="duration">Süre</param>
    void RecordOperationTime(string operationType, TimeSpan duration);

    /// <summary>
    /// Hata sayısını artırır
    /// </summary>
    void RecordError();
}

/// <summary>
/// Cache health check arayüzü
/// </summary>
public interface ICacheHealthCheckService
{
    /// <summary>
    /// Cache health check yapar
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Sağlıklı mı</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaylı health check yapar
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Health check detayları</returns>
    Task<Dictionary<string, object>> GetHealthDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Redis bağlantısını kontrol eder
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Bağlantı sağlıklı mı</returns>
    Task<bool> CheckRedisConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// L1 cache'i kontrol eder
    /// </summary>
    /// <returns>L1 cache sağlıklı mı</returns>
    bool CheckL1Cache();
}