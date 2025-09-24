using System.Diagnostics;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Observability.Services;

public class AdvancedHealthChecksService : IAdvancedHealthChecks
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ObservabilitySettings _settings;
    private readonly ILogger<AdvancedHealthChecksService> _logger;
    private readonly HttpClient _httpClient;

    public AdvancedHealthChecksService(
        IServiceProvider serviceProvider,
        IOptions<ObservabilitySettings> settings,
        ILogger<AdvancedHealthChecksService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient("HealthChecks") ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpClient.Timeout = _settings.HealthChecks.Timeout;
    }

    public async Task<HealthCheckResult> CheckDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // This is a placeholder - actual implementation would use the injected DbContext
            // For now, we'll simulate a database check
            await Task.Delay(10, cancellationToken);
            
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                ["CheckedAt"] = DateTime.UtcNow
            };

            if (stopwatch.ElapsedMilliseconds < 5000)
            {
                return HealthCheckResult.Healthy("Database connection is healthy", data);
            }
            else
            {
                _logger.LogWarning("Database connection slow, response time: {ResponseTime}ms", 
                    stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Degraded("Database connection is slow", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }

    public async Task<HealthCheckResult> CheckRedisConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // This is a placeholder - actual implementation would use the injected Redis connection
            // For now, we'll simulate a Redis check
            await Task.Delay(5, cancellationToken);
            
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                ["CheckedAt"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Redis connection is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis connection check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }

    public async Task<HealthCheckResult> CheckExternalServiceAsync(
        string serviceName, 
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["ServiceName"] = serviceName,
                ["Endpoint"] = endpoint,
                ["StatusCode"] = (int)response.StatusCode,
                ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                ["CheckedAt"] = DateTime.UtcNow
            };

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"{serviceName} is healthy", data);
            }
            else
            {
                _logger.LogWarning("External service {ServiceName} returned {StatusCode}",
                    serviceName, response.StatusCode);
                return HealthCheckResult.Unhealthy($"{serviceName} returned {response.StatusCode}", null, data);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("External service {ServiceName} health check timed out", serviceName);
            return HealthCheckResult.Unhealthy($"{serviceName} health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External service {ServiceName} health check failed", serviceName);
            return HealthCheckResult.Unhealthy($"{serviceName} health check failed", ex);
        }
    }

    public Task<HealthCheckResult> CheckDiskSpaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var healthyDrives = 0;
            var totalDrives = 0;
            var driveInfo = new List<object>();

            foreach (var drive in drives)
            {
                totalDrives++;
                var freeSpacePercentage = (double)drive.TotalFreeSpace / drive.TotalSize * 100;
                
                driveInfo.Add(new
                {
                    Name = drive.Name,
                    TotalSizeGB = Math.Round(drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 2),
                    FreeSpaceGB = Math.Round(drive.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0, 2),
                    FreeSpacePercentage = Math.Round(freeSpacePercentage, 2)
                });

                if (freeSpacePercentage > 2) // 2% minimum free space
                {
                    healthyDrives++;
                }
            }

            var data = new Dictionary<string, object>
            {
                ["Drives"] = driveInfo,
                ["HealthyDrives"] = healthyDrives,
                ["TotalDrives"] = totalDrives,
                ["CheckedAt"] = DateTime.UtcNow
            };

            if (healthyDrives == totalDrives)
            {
                return Task.FromResult(HealthCheckResult.Healthy("All drives have sufficient space", data));
            }
            else
            {
                _logger.LogWarning("{UnhealthyDrives} of {TotalDrives} drives are low on space",
                    totalDrives - healthyDrives, totalDrives);
                return Task.FromResult(HealthCheckResult.Degraded("Some drives are low on space", null, data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }

    public Task<HealthCheckResult> CheckMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var totalMemory = GC.GetTotalMemory(false);

            var data = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = Math.Round(workingSet / 1024.0 / 1024.0, 2),
                ["PrivateMemoryMB"] = Math.Round(privateMemory / 1024.0 / 1024.0, 2),
                ["TotalManagedMemoryMB"] = Math.Round(totalMemory / 1024.0 / 1024.0, 2),
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2),
                ["CheckedAt"] = DateTime.UtcNow
            };

            // Check if memory usage is within acceptable limits
            var memoryUsageMB = workingSet / 1024.0 / 1024.0;
            
            if (memoryUsageMB < 1024) // 1GB threshold
            {
                return Task.FromResult(HealthCheckResult.Healthy("Memory usage is within normal limits", data));
            }
            else if (memoryUsageMB < 2048) // 2GB warning threshold
            {
                _logger.LogWarning("Memory usage is high: {MemoryUsageMB}MB", memoryUsageMB);
                return Task.FromResult(HealthCheckResult.Degraded("Memory usage is high", null, data));
            }
            else
            {
                _logger.LogError("Memory usage is critical: {MemoryUsageMB}MB", memoryUsageMB);
                return Task.FromResult(HealthCheckResult.Unhealthy("Memory usage is critical", null, data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory usage check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory usage check failed", ex));
        }
    }

    public async Task<HealthCheckResult> CheckCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(100, cancellationToken);
            
            process.Refresh();
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            var cpuUsagePercentage = Math.Round(cpuUsageTotal * 100, 2);

            var data = new Dictionary<string, object>
            {
                ["CpuUsagePercentage"] = cpuUsagePercentage,
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["CheckedAt"] = DateTime.UtcNow
            };

            if (cpuUsagePercentage < 70)
            {
                return HealthCheckResult.Healthy("CPU usage is within normal limits", data);
            }
            else if (cpuUsagePercentage < 90)
            {
                _logger.LogWarning("CPU usage is high: {CpuUsagePercentage}%", cpuUsagePercentage);
                return HealthCheckResult.Degraded("CPU usage is high", null, data);
            }
            else
            {
                _logger.LogError("CPU usage is critical: {CpuUsagePercentage}%", cpuUsagePercentage);
                return HealthCheckResult.Unhealthy("CPU usage is critical", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPU usage check failed");
            return HealthCheckResult.Unhealthy("CPU usage check failed", ex);
        }
    }

    public async Task<HealthReport> GetDetailedHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, HealthReportEntry>();

        try
        {
            // Run all health checks in parallel
            var tasks = new[]
            {
                Task.Run(async () => ("Database", await CheckDatabaseConnectionAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("Redis", await CheckRedisConnectionAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("DiskSpace", await CheckDiskSpaceAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("Memory", await CheckMemoryUsageAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("CPU", await CheckCpuUsageAsync(cancellationToken)), cancellationToken)
            };

            var results = await Task.WhenAll(tasks);
            
            foreach (var (name, result) in results)
            {
                healthChecks[name] = new HealthReportEntry(
                    result.Status,
                    result.Description,
                    TimeSpan.Zero,
                    result.Exception,
                    result.Data);
            }

            var overallStatus = healthChecks.Values.All(r => r.Status == HealthStatus.Healthy)
                ? HealthStatus.Healthy
                : healthChecks.Values.Any(r => r.Status == HealthStatus.Unhealthy)
                    ? HealthStatus.Unhealthy
                    : HealthStatus.Degraded;

            _logger.LogInformation("Generated health report with status: {OverallStatus}", overallStatus);
            return new HealthReport(healthChecks, overallStatus, TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate health report");
            throw;
        }
    }

    public Task<Dictionary<string, object>> GetSystemInfoAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            
            var info = new Dictionary<string, object>
            {
                ["MachineName"] = Environment.MachineName,
                ["OSVersion"] = Environment.OSVersion.ToString(),
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem,
                ["Is64BitProcess"] = Environment.Is64BitProcess,
                ["DotNetVersion"] = Environment.Version.ToString(),
                ["ProcessId"] = process.Id,
                ["ProcessName"] = process.ProcessName,
                ["StartTime"] = process.StartTime.ToUniversalTime(),
                ["Uptime"] = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
                ["WorkingDirectory"] = Environment.CurrentDirectory,
                ["UserName"] = Environment.UserName
            };

            return Task.FromResult(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system information");
            return Task.FromResult(new Dictionary<string, object>());
        }
    }
}