using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

namespace Enterprise.Shared.Storage.Services;

/// <summary>
/// Resim işleme servisi implementasyonu
/// </summary>
public class ResimIslemeServisi : IResimIslemeServisi
{
    private readonly ResimIslemeAyarlari _ayarlar;
    private readonly ILogger<ResimIslemeServisi> _logger;

    private static readonly HashSet<string> DesteklenenMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp", "image/tiff"
    };

    public List<string> DesteklenenFormatlar => DesteklenenMimeTypes.ToList();

    public ResimIslemeServisi(
        IOptions<DepolamaAyarlari> ayarlar,
        ILogger<ResimIslemeServisi> logger)
    {
        _ayarlar = ayarlar.Value.ImageProcessing ?? throw new ArgumentNullException(nameof(ayarlar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IslenmisResimSonucu> ResimIsleAsync(Stream resimStream, string dosyaAdi,
        ResimIslemeSecenekleri secenekler, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resim işleme başlatılıyor: {DosyaAdi}", dosyaAdi);

            resimStream.Position = 0;
            using var image = await Image.LoadAsync(resimStream, cancellationToken);
            var sonuclar = new List<IslenmisResim>();

            // Orijinal resmi işle
            var orijinalGenislik = Math.Min(image.Width, secenekler.MaksimumGenislik);
            var orijinalYukseklik = Math.Min(image.Height, secenekler.MaksimumYukseklik);

            var orijinalResim = await TekResimIsleAsync(image, dosyaAdi, orijinalGenislik, orijinalYukseklik, cancellationToken);
            sonuclar.Add(orijinalResim);

            // Küçük resimleri oluştur
            if (secenekler.KucukResimOlustur && _ayarlar.EnableResize)
            {
                var kullanilacakBoyutlar = secenekler.KucukResimBoyutlari.Any() 
                    ? secenekler.KucukResimBoyutlari 
                    : _ayarlar.ThumbnailSizes;

                foreach (var boyut in kullanilacakBoyutlar.Where(b => b < Math.Min(image.Width, image.Height)))
                {
                    var kucukResimAdi = KucukResimAdiniOlustur(dosyaAdi, boyut);
                    var kucukResim = await TekResimIsleAsync(image, kucukResimAdi, boyut, boyut, cancellationToken);
                    sonuclar.Add(kucukResim);
                }
            }

            var metadata = ResimMetadataOlustur(image);

            var sonuc = new IslenmisResimSonucu
            {
                OrijinalResim = sonuclar.First(),
                KucukResimler = sonuclar.Skip(1).ToList(),
                Metadata = metadata
            };

            _logger.LogInformation("Resim işleme tamamlandı: {DosyaAdi}, Toplam çıktı: {ToplamCikti}", 
                dosyaAdi, sonuclar.Count);

            return sonuc;
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogError(ex, "Desteklenmeyen resim formatı: {DosyaAdi}", dosyaAdi);
            throw new InvalidOperationException($"Desteklenmeyen resim formatı: {dosyaAdi}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resim işleme hatası: {DosyaAdi}", dosyaAdi);
            throw;
        }
    }

    public async Task<Stream> ResimBoyutlandirAsync(Stream resimStream, int genislik, int yukseklik,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resim boyutlandırılıyor: {Genislik}x{Yukseklik}", genislik, yukseklik);

            resimStream.Position = 0;
            using var image = await Image.LoadAsync(resimStream, cancellationToken);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(genislik, yukseklik),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));

            var outputStream = new MemoryStream();
            await ResmiKaydetAsync(image, outputStream, _ayarlar.Format, _ayarlar.Quality, cancellationToken);
            outputStream.Position = 0;

            _logger.LogDebug("Resim boyutlandırıldı: {Genislik}x{Yukseklik}, Yeni boyut: {YeniBoyut} bytes", 
                genislik, yukseklik, outputStream.Length);

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resim boyutlandırma hatası: {Genislik}x{Yukseklik}", genislik, yukseklik);
            throw;
        }
    }

    public async Task<Stream> KucukResimOlusturAsync(Stream resimStream, int boyut,
        CancellationToken cancellationToken = default)
    {
        return await ResimBoyutlandirAsync(resimStream, boyut, boyut, cancellationToken);
    }

    public async Task<ResimMetadata> ResimMetadataAlAsync(Stream resimStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            resimStream.Position = 0;
            using var image = await Image.LoadAsync(resimStream, cancellationToken);
            
            return ResimMetadataOlustur(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resim metadata alma hatası");
            throw;
        }
    }

    public async Task<Stream> ResimFormatiniDegistirAsync(Stream resimStream, string hedefFormat,
        int kalite = 85, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resim formatı değiştiriliyor: {HedefFormat}, Kalite: {Kalite}", hedefFormat, kalite);

            resimStream.Position = 0;
            using var image = await Image.LoadAsync(resimStream, cancellationToken);

            var outputStream = new MemoryStream();
            await ResmiKaydetAsync(image, outputStream, hedefFormat, kalite, cancellationToken);
            outputStream.Position = 0;

            _logger.LogDebug("Resim formatı değiştirildi: {HedefFormat}, Yeni boyut: {YeniBoyut} bytes", 
                hedefFormat, outputStream.Length);

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resim format değiştirme hatası: {HedefFormat}", hedefFormat);
            throw;
        }
    }

    public async Task<Stream> WatermarkEkleAsync(Stream resimStream, string watermarkMetni,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Watermark ekleniyor: {WatermarkMetni}", watermarkMetni);

            resimStream.Position = 0;
            using var image = await Image.LoadAsync(resimStream, cancellationToken);

            // Watermark ekle (basit text watermark)
            image.Mutate(x => x.DrawText(
                watermarkMetni,
                SystemFonts.CreateFont("Arial", 12),
                Color.White,
                new PointF(10, image.Height - 30)));

            var outputStream = new MemoryStream();
            await ResmiKaydetAsync(image, outputStream, _ayarlar.Format, _ayarlar.Quality, cancellationToken);
            outputStream.Position = 0;

            _logger.LogDebug("Watermark eklendi: {WatermarkMetni}", watermarkMetni);
            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watermark ekleme hatası: {WatermarkMetni}", watermarkMetni);
            throw;
        }
    }

    public bool ResimDosyasiMi(string icerikTuru)
    {
        return DesteklenenMimeTypes.Contains(icerikTuru);
    }

    #region Private Methods

    private async Task<IslenmisResim> TekResimIsleAsync(Image image, string dosyaAdi, int genislik, int yukseklik,
        CancellationToken cancellationToken)
    {
        var klonlanmisResim = image.CloneAs<Rgba32>();
        
        if (klonlanmisResim.Width != genislik || klonlanmisResim.Height != yukseklik)
        {
            klonlanmisResim.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(genislik, yukseklik),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));
        }

        // Watermark ekle (eğer ayarlarda etkinse)
        if (_ayarlar.EnableWatermark && !string.IsNullOrEmpty(_ayarlar.WatermarkText))
        {
            klonlanmisResim.Mutate(x => x.DrawText(
                _ayarlar.WatermarkText,
                SystemFonts.CreateFont("Arial", Math.Max(8, klonlanmisResim.Width / 50)),
                Color.FromRgba(255, 255, 255, 128),
                new PointF(10, klonlanmisResim.Height - 25)));
        }

        var outputStream = new MemoryStream();
        await ResmiKaydetAsync(klonlanmisResim, outputStream, _ayarlar.Format, _ayarlar.Quality, cancellationToken);
        outputStream.Position = 0;

        var islenmisResim = new IslenmisResim
        {
            DosyaAdi = DosyaAdiniGuncelle(dosyaAdi, _ayarlar.Format),
            Stream = outputStream,
            Genislik = klonlanmisResim.Width,
            Yukseklik = klonlanmisResim.Height,
            Boyut = outputStream.Length
        };

        klonlanmisResim.Dispose();
        return islenmisResim;
    }

    private async Task ResmiKaydetAsync(Image image, Stream outputStream, string format, int kalite,
        CancellationToken cancellationToken)
    {
        var formatUpper = format.ToUpperInvariant();

        switch (formatUpper)
        {
            case "WEBP":
                await image.SaveAsWebpAsync(outputStream, new WebpEncoder
                {
                    Quality = kalite,
                    Method = WebpEncodingMethod.BestQuality
                }, cancellationToken);
                break;
            
            case "JPEG":
            case "JPG":
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = kalite
                }, cancellationToken);
                break;
            
            case "PNG":
                await image.SaveAsPngAsync(outputStream, new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                }, cancellationToken);
                break;
            
            case "GIF":
                await image.SaveAsGifAsync(outputStream, new GifEncoder(), cancellationToken);
                break;
            
            default:
                // Varsayılan olarak WebP kullan
                await image.SaveAsWebpAsync(outputStream, new WebpEncoder
                {
                    Quality = kalite,
                    Method = WebpEncodingMethod.BestQuality
                }, cancellationToken);
                break;
        }
    }

    private ResimMetadata ResimMetadataOlustur(Image image)
    {
        var metadata = new ResimMetadata
        {
            Genislik = image.Width,
            Yukseklik = image.Height,
            Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
            SeffafMi = image.Metadata.DecodedImageFormat?.DefaultMimeType?.Contains("png") == true || 
                       image.Metadata.DecodedImageFormat?.DefaultMimeType?.Contains("gif") == true,
            BitDerinligi = image.PixelType.BitsPerPixel
        };

        // EXIF verilerini ekle (eğer korunacaksa)
        if (_ayarlar.PreserveExifData && image.Metadata.ExifProfile != null)
        {
            try
            {
                foreach (var value in image.Metadata.ExifProfile.Values)
                {
                    metadata.ExifVerileri[value.Tag.ToString()] = value.GetValue()?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EXIF verileri okunamadı");
            }
        }

        return metadata;
    }

    private string KucukResimAdiniOlustur(string orijinalAd, int boyut)
    {
        var uzanti = Path.GetExtension(orijinalAd);
        var adSansUzanti = Path.GetFileNameWithoutExtension(orijinalAd);
        var yeniUzanti = FormatUzantisiAl(_ayarlar.Format);
        
        return $"{adSansUzanti}_thumb_{boyut}x{boyut}{yeniUzanti}";
    }

    private string DosyaAdiniGuncelle(string orijinalAd, string yeniFormat)
    {
        var adSansUzanti = Path.GetFileNameWithoutExtension(orijinalAd);
        var yeniUzanti = FormatUzantisiAl(yeniFormat);
        
        return $"{adSansUzanti}{yeniUzanti}";
    }

    private string FormatUzantisiAl(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "WEBP" => ".webp",
            "JPEG" or "JPG" => ".jpg",
            "PNG" => ".png",
            "GIF" => ".gif",
            _ => ".webp" // Varsayılan
        };
    }

    #endregion
}