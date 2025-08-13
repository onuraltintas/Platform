namespace EgitimPlatform.Shared.Auditing.Configuration;

public class AuditingOptions
{
    public bool EnableAuditing { get; set; } = true;
    public bool AuditOnlyModifiedProperties { get; set; } = true;
    public bool IncludePropertyValues { get; set; } = true;
    public bool AuditSoftDeletes { get; set; } = true;
    public bool CompressAuditData { get; set; } = false;
    public int MaxAuditRecords { get; set; } = 10000;
    public int AuditRetentionDays { get; set; } = 365;
    public string DefaultSchema { get; set; } = "audit";
    public string AuditTableSuffix { get; set; } = "_Audit";
    
    public List<string> ExcludedEntityTypes { get; set; } = new();
    public List<string> ExcludedProperties { get; set; } = new();
    public List<string> SensitiveProperties { get; set; } = new()
    {
        "Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "Token", "RefreshToken", "ApiKey", "Secret", "PrivateKey"
    };
    
    public bool EnablePerformanceAuditing { get; set; } = false;
    public int SlowQueryThresholdMs { get; set; } = 1000;
    
    public bool EnableUserAuditing { get; set; } = true;
    public bool EnableSystemAuditing { get; set; } = true;
    public bool EnableApiAuditing { get; set; } = true;
    
    public DatabaseAuditingOptions Database { get; set; } = new();
    public FileAuditingOptions File { get; set; } = new();
    public ExternalAuditingOptions External { get; set; } = new();
}

public class DatabaseAuditingOptions
{
    public bool Enabled { get; set; } = true;
    public string ConnectionStringName { get; set; } = "DefaultConnection";
    public string SchemaName { get; set; } = "audit";
    public bool AutoCreateTables { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int FlushIntervalSeconds { get; set; } = 30;
}

public class FileAuditingOptions
{
    public bool Enabled { get; set; } = false;
    public string LogPath { get; set; } = "Logs/Audit";
    public string FileNameTemplate { get; set; } = "audit-{Date}.json";
    public int MaxFileSizeMB { get; set; } = 100;
    public int MaxFileCount { get; set; } = 30;
    public bool CompressOldFiles { get; set; } = true;
}

public class ExternalAuditingOptions
{
    public bool Enabled { get; set; } = false;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool FailSilently { get; set; } = true;
}