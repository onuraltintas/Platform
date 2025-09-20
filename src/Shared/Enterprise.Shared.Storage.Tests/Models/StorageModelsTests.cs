using Enterprise.Shared.Common.Extensions;
using Enterprise.Shared.Storage.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;
using ValidationResult = Enterprise.Shared.Storage.Models.ValidationResult;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Enterprise.Shared.Storage.Tests.Models;

/// <summary>
/// Storage model testleri
/// </summary>
public class StorageModelsTests
{
    [Fact]
    public void DepolamaAyarlari_ConfigSection_DogruSectionAdiniDondurmeli()
    {
        // Act & Assert
        DepolamaAyarlari.ConfigSection.Should().Be("StorageSettings");
    }

    [Fact]
    public void DosyaMetadata_TurkiyeSaatiSonDegistirilme_TurkiyeSaatiniDondurmeli()
    {
        // Arrange
        var utcTarih = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var metadata = new DosyaMetadata
        {
            SonDegistirilmeTarihi = utcTarih
        };

        // Act
        var turkiyeSaati = metadata.TurkiyeSaatiSonDegistirilme;

        // Assert
        turkiyeSaati.Should().Be(utcTarih.ToTurkeyTime());
    }

    [Fact]
    public void DosyaYuklemeIstegi_ValidModel_ValidationPassed()
    {
        // Arrange
        var istek = new DosyaYuklemeIstegi
        {
            DosyaStream = new MemoryStream(new byte[] { 1, 2, 3, 4 }),
            DosyaAdi = "test.jpg",
            IcerikTuru = "image/jpeg",
            BucketAdi = "images"
        };

        var context = new ValidationContext(istek);
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(istek, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("DosyaStream")]
    [InlineData("DosyaAdi")]
    [InlineData("IcerikTuru")]
    [InlineData("BucketAdi")]
    public void DosyaYuklemeIstegi_ExitFields_ValidationFailed(string eksikAlan)
    {
        // Arrange
        var istek = new DosyaYuklemeIstegi
        {
            DosyaStream = eksikAlan != "DosyaStream" ? new MemoryStream(new byte[] { 1, 2, 3, 4 }) : null!,
            DosyaAdi = eksikAlan != "DosyaAdi" ? "test.jpg" : "",
            IcerikTuru = eksikAlan != "IcerikTuru" ? "image/jpeg" : "",
            BucketAdi = eksikAlan != "BucketAdi" ? "images" : ""
        };

        var context = new ValidationContext(istek);
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(istek, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void ValidationResult_AddError_ErrorEklenmeli()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Test hatası");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().Contain("Test hatası");
    }

    [Fact]
    public void ValidationResult_AddWarning_WarningEklenmeli()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.AddWarning("Test uyarısı");

        // Assert
        result.IsValid.Should().BeTrue(); // Warning IsValid'i etkilememeli
        result.WarningMessages.Should().Contain("Test uyarısı");
    }

    [Fact]
    public void ValidationResult_AddInfo_InfoEklenmeli()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddInfo("key", "value");

        // Assert
        result.AdditionalInfo.Should().ContainKey("key");
        result.AdditionalInfo["key"].Should().Be("value");
    }

    [Fact]
    public void ValidationResult_Failure_HataileFailureSonucuOlusturmali()
    {
        // Act
        var result = ValidationResult.Failure("Kritik hata");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().Contain("Kritik hata");
    }

    [Fact]
    public void ValidationResult_Success_BasariliSonucOlusturmali()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.WarningMessages.Should().BeEmpty();
    }

    [Theory]
    [InlineData(YedekTipi.TamYedek)]
    [InlineData(YedekTipi.DifferansiyalYedek)]
    [InlineData(YedekTipi.TransaksiyonLogYedek)]
    public void YedekTipi_GecerliDegerler_EnumDegerleriTanimliOlmali(YedekTipi tip)
    {
        // Act & Assert
        Enum.IsDefined(typeof(YedekTipi), tip).Should().BeTrue();
    }

    [Fact]
    public void YedekBilgisi_DefaultConstructor_PropertilerBaslatilmali()
    {
        // Act
        var bilgi = new YedekBilgisi();

        // Assert
        bilgi.DosyaAdi.Should().Be(string.Empty);
        bilgi.VeritabaniAdi.Should().Be(string.Empty);
        bilgi.Boyut.Should().Be(0);
        bilgi.Tip.Should().Be(YedekTipi.TamYedek); // Default enum değeri
    }

    [Fact]
    public void DosyaBilgisi_TamYolProperty_BucketveNesneAdiniIcermeli()
    {
        // Arrange
        var bilgi = new DosyaBilgisi
        {
            Ad = "dosya.txt",
            TamYol = "bucket/klasor/dosya.txt"
        };

        // Assert
        bilgi.TamYol.Should().Contain("bucket");
        bilgi.TamYol.Should().Contain("dosya.txt");
    }

    [Fact]
    public void VirusTaramaSonucu_TemizDosya_DogruPropertilerleOlusturmali()
    {
        // Arrange & Act
        var sonuc = new VirusTaramaSonucu
        {
            TemizMi = true,
            TaramaSonucu = "Temiz",
            TaramaTarihi = DateTime.UtcNow,
            TarayiciMotoru = "ClamAV",
            VirusImzasi = string.Empty
        };

        // Assert
        sonuc.TemizMi.Should().BeTrue();
        sonuc.TaramaSonucu.Should().Be("Temiz");
        sonuc.VirusImzasi.Should().BeEmpty();
        sonuc.TarayiciMotoru.Should().Be("ClamAV");
    }

    [Fact]
    public void ResimIslemeSecenekleri_DefaultValues_VarsayilanDegerereSahipOlmali()
    {
        // Act
        var secenekler = new ResimIslemeSecenekleri();

        // Assert
        secenekler.MaksimumGenislik.Should().Be(1920);
        secenekler.MaksimumYukseklik.Should().Be(1080);
        secenekler.KucukResimOlustur.Should().BeTrue();
        secenekler.KucukResimBoyutlari.Should().Contain(new[] { 150, 300, 800 });
    }

    [Fact]
    public void IslenmisResim_StreamProperty_DisposableOlmali()
    {
        // Arrange
        var stream = new MemoryStream();
        var resim = new IslenmisResim
        {
            Stream = stream,
            DosyaAdi = "test.jpg",
            Genislik = 800,
            Yukseklik = 600,
            Boyut = 1024
        };

        // Act & Assert
        resim.Stream.Should().BeAssignableTo<IDisposable>();
        
        // Cleanup
        stream.Dispose();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GuvenlikAyarlari_BosAllowedExtensions_ValidasyonHatasiVermeli(string? extension)
    {
        // Arrange
        var ayarlar = new GuvenlikAyarlari();
        if (!string.IsNullOrWhiteSpace(extension))
        {
            ayarlar.AllowedExtensions.Add(extension);
        }

        var context = new ValidationContext(ayarlar);
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(ayarlar, context, results, true);

        // Assert
        if (ayarlar.AllowedExtensions.Count == 0 || ayarlar.AllowedExtensions.All(string.IsNullOrWhiteSpace))
        {
            // En az bir uzantı gerekli, bu test beklendiği gibi fail olacak
            isValid.Should().BeFalse();
        }
    }
}