using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Storage.Models;

/// <summary>
/// Depolama ayarları konfigürasyonu
/// </summary>
public class DepolamaAyarlari
{
    public const string ConfigSection = "StorageSettings";

    /// <summary>
    /// MinIO konfigürasyon ayarları
    /// </summary>
    public MinIOAyarlari MinIO { get; set; } = new();

    /// <summary>
    /// Bucket konfigürasyonları
    /// </summary>
    public BucketAyarlari Buckets { get; set; } = new();

    /// <summary>
    /// Güvenlik ayarları
    /// </summary>
    public GuvenlikAyarlari Security { get; set; } = new();

    /// <summary>
    /// Resim işleme ayarları
    /// </summary>
    public ResimIslemeAyarlari ImageProcessing { get; set; } = new();

    /// <summary>
    /// Virüs tarayıcı ayarları
    /// </summary>
    public VirusTarayiciAyarlari VirusScanner { get; set; } = new();
}

/// <summary>
/// MinIO konfigürasyon ayarları
/// </summary>
public class MinIOAyarlari
{
    [Required(ErrorMessage = "MinIO Endpoint gereklidir")]
    public string Endpoint { get; set; } = "localhost:9000";

    [Required(ErrorMessage = "Access Key gereklidir")]
    public string AccessKey { get; set; } = "minioadmin";

    [Required(ErrorMessage = "Secret Key gereklidir")]
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>
    /// SSL kullanılıp kullanılmayacağı
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// AWS region bilgisi
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Bağlantı timeout süresi (saniye)
    /// </summary>
    [Range(5, 300, ErrorMessage = "Timeout süresi 5-300 saniye arasında olmalıdır")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    [Range(0, 10, ErrorMessage = "Maksimum retry sayısı 0-10 arasında olmalıdır")]
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Bucket konfigürasyon ayarları
/// </summary>
public class BucketAyarlari
{
    /// <summary>
    /// Belgeler için bucket adı
    /// </summary>
    [Required(ErrorMessage = "Documents bucket adı gereklidir")]
    public string Documents { get; set; } = "enterprise-documents";

    /// <summary>
    /// Resimler için bucket adı
    /// </summary>
    [Required(ErrorMessage = "Images bucket adı gereklidir")]
    public string Images { get; set; } = "enterprise-images";

    /// <summary>
    /// Kullanıcı yüklemeleri için bucket adı
    /// </summary>
    [Required(ErrorMessage = "UserUploads bucket adı gereklidir")]
    public string UserUploads { get; set; } = "enterprise-user-uploads";

    /// <summary>
    /// Yedekler için bucket adı
    /// </summary>
    [Required(ErrorMessage = "Backups bucket adı gereklidir")]
    public string Backups { get; set; } = "enterprise-backups";

    /// <summary>
    /// Geçici dosyalar için bucket adı
    /// </summary>
    [Required(ErrorMessage = "Temp bucket adı gereklidir")]
    public string Temp { get; set; } = "enterprise-temp";
}

/// <summary>
/// Güvenlik ayarları
/// </summary>
public class GuvenlikAyarlari
{
    /// <summary>
    /// Maksimum dosya boyutu (byte)
    /// </summary>
    [Range(1024, long.MaxValue, ErrorMessage = "Maksimum dosya boyutu en az 1KB olmalıdır")]
    public long MaxFileSize { get; set; } = 104857600; // 100MB

    /// <summary>
    /// İzin verilen dosya uzantıları
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", // Resim dosyaları
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", // Belge dosyaları
        ".txt", ".rtf", ".csv", // Metin dosyaları
        ".zip", ".rar", ".7z" // Arşiv dosyaları
    };

    /// <summary>
    /// Yasak dosya uzantıları
    /// </summary>
    public List<string> BlockedExtensions { get; set; } = new()
    {
        ".exe", ".bat", ".cmd", ".scr", ".com", ".pif", ".vbs", ".js", ".jar", ".msi"
    };

    /// <summary>
    /// Virüs tarama etkin mi
    /// </summary>
    public bool VirusScanEnabled { get; set; } = true;

    /// <summary>
    /// Şifreleme etkin mi
    /// </summary>
    public bool EncryptionEnabled { get; set; } = true;

    /// <summary>
    /// Dosya imza doğrulaması etkin mi
    /// </summary>
    public bool FileSignatureValidation { get; set; } = true;

    /// <summary>
    /// Maksimum dosya adı uzunluğu
    /// </summary>
    [Range(10, 255, ErrorMessage = "Maksimum dosya adı uzunluğu 10-255 arasında olmalıdır")]
    public int MaxFileNameLength { get; set; } = 255;

    /// <summary>
    /// İzin verilen MIME tipleri
    /// </summary>
    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp",
        "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "text/csv"
    };
}

/// <summary>
/// Resim işleme ayarları
/// </summary>
public class ResimIslemeAyarlari
{
    /// <summary>
    /// Resim boyutlandırma etkin mi
    /// </summary>
    public bool EnableResize { get; set; } = true;

    /// <summary>
    /// Küçük resim boyutları (piksel)
    /// </summary>
    public List<int> ThumbnailSizes { get; set; } = new() { 150, 300, 800 };

    /// <summary>
    /// JPEG kalitesi (1-100)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Resim kalitesi 1-100 arasında olmalıdır")]
    public int Quality { get; set; } = 85;

    /// <summary>
    /// Çıktı formatı
    /// </summary>
    [RegularExpression(@"^(JPEG|PNG|WebP|GIF)$", ErrorMessage = "Geçerli format: JPEG, PNG, WebP, GIF")]
    public string Format { get; set; } = "WebP";

    /// <summary>
    /// Maksimum resim genişliği
    /// </summary>
    [Range(100, 10000, ErrorMessage = "Maksimum resim genişliği 100-10000 arasında olmalıdır")]
    public int MaxWidth { get; set; } = 1920;

    /// <summary>
    /// Maksimum resim yüksekliği
    /// </summary>
    [Range(100, 10000, ErrorMessage = "Maksimum resim yüksekliği 100-10000 arasında olmalıdır")]
    public int MaxHeight { get; set; } = 1080;

    /// <summary>
    /// Watermark etkin mi
    /// </summary>
    public bool EnableWatermark { get; set; } = false;

    /// <summary>
    /// Watermark metni
    /// </summary>
    public string WatermarkText { get; set; } = "Enterprise Platform";

    /// <summary>
    /// EXIF verileri korunsun mu
    /// </summary>
    public bool PreserveExifData { get; set; } = false;

    /// <summary>
    /// Progressive JPEG kullanılsın mı
    /// </summary>
    public bool UseProgressiveJpeg { get; set; } = true;
}

/// <summary>
/// Virüs tarayıcı ayarları
/// </summary>
public class VirusTarayiciAyarlari
{
    /// <summary>
    /// Tarama endpoint URL'i
    /// </summary>
    public string ScanEndpoint { get; set; } = "http://localhost:3310/scan";

    /// <summary>
    /// Tarama timeout süresi (saniye)
    /// </summary>
    [Range(5, 300, ErrorMessage = "Tarama timeout süresi 5-300 saniye arasında olmalıdır")]
    public int ScanTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// API anahtarı
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Maksimum dosya boyutu tarama limiti (byte)
    /// </summary>
    [Range(1024, long.MaxValue, ErrorMessage = "Tarama limiti en az 1KB olmalıdır")]
    public long MaxScanFileSize { get; set; } = 52428800; // 50MB

    /// <summary>
    /// Tarama başarısız olduğunda dosya yüklenmesine izin ver
    /// </summary>
    public bool AllowUploadOnScanFailure { get; set; } = false;

    /// <summary>
    /// Karantina bucket adı
    /// </summary>
    public string QuarantineBucket { get; set; } = "enterprise-quarantine";

    /// <summary>
    /// Hızlı tarama modu (daha az detaylı ama daha hızlı)
    /// </summary>
    public bool FastScanMode { get; set; } = false;
}

/// <summary>
/// Performans ayarları
/// </summary>
public class PerformansAyarlari
{
    /// <summary>
    /// Paralel işlem sayısı
    /// </summary>
    [Range(1, 10, ErrorMessage = "Paralel işlem sayısı 1-10 arasında olmalıdır")]
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// Buffer boyutu (byte)
    /// </summary>
    [Range(4096, 1048576, ErrorMessage = "Buffer boyutu 4KB-1MB arasında olmalıdır")]
    public int BufferSize { get; set; } = 65536; // 64KB

    /// <summary>
    /// Multipart upload threshold (byte)
    /// </summary>
    [Range(5242880, long.MaxValue, ErrorMessage = "Multipart threshold en az 5MB olmalıdır")]
    public long MultipartThreshold { get; set; } = 67108864; // 64MB

    /// <summary>
    /// Part boyutu (byte)
    /// </summary>
    [Range(5242880, 536870912, ErrorMessage = "Part boyutu 5MB-512MB arasında olmalıdır")]
    public long PartSize { get; set; } = 16777216; // 16MB

    /// <summary>
    /// Cache süresi (dakika)
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Cache süresi 1-1440 dakika arasında olmalıdır")]
    public int CacheExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Logging ayarları
/// </summary>
public class LoggingAyarlari
{
    /// <summary>
    /// Detaylı loglama etkin mi
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Dosya operasyonları loglanacak mı
    /// </summary>
    public bool LogFileOperations { get; set; } = true;

    /// <summary>
    /// Performans metrikleri loglanacak mı
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Hata detayları loglanacak mı
    /// </summary>
    public bool LogErrorDetails { get; set; } = true;

    /// <summary>
    /// Request/Response içerikleri loglanacak mı
    /// </summary>
    public bool LogRequestResponseContent { get; set; } = false;
}