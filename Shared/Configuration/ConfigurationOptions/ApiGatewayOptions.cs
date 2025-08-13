namespace EgitimPlatform.Shared.Configuration.ConfigurationOptions;

public class ApiGatewayOptions
{
    public const string SectionName = "ApiGateway";
    
    public RateLimitOptions RateLimit { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public LoadBalancerOptions LoadBalancer { get; set; } = new();
}

public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
}

public class CorsOptions
{
    public bool Enabled { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public string[] AllowedHeaders { get; set; } = { "*" };
    public bool AllowCredentials { get; set; } = true;
}

public class LoadBalancerOptions
{
    public string Policy { get; set; } = "RoundRobin";
    public int HealthCheckInterval { get; set; } = 30;
    public int Timeout { get; set; } = 30;
}