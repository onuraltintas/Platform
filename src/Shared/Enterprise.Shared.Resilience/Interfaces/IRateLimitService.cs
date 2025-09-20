using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface IRateLimitService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string rateLimitKey = "default", 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, string rateLimitKey = "default", 
        CancellationToken cancellationToken = default);
    
    Task<bool> TryAcquireAsync(string rateLimitKey = "default", int permits = 1, 
        CancellationToken cancellationToken = default);
    
    RateLimitHealthInfo GetRateLimitHealthInfo(string rateLimitKey);
    
    Dictionary<string, RateLimitHealthInfo> GetAllRateLimitHealthInfo();
}