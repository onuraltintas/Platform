using Enterprise.Shared.Common.Models;

namespace Gateway.Core.Models;

/// <summary>
/// Gateway request information for logging and processing
/// </summary>
public class GatewayRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
    public long ContentLength { get; set; }
    public string? ContentType { get; set; }
}

/// <summary>
/// Gateway response information for logging
/// </summary>
public class GatewayResponse
{
    public string RequestId { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ContentLength { get; set; }
    public string? ContentType { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Rate limiting information
/// </summary>
public class RateLimitInfo
{
    public string Key { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
    public DateTime WindowStart { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public bool IsExceeded => RequestCount >= Limit;
}

/// <summary>
/// Health check result for downstream services
/// </summary>
public class ServiceHealthResult
{
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Service endpoint for service discovery
/// </summary>
public class ServiceEndpoint
{
    public string ServiceName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string HealthEndpoint { get; set; } = "/health";
    public bool IsHealthy { get; set; } = true;
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string Version { get; set; } = "1.0.0";
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// API Key information for authentication
/// </summary>
public class ApiKeyInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;
    public string? PlainKey { get; set; } // Only populated during creation
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long UsageCount { get; set; } = 0;
    public int RateLimitPerMinute { get; set; } = 100;
    public int RateLimitPerHour { get; set; } = 1000;
    public Dictionary<string, object> Metadata { get; set; } = new();
}