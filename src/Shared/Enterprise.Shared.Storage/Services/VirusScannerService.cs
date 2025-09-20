using Enterprise.Shared.Storage.Interfaces;
using Enterprise.Shared.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Enterprise.Shared.Storage.Services;

/// <summary>
/// ClamAV tabanlı virüs tarayıcı servisi implementasyonu
/// </summary>
public class ClamAVTarayiciServisi : IVirusTarayiciServisi
{
    private readonly HttpClient _httpClient;
    private readonly VirusTarayiciAyarlari _ayarlar;
    private readonly ILogger<ClamAVTarayiciServisi> _logger;

    public ClamAVTarayiciServisi(
        HttpClient httpClient,
        IOptions<DepolamaAyarlari> ayarlar,
        ILogger<ClamAVTarayiciServisi> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (ayarlar?.Value?.VirusScanner == null)
            throw new ArgumentNullException(nameof(ayarlar));
        _ayarlar = ayarlar.Value.VirusScanner;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // HTTP client ayarları
        _httpClient.Timeout = TimeSpan.FromSeconds(_ayarlar.ScanTimeoutSeconds);
        
        if (!string.IsNullOrEmpty(_ayarlar.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _ayarlar.ApiKey);
        }
    }

    public async Task<VirusTaramaSonucu> TaraAsync(Stream dosyaStream, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Virüs taraması başlatılıyor, dosya boyutu: {DosyaBoyutu} bytes", dosyaStream.Length);

            // Dosya boyutu kontrolü
            if (dosyaStream.Length > _ayarlar.MaxScanFileSize)
            {
                _logger.LogWarning("Dosya çok büyük, virüs taraması atlanıyor: {DosyaBoyutu} bytes", dosyaStream.Length);
                
                return new VirusTaramaSonucu
                {
                    TemizMi = _ayarlar.AllowUploadOnScanFailure,
                    TaramaSonucu = $"Dosya çok büyük (>{_ayarlar.MaxScanFileSize} bytes), tarama atlandı",
                    TaramaTarihi = DateTime.UtcNow,
                    TarayiciMotoru = "ClamAV",
                    VirusImzasi = string.Empty
                };
            }

            dosyaStream.Position = 0;
            
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(dosyaStream), "file", "uploaded-file");

            if (_ayarlar.FastScanMode)
            {
                content.Add(new StringContent("true"), "fast-scan");
            }

            var response = await _httpClient.PostAsync(_ayarlar.ScanEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var sonuc = TaramaSonucuParsla(responseContent);
            
            _logger.LogDebug("Virüs taraması tamamlandı: Temiz={TemizMi}, Sonuç={Sonuc}", 
                sonuc.TemizMi, sonuc.TaramaSonucu);

            return sonuc;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Virüs taraması zaman aşımına uğradı");
            
            return new VirusTaramaSonucu
            {
                TemizMi = _ayarlar.AllowUploadOnScanFailure,
                TaramaSonucu = "Tarama zaman aşımına uğradı",
                TaramaTarihi = DateTime.UtcNow,
                TarayiciMotoru = "ClamAV",
                VirusImzasi = string.Empty
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Virüs tarayıcı servisiyle bağlantı hatası");
            
            return new VirusTaramaSonucu
            {
                TemizMi = _ayarlar.AllowUploadOnScanFailure,
                TaramaSonucu = $"Tarayıcı servisi bağlantı hatası: {ex.Message}",
                TaramaTarihi = DateTime.UtcNow,
                TarayiciMotoru = "ClamAV",
                VirusImzasi = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virüs taraması genel hatası");
            
            return new VirusTaramaSonucu
            {
                TemizMi = _ayarlar.AllowUploadOnScanFailure,
                TaramaSonucu = $"Tarama hatası: {ex.Message}",
                TaramaTarihi = DateTime.UtcNow,
                TarayiciMotoru = "ClamAV",
                VirusImzasi = string.Empty
            };
        }
    }

    public async Task<bool> TemizMiAsync(Stream dosyaStream, CancellationToken cancellationToken = default)
    {
        var sonuc = await TaraAsync(dosyaStream, cancellationToken);
        return sonuc.TemizMi;
    }

    public async Task<bool> SaglikliMiAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Virüs tarayıcı sağlık kontrolü yapılıyor");

            var healthEndpoint = _ayarlar.ScanEndpoint.Replace("/scan", "/health");
            var response = await _httpClient.GetAsync(healthEndpoint, cancellationToken);

            var saglikli = response.IsSuccessStatusCode;
            
            _logger.LogDebug("Virüs tarayıcı sağlık kontrolü: {Saglikli}", saglikli);
            return saglikli;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virüs tarayıcı sağlık kontrolü hatası");
            return false;
        }
    }

    public async Task<DateTime?> SonGuncellemeTarihiAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Virüs imza veritabanı son güncelleme tarihi alınıyor");

            var versionEndpoint = _ayarlar.ScanEndpoint.Replace("/scan", "/version");
            var response = await _httpClient.GetAsync(versionEndpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                try
                {
                    var versionInfo = JsonSerializer.Deserialize<JsonElement>(content);
                    if (versionInfo.TryGetProperty("database_updated", out var updatedProperty) &&
                        DateTime.TryParse(updatedProperty.GetString(), out var updatedDate))
                    {
                        _logger.LogDebug("Virüs imza veritabanı son güncelleme: {SonGuncelleme}", updatedDate);
                        return updatedDate;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Virüs tarayıcı version response parse edilemedi");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virüs imza veritabanı güncelleme tarihi alma hatası");
            return null;
        }
    }

    #region Private Methods

    private VirusTaramaSonucu TaramaSonucuParsla(string responseContent)
    {
        try
        {
            // JSON response parser
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            var temizMi = true;
            var sonuc = "Temiz";
            var virusImzasi = string.Empty;

            if (jsonElement.TryGetProperty("scan_result", out var scanResultProperty))
            {
                var scanResult = scanResultProperty.GetString() ?? string.Empty;
                temizMi = scanResult.Equals("clean", StringComparison.OrdinalIgnoreCase) ||
                          scanResult.Equals("ok", StringComparison.OrdinalIgnoreCase);
                sonuc = scanResult;
            }

            if (jsonElement.TryGetProperty("threats", out var threatsProperty) && 
                threatsProperty.ValueKind == JsonValueKind.Array && 
                threatsProperty.GetArrayLength() > 0)
            {
                temizMi = false;
                var threats = new List<string>();
                
                foreach (var threat in threatsProperty.EnumerateArray())
                {
                    if (threat.TryGetProperty("name", out var nameProperty))
                    {
                        threats.Add(nameProperty.GetString() ?? "Unknown");
                    }
                }
                
                virusImzasi = string.Join(", ", threats);
                sonuc = $"Tehdit tespit edildi: {virusImzasi}";
            }

            return new VirusTaramaSonucu
            {
                TemizMi = temizMi,
                TaramaSonucu = sonuc,
                TaramaTarihi = DateTime.UtcNow,
                TarayiciMotoru = "ClamAV",
                VirusImzasi = virusImzasi
            };
        }
        catch (JsonException)
        {
            // JSON parse edilemezse, düz text olarak yorumla
            return TaramaMetinSonucuParsla(responseContent);
        }
    }

    private VirusTaramaSonucu TaramaMetinSonucuParsla(string responseContent)
    {
        var temizMi = responseContent.Contains("OK", StringComparison.OrdinalIgnoreCase) ||
                      responseContent.Contains("CLEAN", StringComparison.OrdinalIgnoreCase);

        var virusImzasi = string.Empty;
        
        if (!temizMi)
        {
            // Virüs adını bulmaya çalış
            var lines = responseContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        virusImzasi = parts[1].Replace("FOUND", "").Trim();
                        break;
                    }
                }
            }
        }

        return new VirusTaramaSonucu
        {
            TemizMi = temizMi,
            TaramaSonucu = temizMi ? "Temiz" : $"Tehdit tespit edildi: {virusImzasi}",
            TaramaTarihi = DateTime.UtcNow,
            TarayiciMotoru = "ClamAV",
            VirusImzasi = virusImzasi
        };
    }

    #endregion
}

/// <summary>
/// Mock virüs tarayıcı servisi (test ve development için)
/// </summary>
public class MockVirusTarayiciServisi : IVirusTarayiciServisi
{
    private readonly ILogger<MockVirusTarayiciServisi> _logger;
    private readonly List<string> _virusTeshisListesi;

    public MockVirusTarayiciServisi(ILogger<MockVirusTarayiciServisi> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Test için bazı dosya adlarını virüslü olarak işaretle
        _virusTeshisListesi = new List<string>
        {
            "virus.exe", "malware.bat", "trojan.scr", "test-virus.txt"
        };
    }

    public Task<VirusTaramaSonucu> TaraAsync(Stream dosyaStream, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock virüs taraması yapılıyor");

        // Basit mock implementasyonu - dosya boyutuna göre karar ver
        var temizMi = dosyaStream.Length < 1000000; // 1MB altı temiz kabul et
        
        var sonuc = new VirusTaramaSonucu
        {
            TemizMi = temizMi,
            TaramaSonucu = temizMi ? "Mock tarama: Temiz" : "Mock tarama: Şüpheli dosya boyutu",
            TaramaTarihi = DateTime.UtcNow,
            TarayiciMotoru = "MockScanner",
            VirusImzasi = temizMi ? string.Empty : "MockThreat.Large"
        };

        _logger.LogDebug("Mock virüs tarama sonucu: {TemizMi}", temizMi);
        return Task.FromResult(sonuc);
    }

    public Task<bool> TemizMiAsync(Stream dosyaStream, CancellationToken cancellationToken = default)
    {
        // Mock olarak hep temiz döndür
        return Task.FromResult(true);
    }

    public Task<bool> SaglikliMiAsync(CancellationToken cancellationToken = default)
    {
        // Mock olarak hep sağlıklı döndür
        return Task.FromResult(true);
    }

    public Task<DateTime?> SonGuncellemeTarihiAsync(CancellationToken cancellationToken = default)
    {
        // Mock olarak bugünü döndür
        return Task.FromResult<DateTime?>(DateTime.Today);
    }
}