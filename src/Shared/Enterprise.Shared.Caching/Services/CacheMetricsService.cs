using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Enterprise.Shared.Caching.Services;

public class CacheMetricsService : ICacheMetricsService
{
    private readonly ILogger<CacheMetricsService> _logger;
    private readonly IDatabase? _database;
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _operationTimes = new();
    
    private long _hitSayisi = 0;
    private long _missSayisi = 0;
    private long _l1HitSayisi = 0;
    private long _l2HitSayisi = 0;
    private long _hataSayisi = 0;
    private DateTime _sonSifirlamaTarihi = DateTime.UtcNow;

    public CacheMetricsService(ILogger<CacheMetricsService> logger, IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _logger = logger;
        _database = connectionMultiplexer?.GetDatabase();
    }

    public void RecordHit(bool l1Hit = false)
    {
        Interlocked.Increment(ref _hitSayisi);
        
        if (l1Hit)
        {
            Interlocked.Increment(ref _l1HitSayisi);
        }
        else
        {
            Interlocked.Increment(ref _l2HitSayisi);
        }

        _logger.LogDebug("Cache hit kaydedildi. L1: {L1Hit}", l1Hit);
    }

    public void RecordMiss()
    {
        Interlocked.Increment(ref _missSayisi);
        _logger.LogDebug("Cache miss kaydedildi");
    }

    public void RecordOperationTime(string operationType, TimeSpan duration)
    {
        _operationTimes.AddOrUpdate(operationType, 
            new List<TimeSpan> { duration },
            (key, list) => 
            {
                lock (list)
                {
                    list.Add(duration);
                    // Son 1000 kayıdı tutuyoruz
                    if (list.Count > 1000)
                    {
                        list.RemoveRange(0, list.Count - 1000);
                    }
                    return list;
                }
            });

        _logger.LogDebug("Operasyon süresi kaydedildi. Tür: {OperationType}, Süre: {Duration}ms", 
            operationType, duration.TotalMilliseconds);
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _hataSayisi);
        _logger.LogWarning("Cache hata sayısı artırıldı. Toplam hata: {ErrorCount}", _hataSayisi);
    }

    public async Task<CacheMetrikleri> GetMetricsAsync(string? keyPattern = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var metriks = new CacheMetrikleri
            {
                HitSayisi = _hitSayisi,
                MissSayisi = _missSayisi,
                L1HitSayisi = _l1HitSayisi,
                L2HitSayisi = _l2HitSayisi,
                HataSayisi = _hataSayisi,
                SonSifirlamaTarihi = TimeZoneInfo.ConvertTimeFromUtc(_sonSifirlamaTarihi, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")),
                OrtalamaGetSuresi = GetAverageOperationTime("get"),
                OrtalamaSetSuresi = GetAverageOperationTime("set")
            };

            // Redis bilgilerini al (database null olsa bile)
            var redisInfo = await GetRedisInfoAsync(cancellationToken);
            metriks.Redis = redisInfo;

            if (_database != null)
            {

                // Cache boyutu hesapla (keyPattern varsa ona göre)
                if (!string.IsNullOrEmpty(keyPattern))
                {
                    var keys = await GetKeysAsync(keyPattern, 10000, cancellationToken);
                    metriks.ToplamKeySayisi = keys.Count;
                    
                    // Her key için boyut hesapla (sample)
                    var sampleKeys = keys.Take(100).ToList();
                    long totalSize = 0;
                    foreach (var key in sampleKeys)
                    {
                        try
                        {
                            var size = await _database.StringLengthAsync(key);
                            totalSize += size;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Key boyutu hesaplanamadı: {Key}", key);
                        }
                    }
                    
                    // Ortalama boyuttan toplam boyutu tahmin et
                    if (sampleKeys.Count > 0)
                    {
                        var avgSize = totalSize / sampleKeys.Count;
                        metriks.ToplamBoyut = avgSize * keys.Count;
                    }
                }
                else
                {
                    try
                    {
                        var info = await _database.ExecuteAsync("INFO", "memory");
                        if (!info.IsNull)
                        {
                            var memoryInfo = info.ToString();
                            metriks.BellekKullanimiMB = ParseMemoryUsage(memoryInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Redis memory info alınırken hata oluştu");
                    }
                }
            }

            _logger.LogDebug("Cache metrikleri alındı. Hit oranı: {HitRatio:P2}", metriks.HitOrani);
            return metriks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache metrikleri alınırken hata oluştu");
            RecordError();
            throw;
        }
    }

    public Task<bool> ResetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Interlocked.Exchange(ref _hitSayisi, 0);
            Interlocked.Exchange(ref _missSayisi, 0);
            Interlocked.Exchange(ref _l1HitSayisi, 0);
            Interlocked.Exchange(ref _l2HitSayisi, 0);
            Interlocked.Exchange(ref _hataSayisi, 0);
            _sonSifirlamaTarihi = DateTime.UtcNow;
            
            _operationTimes.Clear();

            _logger.LogInformation("Cache metrikleri sıfırlandı");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache metrikleri sıfırlanırken hata oluştu");
            return Task.FromResult(false);
        }
    }

    private TimeSpan GetAverageOperationTime(string operationType)
    {
        if (!_operationTimes.TryGetValue(operationType, out var times) || times.Count == 0)
        {
            return TimeSpan.Zero;
        }

        lock (times)
        {
            if (times.Count == 0) return TimeSpan.Zero;
            
            var totalTicks = times.Sum(t => t.Ticks);
            return new TimeSpan(totalTicks / times.Count);
        }
    }

    private async Task<RedisBilgileri> GetRedisInfoAsync(CancellationToken cancellationToken)
    {
        var redisBilgi = new RedisBilgileri();
        
        if (_database == null) 
        {
            redisBilgi.Version = "Unknown";
            return redisBilgi;
        }

        try
        {
            var info = await _database.ExecuteAsync("INFO", "server");
            if (!info.IsNull)
            {
                var serverInfo = info.ToString();
                redisBilgi.Version = ParseRedisVersion(serverInfo);
            }

            var clientsInfo = await _database.ExecuteAsync("INFO", "clients");
            if (!clientsInfo.IsNull)
            {
                var clients = clientsInfo.ToString();
                redisBilgi.BagliClientSayisi = ParseConnectedClients(clients);
            }

            var memoryInfo = await _database.ExecuteAsync("INFO", "memory");
            if (!memoryInfo.IsNull)
            {
                var memory = memoryInfo.ToString();
                redisBilgi.KullanılanBellek = ParseUsedMemory(memory);
                redisBilgi.PeakBellekKullanimi = ParsePeakMemory(memory);
            }

            var statsInfo = await _database.ExecuteAsync("INFO", "stats");
            if (!statsInfo.IsNull)
            {
                var stats = statsInfo.ToString();
                redisBilgi.IslenenKomutSayisi = ParseTotalCommands(stats);
                redisBilgi.SaniyeBasiKomutSayisi = ParseCommandsPerSecond(stats);
            }

            var serverInfo2 = await _database.ExecuteAsync("INFO", "server");
            if (!serverInfo2.IsNull)
            {
                var server = serverInfo2.ToString();
                redisBilgi.UptimeSaniye = ParseUptime(server);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis bilgileri alınırken hata oluştu");
        }

        return redisBilgi;
    }

    private Task<List<string>> GetKeysAsync(string pattern, int limit, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        
        if (_database == null) return Task.FromResult(keys);

        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var redisKeys = server.Keys(pattern: pattern, pageSize: limit);
            
            foreach (var key in redisKeys.Take(limit))
            {
                keys.Add(key.ToString());
                if (cancellationToken.IsCancellationRequested) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keys alınırken hata oluştu. Pattern: {Pattern}", pattern);
        }

        return Task.FromResult(keys);
    }

    private static string ParseRedisVersion(string serverInfo)
    {
        var lines = serverInfo.Split('\n');
        var versionLine = lines.FirstOrDefault(l => l.StartsWith("redis_version:"));
        return versionLine?.Substring("redis_version:".Length).Trim() ?? "Unknown";
    }

    private static int ParseConnectedClients(string clientsInfo)
    {
        var lines = clientsInfo.Split('\n');
        var clientsLine = lines.FirstOrDefault(l => l.StartsWith("connected_clients:"));
        if (clientsLine != null && int.TryParse(clientsLine.Substring("connected_clients:".Length).Trim(), out var clients))
        {
            return clients;
        }
        return 0;
    }

    private static long ParseUsedMemory(string memoryInfo)
    {
        var lines = memoryInfo.Split('\n');
        var memoryLine = lines.FirstOrDefault(l => l.StartsWith("used_memory:"));
        if (memoryLine != null && long.TryParse(memoryLine.Substring("used_memory:".Length).Trim(), out var memory))
        {
            return memory;
        }
        return 0;
    }

    private static long ParsePeakMemory(string memoryInfo)
    {
        var lines = memoryInfo.Split('\n');
        var peakLine = lines.FirstOrDefault(l => l.StartsWith("used_memory_peak:"));
        if (peakLine != null && long.TryParse(peakLine.Substring("used_memory_peak:".Length).Trim(), out var peak))
        {
            return peak;
        }
        return 0;
    }

    private static double ParseMemoryUsage(string memoryInfo)
    {
        var usedMemory = ParseUsedMemory(memoryInfo);
        return Math.Round(usedMemory / (1024.0 * 1024.0), 2); // MB cinsinden
    }

    private static long ParseTotalCommands(string statsInfo)
    {
        var lines = statsInfo.Split('\n');
        var commandsLine = lines.FirstOrDefault(l => l.StartsWith("total_commands_processed:"));
        if (commandsLine != null && long.TryParse(commandsLine.Substring("total_commands_processed:".Length).Trim(), out var commands))
        {
            return commands;
        }
        return 0;
    }

    private static double ParseCommandsPerSecond(string statsInfo)
    {
        var lines = statsInfo.Split('\n');
        var opsLine = lines.FirstOrDefault(l => l.StartsWith("instantaneous_ops_per_sec:"));
        if (opsLine != null && double.TryParse(opsLine.Substring("instantaneous_ops_per_sec:".Length).Trim(), out var ops))
        {
            return ops;
        }
        return 0;
    }

    private static long ParseUptime(string serverInfo)
    {
        var lines = serverInfo.Split('\n');
        var uptimeLine = lines.FirstOrDefault(l => l.StartsWith("uptime_in_seconds:"));
        if (uptimeLine != null && long.TryParse(uptimeLine.Substring("uptime_in_seconds:".Length).Trim(), out var uptime))
        {
            return uptime;
        }
        return 0;
    }
}