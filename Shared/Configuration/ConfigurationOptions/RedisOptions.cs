namespace EgitimPlatform.Shared.Configuration.ConfigurationOptions;

public class RedisOptions
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "EgitimPlatform";
    public int Database { get; set; } = 0;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; } = false;
}