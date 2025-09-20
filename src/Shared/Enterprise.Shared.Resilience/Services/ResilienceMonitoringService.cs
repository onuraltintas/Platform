using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Resilience.Services;

public class ResilienceMonitoringService : IResilienceMonitoringService
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly IBulkheadService _bulkheadService;
    private readonly ITimeoutService _timeoutService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<ResilienceMonitoringService> _logger;

    public ResilienceMonitoringService(
        ICircuitBreakerService circuitBreakerService,
        IBulkheadService bulkheadService,
        ITimeoutService timeoutService,
        IRateLimitService rateLimitService,
        ILogger<ResilienceMonitoringService> logger)
    {
        _circuitBreakerService = circuitBreakerService;
        _bulkheadService = bulkheadService;
        _timeoutService = timeoutService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<ResilienceReport> GenerateReportAsync()
    {
        try
        {
            var report = new ResilienceReport
            {
                CircuitBreakerStates = _circuitBreakerService.GetAllCircuitBreakerStates(),
                BulkheadUtilization = _bulkheadService.GetAllBulkheadStats(),
                TimeoutRates = _timeoutService.GetAllTimeoutStats()
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TimeoutRate),
                RateLimitUtilization = _rateLimitService.GetAllRateLimitHealthInfo()
                    .ToDictionary(kvp => kvp.Key, kvp => CalculateRateLimitUtilization(kvp.Value)),
                GeneratedAt = DateTime.UtcNow
            };

            // Calculate retry rates (this would need to be implemented in retry service)
            report.RetryRates = new Dictionary<string, double>();

            report.OverallHealthScore = await CalculateOverallHealthScoreAsync();
            report.Recommendations = await GetHealthRecommendationsAsync();

            _logger.LogInformation("Generated resilience report with overall health score: {HealthScore}", 
                report.OverallHealthScore);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate resilience report");
            throw;
        }
    }

    public async Task<CircuitBreakerHealthInfo> GetCircuitBreakerHealthAsync(string key)
    {
        return await Task.FromResult(_circuitBreakerService.GetCircuitBreakerHealthInfo(key));
    }

    public async Task<BulkheadStats> GetBulkheadStatsAsync(string key)
    {
        return await Task.FromResult(_bulkheadService.GetBulkheadStats(key));
    }

    public async Task<TimeoutStats> GetTimeoutStatsAsync(string key)
    {
        return await Task.FromResult(_timeoutService.GetTimeoutStats(key));
    }

    public async Task<RateLimitHealthInfo> GetRateLimitHealthAsync(string key)
    {
        return await Task.FromResult(_rateLimitService.GetRateLimitHealthInfo(key));
    }

    public async Task<double> CalculateOverallHealthScoreAsync()
    {
        var scores = new List<double>();

        // Circuit Breaker Health Score
        var circuitBreakerStates = _circuitBreakerService.GetAllCircuitBreakerStates();
        if (circuitBreakerStates.Any())
        {
            var healthyCircuitBreakers = circuitBreakerStates.Count(cb => cb.Value == CircuitBreakerState.Closed);
            scores.Add((double)healthyCircuitBreakers / circuitBreakerStates.Count * 100);
        }

        // Bulkhead Health Score
        var bulkheadStats = _bulkheadService.GetAllBulkheadStats();
        if (bulkheadStats.Any())
        {
            var avgSuccessRate = bulkheadStats.Values.Average(bs => bs.SuccessRate) * 100;
            var avgRejectionRate = bulkheadStats.Values.Average(bs => bs.RejectionRate);
            var bulkheadScore = Math.Max(0, avgSuccessRate - (avgRejectionRate * 50)); // Penalty for high rejection rates
            scores.Add(bulkheadScore);
        }

        // Timeout Health Score
        var timeoutStats = _timeoutService.GetAllTimeoutStats();
        if (timeoutStats.Any())
        {
            var avgTimeoutRate = timeoutStats.Values.Average(ts => ts.TimeoutRate);
            var timeoutScore = Math.Max(0, 100 - (avgTimeoutRate * 100)); // Lower score for higher timeout rates
            scores.Add(timeoutScore);
        }

        // Rate Limit Health Score
        var rateLimitStats = _rateLimitService.GetAllRateLimitHealthInfo();
        if (rateLimitStats.Any())
        {
            var rateLimitScore = rateLimitStats.Values.Average(rli => 
            {
                var rejectionRate = rli.TotalRequests > 0 ? (double)rli.RejectedRequests / rli.TotalRequests : 0;
                return Math.Max(0, 100 - (rejectionRate * 100));
            });
            scores.Add(rateLimitScore);
        }

        return await Task.FromResult(scores.Any() ? scores.Average() : 100.0);
    }

    public async Task<List<string>> GetHealthRecommendationsAsync()
    {
        var recommendations = new List<string>();

        // Check Circuit Breakers
        var circuitBreakerStates = _circuitBreakerService.GetAllCircuitBreakerStates();
        var openCircuitBreakers = circuitBreakerStates.Where(cb => cb.Value == CircuitBreakerState.Open).ToList();
        if (openCircuitBreakers.Any())
        {
            recommendations.Add($"Circuit breakers are open: {string.Join(", ", openCircuitBreakers.Select(cb => cb.Key))}. Check dependent services.");
        }

        // Check Bulkheads
        var bulkheadStats = _bulkheadService.GetAllBulkheadStats();
        var overloadedBulkheads = bulkheadStats.Where(bs => bs.Value.RejectionRate > 0.1).ToList();
        if (overloadedBulkheads.Any())
        {
            recommendations.Add($"Bulkheads with high rejection rates (>10%): {string.Join(", ", overloadedBulkheads.Select(ob => ob.Key))}. Consider increasing parallelization limits.");
        }

        // Check Timeouts
        var timeoutStats = _timeoutService.GetAllTimeoutStats();
        var highTimeoutRates = timeoutStats.Where(ts => ts.Value.TimeoutRate > 0.05).ToList();
        if (highTimeoutRates.Any())
        {
            recommendations.Add($"High timeout rates (>5%): {string.Join(", ", highTimeoutRates.Select(htr => htr.Key))}. Consider increasing timeout values or optimizing operations.");
        }

        // Check Rate Limits
        var rateLimitStats = _rateLimitService.GetAllRateLimitHealthInfo();
        var highRejectionRates = rateLimitStats.Where(rls => 
        {
            var rejectionRate = rls.Value.TotalRequests > 0 ? (double)rls.Value.RejectedRequests / rls.Value.TotalRequests : 0;
            return rejectionRate > 0.1;
        }).ToList();
        
        if (highRejectionRates.Any())
        {
            recommendations.Add($"Rate limiters with high rejection rates (>10%): {string.Join(", ", highRejectionRates.Select(hrr => hrr.Key))}. Consider increasing permit limits or optimizing request patterns.");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("All resilience patterns are operating within healthy parameters.");
        }

        return await Task.FromResult(recommendations);
    }

    private static double CalculateRateLimitUtilization(RateLimitHealthInfo healthInfo)
    {
        if (healthInfo.TotalRequests == 0) return 0;
        return (double)healthInfo.PermittedRequests / healthInfo.TotalRequests * 100;
    }
}