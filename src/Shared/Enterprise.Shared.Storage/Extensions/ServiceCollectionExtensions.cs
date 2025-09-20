using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Enterprise.Shared.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Storage.Extensions;

/// <summary>
/// Service collection extension metodları
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enterprise Storage servislerini konfigürasyon ile ekler
    /// </summary>
    public static IServiceCollection AddEnterpriseStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Konfigürasyon ayarları
        services.Configure<DepolamaAyarlari>(
            configuration.GetSection(DepolamaAyarlari.ConfigSection));

        return services.AddEnterpriseStorageCore();
    }

    /// <summary>
    /// Enterprise Storage servislerini action konfigürasyonu ile ekler
    /// </summary>
    public static IServiceCollection AddEnterpriseStorage(
        this IServiceCollection services,
        Action<DepolamaAyarlari> konfigurasyonAction)
    {
        services.Configure(konfigurasyonAction);
        return services.AddEnterpriseStorageCore();
    }

    /// <summary>
    /// Enterprise Storage servislerini varsayılan ayarlarla ekler
    /// </summary>
    public static IServiceCollection AddEnterpriseStorage(
        this IServiceCollection services)
    {
        // Varsayılan ayarlar
        services.Configure<DepolamaAyarlari>(ayarlar =>
        {
            ayarlar.MinIO.Endpoint = "localhost:9000";
            ayarlar.MinIO.AccessKey = "minioadmin";
            ayarlar.MinIO.SecretKey = "minioadmin";
            ayarlar.MinIO.UseSSL = false;
            ayarlar.MinIO.Region = "us-east-1";

            ayarlar.Security.MaxFileSize = 104857600; // 100MB
            ayarlar.Security.VirusScanEnabled = false; // Varsayılan olarak kapalı
            ayarlar.Security.EncryptionEnabled = true;
            ayarlar.Security.FileSignatureValidation = true;

            ayarlar.ImageProcessing.EnableResize = true;
            ayarlar.ImageProcessing.Quality = 85;
            ayarlar.ImageProcessing.Format = "WebP";
        });

        return services.AddEnterpriseStorageCore();
    }

    /// <summary>
    /// MinIO storage servisini özelleştirilmiş ayarlarla ekler
    /// </summary>
    public static IServiceCollection AddMinIOStorage(
        this IServiceCollection services,
        string endpoint,
        string accessKey,
        string secretKey,
        bool useSSL = false,
        string region = "us-east-1")
    {
        services.Configure<DepolamaAyarlari>(ayarlar =>
        {
            ayarlar.MinIO.Endpoint = endpoint;
            ayarlar.MinIO.AccessKey = accessKey;
            ayarlar.MinIO.SecretKey = secretKey;
            ayarlar.MinIO.UseSSL = useSSL;
            ayarlar.MinIO.Region = region;
        });

        return services.AddEnterpriseStorageCore();
    }

    /// <summary>
    /// Sadece resim işleme servislerini ekler
    /// </summary>
    public static IServiceCollection AddImageProcessing(
        this IServiceCollection services,
        Action<ResimIslemeAyarlari>? konfigurasyonAction = null)
    {
        services.Configure<DepolamaAyarlari>(ayarlar =>
        {
            konfigurasyonAction?.Invoke(ayarlar.ImageProcessing);
        });

        services.AddScoped<IResimIslemeServisi, ResimIslemeServisi>();
        
        return services;
    }

    /// <summary>
    /// Virüs tarayıcı servisini ClamAV ile ekler
    /// </summary>
    public static IServiceCollection AddClamAVVirusScanner(
        this IServiceCollection services,
        string scanEndpoint = "http://localhost:3310/scan",
        string? apiKey = null,
        int timeoutSeconds = 60)
    {
        services.Configure<DepolamaAyarlari>(ayarlar =>
        {
            ayarlar.Security.VirusScanEnabled = true;
            ayarlar.VirusScanner.ScanEndpoint = scanEndpoint;
            ayarlar.VirusScanner.ApiKey = apiKey;
            ayarlar.VirusScanner.ScanTimeoutSeconds = timeoutSeconds;
        });

        services.AddHttpClient<IVirusTarayiciServisi, ClamAVTarayiciServisi>();
        
        return services;
    }

    /// <summary>
    /// Mock virüs tarayıcı servisini ekler (test için)
    /// </summary>
    public static IServiceCollection AddMockVirusScanner(
        this IServiceCollection services)
    {
        services.Configure<DepolamaAyarlari>(ayarlar =>
        {
            ayarlar.Security.VirusScanEnabled = true;
        });

        services.AddScoped<IVirusTarayiciServisi, MockVirusTarayiciServisi>();
        
        return services;
    }

    /// <summary>
    /// Ana storage servislerini ekler
    /// </summary>
    private static IServiceCollection AddEnterpriseStorageCore(this IServiceCollection services)
    {
        // Konfigürasyon validasyonu
        services.AddSingleton<IValidateOptions<DepolamaAyarlari>, DepolamaAyarlariValidatoru>();

        // MinIO client
        services.AddSingleton<IMinioClient>(provider =>
        {
            var ayarlar = provider.GetRequiredService<IOptions<DepolamaAyarlari>>().Value;
            
            var client = new MinioClient()
                .WithEndpoint(ayarlar.MinIO.Endpoint)
                .WithCredentials(ayarlar.MinIO.AccessKey, ayarlar.MinIO.SecretKey)
                .WithRegion(ayarlar.MinIO.Region);

            if (ayarlar.MinIO.UseSSL)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        // Ana servisler
        services.AddScoped<IDosyaValidasyonServisi, DosyaValidasyonServisi>();
        services.AddScoped<IDepolamaServisi, MinIODepolamaServisi>();
        services.AddScoped<IResimIslemeServisi, ResimIslemeServisi>();
        services.AddScoped<IYedeklemeServisi, YedeklemeServisi>();
        
        // Yardımcı servisler
        services.AddScoped<DepolamaYardimciServis>();

        // HTTP client (virüs tarayıcı için)
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Konfigürasyon ayarlarını doğrular
    /// </summary>
    private class DepolamaAyarlariValidatoru : IValidateOptions<DepolamaAyarlari>
    {
        public ValidateOptionsResult Validate(string? name, DepolamaAyarlari ayarlar)
        {
            var hatalar = new List<string>();

            // MinIO ayarları kontrolü
            if (string.IsNullOrEmpty(ayarlar.MinIO.Endpoint))
                hatalar.Add("MinIO Endpoint boş olamaz");

            if (string.IsNullOrEmpty(ayarlar.MinIO.AccessKey))
                hatalar.Add("MinIO Access Key boş olamaz");

            if (string.IsNullOrEmpty(ayarlar.MinIO.SecretKey))
                hatalar.Add("MinIO Secret Key boş olamaz");

            // Bucket adları kontrolü
            if (string.IsNullOrEmpty(ayarlar.Buckets.Documents))
                hatalar.Add("Documents bucket adı boş olamaz");

            if (string.IsNullOrEmpty(ayarlar.Buckets.Images))
                hatalar.Add("Images bucket adı boş olamaz");

            if (string.IsNullOrEmpty(ayarlar.Buckets.UserUploads))
                hatalar.Add("UserUploads bucket adı boş olamaz");

            if (string.IsNullOrEmpty(ayarlar.Buckets.Backups))
                hatalar.Add("Backups bucket adı boş olamaz");

            // Güvenlik ayarları kontrolü
            if (ayarlar.Security.MaxFileSize <= 0)
                hatalar.Add("Maksimum dosya boyutu pozitif olmalıdır");

            if (!ayarlar.Security.AllowedExtensions.Any())
                hatalar.Add("En az bir dosya uzantısı izin verilmelidir");

            // Resim işleme ayarları kontrolü
            if (ayarlar.ImageProcessing.Quality < 1 || ayarlar.ImageProcessing.Quality > 100)
                hatalar.Add("Resim kalitesi 1-100 arasında olmalıdır");

            if (ayarlar.ImageProcessing.MaxWidth <= 0 || ayarlar.ImageProcessing.MaxHeight <= 0)
                hatalar.Add("Maksimum resim boyutları pozitif olmalıdır");

            // Virüs tarayıcı ayarları kontrolü (eğer etkinse)
            if (ayarlar.Security.VirusScanEnabled)
            {
                if (string.IsNullOrEmpty(ayarlar.VirusScanner.ScanEndpoint))
                    hatalar.Add("Virüs tarayıcı endpoint'i boş olamaz");

                if (ayarlar.VirusScanner.ScanTimeoutSeconds <= 0)
                    hatalar.Add("Virüs tarayıcı timeout süresi pozitif olmalıdır");
            }

            if (hatalar.Any())
            {
                var hataMesaji = $"Enterprise Storage konfigürasyon hataları: {string.Join(", ", hatalar)}";
                return ValidateOptionsResult.Fail(hataMesaji);
            }

            return ValidateOptionsResult.Success;
        }
    }
}

/// <summary>
/// Konfigürasyon extension metodları
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Storage ayarlarını konfigürasyondan alır
    /// </summary>
    public static DepolamaAyarlari GetStorageSettings(this IConfiguration configuration)
    {
        var section = configuration.GetSection(DepolamaAyarlari.ConfigSection);
        var ayarlar = section.Get<DepolamaAyarlari>() ?? new DepolamaAyarlari();
        
        // Validasyon
        var context = new ValidationContext(ayarlar);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(ayarlar, context, validationResults, true))
        {
            var hatalar = validationResults.Select(vr => vr.ErrorMessage).ToList();
            throw new InvalidOperationException($"Storage konfigürasyonu geçersiz: {string.Join(", ", hatalar)}");
        }
        
        return ayarlar;
    }

    /// <summary>
    /// Storage ayarları konfigürasyon bölümünün var olup olmadığını kontrol eder
    /// </summary>
    public static bool HasStorageSettings(this IConfiguration configuration)
    {
        return configuration.GetSection(DepolamaAyarlari.ConfigSection).Exists();
    }

    /// <summary>
    /// MinIO bağlantı stringini oluşturur
    /// </summary>
    public static string GetMinIOConnectionString(this IConfiguration configuration)
    {
        var ayarlar = configuration.GetStorageSettings();
        var protocol = ayarlar.MinIO.UseSSL ? "https" : "http";
        
        return $"{protocol}://{ayarlar.MinIO.AccessKey}:{ayarlar.MinIO.SecretKey}@{ayarlar.MinIO.Endpoint}";
    }
}

/// <summary>
/// Depolama servisi factory
/// </summary>
public class DepolamaServisiFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DepolamaServisiFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Depolama servisini oluşturur
    /// </summary>
    public IDepolamaServisi CreateStorageService(string? providerName = null)
    {
        // Şu anda sadece MinIO destekleniyor
        // Gelecekte AWS S3, Azure Blob Storage vs. eklenebilir
        
        return providerName?.ToLowerInvariant() switch
        {
            "minio" or null => _serviceProvider.GetRequiredService<IDepolamaServisi>(),
            _ => throw new NotSupportedException($"Desteklenmeyen depolama sağlayıcısı: {providerName}")
        };
    }

    /// <summary>
    /// Resim işleme servisini oluşturur
    /// </summary>
    public IResimIslemeServisi CreateImageProcessingService()
    {
        return _serviceProvider.GetRequiredService<IResimIslemeServisi>();
    }

    /// <summary>
    /// Virüs tarayıcı servisini oluşturur
    /// </summary>
    public IVirusTarayiciServisi? CreateVirusScannerService()
    {
        return _serviceProvider.GetService<IVirusTarayiciServisi>();
    }

    /// <summary>
    /// Yedekleme servisini oluşturur
    /// </summary>
    public IYedeklemeServisi CreateBackupService()
    {
        return _serviceProvider.GetRequiredService<IYedeklemeServisi>();
    }
}