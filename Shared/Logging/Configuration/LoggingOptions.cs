namespace EgitimPlatform.Shared.Logging.Configuration;

public class LoggingOptions
{
    public const string SectionName = "Logging";
    
    public string MinimumLevel { get; set; } = "Information";
    public ConsoleLoggingOptions Console { get; set; } = new();
    public FileLoggingOptions File { get; set; } = new();
    public ElasticsearchLoggingOptions Elasticsearch { get; set; } = new();
    public SeqLoggingOptions Seq { get; set; } = new();
    public EnrichmentOptions Enrichment { get; set; } = new();
}

public class ConsoleLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
}

public class FileLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "logs/app-.log";
    public string RollingInterval { get; set; } = "Day";
    public long? FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public int RetainedFileCountLimit { get; set; } = 31;
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
}

public class ElasticsearchLoggingOptions
{
    public bool Enabled { get; set; } = false;
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexFormat { get; set; } = "egitimplatform-logs-{0:yyyy.MM.dd}";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SeqLoggingOptions
{
    public bool Enabled { get; set; } = false;
    public string ServerUrl { get; set; } = "http://localhost:5341";
    public string ApiKey { get; set; } = string.Empty;
}

public class EnrichmentOptions
{
    public bool WithMachineName { get; set; } = true;
    public bool WithEnvironmentUserName { get; set; } = true;
    public bool WithProcessId { get; set; } = true;
    public bool WithProcessName { get; set; } = true;
    public bool WithThreadId { get; set; } = true;
    public bool WithThreadName { get; set; } = true;
}