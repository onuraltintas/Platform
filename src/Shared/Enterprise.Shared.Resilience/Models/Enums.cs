namespace Enterprise.Shared.Resilience.Models;

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen,
    Isolated
}

public enum ResilienceOperationResult
{
    Success,
    Failed,
    Timeout,
    CircuitBreakerOpen,
    BulkheadRejected,
    RateLimitExceeded
}