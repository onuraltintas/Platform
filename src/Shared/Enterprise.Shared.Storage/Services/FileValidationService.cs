using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Storage.Services;

/// <summary>
/// Dosya validasyon servisi implementasyonu
/// </summary>
public class DosyaValidasyonServisi : IDosyaValidasyonServisi
{
    private readonly GuvenlikAyarlari _guvenlikAyarlari;
    private readonly ILogger<DosyaValidasyonServisi> _logger;

    // Dosya imza patterns (Magic Numbers)
    private static readonly Dictionary<string, List<byte[]>> DosyaImzalari = new()
    {
        // Resim dosyaları
        ["image/jpeg"] = new List<byte[]>
        {
            new byte[] { 0xFF, 0xD8, 0xFF }
        },
        ["image/png"] = new List<byte[]>
        {
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
        },
        ["image/gif"] = new List<byte[]>
        {
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
        },
        ["image/bmp"] = new List<byte[]>
        {
            new byte[] { 0x42, 0x4D }
        },
        ["image/webp"] = new List<byte[]>
        {
            new byte[] { 0x52, 0x49, 0x46, 0x46 } // "RIFF"
        },
        
        // Belge dosyaları
        ["application/pdf"] = new List<byte[]>
        {
            new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } // "%PDF-"
        },
        ["application/zip"] = new List<byte[]>
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        },
        
        // Executable dosyalar (güvenlik için)
        ["application/x-executable"] = new List<byte[]>
        {
            new byte[] { 0x4D, 0x5A }, // PE executable
            new byte[] { 0x7F, 0x45, 0x4C, 0x46 } // ELF
        }
    };

    public DosyaValidasyonServisi(
        IOptions<DepolamaAyarlari> ayarlar,
        ILogger<DosyaValidasyonServisi> logger)
    {
        _guvenlikAyarlari = ayarlar.Value.Security ?? throw new ArgumentNullException(nameof(ayarlar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResult> ValidateAsync(Stream dosyaStream, string dosyaAdi, string icerikTuru,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Dosya validasyonu başlatılıyor: {DosyaAdi}, İçerik türü: {IcerikTuru}, Boyut: {Boyut}",
                dosyaAdi ?? "null", icerikTuru, dosyaStream.Length);

            var sonuc = new ValidationResult { IsValid = true };

            // Dosya adı validasyonu
            ValidateDosyaAdi(dosyaAdi, sonuc);

            // Dosya boyutu validasyonu
            ValidateDosyaBoyutu(dosyaStream.Length, sonuc);

            // Dosya uzantısı validasyonu
            if (!string.IsNullOrWhiteSpace(dosyaAdi))
            {
                ValidateDosyaUzantisi(dosyaAdi, sonuc);
            }

            // MIME tipi validasyonu
            ValidateMimeTipi(icerikTuru, sonuc);

            // Dosya imza validasyonu (eğer etkinse)
            if (_guvenlikAyarlari.FileSignatureValidation)
            {
                await ValidateDosyaImzasiAsync(dosyaStream, icerikTuru, sonuc, cancellationToken);
            }

            // Güvenlik kontrolleri
            if (!string.IsNullOrWhiteSpace(dosyaAdi))
            {
                ValidateGuvenlik(dosyaAdi, icerikTuru, sonuc);
            }

            _logger.LogDebug("Dosya validasyonu tamamlandı: {DosyaAdi}, Geçerli: {Gecerli}, Hata sayısı: {HataSayisi}",
                dosyaAdi, sonuc.IsValid, sonuc.ErrorMessages.Count);

            return sonuc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya validasyonu sırasında hata: {DosyaAdi}", dosyaAdi);
            return ValidationResult.Failure($"Validasyon hatası: {ex.Message}");
        }
    }

    public bool DosyaUzantisiGecerliMi(string dosyaAdi)
    {
        if (string.IsNullOrEmpty(dosyaAdi))
            return false;

        var uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();
        
        // Önce yasak listesini kontrol et
        if (_guvenlikAyarlari.BlockedExtensions.Contains(uzanti))
        {
            return false;
        }

        // Sonra izin verilen listesini kontrol et
        return _guvenlikAyarlari.AllowedExtensions.Contains(uzanti);
    }

    public bool DosyaBoyutuGecerliMi(long dosyaBoyutu)
    {
        return dosyaBoyutu > 0 && dosyaBoyutu <= _guvenlikAyarlari.MaxFileSize;
    }

    public bool MimeTipiGecerliMi(string icerikTuru)
    {
        if (string.IsNullOrEmpty(icerikTuru))
            return false;

        return _guvenlikAyarlari.AllowedMimeTypes.Contains(icerikTuru.ToLowerInvariant());
    }

    public async Task<bool> DosyaImzasiGecerliMiAsync(Stream dosyaStream, string beklenenTip)
    {
        try
        {
            if (!DosyaImzalari.TryGetValue(beklenenTip.ToLowerInvariant(), out var imzalar))
            {
                // Bilinmeyen tip için null check
                return true;
            }

            dosyaStream.Position = 0;
            var buffer = new byte[16]; // İlk 16 byte'ı oku
            var bytesRead = await dosyaStream.ReadAsync(buffer, 0, buffer.Length);
            dosyaStream.Position = 0;

            if (bytesRead < 4)
            {
                return false;
            }

            foreach (var imza in imzalar)
            {
                if (ImzaEslestir(buffer, imza))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya imzası kontrolü hatası");
            return false;
        }
    }

    #region Private Methods

    private void ValidateDosyaAdi(string? dosyaAdi, ValidationResult sonuc)
    {
        if (string.IsNullOrWhiteSpace(dosyaAdi))
        {
            sonuc.AddError("Dosya adı boş olamaz");
            return;
        }

        if (dosyaAdi.Length > _guvenlikAyarlari.MaxFileNameLength)
        {
            sonuc.AddError($"Dosya adı çok uzun (maksimum {_guvenlikAyarlari.MaxFileNameLength} karakter)");
        }

        // Geçersiz karakterleri kontrol et
        var gecersizKarakterler = Path.GetInvalidFileNameChars();
        if (dosyaAdi.IndexOfAny(gecersizKarakterler) >= 0)
        {
            sonuc.AddError("Dosya adında geçersiz karakterler bulunuyor");
        }

        // Tehlikeli kelimeler
        var tehlikeliKelimeler = new[] { "script", "exe", "bat", "cmd", "com", "scr", "pif" };
        var dosyaAdiKucuk = dosyaAdi.ToLowerInvariant();
        
        if (tehlikeliKelimeler.Any(kelime => dosyaAdiKucuk.Contains(kelime)))
        {
            sonuc.AddWarning("Dosya adında şüpheli kelimeler tespit edildi");
        }
    }

    private void ValidateDosyaBoyutu(long dosyaBoyutu, ValidationResult sonuc)
    {
        if (dosyaBoyutu <= 0)
        {
            sonuc.AddError("Dosya boyutu geçersiz");
        }
        else if (dosyaBoyutu > _guvenlikAyarlari.MaxFileSize)
        {
            sonuc.AddError($"Dosya çok büyük (maksimum {_guvenlikAyarlari.MaxFileSize / (1024 * 1024)} MB)");
        }

        sonuc.AddInfo("FileSizeBytes", dosyaBoyutu);
        sonuc.AddInfo("FileSizeMB", Math.Round(dosyaBoyutu / (1024.0 * 1024.0), 2));
    }

    private void ValidateDosyaUzantisi(string dosyaAdi, ValidationResult sonuc)
    {
        if (!DosyaUzantisiGecerliMi(dosyaAdi))
        {
            var uzanti = Path.GetExtension(dosyaAdi);
            
            if (_guvenlikAyarlari.BlockedExtensions.Contains(uzanti.ToLowerInvariant()))
            {
                sonuc.AddError($"Dosya uzantısı güvenlik nedeniyle yasaklanmış: {uzanti}");
            }
            else
            {
                sonuc.AddError($"Desteklenmeyen dosya uzantısı: {uzanti}");
            }
        }
    }

    private void ValidateMimeTipi(string icerikTuru, ValidationResult sonuc)
    {
        if (!MimeTipiGecerliMi(icerikTuru))
        {
            sonuc.AddError($"Desteklenmeyen içerik türü: {icerikTuru}");
        }

        sonuc.AddInfo("ContentType", icerikTuru);
    }

    private async Task ValidateDosyaImzasiAsync(Stream dosyaStream, string icerikTuru, ValidationResult sonuc,
        CancellationToken cancellationToken)
    {
        var imzaGecerli = await DosyaImzasiGecerliMiAsync(dosyaStream, icerikTuru);
        
        if (!imzaGecerli)
        {
            sonuc.AddWarning($"Dosya imzası beklenen içerik türüyle eşleşmiyor: {icerikTuru}");
        }

        sonuc.AddInfo("FileSignatureValid", imzaGecerli);
    }

    private void ValidateGuvenlik(string dosyaAdi, string icerikTuru, ValidationResult sonuc)
    {
        // Double extension kontrolü
        var dosyaAdiKucuk = dosyaAdi.ToLowerInvariant();
        var tehlikeliDubleUzantilar = new[] { ".tar.gz", ".tar.bz2" };
        
        if (tehlikeliDubleUzantilar.Any(uzanti => dosyaAdiKucuk.Contains(uzanti)))
        {
            sonuc.AddWarning("Çift uzantılı dosya tespit edildi");
        }

        // Executable MIME type kontrolü
        var tehlikeliMimeTypes = new[]
        {
            "application/x-executable",
            "application/x-msdownload",
            "application/x-msdos-program",
            "application/x-winexe"
        };

        if (tehlikeliMimeTypes.Contains(icerikTuru.ToLowerInvariant()))
        {
            sonuc.AddError("Çalıştırılabilir dosya türü tespit edildi");
        }

        // Script file kontrolü
        var scriptUzantilari = new[] { ".js", ".vbs", ".ps1", ".sh", ".py", ".rb" };
        var uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();
        
        if (scriptUzantilari.Contains(uzanti))
        {
            sonuc.AddWarning("Script dosyası tespit edildi");
        }
    }

    private static bool ImzaEslestir(byte[] dosyaBytes, byte[] imza)
    {
        if (dosyaBytes.Length < imza.Length)
            return false;

        for (int i = 0; i < imza.Length; i++)
        {
            if (dosyaBytes[i] != imza[i])
                return false;
        }

        return true;
    }

    #endregion
}