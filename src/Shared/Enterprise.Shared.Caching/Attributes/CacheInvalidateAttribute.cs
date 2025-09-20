namespace Enterprise.Shared.Caching.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CacheInvalidateAttribute : Attribute
{
    /// <summary>
    /// Silinecek cache key pattern'ları
    /// Örnek: ["user:*", "users:list:*", "user:{0}:*"]
    /// </summary>
    public string[] Patterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Belirli key'leri sil
    /// Örnek: ["user:{0}", "user:{0}:profile"]
    /// </summary>
    public string[] Keys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Tüm cache'i temizle (dikkatli kullanın!)
    /// </summary>
    public bool AllEntries { get; set; } = false;

    /// <summary>
    /// Invalidation işlemi before method mu after method mu yapılsın
    /// </summary>
    public bool BeforeInvocation { get; set; } = false;

    /// <summary>
    /// Condition expression - invalidation yapılacak koşul
    /// Örnek: "#{args[0] > 0}" - ilk parametre 0'dan büyükse invalidate yap
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Cache provider'ın adı (multiple provider varsa)
    /// </summary>
    public string? CacheManager { get; set; }

    public CacheInvalidateAttribute()
    {
    }

    public CacheInvalidateAttribute(params string[] patterns)
    {
        Patterns = patterns;
    }
}