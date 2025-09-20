using Enterprise.Shared.Storage.Models;

namespace Enterprise.Shared.Storage.Interfaces;

/// <summary>
/// Ana depolama servisi arayüzü
/// </summary>
public interface IDepolamaServisi
{
    /// <summary>
    /// Dosya yükler
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="icerikTuru">İçerik türü</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yüklenen dosyanın adı</returns>
    Task<string> DosyaYukleAsync(string bucketAdi, string nesneAdi,
        Stream dosyaStream, string icerikTuru, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya yükler (metadata ile)
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="icerikTuru">İçerik türü</param>
    /// <param name="metadata">Metadata bilgileri</param>
    /// <param name="etiketler">Etiket listesi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yüklenen dosyanın adı</returns>
    Task<string> DosyaYukleAsync(string bucketAdi, string nesneAdi,
        Stream dosyaStream, string icerikTuru, Dictionary<string, string>? metadata,
        Dictionary<string, string>? etiketler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya indirir
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dosya stream'i</returns>
    Task<Stream> DosyaIndirAsync(string bucketAdi, string nesneAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya siler
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silme işlemi başarılı mı</returns>
    Task<bool> DosyaSilAsync(string bucketAdi, string nesneAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ön imzalı URL alır
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="gecerlilikSuresi">Geçerlilik süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Ön imzalı URL</returns>
    Task<string> OnImzaliUrlAlAsync(string bucketAdi, string nesneAdi,
        TimeSpan gecerlilikSuresi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya metadata bilgilerini alır
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dosya metadata bilgileri</returns>
    Task<DosyaMetadata> DosyaMetadataAlAsync(string bucketAdi, string nesneAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyaları listeler
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="onEk">Ön ek filtresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dosya listesi</returns>
    Task<IEnumerable<DosyaBilgisi>> DosyalariListeleAsync(string bucketAdi, string onEk = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya var mı kontrol eder
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="nesneAdi">Nesne adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dosya var mı</returns>
    Task<bool> DosyaVarMiAsync(string bucketAdi, string nesneAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyayı kopyalar
    /// </summary>
    /// <param name="kaynakBucket">Kaynak bucket</param>
    /// <param name="kaynakNesne">Kaynak nesne</param>
    /// <param name="hedefBucket">Hedef bucket</param>
    /// <param name="hedefNesne">Hedef nesne</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kopyalama başarılı mı</returns>
    Task<bool> DosyaKopyalaAsync(string kaynakBucket, string kaynakNesne,
        string hedefBucket, string hedefNesne, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyayı taşır
    /// </summary>
    /// <param name="kaynakBucket">Kaynak bucket</param>
    /// <param name="kaynakNesne">Kaynak nesne</param>
    /// <param name="hedefBucket">Hedef bucket</param>
    /// <param name="hedefNesne">Hedef nesne</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Taşıma başarılı mı</returns>
    Task<bool> DosyaTasiAsync(string kaynakBucket, string kaynakNesne,
        string hedefBucket, string hedefNesne, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bucket oluşturur
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Oluşturma başarılı mı</returns>
    Task<bool> BucketOlusturAsync(string bucketAdi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bucket var mı kontrol eder
    /// </summary>
    /// <param name="bucketAdi">Bucket adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Bucket var mı</returns>
    Task<bool> BucketVarMiAsync(string bucketAdi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bucket'ları listeler
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Bucket listesi</returns>
    Task<IEnumerable<string>> BucketlariListeleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Depolama sağlık kontrolü yapar
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Sağlık kontrol sonucu</returns>
    Task<DepolamaSaglikKontrolu> SaglikKontroluAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resim işleme servisi arayüzü
/// </summary>
public interface IResimIslemeServisi
{
    /// <summary>
    /// Resim işler (boyutlandırma, küçük resim oluşturma)
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="dosyaAdi">Dosya adı</param>
    /// <param name="secenekler">İşleme seçenekleri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İşlenmiş resim sonucu</returns>
    Task<IslenmisResimSonucu> ResimIsleAsync(Stream resimStream, string dosyaAdi,
        ResimIslemeSecenekleri secenekler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resim boyutlandırır
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="genislik">Yeni genişlik</param>
    /// <param name="yukseklik">Yeni yükseklik</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Boyutlandırılmış resim stream'i</returns>
    Task<Stream> ResimBoyutlandirAsync(Stream resimStream, int genislik, int yukseklik,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Küçük resim oluşturur
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="boyut">Küçük resim boyutu</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Küçük resim stream'i</returns>
    Task<Stream> KucukResimOlusturAsync(Stream resimStream, int boyut,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resim metadata bilgilerini alır
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Resim metadata bilgileri</returns>
    Task<ResimMetadata> ResimMetadataAlAsync(Stream resimStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resim formatını değiştirir
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="hedefFormat">Hedef format (JPEG, PNG, WebP, GIF)</param>
    /// <param name="kalite">Kalite (1-100)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dönüştürülmüş resim stream'i</returns>
    Task<Stream> ResimFormatiniDegistirAsync(Stream resimStream, string hedefFormat,
        int kalite = 85, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resme watermark ekler
    /// </summary>
    /// <param name="resimStream">Resim stream'i</param>
    /// <param name="watermarkMetni">Watermark metni</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Watermark'lı resim stream'i</returns>
    Task<Stream> WatermarkEkleAsync(Stream resimStream, string watermarkMetni,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resim dosyası mı kontrol eder
    /// </summary>
    /// <param name="icerikTuru">İçerik türü</param>
    /// <returns>Resim dosyası mı</returns>
    bool ResimDosyasiMi(string icerikTuru);

    /// <summary>
    /// Desteklenen resim formatları
    /// </summary>
    List<string> DesteklenenFormatlar { get; }
}

/// <summary>
/// Virüs tarayıcı servisi arayüzü
/// </summary>
public interface IVirusTarayiciServisi
{
    /// <summary>
    /// Dosyayı virüse karşı tarar
    /// </summary>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Tarama sonucu</returns>
    Task<VirusTaramaSonucu> TaraAsync(Stream dosyaStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyanın temiz olup olmadığını kontrol eder
    /// </summary>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dosya temiz mi</returns>
    Task<bool> TemizMiAsync(Stream dosyaStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tarayıcının sağlık durumunu kontrol eder
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Tarayıcı sağlıklı mı</returns>
    Task<bool> SaglikliMiAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Virüs imza veritabanının son güncellenme tarihini alır
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Son güncelleme tarihi</returns>
    Task<DateTime?> SonGuncellemeTarihiAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Yedekleme servisi arayüzü
/// </summary>
public interface IYedeklemeServisi
{
    /// <summary>
    /// Yedek oluşturur
    /// </summary>
    /// <param name="veritabaniAdi">Veritabanı adı</param>
    /// <param name="yedekStream">Yedek stream'i</param>
    /// <param name="yedekTipi">Yedek tipi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yedek dosya adı</returns>
    Task<string> YedekOlusturAsync(string veritabaniAdi, Stream yedekStream,
        YedekTipi yedekTipi = YedekTipi.TamYedek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yedekleri listeler
    /// </summary>
    /// <param name="veritabaniAdi">Veritabanı adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yedek listesi</returns>
    Task<IEnumerable<YedekBilgisi>> YedekleriListeleAsync(string veritabaniAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yedek geri yükler
    /// </summary>
    /// <param name="yedekDosyaAdi">Yedek dosya adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yedek stream'i</returns>
    Task<Stream> YedekGeriYukleAsync(string yedekDosyaAdi,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Eski yedekleri temizler
    /// </summary>
    /// <param name="veritabaniAdi">Veritabanı adı</param>
    /// <param name="tutulacakYedekSayisi">Tutulacak yedek sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silinen yedek sayısı</returns>
    Task<int> EskiYedekleriTemizleAsync(string veritabaniAdi, int tutulacakYedekSayisi = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Dosya validasyonu servisi arayüzü
/// </summary>
public interface IDosyaValidasyonServisi
{
    /// <summary>
    /// Dosyayı validate eder
    /// </summary>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="dosyaAdi">Dosya adı</param>
    /// <param name="icerikTuru">İçerik türü</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateAsync(Stream dosyaStream, string dosyaAdi, string icerikTuru,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya uzantısını validate eder
    /// </summary>
    /// <param name="dosyaAdi">Dosya adı</param>
    /// <returns>Uzantı geçerli mi</returns>
    bool DosyaUzantisiGecerliMi(string dosyaAdi);

    /// <summary>
    /// Dosya boyutunu validate eder
    /// </summary>
    /// <param name="dosyaBoyutu">Dosya boyutu</param>
    /// <returns>Boyut geçerli mi</returns>
    bool DosyaBoyutuGecerliMi(long dosyaBoyutu);

    /// <summary>
    /// MIME tipini validate eder
    /// </summary>
    /// <param name="icerikTuru">İçerik türü</param>
    /// <returns>MIME tipi geçerli mi</returns>
    bool MimeTipiGecerliMi(string icerikTuru);

    /// <summary>
    /// Dosya imzasını validate eder
    /// </summary>
    /// <param name="dosyaStream">Dosya stream'i</param>
    /// <param name="beklenenTip">Beklenen dosya tipi</param>
    /// <returns>İmza geçerli mi</returns>
    Task<bool> DosyaImzasiGecerliMiAsync(Stream dosyaStream, string beklenenTip);
}