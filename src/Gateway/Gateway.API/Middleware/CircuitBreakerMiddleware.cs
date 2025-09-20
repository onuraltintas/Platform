using Gateway.Core.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Gateway.API.Middleware;

public class CircuitBreakerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly GatewayOptions _options;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers;

    public CircuitBreakerMiddleware(
        RequestDelegate next,
        ILogger<CircuitBreakerMiddleware> logger,
        IOptions<GatewayOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerState>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = GetEndpointFromPath(context.Request.Path);
        if (string.IsNullOrEmpty(endpoint))
        {
            await _next(context);
            return;
        }

        var circuitBreaker = _circuitBreakers.GetOrAdd(endpoint, _ => new CircuitBreakerState());
        
        if (circuitBreaker.State == CircuitState.Open)
        {
            if (DateTime.UtcNow - circuitBreaker.LastFailureTime > TimeSpan.FromSeconds(60))
            {
                circuitBreaker.State = CircuitState.HalfOpen;
                _logger.LogInformation("Circuit breaker for {Endpoint} moved to half-open state", endpoint);
            }
            else
            {
                _logger.LogWarning("Circuit breaker for {Endpoint} is open, rejecting request", endpoint);
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync("Service temporarily unavailable");
                return;
            }
        }

        try
        {
            await _next(context);
            
            if (context.Response.StatusCode < 500)
            {
                circuitBreaker.RecordSuccess();
            }
            else
            {
                circuitBreaker.RecordFailure();
            }
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error in circuit breaker for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    private string GetEndpointFromPath(string path)
    {
        if (path.StartsWith("/api/identity"))
            return "identity";
        if (path.StartsWith("/api/users"))
            return "user";
        if (path.StartsWith("/api/notifications"))
            return "notification";
        
        return string.Empty;
    }
}

public class CircuitBreakerState
{
    private readonly object _lock = new();
    private int _failureCount = 0;
    private int _successCount = 0;
    private const int FailureThreshold = 5;
    private const int SuccessThreshold = 3;

    public CircuitState State { get; set; } = CircuitState.Closed;
    public DateTime LastFailureTime { get; private set; }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _successCount++;
            
            if (State == CircuitState.HalfOpen && _successCount >= SuccessThreshold)
            {
                State = CircuitState.Closed;
                _failureCount = 0;
                _successCount = 0;
            }
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            LastFailureTime = DateTime.UtcNow;
            
            if (State == CircuitState.Closed && _failureCount >= FailureThreshold)
            {
                State = CircuitState.Open;
                _successCount = 0;
            }
            else if (State == CircuitState.HalfOpen)
            {
                State = CircuitState.Open;
                _successCount = 0;
            }
        }
    }
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}