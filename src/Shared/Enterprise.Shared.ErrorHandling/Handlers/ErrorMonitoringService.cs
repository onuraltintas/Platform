using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public interface IErrorMonitoringService
{
    Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime from, DateTime to);
    Task LogErrorAsync(Exception exception, string correlationId, Dictionary<string, object>? context = null);
    Task<List<TopErrorInfo>> GetTopErrorsAsync(DateTime from, DateTime to, int count = 10);
    Task<double> CalculateErrorRateAsync(DateTime from, DateTime to);
}

public class ErrorMonitoringService : IErrorMonitoringService
{
    private readonly ILogger<ErrorMonitoringService> _logger;
    private readonly List<ErrorLogEntry> _errorLog = new(); // In-memory for demo, use real storage in production

    public ErrorMonitoringService(ILogger<ErrorMonitoringService> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime from, DateTime to)
    {
        await Task.CompletedTask; // Simulate async operation

        var filteredErrors = _errorLog
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToList();

        var totalRequests = GetTotalRequests(from, to); // This would come from request logs

        return new ErrorStatistics
        {
            TotalErrors = filteredErrors.Count,
            ErrorsByType = GetErrorsByType(filteredErrors),
            ErrorsByStatusCode = GetErrorsByStatusCode(filteredErrors),
            TopErrors = await GetTopErrorsAsync(from, to),
            ErrorRate = totalRequests > 0 ? (double)filteredErrors.Count / totalRequests : 0
        };
    }

    public async Task LogErrorAsync(Exception exception, string correlationId, Dictionary<string, object>? context = null)
    {
        await Task.CompletedTask; // Simulate async operation

        var entry = new ErrorLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            CorrelationId = correlationId,
            Context = context ?? new Dictionary<string, object>()
        };

        _errorLog.Add(entry);
        
        // Keep only last 10000 entries to prevent memory issues
        if (_errorLog.Count > 10000)
        {
            _errorLog.RemoveAt(0);
        }

        _logger.LogInformation("Error logged with correlation ID: {CorrelationId}", correlationId);
    }

    public async Task<List<TopErrorInfo>> GetTopErrorsAsync(DateTime from, DateTime to, int count = 10)
    {
        await Task.CompletedTask; // Simulate async operation

        return _errorLog
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .GroupBy(e => new { e.ExceptionType, e.Message })
            .Select(g => new TopErrorInfo
            {
                ErrorCode = g.Key.ExceptionType,
                Message = g.Key.Message,
                Count = g.Count(),
                LastOccurrence = g.Max(e => e.Timestamp)
            })
            .OrderByDescending(e => e.Count)
            .Take(count)
            .ToList();
    }

    public async Task<double> CalculateErrorRateAsync(DateTime from, DateTime to)
    {
        await Task.CompletedTask; // Simulate async operation

        var errorCount = _errorLog.Count(e => e.Timestamp >= from && e.Timestamp <= to);
        var totalRequests = GetTotalRequests(from, to);

        return totalRequests > 0 ? (double)errorCount / totalRequests : 0;
    }

    private Dictionary<string, int> GetErrorsByType(List<ErrorLogEntry> errors)
    {
        return errors
            .GroupBy(e => e.ExceptionType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<int, int> GetErrorsByStatusCode(List<ErrorLogEntry> errors)
    {
        return errors
            .Where(e => e.Context.ContainsKey("StatusCode"))
            .GroupBy(e => (int)e.Context["StatusCode"])
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private int GetTotalRequests(DateTime from, DateTime to)
    {
        // This would typically come from request logging/metrics
        // For demo purposes, return a simulated value
        var timeSpan = to - from;
        return (int)(timeSpan.TotalMinutes * 100); // Assume 100 requests per minute
    }
}

internal class ErrorLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}