using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Storage.Models;

/// <summary>
/// Dosya metadata bilgilerini içeren model
/// </summary>
public class DosyaMetadata
{
    public string DosyaAdi { get; set; } = string.Empty;
    public string IcerikTuru { get; set; } = string.Empty;
    public long Boyut { get; set; }
    public DateTime SonDegistirilmeTarihi { get; set; }
    public string ETag { get; set; } = string.Empty;
    public Dictionary<string, string> Etiketler { get; set; } = new();
    public Dictionary<string, string> KullaniciMetadata { get; set; } = new();
    
    /// <summary>
    /// Türkiye saatine göre son değiştirilme tarihi
    /// </summary>
    public DateTime TurkiyeSaatiSonDegistirilme => SonDegistirilmeTarihi.ToTurkeyTime();
}

/// <summary>
/// Dosya bilgilerini içeren model
/// </summary>
public class DosyaBilgisi
{
    public string Ad { get; set; } = string.Empty;
    public long Boyut { get; set; }
    public DateTime SonDegistirilmeTarihi { get; set; }
    public string ETag { get; set; } = string.Empty;
    public bool KlasorMu { get; set; }
    public string TamYol { get; set; } = string.Empty;
    
    /// <summary>
    /// Türkiye saatine göre son değiştirilme tarihi
    /// </summary>
    public DateTime TurkiyeSaatiSonDegistirilme => SonDegistirilmeTarihi.ToTurkeyTime();
}

/// <summary>
/// Dosya yükleme isteği modeli
/// </summary>
public class DosyaYuklemeIstegi
{
    [Required(ErrorMessage = "Dosya stream'i gereklidir")]
    public Stream DosyaStream { get; set; } = null!;
    
    [Required(ErrorMessage = "Dosya adı gereklidir")]
    public string DosyaAdi { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "İçerik türü gereklidir")]
    public string IcerikTuru { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Etiketler { get; set; } = new();
    
    [Required(ErrorMessage = "Bucket adı gereklidir")]
    public string BucketAdi { get; set; } = string.Empty;
}

/// <summary>
/// Dosya yükleme yanıt modeli
/// </summary>
public class DosyaYuklemeYaniti
{
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaUrl { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public string IcerikTuru { get; set; } = string.Empty;
    public List<KucukResimBilgisi> KucukResimler { get; set; } = new();
    public DateTime YuklemeTarihi { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Türkiye saatine göre yükleme tarihi
    /// </summary>
    public DateTime TurkiyeSaatiYuklemeTarihi => YuklemeTarihi.ToTurkeyTime();
}

/// <summary>
/// Küçük resim bilgisi modeli
/// </summary>
public class KucukResimBilgisi
{
    public int Boyut { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Genislik { get; set; }
    public int Yukseklik { get; set; }
}

/// <summary>
/// Resim işleme seçenekleri
/// </summary>
public class ResimIslemeSecenekleri
{
    public bool KucukResimOlustur { get; set; } = true;
    public int MaksimumGenislik { get; set; } = 1920;
    public int MaksimumYukseklik { get; set; } = 1080;
    public List<int> KucukResimBoyutlari { get; set; } = new() { 150, 300, 800 };
    public int Kalite { get; set; } = 85;
    public string Format { get; set; } = "WebP";
}

/// <summary>
/// İşlenmiş resim sonucu
/// </summary>
public class IslenmisResimSonucu
{
    public IslenmisResim OrijinalResim { get; set; } = null!;
    public List<IslenmisResim> KucukResimler { get; set; } = new();
    public ResimMetadata Metadata { get; set; } = null!;
}

/// <summary>
/// İşlenmiş resim modeli
/// </summary>
public class IslenmisResim : IDisposable
{
    public string DosyaAdi { get; set; } = string.Empty;
    public Stream Stream { get; set; } = null!;
    public int Genislik { get; set; }
    public int Yukseklik { get; set; }
    public long Boyut { get; set; }
    
    public void Dispose()
    {
        Stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Resim metadata bilgileri
/// </summary>
public class ResimMetadata
{
    public int Genislik { get; set; }
    public int Yukseklik { get; set; }
    public string Format { get; set; } = string.Empty;
    public int BitDerinligi { get; set; }
    public bool SeffafMi { get; set; }
    public Dictionary<string, string> ExifVerileri { get; set; } = new();
}

/// <summary>
/// Virüs tarama sonucu
/// </summary>
public class VirusTaramaSonucu
{
    public bool TemizMi { get; set; }
    public string TaramaSonucu { get; set; } = string.Empty;
    public DateTime TaramaTarihi { get; set; }
    public string TarayiciMotoru { get; set; } = string.Empty;
    public string VirusImzasi { get; set; } = string.Empty;
    
    /// <summary>
    /// Türkiye saatine göre tarama tarihi
    /// </summary>
    public DateTime TurkiyeSaatiTaramaTarihi => TaramaTarihi.ToTurkeyTime();
}

/// <summary>
/// Avatar yükleme sonucu
/// </summary>
public class AvatarYuklemeSonucu
{
    public string OrijinalUrl { get; set; } = string.Empty;
    public Dictionary<int, string> KucukResimUrlleri { get; set; } = new();
    public ResimMetadata Metadata { get; set; } = null!;
}

/// <summary>
/// Yedek bilgisi modeli
/// </summary>
public class YedekBilgisi
{
    public string DosyaAdi { get; set; } = string.Empty;
    public long Boyut { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public string VeritabaniAdi { get; set; } = string.Empty;
    public YedekTipi Tip { get; set; }
    
    /// <summary>
    /// Türkiye saatine göre oluşturulma tarihi
    /// </summary>
    public DateTime TurkiyeSaatiOlusturulmaTarihi => OlusturulmaTarihi.ToTurkeyTime();
}

/// <summary>
/// Yedek tipi enum'u
/// </summary>
public enum YedekTipi
{
    TamYedek,
    DifferansiyalYedek,
    TransaksiyonLogYedek
}

/// <summary>
/// Depolama sağlık kontrolü
/// </summary>
public class DepolamaSaglikKontrolu
{
    public bool SaglikliMi { get; set; }
    public int BucketSayisi { get; set; }
    public DateTime SonKontrolTarihi { get; set; }
    public string? Hata { get; set; }
    public Dictionary<string, object> EkBilgiler { get; set; } = new();
    
    /// <summary>
    /// Türkiye saatine göre son kontrol tarihi
    /// </summary>
    public DateTime TurkiyeSaatiSonKontrolTarihi => SonKontrolTarihi.ToTurkeyTime();
}

/// <summary>
/// Dosya işleme durumu
/// </summary>
public enum DosyaIslemeDurumu
{
    Bekleniyor,
    Isleniyor,
    Tamamlandi,
    Hata
}

/// <summary>
/// Dosya işleme sonucu
/// </summary>
public class DosyaIslemeSonucu
{
    public string IslemKimlik { get; set; } = string.Empty;
    public DosyaIslemeDurumu Durum { get; set; }
    public string? Mesaj { get; set; }
    public Dictionary<string, object> Sonuclar { get; set; } = new();
    public DateTime BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    
    /// <summary>
    /// Türkiye saatine göre başlangıç tarihi
    /// </summary>
    public DateTime TurkiyeSaatiBaslangicTarihi => BaslangicTarihi.ToTurkeyTime();
    
    /// <summary>
    /// Türkiye saatine göre bitiş tarihi
    /// </summary>
    public DateTime? TurkiyeSaatiBitisTarihi => BitisTarihi?.ToTurkeyTime();
}

/// <summary>
/// Türkiye saati extension metodları
/// </summary>
public static class TurkiyeSaatiExtensions
{
    private static readonly TimeZoneInfo TurkiyeSaatDilimi = 
        TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    /// <summary>
    /// UTC zamanını Türkiye saatine çevirir
    /// </summary>
    public static DateTime ToTurkeyTime(this DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkiyeSaatDilimi);
    }

    /// <summary>
    /// Türkiye saatini UTC'ye çevirir
    /// </summary>
    public static DateTime ToUtcFromTurkeyTime(this DateTime turkeyDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkiyeSaatDilimi);
    }

    /// <summary>
    /// Şu anki Türkiye saatini döndürür
    /// </summary>
    public static DateTime SuankiTurkiyeSaati()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkiyeSaatDilimi);
    }
}