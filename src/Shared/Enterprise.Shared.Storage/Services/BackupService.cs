using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Enterprise.Shared.Storage.Services;

/// <summary>
/// Yedekleme servisi implementasyonu
/// </summary>
public class YedeklemeServisi : IYedeklemeServisi
{
    private readonly IDepolamaServisi _depolamaServisi;
    private readonly BucketAyarlari _bucketAyarlari;
    private readonly ILogger<YedeklemeServisi> _logger;

    public YedeklemeServisi(
        IDepolamaServisi depolamaServisi,
        IOptions<DepolamaAyarlari> ayarlar,
        ILogger<YedeklemeServisi> logger)
    {
        _depolamaServisi = depolamaServisi ?? throw new ArgumentNullException(nameof(depolamaServisi));
        _bucketAyarlari = ayarlar.Value.Buckets ?? throw new ArgumentNullException(nameof(ayarlar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> YedekOlusturAsync(string veritabaniAdi, Stream yedekStream,
        YedekTipi yedekTipi = YedekTipi.TamYedek, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Yedek oluşturma başlatılıyor: {VeritabaniAdi}, Tip: {YedekTipi}", 
                veritabaniAdi, yedekTipi);

            var suankiTarih = TurkiyeSaatiExtensions.SuankiTurkiyeSaati();
            var yedekDosyaAdi = YedekDosyaAdiOlustur(veritabaniAdi, yedekTipi, suankiTarih);

            var metadata = new Dictionary<string, string>
            {
                ["database-name"] = veritabaniAdi,
                ["backup-type"] = yedekTipi.ToString(),
                ["creation-date"] = suankiTarih.ToString("yyyy-MM-dd HH:mm:ss"),
                ["created-by"] = Environment.UserName,
                ["backup-version"] = "1.0"
            };

            var etiketler = new Dictionary<string, string>
            {
                ["type"] = "database-backup",
                ["database"] = veritabaniAdi,
                ["backup-type"] = yedekTipi.ToString().ToLowerInvariant()
            };

            var uploadedFile = await _depolamaServisi.DosyaYukleAsync(
                _bucketAyarlari.Backups,
                yedekDosyaAdi,
                yedekStream,
                "application/octet-stream",
                metadata,
                etiketler,
                cancellationToken);

            _logger.LogInformation("Yedek başarıyla oluşturuldu: {YedekDosyaAdi}, Boyut: {Boyut} bytes", 
                uploadedFile, yedekStream.Length);

            return uploadedFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedek oluşturma hatası: {VeritabaniAdi}, Tip: {YedekTipi}", veritabaniAdi, yedekTipi);
            throw;
        }
    }

    public async Task<IEnumerable<YedekBilgisi>> YedekleriListeleAsync(string veritabaniAdi,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Yedekler listeleniyor: {VeritabaniAdi}", veritabaniAdi);

            var onEk = $"database-backups/{veritabaniAdi}/";
            var dosyalar = await _depolamaServisi.DosyalariListeleAsync(_bucketAyarlari.Backups, onEk, cancellationToken);

            var yedekBilgileri = new List<YedekBilgisi>();

            foreach (var dosya in dosyalar.Where(d => !d.KlasorMu))
            {
                try
                {
                    var metadata = await _depolamaServisi.DosyaMetadataAlAsync(_bucketAyarlari.Backups, dosya.Ad, cancellationToken);
                    
                    var yedekBilgisi = new YedekBilgisi
                    {
                        DosyaAdi = dosya.Ad,
                        Boyut = dosya.Boyut,
                        OlusturulmaTarihi = dosya.SonDegistirilmeTarihi,
                        VeritabaniAdi = veritabaniAdi,
                        Tip = YedekTipiniCozumle(metadata)
                    };

                    yedekBilgileri.Add(yedekBilgisi);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Yedek metadata'sı alınamadı: {DosyaAdi}", dosya.Ad);
                }
            }

            // Tarihe göre sırala (en yeni önce)
            var siraliYedekler = yedekBilgileri.OrderByDescending(y => y.OlusturulmaTarihi).ToList();

            _logger.LogDebug("Yedekler listelendi: {VeritabaniAdi}, Toplam: {Toplam}", 
                veritabaniAdi, siraliYedekler.Count);

            return siraliYedekler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedek listeleme hatası: {VeritabaniAdi}", veritabaniAdi);
            throw;
        }
    }

    public async Task<Stream> YedekGeriYukleAsync(string yedekDosyaAdi,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Yedek geri yükleniyor: {YedekDosyaAdi}", yedekDosyaAdi);

            var yedekStream = await _depolamaServisi.DosyaIndirAsync(_bucketAyarlari.Backups, yedekDosyaAdi, cancellationToken);

            _logger.LogInformation("Yedek başarıyla indirildi: {YedekDosyaAdi}", yedekDosyaAdi);
            return yedekStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yedek geri yükleme hatası: {YedekDosyaAdi}", yedekDosyaAdi);
            throw;
        }
    }

    public async Task<int> EskiYedekleriTemizleAsync(string veritabaniAdi, int tutulacakYedekSayisi = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Eski yedekler temizleniyor: {VeritabaniAdi}, Tutulacak: {TutulacakSayi}", 
                veritabaniAdi, tutulacakYedekSayisi);

            var tumYedekler = await YedekleriListeleAsync(veritabaniAdi, cancellationToken);
            var eskiYedekler = tumYedekler.Skip(tutulacakYedekSayisi).ToList();

            var silinenSayisi = 0;

            foreach (var eskiYedek in eskiYedekler)
            {
                try
                {
                    var silmeBasarili = await _depolamaServisi.DosyaSilAsync(_bucketAyarlari.Backups, eskiYedek.DosyaAdi, cancellationToken);
                    
                    if (silmeBasarili)
                    {
                        silinenSayisi++;
                        _logger.LogDebug("Eski yedek silindi: {DosyaAdi}", eskiYedek.DosyaAdi);
                    }
                    else
                    {
                        _logger.LogWarning("Eski yedek silinemedi: {DosyaAdi}", eskiYedek.DosyaAdi);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eski yedek silme hatası: {DosyaAdi}", eskiYedek.DosyaAdi);
                }
            }

            _logger.LogInformation("Eski yedek temizleme tamamlandı: {VeritabaniAdi}, Silinen: {SilinenSayisi}/{ToplamSayisi}", 
                veritabaniAdi, silinenSayisi, eskiYedekler.Count);

            return silinenSayisi;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eski yedek temizleme hatası: {VeritabaniAdi}", veritabaniAdi);
            throw;
        }
    }

    #region Private Methods

    private string YedekDosyaAdiOlustur(string veritabaniAdi, YedekTipi yedekTipi, DateTime tarih)
    {
        var tipKisaltma = yedekTipi switch
        {
            YedekTipi.TamYedek => "full",
            YedekTipi.DifferansiyalYedek => "diff",
            YedekTipi.TransaksiyonLogYedek => "log",
            _ => "backup"
        };

        return $"database-backups/{veritabaniAdi}/{tarih:yyyy-MM-dd-HH-mm-ss}_{tipKisaltma}.bak";
    }

    private YedekTipi YedekTipiniCozumle(DosyaMetadata metadata)
    {
        if (metadata.KullaniciMetadata.TryGetValue("backup-type", out var tipStr) &&
            Enum.TryParse<YedekTipi>(tipStr, out var tip))
        {
            return tip;
        }

        // Dosya adından tip çıkarmaya çalış
        if (metadata.DosyaAdi.Contains("_full"))
            return YedekTipi.TamYedek;
        else if (metadata.DosyaAdi.Contains("_diff"))
            return YedekTipi.DifferansiyalYedek;
        else if (metadata.DosyaAdi.Contains("_log"))
            return YedekTipi.TransaksiyonLogYedek;

        return YedekTipi.TamYedek; // Varsayılan
    }

    #endregion
}

/// <summary>
/// Depolama yardımcı servisleri
/// </summary>
public class DepolamaYardimciServis
{
    private readonly IDepolamaServisi _depolamaServisi;
    private readonly ILogger<DepolamaYardimciServis> _logger;

    public DepolamaYardimciServis(
        IDepolamaServisi depolamaServisi,
        ILogger<DepolamaYardimciServis> logger)
    {
        _depolamaServisi = depolamaServisi ?? throw new ArgumentNullException(nameof(depolamaServisi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dosya türüne göre uygun bucket adını döndürür
    /// </summary>
    public string DosyaTipineGoreBucketAl(string icerikTuru, BucketAyarlari bucketAyarlari)
    {
        return icerikTuru.ToLowerInvariant() switch
        {
            var tip when tip.StartsWith("image/") => bucketAyarlari.Images,
            "application/pdf" => bucketAyarlari.Documents,
            var tip when tip.Contains("document") || tip.Contains("spreadsheet") || tip.Contains("presentation") => bucketAyarlari.Documents,
            _ => bucketAyarlari.UserUploads
        };
    }

    /// <summary>
    /// Benzersiz nesne adı oluşturur
    /// </summary>
    public string BenzersizNesneAdiOlustur(string orijinalDosyaAdi, string? klasorYolu = null)
    {
        var guid = Guid.NewGuid().ToString("N")[..8]; // İlk 8 karakter
        var tarih = TurkiyeSaatiExtensions.SuankiTurkiyeSaati().ToString("yyyy/MM/dd");
        var dosyaAdi = Path.GetFileNameWithoutExtension(orijinalDosyaAdi);
        var uzanti = Path.GetExtension(orijinalDosyaAdi);
        
        // Güvenli dosya adı oluştur
        dosyaAdi = GuvenliDosyaAdiOlustur(dosyaAdi);
        
        var nesneAdi = string.IsNullOrEmpty(klasorYolu)
            ? $"{tarih}/{guid}_{dosyaAdi}{uzanti}"
            : $"{klasorYolu.Trim('/')}/{tarih}/{guid}_{dosyaAdi}{uzanti}";

        return nesneAdi;
    }

    /// <summary>
    /// Dosya adından güvenli karakter kullanarak temizler
    /// </summary>
    public string GuvenliDosyaAdiOlustur(string dosyaAdi)
    {
        if (string.IsNullOrEmpty(dosyaAdi))
            return "file";

        // Türkçe karakterleri değiştir
        var turkceKarakterler = new Dictionary<char, char>
        {
            ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u',
            ['Ç'] = 'C', ['Ğ'] = 'G', ['İ'] = 'I', ['Ö'] = 'O', ['Ş'] = 'S', ['Ü'] = 'U'
        };

        var temizDosyaAdi = new StringBuilder();
        
        foreach (var karakter in dosyaAdi)
        {
            if (turkceKarakterler.TryGetValue(karakter, out var yeniKarakter))
            {
                temizDosyaAdi.Append(yeniKarakter);
            }
            else if (char.IsLetterOrDigit(karakter) || karakter == '-' || karakter == '_')
            {
                temizDosyaAdi.Append(karakter);
            }
            else if (char.IsWhiteSpace(karakter))
            {
                temizDosyaAdi.Append('_');
            }
        }

        var sonuc = temizDosyaAdi.ToString().Trim('_');
        return string.IsNullOrEmpty(sonuc) ? "file" : sonuc;
    }

    /// <summary>
    /// Dosya boyutunu okunabilir formata çevirir
    /// </summary>
    public string DosyaBoyutunuFormatla(long bytes)
    {
        string[] birimler = { "B", "KB", "MB", "GB", "TB" };
        double boyut = bytes;
        int birimIndex = 0;

        while (boyut >= 1024 && birimIndex < birimler.Length - 1)
        {
            boyut /= 1024;
            birimIndex++;
        }

        return $"{boyut:F2} {birimler[birimIndex]}";
    }

    /// <summary>
    /// Geçici URL oluşturur
    /// </summary>
    public async Task<string> GeciciUrlOlusturAsync(string bucketAdi, string nesneAdi, 
        TimeSpan? gecerlilikSuresi = null, CancellationToken cancellationToken = default)
    {
        var sure = gecerlilikSuresi ?? TimeSpan.FromHours(1);
        return await _depolamaServisi.OnImzaliUrlAlAsync(bucketAdi, nesneAdi, sure, cancellationToken);
    }

    /// <summary>
    /// Toplu dosya silme işlemi
    /// </summary>
    public async Task<int> TopluDosyaSilAsync(string bucketAdi, IEnumerable<string> nesneAdlari,
        CancellationToken cancellationToken = default)
    {
        var silinenSayisi = 0;
        var tasks = new List<Task<bool>>();

        foreach (var nesneAdi in nesneAdlari)
        {
            tasks.Add(_depolamaServisi.DosyaSilAsync(bucketAdi, nesneAdi, cancellationToken));
        }

        var sonuclar = await Task.WhenAll(tasks);
        silinenSayisi = sonuclar.Count(s => s);

        _logger.LogInformation("Toplu dosya silme tamamlandı: {BucketAdi}, Silinen: {SilinenSayisi}/{ToplamSayisi}",
            bucketAdi, silinenSayisi, nesneAdlari.Count());

        return silinenSayisi;
    }
}