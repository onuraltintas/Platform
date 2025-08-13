namespace EgitimPlatform.Shared.Resilience.Configuration;

public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public BulkheadOptions Bulkhead { get; set; } = new();
    public RateLimiterOptions RateLimiter { get; set; } = new();
    public FallbackOptions Fallback { get; set; } = new();
    public HedgingOptions Hedging { get; set; } = new();
    
    public Dictionary<string, ServiceResilienceOptions> Services { get; set; } = new();
}

public class RetryOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
    public double MaxDelaySeconds { get; set; } = 30.0;
    public BackoffType BackoffType { get; set; } = BackoffType.Exponential;
    public double JitterFactor { get; set; } = 0.1;
    public List<string> RetryableExceptions { get; set; } = new()
    {
        "System.Net.Http.HttpRequestException",
        "System.TimeoutException",
        "System.Net.Sockets.SocketException",
        "System.Threading.Tasks.TaskCanceledException"
    };
    public List<int> RetryableHttpStatusCodes { get; set; } = new() { 500, 502, 503, 504, 408, 429 };
}

public class CircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;
    public int FailureThreshold { get; set; } = 5;
    public double FailureRatio { get; set; } = 0.5;
    public int MinimumThroughput { get; set; } = 10;
    public double SamplingDurationSeconds { get; set; } = 30.0;
    public double BreakDurationSeconds { get; set; } = 60.0;
    public List<string> HandledExceptions { get; set; } = new()
    {
        "System.Net.Http.HttpRequestException",
        "System.TimeoutException",
        "System.Net.Sockets.SocketException"
    };
    public List<int> HandledHttpStatusCodes { get; set; } = new() { 500, 502, 503, 504 };
}

public class TimeoutOptions
{
    public bool Enabled { get; set; } = true;
    public double TimeoutSeconds { get; set; } = 30.0;
    public TimeoutStrategy Strategy { get; set; } = TimeoutStrategy.Pessimistic;
}

public class BulkheadOptions
{
    public bool Enabled { get; set; } = false;
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuedActions { get; set; } = 100;
}

public class RateLimiterOptions
{
    public bool Enabled { get; set; } = false;
    public RateLimiterType Type { get; set; } = RateLimiterType.TokenBucket;
    public int PermitLimit { get; set; } = 100;
    public double WindowSeconds { get; set; } = 60.0;
    public int QueueLimit { get; set; } = 0;
    public bool AutoReplenishment { get; set; } = true;
    public double ReplenishmentPeriodSeconds { get; set; } = 1.0;
    public int TokensPerPeriod { get; set; } = 10;
}

public class FallbackOptions
{
    public bool Enabled { get; set; } = false;
    public FallbackAction Action { get; set; } = FallbackAction.ReturnDefault;
    public string? FallbackValue { get; set; }
    public List<string> HandledExceptions { get; set; } = new()
    {
        "System.Net.Http.HttpRequestException",
        "System.TimeoutException",
        "Polly.CircuitBreaker.CircuitBreakerOpenException"
    };
}

public class HedgingOptions
{
    public bool Enabled { get; set; } = false;
    public int MaxHedgedAttempts { get; set; } = 2;
    public double DelaySeconds { get; set; } = 1.0;
    public List<string> HandledExceptions { get; set; } = new()
    {
        "System.TimeoutException",
        "System.Threading.Tasks.TaskCanceledException"
    };
}

public class ServiceResilienceOptions
{
    public string ServiceName { get; set; } = string.Empty;
    public RetryOptions? Retry { get; set; }
    public CircuitBreakerOptions? CircuitBreaker { get; set; }
    public TimeoutOptions? Timeout { get; set; }
    public BulkheadOptions? Bulkhead { get; set; }
    public RateLimiterOptions? RateLimiter { get; set; }
    public FallbackOptions? Fallback { get; set; }
    public HedgingOptions? Hedging { get; set; }
}

public enum BackoffType
{
    Constant,
    Linear,
    Exponential,
    ExponentialWithJitter
}

public enum TimeoutStrategy
{
    Optimistic,
    Pessimistic
}

public enum RateLimiterType
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
    Concurrency
}

public enum FallbackAction
{
    ReturnDefault,
    ReturnCached,
    ExecuteAlternative,
    ThrowException
}