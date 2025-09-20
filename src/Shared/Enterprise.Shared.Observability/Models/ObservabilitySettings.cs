namespace Enterprise.Shared.Observability.Models;

public class ObservabilitySettings
{
    public string ServiceName { get; set; } = "Enterprise.Service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableBusinessMetrics { get; set; } = true;
    public double SamplingRate { get; set; } = 0.1;
    
    public TracingSettings Tracing { get; set; } = new();
    public MetricsSettings Metrics { get; set; } = new();
    public HealthCheckSettings HealthChecks { get; set; } = new();
    public CorrelationIdSettings CorrelationId { get; set; } = new();
    public BusinessMetricsSettings BusinessMetrics { get; set; } = new();
}

public class TracingSettings
{
    public string? JaegerEndpoint { get; set; }
    public string? ZipkinEndpoint { get; set; }
    public string? OtlpEndpoint { get; set; }
    public bool ConsoleExporter { get; set; } = false;
    public bool EnableSqlInstrumentation { get; set; } = true;
    public bool EnableHttpInstrumentation { get; set; } = true;
    public bool EnableRedisInstrumentation { get; set; } = true;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
}

public class MetricsSettings
{
    public string PrometheusEndpoint { get; set; } = "/metrics";
    public bool EnableRuntimeMetrics { get; set; } = true;
    public bool EnableHttpMetrics { get; set; } = true;
    public bool EnableProcessMetrics { get; set; } = true;
    public bool EnableBusinessMetrics { get; set; } = true;
    public string CustomMetricsPrefix { get; set; } = "enterprise_";
    public int MetricsPort { get; set; } = 9090;
}

public class HealthCheckSettings
{
    public string Endpoint { get; set; } = "/health";
    public string DetailedEndpoint { get; set; } = "/health/detailed";
    public string ReadyEndpoint { get; set; } = "/health/ready";
    public string LiveEndpoint { get; set; } = "/health/live";
    public int CheckIntervalSeconds { get; set; } = 30;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool EnableDependencyChecks { get; set; } = true;
    public bool EnableUiEndpoint { get; set; } = false;
    public string? UiEndpoint { get; set; } = "/health-ui";
}

public class CorrelationIdSettings
{
    public string HeaderName { get; set; } = "X-Correlation-ID";
    public bool EnableLogging { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
    public bool GenerateIfMissing { get; set; } = true;
}

public class BusinessMetricsSettings
{
    public bool EnableUserMetrics { get; set; } = true;
    public bool EnableOrderMetrics { get; set; } = true;
    public bool EnablePaymentMetrics { get; set; } = true;
    public bool EnableFeatureUsageMetrics { get; set; } = true;
    public int MetricsRetentionDays { get; set; } = 90;
    public string? StorageConnectionString { get; set; }
}