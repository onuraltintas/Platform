namespace Enterprise.Shared.Observability.Models;

public class BusinessMetricData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MetricName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Value { get; set; }
    public Dictionary<string, object> Dimensions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class BusinessMetricsReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, double> TotalMetrics { get; set; } = new();
    public Dictionary<string, List<TimeSeriesDataPoint>> TimeSeries { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> DimensionBreakdown { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, object>? Tags { get; set; }
}

public class CorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public Dictionary<string, string> Baggage { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MetricDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string[] Labels { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Tags { get; set; } = new();
}

public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Summary
}

public class HealthCheckInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public HealthCheckStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public Exception? Exception { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public enum HealthCheckStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public class ApplicationMetrics
{
    public double CpuUsagePercentage { get; set; }
    public double MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }
    public double TotalAllocatedMB { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}