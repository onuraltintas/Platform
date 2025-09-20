using Enterprise.Shared.Storage.Extensions;
using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Enterprise.Shared.Storage.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Storage.Tests.Extensions;

/// <summary>
/// ServiceCollection extension testleri
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEnterpriseStorage_GecerliKonfigürasyon_ServislerEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:MinIO:Endpoint"] = "localhost:9000",
                ["StorageSettings:MinIO:AccessKey"] = "test",
                ["StorageSettings:MinIO:SecretKey"] = "test123",
                ["StorageSettings:Security:MaxFileSize"] = "10485760",
                ["StorageSettings:Buckets:Documents"] = "documents",
                ["StorageSettings:Buckets:Images"] = "images",
                ["StorageSettings:Buckets:UserUploads"] = "uploads",
                ["StorageSettings:Buckets:Backups"] = "backups"
            })
            .Build();

        // Act
        services.AddEnterpriseStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<IDepolamaServisi>().Should().NotBeNull();
        serviceProvider.GetService<IDosyaValidasyonServisi>().Should().NotBeNull();
        serviceProvider.GetService<IResimIslemeServisi>().Should().NotBeNull();
        serviceProvider.GetService<IYedeklemeServisi>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<DepolamaAyarlari>>().Should().NotBeNull();
    }

    [Fact]
    public void AddEnterpriseStorage_ActionKonfigürasyon_ServislerEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEnterpriseStorage(ayarlar =>
        {
            ayarlar.MinIO.Endpoint = "test:9000";
            ayarlar.MinIO.AccessKey = "testkey";
            ayarlar.MinIO.SecretKey = "testsecret";
            ayarlar.Buckets.Documents = "docs";
            ayarlar.Buckets.Images = "imgs";
            ayarlar.Buckets.UserUploads = "uploads";
            ayarlar.Buckets.Backups = "backups";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        options.Should().NotBeNull();
        options!.Value.MinIO.Endpoint.Should().Be("test:9000");
        options.Value.MinIO.AccessKey.Should().Be("testkey");
    }

    [Fact]
    public void AddEnterpriseStorage_VarsayilanAyarlar_DefaultDegerlerleServislerEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEnterpriseStorage();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        options.Should().NotBeNull();
        options!.Value.MinIO.Endpoint.Should().Be("localhost:9000");
        options.Value.MinIO.AccessKey.Should().Be("minioadmin");
        options.Value.Security.MaxFileSize.Should().Be(104857600); // 100MB
    }

    [Fact]
    public void AddMinIOStorage_OzelAyarlar_MinIOAyarlariGuncellenmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        const string endpoint = "custom:9000";
        const string accessKey = "customkey";
        const string secretKey = "customsecret";

        // Act
        services.AddMinIOStorage(endpoint, accessKey, secretKey, true, "eu-west-1");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        options.Should().NotBeNull();
        options!.Value.MinIO.Endpoint.Should().Be(endpoint);
        options.Value.MinIO.AccessKey.Should().Be(accessKey);
        options.Value.MinIO.SecretKey.Should().Be(secretKey);
        options.Value.MinIO.UseSSL.Should().BeTrue();
        options.Value.MinIO.Region.Should().Be("eu-west-1");
    }

    [Fact]
    public void AddImageProcessing_KonfigurasyonAction_ResimServisiEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Logger servisi ekle

        // Act
        services.AddImageProcessing(ayarlar =>
        {
            ayarlar.EnableResize = true;
            ayarlar.Quality = 90;
            ayarlar.Format = "JPEG";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var resimServisi = serviceProvider.GetService<IResimIslemeServisi>();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        resimServisi.Should().NotBeNull();
        options.Should().NotBeNull();
        options!.Value.ImageProcessing.Quality.Should().Be(90);
        options.Value.ImageProcessing.Format.Should().Be("JPEG");
    }

    [Fact]
    public void AddClamAVVirusScanner_Ayarlar_VirusServisiveHttpClientEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        const string endpoint = "http://clamav:3310/scan";
        const string apiKey = "test-api-key";

        // Act
        services.AddClamAVVirusScanner(endpoint, apiKey, 120);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var virusServisi = serviceProvider.GetService<IVirusTarayiciServisi>();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        virusServisi.Should().NotBeNull();
        options.Should().NotBeNull();
        options!.Value.Security.VirusScanEnabled.Should().BeTrue();
        options.Value.VirusScanner.ScanEndpoint.Should().Be(endpoint);
        options.Value.VirusScanner.ApiKey.Should().Be(apiKey);
        options.Value.VirusScanner.ScanTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void AddMockVirusScanner_MockServisEklenmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Logger servisi ekle

        // Act
        services.AddMockVirusScanner();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var virusServisi = serviceProvider.GetService<IVirusTarayiciServisi>();
        var options = serviceProvider.GetService<IOptions<DepolamaAyarlari>>();
        
        virusServisi.Should().NotBeNull();
        virusServisi.Should().BeOfType<MockVirusTarayiciServisi>();
        options.Should().NotBeNull();
        options!.Value.Security.VirusScanEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetStorageSettings_GecerliKonfigürasyon_AyarlariDondururMeli()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:MinIO:Endpoint"] = "test:9000",
                ["StorageSettings:MinIO:AccessKey"] = "testkey",
                ["StorageSettings:MinIO:SecretKey"] = "testsecret",
                ["StorageSettings:Security:MaxFileSize"] = "5242880",
                ["StorageSettings:Buckets:Documents"] = "docs",
                ["StorageSettings:Buckets:Images"] = "images",
                ["StorageSettings:Buckets:UserUploads"] = "uploads",
                ["StorageSettings:Buckets:Backups"] = "backups"
            })
            .Build();

        // Act
        var ayarlar = configuration.GetStorageSettings();

        // Assert
        ayarlar.Should().NotBeNull();
        ayarlar.MinIO.Endpoint.Should().Be("test:9000");
        ayarlar.MinIO.AccessKey.Should().Be("testkey");
        ayarlar.Security.MaxFileSize.Should().Be(5242880);
    }

    [Fact]
    public void HasStorageSettings_KonfigürasyonVar_TrueDondurmeli()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:MinIO:Endpoint"] = "test:9000"
            })
            .Build();

        // Act & Assert
        configuration.HasStorageSettings().Should().BeTrue();
    }

    [Fact]
    public void HasStorageSettings_KonfigürasyonYok_FalseDondurmeli()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        configuration.HasStorageSettings().Should().BeFalse();
    }

    [Fact]
    public void GetMinIOConnectionString_GecerliAyarlar_ConnectionStringOlusturmali()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:MinIO:Endpoint"] = "minio:9000",
                ["StorageSettings:MinIO:AccessKey"] = "admin",
                ["StorageSettings:MinIO:SecretKey"] = "password123",
                ["StorageSettings:MinIO:UseSSL"] = "true",
                ["StorageSettings:Buckets:Documents"] = "docs",
                ["StorageSettings:Buckets:Images"] = "images",
                ["StorageSettings:Buckets:UserUploads"] = "uploads",
                ["StorageSettings:Buckets:Backups"] = "backups"
            })
            .Build();

        // Act
        var connectionString = configuration.GetMinIOConnectionString();

        // Assert
        connectionString.Should().Be("https://admin:password123@minio:9000");
    }
}

/// <summary>
/// DepolamaServisiFactory testleri
/// </summary>
public class DepolamaServisiFactoryTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ArgumentNullExceptionFirlatmali()
    {
        // Act & Assert
        var act = () => new DepolamaServisiFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateStorageService_MinIOProvider_IDepolamaServisinerendurmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnterpriseStorage();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DepolamaServisiFactory(serviceProvider);

        // Act
        var storageService = factory.CreateStorageService("minio");

        // Assert
        storageService.Should().NotBeNull();
        storageService.Should().BeAssignableTo<IDepolamaServisi>();
    }

    [Fact]
    public void CreateStorageService_NullProvider_DefaultServiceDondurmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnterpriseStorage();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DepolamaServisiFactory(serviceProvider);

        // Act
        var storageService = factory.CreateStorageService(null);

        // Assert
        storageService.Should().NotBeNull();
        storageService.Should().BeAssignableTo<IDepolamaServisi>();
    }

    [Fact]
    public void CreateStorageService_DesteklenmevenProvider_NotSupportedExceptionFirlatmali()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnterpriseStorage();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DepolamaServisiFactory(serviceProvider);

        // Act & Assert
        var act = () => factory.CreateStorageService("aws-s3");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void CreateImageProcessingService_IResimIslemeServisimrDerendurmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnterpriseStorage();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DepolamaServisiFactory(serviceProvider);

        // Act
        var imageService = factory.CreateImageProcessingService();

        // Assert
        imageService.Should().NotBeNull();
        imageService.Should().BeAssignableTo<IResimIslemeServisi>();
    }

    [Fact]
    public void CreateBackupService_IYedeklemeServisinerendurmeli()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEnterpriseStorage();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DepolamaServisiFactory(serviceProvider);

        // Act
        var backupService = factory.CreateBackupService();

        // Assert
        backupService.Should().NotBeNull();
        backupService.Should().BeAssignableTo<IYedeklemeServisi>();
    }
}