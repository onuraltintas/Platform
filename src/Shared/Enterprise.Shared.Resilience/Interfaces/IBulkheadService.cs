using Enterprise.Shared.Resilience.Models;

namespace Enterprise.Shared.Resilience.Interfaces;

public interface IBulkheadService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string bulkheadKey = "default", 
        CancellationToken cancellationToken = default);
    
    Task ExecuteAsync(Func<Task> operation, string bulkheadKey = "default", 
        CancellationToken cancellationToken = default);
    
    BulkheadStats GetBulkheadStats(string bulkheadKey);
    
    Task<BulkheadHealthInfo> GetHealthInfoAsync(string bulkheadKey);
    
    Dictionary<string, BulkheadStats> GetAllBulkheadStats();
}