using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface ITimeoutService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, TimeSpan timeout, 
        CancellationToken cancellationToken = default);
    
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string timeoutKey = "default", 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, TimeSpan timeout, 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, string timeoutKey = "default", 
        CancellationToken cancellationToken = default);
    
    TimeoutStats GetTimeoutStats(string timeoutKey);
    
    Dictionary<string, TimeoutStats> GetAllTimeoutStats();
}