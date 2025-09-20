using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string circuitBreakerKey, 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, string circuitBreakerKey, 
        CancellationToken cancellationToken = default);
    
    CircuitBreakerState GetCircuitBreakerState(string key);
    
    void ResetCircuitBreaker(string key);
    
    void IsolateCircuitBreaker(string key);
    
    void CloseCircuitBreaker(string key);
    
    CircuitBreakerHealthInfo GetCircuitBreakerHealthInfo(string key);
    
    Dictionary<string, CircuitBreakerState> GetAllCircuitBreakerStates();
}