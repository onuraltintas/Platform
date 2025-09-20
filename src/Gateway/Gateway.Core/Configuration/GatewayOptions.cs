using System.ComponentModel.DataAnnotations;

namespace Gateway.Core.Configuration;

/// <summary>
/// Gateway configuration options
/// </summary>
public class GatewayOptions
{
    public const string SectionName = "Gateway";

    [Required]
    public string Environment { get; set; } = "Development";

    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    public bool EnableSwagger { get; set; } = true;
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseLogging { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;

    [Required]
    public SecurityOptions Security { get; set; } = new();

    [Required] 
    public RateLimitingOptions RateLimiting { get; set; } = new();

    [Required]
    public CorsOptions Cors { get; set; } = new();

    [Required]
    public DownstreamServicesOptions DownstreamServices { get; set; } = new();
}

/// <summary>
/// Security configuration options
/// </summary>
public class SecurityOptions
{
    [Required]
    public string JwtSecret { get; set; } = string.Empty;

    [Required]
    public string JwtIssuer { get; set; } = string.Empty;

    [Required]
    public string JwtAudience { get; set; } = string.Empty;

    public bool RequireHttps { get; set; } = true;
    public bool EnableApiKeyAuthentication { get; set; } = false;
    public int TokenExpirationMinutes { get; set; } = 60;
    public long MaxRequestSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public bool EnablePerUserLimits { get; set; } = true;
    public int AuthenticatedUserRequestsPerMinute { get; set; } = 200;
    public List<EndpointRateLimit> EndpointLimits { get; set; } = new();
}

/// <summary>
/// Endpoint-specific rate limits
/// </summary>
public class EndpointRateLimit
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; } = 50;
    public int RequestsPerHour { get; set; } = 500;
}

/// <summary>
/// CORS configuration options
/// </summary>
public class CorsOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "https://localhost:3000" };
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };
    public List<string> AllowedHeaders { get; set; } = new() { "*" };
    public bool AllowCredentials { get; set; } = true;
    public int PreflightMaxAge { get; set; } = 86400; // 24 hours
}

/// <summary>
/// Downstream services configuration
/// </summary>
public class DownstreamServicesOptions
{
    public IdentityServiceOptions Identity { get; set; } = new();
    public IdentityServiceOptions User { get; set; } = new();
    public IdentityServiceOptions Notification { get; set; } = new();
}

/// <summary>
/// Identity service configuration
/// </summary>
public class IdentityServiceOptions
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    
    [Required]
    public string HealthEndpoint { get; set; } = "/health";
    
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool EnableCircuitBreaker { get; set; } = true;
    public double FailureThreshold { get; set; } = 0.5;
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;
}