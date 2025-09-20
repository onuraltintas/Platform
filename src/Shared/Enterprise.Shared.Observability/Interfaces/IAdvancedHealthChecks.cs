using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Enterprise.Shared.Observability.Interfaces;

public interface IAdvancedHealthChecks
{
    Task<HealthCheckResult> CheckDatabaseConnectionAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckRedisConnectionAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckExternalServiceAsync(string serviceName, string endpoint, CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckDiskSpaceAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckMemoryUsageAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckCpuUsageAsync(CancellationToken cancellationToken = default);
    Task<HealthReport> GetDetailedHealthReportAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetSystemInfoAsync();
}