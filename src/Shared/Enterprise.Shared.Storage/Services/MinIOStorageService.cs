using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Reactive.Linq;

namespace Enterprise.Shared.Storage.Services;

/// <summary>
/// MinIO tabanlı depolama servisi implementasyonu
/// </summary>
public class MinIODepolamaServisi : IDepolamaServisi
{
    private readonly IMinioClient _minioClient;
    private readonly DepolamaAyarlari _ayarlar;
    private readonly ILogger<MinIODepolamaServisi> _logger;
    private readonly IVirusTarayiciServisi? _virusTarayiciServisi;
    private readonly IDosyaValidasyonServisi _validasyonServisi;

    public MinIODepolamaServisi(
        IMinioClient minioClient,
        IOptions<DepolamaAyarlari> ayarlar,
        ILogger<MinIODepolamaServisi> logger,
        IDosyaValidasyonServisi validasyonServisi,
        IVirusTarayiciServisi? virusTarayiciServisi = null)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _ayarlar = ayarlar.Value ?? throw new ArgumentNullException(nameof(ayarlar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validasyonServisi = validasyonServisi ?? throw new ArgumentNullException(nameof(validasyonServisi));
        _virusTarayiciServisi = virusTarayiciServisi;
    }

    public async Task<string> DosyaYukleAsync(string bucketAdi, string nesneAdi, Stream dosyaStream, 
        string icerikTuru, CancellationToken cancellationToken = default)
    {
        return await DosyaYukleAsync(bucketAdi, nesneAdi, dosyaStream, icerikTuru, null, null, cancellationToken);
    }

    public async Task<string> DosyaYukleAsync(string bucketAdi, string nesneAdi, Stream dosyaStream, 
        string icerikTuru, Dictionary<string, string>? metadata, Dictionary<string, string>? etiketler, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dosya yükleme başlatılıyor: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);

            // Bucket kontrolü ve oluşturma
            await BucketVarlikKontroluAsync(bucketAdi, cancellationToken);

            // Dosya validasyonu
            await DosyaValidasyonuAsync(dosyaStream, nesneAdi, icerikTuru, cancellationToken);

            // Virüs taraması
            if (_ayarlar.Security.VirusScanEnabled && _virusTarayiciServisi != null)
            {
                await VirusTaramasiAsync(dosyaStream, nesneAdi, cancellationToken);
            }

            dosyaStream.Position = 0;

            // MinIO'ya yükleme parametrelerini hazırla
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketAdi)
                .WithObject(nesneAdi)
                .WithStreamData(dosyaStream)
                .WithObjectSize(dosyaStream.Length)
                .WithContentType(icerikTuru);

            // Metadata ekleme
            if (metadata?.Any() == true)
            {
                foreach (var item in metadata)
                {
                    putObjectArgs.WithHeaders(new Dictionary<string, string> { { $"x-amz-meta-{item.Key}", item.Value } });
                }
            }

            // Etiket ekleme
            if (etiketler?.Any() == true)
            {
                var tagging = new Minio.DataModel.Tags.Tagging();
                foreach (var kvp in etiketler)
                {
                    tagging.Tags.Add(kvp.Key, kvp.Value);
                }
                putObjectArgs.WithTagging(tagging);
            }

            // Şifreleme ayarları (basitleştirildi)
            // if (_ayarlar.Security.EncryptionEnabled)
            // {
            //     // Şifreleme için daha detaylı konfigürasyon gerekir
            // }

            // Dosyayı yükle
            var response = await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation("Dosya başarıyla yüklendi: {BucketAdi}/{NesneAdi}, ETag: {ETag}", 
                bucketAdi, nesneAdi, response.Etag);

            return response.ObjectName;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya yükleme başarısız: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw new InvalidOperationException($"Dosya yükleme hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yükleme hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw;
        }
    }

    public async Task<Stream> DosyaIndirAsync(string bucketAdi, string nesneAdi, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dosya indirme başlatılıyor: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);

            var memoryStream = new MemoryStream();
            
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketAdi)
                .WithObject(nesneAdi)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            
            memoryStream.Position = 0;
            
            _logger.LogInformation("Dosya başarıyla indirildi: {BucketAdi}/{NesneAdi}, Boyut: {Boyut} bytes", 
                bucketAdi, nesneAdi, memoryStream.Length);

            return memoryStream;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya indirme başarısız: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw new FileNotFoundException($"Dosya bulunamadı: {bucketAdi}/{nesneAdi}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya indirme hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw;
        }
    }

    public async Task<bool> DosyaSilAsync(string bucketAdi, string nesneAdi, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dosya silme başlatılıyor: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketAdi)
                .WithObject(nesneAdi);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation("Dosya başarıyla silindi: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya silme başarısız: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya silme hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return false;
        }
    }

    public async Task<string> OnImzaliUrlAlAsync(string bucketAdi, string nesneAdi, TimeSpan gecerlilikSuresi, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Ön imzalı URL oluşturuluyor: {BucketAdi}/{NesneAdi}, Geçerlilik: {Gecerlilik}s", 
                bucketAdi, nesneAdi, gecerlilikSuresi.TotalSeconds);

            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketAdi)
                .WithObject(nesneAdi)
                .WithExpiry((int)gecerlilikSuresi.TotalSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogDebug("Ön imzalı URL oluşturuldu: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return url;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Ön imzalı URL oluşturulamadı: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw new InvalidOperationException($"Ön imzalı URL oluşturma hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ön imzalı URL oluşturma hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw;
        }
    }

    public async Task<DosyaMetadata> DosyaMetadataAlAsync(string bucketAdi, string nesneAdi, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Dosya metadata alınıyor: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketAdi)
                .WithObject(nesneAdi);

            var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            var metadata = new DosyaMetadata
            {
                DosyaAdi = objectStat.ObjectName,
                IcerikTuru = objectStat.ContentType,
                Boyut = objectStat.Size,
                SonDegistirilmeTarihi = objectStat.LastModified,
                ETag = objectStat.ETag
            };

            // Metadata ve etiketleri ekle
            if (objectStat.MetaData?.Any() == true)
            {
                foreach (var item in objectStat.MetaData)
                {
                    if (item.Key.StartsWith("x-amz-meta-"))
                    {
                        var key = item.Key.Substring("x-amz-meta-".Length);
                        metadata.KullaniciMetadata[key] = item.Value;
                    }
                }
            }

            _logger.LogDebug("Dosya metadata alındı: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return metadata;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya metadata alınamadı: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw new FileNotFoundException($"Dosya metadata'sı alınamadı: {bucketAdi}/{nesneAdi}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya metadata alma hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            throw;
        }
    }

    public async Task<IEnumerable<DosyaBilgisi>> DosyalariListeleAsync(string bucketAdi, string onEk = "", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Dosyalar listeleniyor: {BucketAdi}, OnEk: {OnEk}", bucketAdi, onEk);

            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketAdi)
                .WithPrefix(onEk)
                .WithRecursive(true);

            var dosyalar = new List<DosyaBilgisi>();

            var observable = _minioClient.ListObjectsAsync(listObjectsArgs, cancellationToken);
            var tcs = new TaskCompletionSource<bool>();
            
            observable.Subscribe(
                item =>
                {
                    dosyalar.Add(new DosyaBilgisi
                    {
                        Ad = item.Key,
                        Boyut = (long)item.Size,
                        SonDegistirilmeTarihi = item.LastModifiedDateTime ?? DateTime.UtcNow,
                        ETag = item.ETag ?? string.Empty,
                        KlasorMu = item.IsDir,
                        TamYol = $"{bucketAdi}/{item.Key}"
                    });
                },
                ex => tcs.SetException(ex),
                () => tcs.SetResult(true)
            );
            
            await tcs.Task;

            _logger.LogDebug("Dosyalar listelendi: {BucketAdi}, Toplam: {Toplam}", bucketAdi, dosyalar.Count);
            return dosyalar;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya listesi alınamadı: {BucketAdi}", bucketAdi);
            throw new InvalidOperationException($"Dosya listesi alma hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya listeleme hatası: {BucketAdi}", bucketAdi);
            throw;
        }
    }

    public async Task<bool> DosyaVarMiAsync(string bucketAdi, string nesneAdi, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await DosyaMetadataAlAsync(bucketAdi, nesneAdi, cancellationToken);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya varlık kontrolü hatası: {BucketAdi}/{NesneAdi}", bucketAdi, nesneAdi);
            return false;
        }
    }

    public async Task<bool> DosyaKopyalaAsync(string kaynakBucket, string kaynakNesne, string hedefBucket, 
        string hedefNesne, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dosya kopyalama başlatılıyor: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);

            await BucketVarlikKontroluAsync(hedefBucket, cancellationToken);

            var copyObjectArgs = new CopyObjectArgs()
                .WithBucket(hedefBucket)
                .WithObject(hedefNesne)
                .WithCopyObjectSource(new CopySourceObjectArgs()
                    .WithBucket(kaynakBucket)
                    .WithObject(kaynakNesne));

            await _minioClient.CopyObjectAsync(copyObjectArgs, cancellationToken);

            _logger.LogInformation("Dosya başarıyla kopyalandı: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);

            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Dosya kopyalama başarısız: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya kopyalama hatası: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);
            return false;
        }
    }

    public async Task<bool> DosyaTasiAsync(string kaynakBucket, string kaynakNesne, string hedefBucket, 
        string hedefNesne, CancellationToken cancellationToken = default)
    {
        try
        {
            // Önce kopyala
            var kopyalamaBasarili = await DosyaKopyalaAsync(kaynakBucket, kaynakNesne, hedefBucket, hedefNesne, cancellationToken);
            
            if (!kopyalamaBasarili)
            {
                return false;
            }

            // Sonra orijinali sil
            var silmeBasarili = await DosyaSilAsync(kaynakBucket, kaynakNesne, cancellationToken);
            
            if (!silmeBasarili)
            {
                _logger.LogWarning("Dosya taşımada kaynak silinemedi: {KaynakBucket}/{KaynakNesne}", kaynakBucket, kaynakNesne);
                // Hedefi sil çünkü taşıma tamamlanamadı
                await DosyaSilAsync(hedefBucket, hedefNesne, cancellationToken);
                return false;
            }

            _logger.LogInformation("Dosya başarıyla taşındı: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya taşıma hatası: {KaynakBucket}/{KaynakNesne} -> {HedefBucket}/{HedefNesne}", 
                kaynakBucket, kaynakNesne, hedefBucket, hedefNesne);
            return false;
        }
    }

    public async Task<bool> BucketOlusturAsync(string bucketAdi, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bucket oluşturuluyor: {BucketAdi}", bucketAdi);

            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(bucketAdi)
                .WithLocation(_ayarlar.MinIO.Region);

            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

            _logger.LogInformation("Bucket başarıyla oluşturuldu: {BucketAdi}", bucketAdi);
            return true;
        }
        catch (MinioException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogDebug("Bucket zaten mevcut: {BucketAdi}", bucketAdi);
            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Bucket oluşturulamadı: {BucketAdi}", bucketAdi);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bucket oluşturma hatası: {BucketAdi}", bucketAdi);
            return false;
        }
    }

    public async Task<bool> BucketVarMiAsync(string bucketAdi, CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketAdi);

            return await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bucket varlık kontrolü hatası: {BucketAdi}", bucketAdi);
            return false;
        }
    }

    public async Task<IEnumerable<string>> BucketlariListeleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bucket'lar listeleniyor");

            var buckets = await _minioClient.ListBucketsAsync(cancellationToken);
            var bucketAdlari = buckets.Buckets.Select(b => b.Name).ToList();

            _logger.LogDebug("Bucket'lar listelendi: Toplam {Toplam}", bucketAdlari.Count);
            return bucketAdlari;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO hatası - Bucket listesi alınamadı");
            throw new InvalidOperationException($"Bucket listesi alma hatası: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bucket listeleme hatası");
            throw;
        }
    }

    public async Task<DepolamaSaglikKontrolu> SaglikKontroluAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Depolama sağlık kontrolü başlatılıyor");

            var buckets = await BucketlariListeleAsync(cancellationToken);
            var bucketSayisi = buckets.Count();

            var saglikKontrolu = new DepolamaSaglikKontrolu
            {
                SaglikliMi = true,
                BucketSayisi = bucketSayisi,
                SonKontrolTarihi = DateTime.UtcNow
            };

            saglikKontrolu.EkBilgiler["MinIOEndpoint"] = _ayarlar.MinIO.Endpoint;
            saglikKontrolu.EkBilgiler["UseSSL"] = _ayarlar.MinIO.UseSSL;
            saglikKontrolu.EkBilgiler["Region"] = _ayarlar.MinIO.Region;

            _logger.LogDebug("Depolama sağlık kontrolü tamamlandı: Sağlıklı={Saglikli}, BucketSayisi={BucketSayisi}", 
                saglikKontrolu.SaglikliMi, bucketSayisi);

            return saglikKontrolu;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Depolama sağlık kontrolü hatası");

            return new DepolamaSaglikKontrolu
            {
                SaglikliMi = false,
                SonKontrolTarihi = DateTime.UtcNow,
                Hata = ex.Message
            };
        }
    }

    #region Private Methods

    private async Task BucketVarlikKontroluAsync(string bucketAdi, CancellationToken cancellationToken)
    {
        var bucketVar = await BucketVarMiAsync(bucketAdi, cancellationToken);
        if (!bucketVar)
        {
            var bucketOlusturuldu = await BucketOlusturAsync(bucketAdi, cancellationToken);
            if (!bucketOlusturuldu)
            {
                throw new InvalidOperationException($"Bucket oluşturulamadı: {bucketAdi}");
            }
        }
    }

    private async Task DosyaValidasyonuAsync(Stream dosyaStream, string dosyaAdi, string icerikTuru, 
        CancellationToken cancellationToken)
    {
        var validasyonSonucu = await _validasyonServisi.ValidateAsync(dosyaStream, dosyaAdi, icerikTuru, cancellationToken);
        
        if (!validasyonSonucu.IsValid)
        {
            var hataMesaji = string.Join(", ", validasyonSonucu.ErrorMessages);
            throw new InvalidOperationException($"Dosya validasyonu başarısız: {hataMesaji}");
        }

        if (validasyonSonucu.WarningMessages.Any())
        {
            _logger.LogWarning("Dosya validasyon uyarıları: {Uyarilar}", 
                string.Join(", ", validasyonSonucu.WarningMessages));
        }
    }

    private async Task VirusTaramasiAsync(Stream dosyaStream, string dosyaAdi, CancellationToken cancellationToken)
    {
        dosyaStream.Position = 0;
        var taramaSonucu = await _virusTarayiciServisi!.TaraAsync(dosyaStream, cancellationToken);

        if (!taramaSonucu.TemizMi)
        {
            _logger.LogWarning("Dosyada virüs tespit edildi: {DosyaAdi}, Sonuç: {Sonuc}", 
                dosyaAdi, taramaSonucu.TaramaSonucu);
            throw new InvalidOperationException($"Dosyada güvenlik tehdidi tespit edildi: {taramaSonucu.TaramaSonucu}");
        }

        _logger.LogDebug("Virüs taraması başarılı: {DosyaAdi}", dosyaAdi);
    }

    #endregion
}