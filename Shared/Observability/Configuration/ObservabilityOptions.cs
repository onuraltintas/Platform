namespace EgitimPlatform.Shared.Observability.Configuration;

public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "EgitimPlatform";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public string ServiceInstanceId { get; set; } = string.Empty;

    public TracingOptions Tracing { get; set; } = new();
    public MetricsOptions Metrics { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public ExporterOptions Exporters { get; set; } = new();
}

public class TracingOptions
{
    public bool Enabled { get; set; } = true;
    public double SamplingRatio { get; set; } = 1.0; // 1.0 = 100% sampling
    public bool TraceConsoleExporter { get; set; } = false;
    public bool IncludeFormattedMessage { get; set; } = true;
    public bool IncludeScopes { get; set; } = true;
    public bool RecordException { get; set; } = true;
    public bool SetErrorStatusOnException { get; set; } = true;
    
    // Instrumentation settings
    public AspNetCoreInstrumentationOptions AspNetCore { get; set; } = new();
    public HttpInstrumentationOptions Http { get; set; } = new();
    public SqlInstrumentationOptions Sql { get; set; } = new();
    public EntityFrameworkInstrumentationOptions EntityFramework { get; set; } = new();
    public MassTransitInstrumentationOptions MassTransit { get; set; } = new();
}

public class MetricsOptions
{
    public bool Enabled { get; set; } = true;
    public bool PrometheusEnabled { get; set; } = true;
    public string PrometheusEndpoint { get; set; } = "/metrics";
    public int PrometheusPort { get; set; } = 9090;
    public bool ConsoleExporter { get; set; } = false;
    public int ExportIntervalMilliseconds { get; set; } = 30000;
    public bool IncludeDefaultMetrics { get; set; } = true;
    public bool IncludeCustomMetrics { get; set; } = true;
}

public class LoggingOptions
{
    public bool IncludeTraceId { get; set; } = true;
    public bool IncludeSpanId { get; set; } = true;
    public bool IncludeActivity { get; set; } = true;
    public bool EnableStructuredLogging { get; set; } = true;
}

public class ExporterOptions
{
    public JaegerOptions Jaeger { get; set; } = new();
    public ZipkinOptions Zipkin { get; set; } = new();
    public OtlpOptions Otlp { get; set; } = new();
}

public class JaegerOptions
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "http://localhost:14268/api/traces";
    public string Protocol { get; set; } = "UdpCompactThrift"; // UdpCompactThrift, HttpBinaryThrift
}

public class ZipkinOptions
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";
}

public class OtlpOptions
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "http://localhost:4317";
    public string Protocol { get; set; } = "Grpc"; // Grpc, HttpProtobuf
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class AspNetCoreInstrumentationOptions
{
    public bool Enabled { get; set; } = true;
    public bool RecordException { get; set; } = true;
    public bool EnableGrpcAspNetCoreSupport { get; set; } = false;
    public List<string> IgnorePatterns { get; set; } = new() { "/health", "/metrics", "/favicon.ico" };
}

public class HttpInstrumentationOptions
{
    public bool Enabled { get; set; } = true;
    public bool RecordException { get; set; } = true;
    public List<string> IgnorePatterns { get; set; } = new();
}

public class SqlInstrumentationOptions
{
    public bool Enabled { get; set; } = true;
    public bool SetDbStatementForText { get; set; } = false; // Security consideration
    public bool SetDbStatementForStoredProcedure { get; set; } = true;
    public bool RecordException { get; set; } = true;
    public bool EnableConnectionLevelAttributes { get; set; } = true;
}

public class EntityFrameworkInstrumentationOptions
{
    public bool Enabled { get; set; } = true;
    public bool SetDbStatementForText { get; set; } = false; // Security consideration
    public bool SetDbStatementForStoredProcedure { get; set; } = true;
}

public class MassTransitInstrumentationOptions
{
    public bool Enabled { get; set; } = true;
    public bool CaptureMessageHeaders { get; set; } = true;
    public bool CaptureMessagePayload { get; set; } = false; // Security consideration
}