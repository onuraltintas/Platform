using System.Collections.Concurrent;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Observability.Services;

public class InMemoryBusinessMetricsRepository : IBusinessMetricsRepository
{
    private readonly ConcurrentBag<BusinessMetricData> _metrics = new();
    private readonly ILogger<InMemoryBusinessMetricsRepository> _logger;

    public InMemoryBusinessMetricsRepository(ILogger<InMemoryBusinessMetricsRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StoreMetricAsync(BusinessMetricData metric)
    {
        ArgumentNullException.ThrowIfNull(metric);
        
        _metrics.Add(metric);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BusinessMetricData>> GetMetricsAsync(string metricName, DateTime from, DateTime to)
    {
        var result = _metrics
            .Where(m => m.MetricName == metricName && m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToList();
        
        
        return Task.FromResult<IEnumerable<BusinessMetricData>>(result);
    }

    public Task<IEnumerable<BusinessMetricData>> GetMetricsByUserAsync(string userId, DateTime from, DateTime to)
    {
        var result = _metrics
            .Where(m => m.UserId == userId && m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToList();
        
        
        return Task.FromResult<IEnumerable<BusinessMetricData>>(result);
    }

    public Task<Dictionary<string, double>> AggregateMetricsAsync(
        string metricName, 
        DateTime from, 
        DateTime to, 
        string aggregationType)
    {
        var metrics = _metrics
            .Where(m => m.MetricName == metricName && m.Timestamp >= from && m.Timestamp <= to)
            .ToList();
        
        var result = new Dictionary<string, double>();
        
        switch (aggregationType.ToLower())
        {
            case "sum":
                result["sum"] = metrics.Sum(m => m.Value);
                break;
            case "avg":
            case "average":
                result["average"] = metrics.Any() ? metrics.Average(m => m.Value) : 0;
                break;
            case "min":
                result["min"] = metrics.Any() ? metrics.Min(m => m.Value) : 0;
                break;
            case "max":
                result["max"] = metrics.Any() ? metrics.Max(m => m.Value) : 0;
                break;
            case "count":
                result["count"] = metrics.Count;
                break;
            default:
                result["sum"] = metrics.Sum(m => m.Value);
                break;
        }
        
        
        return Task.FromResult(result);
    }

    public Task CleanupExpiredMetricsAsync(DateTime cutoffDate)
    {
        var oldMetrics = _metrics.Where(m => m.Timestamp < cutoffDate).ToList();
        
        // ConcurrentBag doesn't support removing specific items efficiently
        // For a production system, consider using a different data structure
        var remainingMetrics = _metrics.Where(m => m.Timestamp >= cutoffDate).ToArray();
        _metrics.Clear();
        
        foreach (var metric in remainingMetrics)
        {
            _metrics.Add(metric);
        }
        
        _logger.LogInformation("Cleaned up {Count} metrics older than {CutoffDate}", 
            oldMetrics.Count, cutoffDate);
        
        return Task.CompletedTask;
    }
    
    // Helper methods for testing
    public void Clear()
    {
        _metrics.Clear();
    }
    
    public int Count => _metrics.Count;
}