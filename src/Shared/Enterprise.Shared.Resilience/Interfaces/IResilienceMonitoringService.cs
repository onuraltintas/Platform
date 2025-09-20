using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface IResilienceMonitoringService
{
    Task<ResilienceReport> GenerateReportAsync();
    
    Task<CircuitBreakerHealthInfo> GetCircuitBreakerHealthAsync(string key);
    
    Task<BulkheadStats> GetBulkheadStatsAsync(string key);
    
    Task<TimeoutStats> GetTimeoutStatsAsync(string key);
    
    Task<RateLimitHealthInfo> GetRateLimitHealthAsync(string key);
    
    Task<double> CalculateOverallHealthScoreAsync();
    
    Task<List<string>> GetHealthRecommendationsAsync();
}