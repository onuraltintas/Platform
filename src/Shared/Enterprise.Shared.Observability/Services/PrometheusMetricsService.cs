using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace Enterprise.Shared.Observability.Services;

public class PrometheusMetricsService : IMetricsService
{
    private readonly ILogger<PrometheusMetricsService> _logger;
    private readonly ObservabilitySettings _settings;
    private readonly IBusinessMetricsRepository? _repository;
    
    // Counters
    private readonly Counter _apiCallsTotal;
    private readonly Counter _userActionsTotal;
    private readonly Counter _databaseQueriesTotal;
    private readonly Counter _cacheOperationsTotal;
    private readonly Counter _businessEventsTotal;
    
    // Histograms
    private readonly Histogram _apiCallDuration;
    private readonly Histogram _databaseQueryDuration;
    private readonly Histogram _cacheOperationDuration;
    
    // Gauges
    private readonly Gauge _activeUsers;
    private readonly Gauge _systemHealth;
    private readonly Gauge _memoryUsage;
    private readonly Gauge _cpuUsage;

    public PrometheusMetricsService(
        ILogger<PrometheusMetricsService> logger,
        IOptions<ObservabilitySettings> settings,
        IBusinessMetricsRepository? repository = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _repository = repository;
        
        var prefix = _settings.Metrics.CustomMetricsPrefix;
        
        // Initialize Counters
        _apiCallsTotal = Metrics.CreateCounter(
            $"{prefix}api_calls_total", 
            "Total API calls", 
            new[] { "method", "endpoint", "status_code" });
            
        _userActionsTotal = Metrics.CreateCounter(
            $"{prefix}user_actions_total", 
            "Total user actions", 
            new[] { "action", "user_id" });
            
        _databaseQueriesTotal = Metrics.CreateCounter(
            $"{prefix}database_queries_total", 
            "Total database queries", 
            new[] { "operation", "table", "success" });
            
        _cacheOperationsTotal = Metrics.CreateCounter(
            $"{prefix}cache_operations_total",
            "Total cache operations",
            new[] { "operation", "hit" });
            
        _businessEventsTotal = Metrics.CreateCounter(
            $"{prefix}business_events_total",
            "Total business events",
            new[] { "event_type" });

        // Initialize Histograms
        _apiCallDuration = Metrics.CreateHistogram(
            $"{prefix}api_call_duration_seconds", 
            "API call duration", 
            new[] { "method", "endpoint" });
            
        _databaseQueryDuration = Metrics.CreateHistogram(
            $"{prefix}database_query_duration_seconds", 
            "Database query duration", 
            new[] { "operation", "table" });
            
        _cacheOperationDuration = Metrics.CreateHistogram(
            $"{prefix}cache_operation_duration_seconds",
            "Cache operation duration",
            new[] { "operation" });

        // Initialize Gauges
        _activeUsers = Metrics.CreateGauge(
            $"{prefix}active_users_total", 
            "Current active users");
            
        _systemHealth = Metrics.CreateGauge(
            $"{prefix}system_health_score", 
            "System health score (0-1)");
            
        _memoryUsage = Metrics.CreateGauge(
            $"{prefix}memory_usage_mb",
            "Memory usage in MB");
            
        _cpuUsage = Metrics.CreateGauge(
            $"{prefix}cpu_usage_percentage",
            "CPU usage percentage");
    }

    public void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object>[] tags)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            var fullName = $"{_settings.Metrics.CustomMetricsPrefix}{name}";
            var labelNames = tags.Select(t => t.Key).ToArray();
            var labelValues = tags.Select(t => t.Value?.ToString() ?? "").ToArray();
            
            var counter = Metrics.CreateCounter(fullName, $"Custom counter: {name}", labelNames);
            counter.WithLabels(labelValues).Inc(value);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            var fullName = $"{_settings.Metrics.CustomMetricsPrefix}{name}";
            var labelNames = tags.Select(t => t.Key).ToArray();
            var labelValues = tags.Select(t => t.Value?.ToString() ?? "").ToArray();
            
            var histogram = Metrics.CreateHistogram(fullName, $"Custom histogram: {name}", labelNames);
            histogram.WithLabels(labelValues).Observe(value);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram {Name}", name);
        }
    }

    public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            var fullName = $"{_settings.Metrics.CustomMetricsPrefix}{name}";
            var labelNames = tags.Select(t => t.Key).ToArray();
            var labelValues = tags.Select(t => t.Value?.ToString() ?? "").ToArray();
            
            var gauge = Metrics.CreateGauge(fullName, $"Custom gauge: {name}", labelNames);
            gauge.WithLabels(labelValues).Set(value);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting gauge {Name}", name);
        }
    }

    public void RecordBusinessMetric(string metricName, double value, Dictionary<string, object>? dimensions = null)
    {
        if (!_settings.EnableBusinessMetrics)
            return;

        try
        {
            var tags = dimensions?.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)).ToArray() 
                       ?? Array.Empty<KeyValuePair<string, object>>();
            
            RecordHistogram($"business_{metricName}", value, tags);
            _businessEventsTotal.WithLabels(metricName).Inc();
            
            // Store in repository if available
            if (_repository != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _repository.StoreMetricAsync(new BusinessMetricData
                        {
                            MetricName = metricName,
                            Value = value,
                            Dimensions = dimensions ?? new Dictionary<string, object>()
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to store business metric {MetricName}", metricName);
                    }
                });
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording business metric {MetricName}", metricName);
        }
    }

    public void IncrementUserAction(string action, string userId)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            // Use hashed user ID to protect privacy
            var hashedUserId = HashUserId(userId);
            _userActionsTotal.WithLabels(action, hashedUserId).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing user action {Action}", action);
        }
    }

    public void RecordApiCall(string method, string endpoint, int statusCode, double durationMs)
    {
        if (!_settings.EnableMetrics || !_settings.Metrics.EnableHttpMetrics)
            return;

        try
        {
            _apiCallsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
            _apiCallDuration.WithLabels(method, endpoint).Observe(durationMs / 1000.0);
            
            // Track API errors
            if (statusCode >= 500)
            {
                IncrementCounter("api_errors_total", 1,
                    new KeyValuePair<string, object>("method", method),
                    new KeyValuePair<string, object>("endpoint", endpoint),
                    new KeyValuePair<string, object>("status_code", statusCode));
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording API call metrics");
        }
    }

    public void RecordDatabaseQuery(string operation, string table, double durationMs, bool success)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            _databaseQueriesTotal.WithLabels(operation, table, success.ToString()).Inc();
            _databaseQueryDuration.WithLabels(operation, table).Observe(durationMs / 1000.0);
            
            // Track slow queries
            if (durationMs > 1000)
            {
                IncrementCounter("slow_queries_total", 1,
                    new KeyValuePair<string, object>("operation", operation),
                    new KeyValuePair<string, object>("table", table));
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording database query metrics");
        }
    }

    public void RecordCacheOperation(string operation, string key, bool hit, double durationMs)
    {
        if (!_settings.EnableMetrics)
            return;

        try
        {
            _cacheOperationsTotal.WithLabels(operation, hit.ToString()).Inc();
            _cacheOperationDuration.WithLabels(operation).Observe(durationMs / 1000.0);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cache operation metrics");
        }
    }

    public async Task<ApplicationMetrics> GetApplicationMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            
            var metrics = new ApplicationMetrics
            {
                MemoryUsageMB = process.WorkingSet64 / 1024.0 / 1024.0,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalAllocatedMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime()
            };
            
            // Update gauges
            _memoryUsage.Set(metrics.MemoryUsageMB);
            
            // CPU usage requires a bit more work
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            await Task.Delay(100);
            process.Refresh();
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            metrics.CpuUsagePercentage = Math.Round(cpuUsageTotal * 100, 2);
            _cpuUsage.Set(metrics.CpuUsagePercentage);
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application metrics");
            return new ApplicationMetrics();
        }
    }
    
    private static string HashUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "anonymous";
            
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes)[..8]; // Take first 8 chars for brevity
    }
    
    private static string SanitizeMetricValue(string? value, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";
            
        // Remove any potentially dangerous characters and limit length
        var sanitized = new string(value.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.').ToArray());
        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }
}