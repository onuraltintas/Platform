using Enterprise.Shared.Storage.Models;
using Enterprise.Shared.Storage.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace Enterprise.Shared.Storage.Tests.Services;

/// <summary>
/// DosyaValidasyonServisi testleri
/// </summary>
public class DosyaValidasyonServisTestleri
{
    private readonly Mock<ILogger<DosyaValidasyonServisi>> _mockLogger;
    private readonly DosyaValidasyonServisi _servis;
    private readonly DepolamaAyarlari _ayarlar;

    public DosyaValidasyonServisTestleri()
    {
        _mockLogger = new Mock<ILogger<DosyaValidasyonServisi>>();
        
        _ayarlar = new DepolamaAyarlari
        {
            Security = new GuvenlikAyarlari
            {
                MaxFileSize = 10 * 1024 * 1024, // 10MB
                MaxFileNameLength = 255,
                AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf", ".txt"],
                BlockedExtensions = [".exe", ".bat", ".scr"],
                AllowedMimeTypes = ["image/jpeg", "image/png", "application/pdf", "text/plain"],
                FileSignatureValidation = true
            }
        };

        var mockOptions = new Mock<IOptions<DepolamaAyarlari>>();
        mockOptions.Setup(x => x.Value).Returns(_ayarlar);

        _servis = new DosyaValidasyonServisi(mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAsync_GecerliDosya_BasariliSonucDondururMeli()
    {
        // Arrange
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegHeader);
        
        // Act
        var sonuc = await _servis.ValidateAsync(stream, "test.jpg", "image/jpeg");
        
        // Assert
        sonuc.IsValid.Should().BeTrue();
        sonuc.ErrorMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_BuyukDosya_HataVermeliMeli()
    {
        // Arrange
        var buyukVeri = new byte[_ayarlar.Security.MaxFileSize + 1];
        using var stream = new MemoryStream(buyukVeri);
        
        // Act
        var sonuc = await _servis.ValidateAsync(stream, "buyuk.txt", "text/plain");
        
        // Assert
        sonuc.IsValid.Should().BeFalse();
        sonuc.ErrorMessages.Should().Contain(x => x.Contains("çok büyük"));
    }

    [Fact]
    public void DosyaUzantisiGecerliMi_GecerliUzanti_TrueDondurmeli()
    {
        // Act & Assert
        _servis.DosyaUzantisiGecerliMi("test.jpg").Should().BeTrue();
        _servis.DosyaUzantisiGecerliMi("document.pdf").Should().BeTrue();
    }

    [Fact]
    public void DosyaUzantisiGecerliMi_YasakliUzanti_FalseDondurmeli()
    {
        // Act & Assert
        _servis.DosyaUzantisiGecerliMi("virus.exe").Should().BeFalse();
        _servis.DosyaUzantisiGecerliMi("script.bat").Should().BeFalse();
    }

    [Fact]
    public void DosyaBoyutuGecerliMi_GecerliBoyut_TrueDondurmeli()
    {
        // Act & Assert
        _servis.DosyaBoyutuGecerliMi(1024).Should().BeTrue();
        _servis.DosyaBoyutuGecerliMi(_ayarlar.Security.MaxFileSize).Should().BeTrue();
    }

    [Fact]
    public void DosyaBoyutuGecerliMi_SifirVeyaNegatif_FalseDondurmeli()
    {
        // Act & Assert
        _servis.DosyaBoyutuGecerliMi(0).Should().BeFalse();
        _servis.DosyaBoyutuGecerliMi(-1).Should().BeFalse();
    }

    [Fact]
    public void MimeTipiGecerliMi_GecerliTip_TrueDondurmeli()
    {
        // Act & Assert
        _servis.MimeTipiGecerliMi("image/jpeg").Should().BeTrue();
        _servis.MimeTipiGecerliMi("application/pdf").Should().BeTrue();
    }

    [Fact]
    public async Task DosyaImzasiGecerliMiAsync_GecerliJpegImza_TrueDondurmeli()
    {
        // Arrange
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegHeader);
        
        // Act
        var sonuc = await _servis.DosyaImzasiGecerliMiAsync(stream, "image/jpeg");
        
        // Assert
        sonuc.Should().BeTrue();
    }

    [Fact]
    public async Task DosyaImzasiGecerliMiAsync_YanlisImza_FalseDondurmeli()
    {
        // Arrange
        var yanlisHeader = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(yanlisHeader);
        
        // Act
        var sonuc = await _servis.DosyaImzasiGecerliMiAsync(stream, "image/jpeg");
        
        // Assert
        sonuc.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_GeçersizDosyaAdi_HataVermeliMeli(string dosyaAdi)
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        
        // Act
        var sonuc = await _servis.ValidateAsync(stream, dosyaAdi, "text/plain");
        
        // Assert
        sonuc.IsValid.Should().BeFalse();
        sonuc.ErrorMessages.Should().Contain(x => x.Contains("Dosya adı"));
    }

    [Fact]
    public async Task ValidateAsync_UzunDosyaAdi_HataVermeliMeli()
    {
        // Arrange
        var uzunAd = new string('a', _ayarlar.Security.MaxFileNameLength + 1) + ".txt";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        
        // Act
        var sonuc = await _servis.ValidateAsync(stream, uzunAd, "text/plain");
        
        // Assert
        sonuc.IsValid.Should().BeFalse();
        sonuc.ErrorMessages.Should().Contain(x => x.Contains("çok uzun"));
    }

    [Theory]
    [InlineData("test\0file.txt")] // Null character - tüm sistemlerde geçersiz
    public async Task ValidateAsync_GecersizKarakterler_HataVermeliMeli(string dosyaAdi)
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        
        // Act
        var sonuc = await _servis.ValidateAsync(stream, dosyaAdi, "text/plain");
        
        // Assert
        sonuc.IsValid.Should().BeFalse();
        sonuc.ErrorMessages.Should().Contain(x => x.Contains("geçersiz karakterler"));
    }
}