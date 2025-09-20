namespace Enterprise.Shared.Caching.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CacheableAttribute : Attribute
{
    /// <summary>
    /// Cache key template'i. {0}, {1} gibi parametreler kullanılabilir
    /// Örnek: "user:{0}:profile", "product:category:{0}:page:{1}"
    /// </summary>
    public string? KeyTemplate { get; set; }

    /// <summary>
    /// Cache süresi (dakika cinsinden)
    /// </summary>
    public int TtlMinutes { get; set; } = 60;

    /// <summary>
    /// L1 cache kullanılsın mı
    /// </summary>
    public bool UseL1Cache { get; set; } = true;

    /// <summary>
    /// Cache key prefix'i (otomatik olarak eklenir)
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Condition expression - cache işlemi yapılacak koşul
    /// Örnek: "#{args[0] > 0}" - ilk parametre 0'dan büyükse cache yap
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Unless expression - cache işlemi yapılmayacak koşul  
    /// Örnek: "#{result == null}" - sonuç null ise cache yapma
    /// </summary>
    public string? Unless { get; set; }

    /// <summary>
    /// Sync root - aynı key için eş zamanlı erişimi kontrol eder
    /// </summary>
    public bool Sync { get; set; } = false;

    /// <summary>
    /// Cache provider'ın adı (multiple provider varsa)
    /// </summary>
    public string? CacheManager { get; set; }

    public CacheableAttribute()
    {
    }

    public CacheableAttribute(string keyTemplate, int ttlMinutes = 60)
    {
        KeyTemplate = keyTemplate;
        TtlMinutes = ttlMinutes;
    }
}