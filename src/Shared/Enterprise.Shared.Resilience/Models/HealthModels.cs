namespace Enterprise.Shared.Resilience.Models;

public class BulkheadHealthInfo
{
    public string BulkheadKey { get; set; } = string.Empty;
    public int MaxParallelization { get; set; }
    public int CurrentExecutions { get; set; }
    public long TotalExecutions { get; set; }
    public long TotalRejections { get; set; }
    public long TotalFailures { get; set; }
    public TimeSpan TotalWaitTime { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSuccessfulExecution { get; set; }
    public DateTime? LastFailure { get; set; }
    public string? LastFailureReason { get; set; }
}

public class BulkheadStats
{
    public string BulkheadKey { get; set; } = string.Empty;
    public int MaxParallelization { get; set; }
    public int CurrentExecutions { get; set; }
    public int AvailableSlots { get; set; }
    public long TotalExecutions { get; set; }
    public long TotalRejections { get; set; }
    public long TotalFailures { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public double SuccessRate { get; set; }
    public double RejectionRate => TotalExecutions + TotalRejections > 0 
        ? (double)TotalRejections / (TotalExecutions + TotalRejections) 
        : 0;
}

public class TimeoutHealthInfo
{
    public string TimeoutKey { get; set; } = string.Empty;
    public TimeSpan ConfiguredTimeout { get; set; }
    public long TotalAttempts { get; set; }
    public long TotalSuccesses { get; set; }
    public long TotalTimeouts { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTimeout { get; set; }
    public TimeSpan LastTimeoutDuration { get; set; }
}

public class TimeoutStats
{
    public string TimeoutKey { get; set; } = string.Empty;
    public TimeSpan ConfiguredTimeout { get; set; }
    public long TotalAttempts { get; set; }
    public long TotalSuccesses { get; set; }
    public long TotalTimeouts { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public double TimeoutRate { get; set; }
    public DateTime? LastTimeout { get; set; }
}

public class CircuitBreakerHealthInfo
{
    public string CircuitBreakerKey { get; set; } = string.Empty;
    public CircuitBreakerState State { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double FailureRate { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? StateLastChangedTime { get; set; }
    public TimeSpan TimeInCurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RateLimitHealthInfo
{
    public string RateLimitKey { get; set; } = string.Empty;
    public int PermitLimit { get; set; }
    public int CurrentPermits { get; set; }
    public TimeSpan Window { get; set; }
    public long TotalRequests { get; set; }
    public long PermittedRequests { get; set; }
    public long RejectedRequests { get; set; }
    public DateTime? LastRejection { get; set; }
    public DateTime? LastPermission { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResilienceReport
{
    public Dictionary<string, CircuitBreakerState> CircuitBreakerStates { get; set; } = new();
    public Dictionary<string, BulkheadStats> BulkheadUtilization { get; set; } = new();
    public Dictionary<string, double> RetryRates { get; set; } = new();
    public Dictionary<string, double> TimeoutRates { get; set; } = new();
    public Dictionary<string, double> RateLimitUtilization { get; set; } = new();
    public double OverallHealthScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<string> Recommendations { get; set; } = new();
}