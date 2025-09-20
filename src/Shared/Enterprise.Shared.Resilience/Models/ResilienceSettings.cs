namespace Enterprise.Shared.Resilience.Models;

public class ResilienceSettings
{
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
    public RetrySettings Retry { get; set; } = new();
    public BulkheadSettings Bulkhead { get; set; } = new();
    public TimeoutSettings Timeout { get; set; } = new();
    public RateLimitSettings RateLimit { get; set; } = new();
}

public class CircuitBreakerSettings
{
    public int FailureThreshold { get; set; } = 5;
    public int MinimumThroughput { get; set; } = 10;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableLogging { get; set; } = true;
    public bool EnableHealthCheck { get; set; } = true;
}

public class RetrySettings
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 60000;
    public string BackoffType { get; set; } = "Exponential";
    public bool UseJitter { get; set; } = true;
    public bool EnableRetryLogging { get; set; } = true;
}

public class BulkheadSettings
{
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuedActions { get; set; } = 25;
    public bool EnableBulkheadLogging { get; set; } = true;
}

public class TimeoutSettings
{
    public int DefaultTimeoutMs { get; set; } = 30000;
    public int HttpTimeoutMs { get; set; } = 30000;
    public int DatabaseTimeoutMs { get; set; } = 15000;
    public bool EnableTimeoutLogging { get; set; } = true;
}

public class RateLimitSettings
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public int QueueLimit { get; set; } = 50;
    public bool AutoReplenishment { get; set; } = true;
    public bool EnableRateLimitLogging { get; set; } = true;
}