using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Resilience.Services;

public class ResilienceHealthCheck : IHealthCheck
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly IBulkheadService _bulkheadService;
    private readonly ITimeoutService _timeoutService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<ResilienceHealthCheck> _logger;

    public ResilienceHealthCheck(
        ICircuitBreakerService circuitBreakerService,
        IBulkheadService bulkheadService,
        ITimeoutService timeoutService,
        IRateLimitService rateLimitService,
        ILogger<ResilienceHealthCheck> logger)
    {
        _circuitBreakerService = circuitBreakerService;
        _bulkheadService = bulkheadService;
        _timeoutService = timeoutService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();
            var status = HealthStatus.Healthy;
            var issues = new List<string>();

            // Check circuit breakers
            var circuitBreakerStates = _circuitBreakerService.GetAllCircuitBreakerStates();
            var openCircuitBreakers = circuitBreakerStates
                .Where(cb => cb.Value == CircuitBreakerState.Open)
                .Select(cb => cb.Key)
                .ToArray();

            healthData["CircuitBreakers"] = circuitBreakerStates.ToDictionary(cb => cb.Key, cb => cb.Value.ToString());
            healthData["OpenCircuitBreakers"] = openCircuitBreakers;

            if (openCircuitBreakers.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"Open circuit breakers: {string.Join(", ", openCircuitBreakers)}");
            }

            // Check bulkheads
            var bulkheadStats = _bulkheadService.GetAllBulkheadStats();
            var overloadedBulkheads = bulkheadStats
                .Where(bs => bs.Value.RejectionRate > 0.1) // More than 10% rejection rate
                .Select(bs => bs.Key)
                .ToArray();

            healthData["Bulkheads"] = bulkheadStats.ToDictionary(bs => bs.Key, bs => new
            {
                bs.Value.CurrentExecutions,
                bs.Value.AvailableSlots,
                bs.Value.RejectionRate,
                bs.Value.SuccessRate
            });
            healthData["OverloadedBulkheads"] = overloadedBulkheads;

            if (overloadedBulkheads.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"Overloaded bulkheads: {string.Join(", ", overloadedBulkheads)}");
            }

            // Check timeout rates
            var timeoutStats = _timeoutService.GetAllTimeoutStats();
            var highTimeoutRates = timeoutStats
                .Where(ts => ts.Value.TimeoutRate > 0.05) // More than 5% timeout rate
                .Select(ts => ts.Key)
                .ToArray();

            healthData["TimeoutStats"] = timeoutStats.ToDictionary(ts => ts.Key, ts => new
            {
                ts.Value.TimeoutRate,
                ts.Value.AverageDuration,
                ts.Value.TotalTimeouts
            });
            healthData["HighTimeoutRates"] = highTimeoutRates;

            if (highTimeoutRates.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"High timeout rates: {string.Join(", ", highTimeoutRates)}");
            }

            // Check rate limit rejection rates
            var rateLimitStats = _rateLimitService.GetAllRateLimitHealthInfo();
            var highRejectionRates = rateLimitStats
                .Where(rls =>
                {
                    var rejectionRate = rls.Value.TotalRequests > 0 
                        ? (double)rls.Value.RejectedRequests / rls.Value.TotalRequests 
                        : 0;
                    return rejectionRate > 0.1; // More than 10% rejection rate
                })
                .Select(rls => rls.Key)
                .ToArray();

            healthData["RateLimitStats"] = rateLimitStats.ToDictionary(rls => rls.Key, rls => new
            {
                RejectionRate = rls.Value.TotalRequests > 0 
                    ? (double)rls.Value.RejectedRequests / rls.Value.TotalRequests 
                    : 0,
                rls.Value.CurrentPermits,
                rls.Value.PermitLimit
            });
            healthData["HighRejectionRates"] = highRejectionRates;

            if (highRejectionRates.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"High rate limit rejection rates: {string.Join(", ", highRejectionRates)}");
            }

            // Add overall statistics
            healthData["TotalCircuitBreakers"] = circuitBreakerStates.Count;
            healthData["TotalBulkheads"] = bulkheadStats.Count;
            healthData["TotalTimeoutConfigurations"] = timeoutStats.Count;
            healthData["TotalRateLimiters"] = rateLimitStats.Count;
            healthData["CheckedAt"] = DateTime.UtcNow;

            var description = status == HealthStatus.Healthy 
                ? "All resilience patterns are healthy"
                : string.Join("; ", issues);

            _logger.LogDebug("Resilience health check completed with status: {Status}", status);

            return Task.FromResult(new HealthCheckResult(status, description, data: healthData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resilience patterns health");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check resilience patterns health", ex));
        }
    }
}