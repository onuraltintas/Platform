using Enterprise.Shared.Caching.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Caching.Services;

public class CacheHealthCheck : IHealthCheck
{
    private readonly ICacheHealthCheckService _cacheHealthCheckService;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(
        ICacheHealthCheckService cacheHealthCheckService,
        ILogger<CacheHealthCheck> logger)
    {
        _cacheHealthCheckService = cacheHealthCheckService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _cacheHealthCheckService.IsHealthyAsync(cancellationToken);
            
            if (isHealthy)
            {
                var details = await _cacheHealthCheckService.GetHealthDetailsAsync(cancellationToken);
                
                return HealthCheckResult.Healthy("Cache sistemi sağlıklı", details);
            }

            _logger.LogWarning("Cache health check başarısız");
            return HealthCheckResult.Unhealthy("Cache sistemi sağlıksız");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check sırasında hata oluştu");
            return HealthCheckResult.Unhealthy("Cache health check hatası", ex);
        }
    }
}