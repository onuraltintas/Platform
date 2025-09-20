using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface IRetryService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string? retryKey = null, 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, string? retryKey = null, 
        CancellationToken cancellationToken = default);
    
    Task<T> ExecuteWithCustomPolicyAsync<T>(Func<Task<T>> operation, RetryPolicy customPolicy,
        CancellationToken cancellationToken = default);
    
    Task ExecuteWithCustomPolicyAsync(Func<Task> operation, RetryPolicy customPolicy,
        CancellationToken cancellationToken = default);
}