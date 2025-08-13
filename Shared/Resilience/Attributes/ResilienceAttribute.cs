using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using EgitimPlatform.Shared.Resilience.Configuration;
using EgitimPlatform.Shared.Resilience.Policies;

namespace EgitimPlatform.Shared.Resilience.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ResilienceAttribute : ActionFilterAttribute
{
    public string? ServiceName { get; set; }
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
    public BackoffType BackoffType { get; set; } = BackoffType.Exponential;
    public bool EnableTimeout { get; set; } = true;
    public double TimeoutSeconds { get; set; } = 30.0;
    public bool EnableCircuitBreaker { get; set; } = false;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var serviceProvider = context.HttpContext.RequestServices;
        var logger = serviceProvider.GetRequiredService<ILogger<ResilienceAttribute>>();
        
        try
        {
            var policyFactory = serviceProvider.GetService<IResiliencePolicyFactory>();
            if (policyFactory != null)
            {
                var pipeline = policyFactory.CreateCustomPipeline(ServiceName ?? "Controller");
                
                await pipeline.ExecuteAsync(async (ct) =>
                {
                    await next();
                }, context.HttpContext.RequestAborted);
            }
            else
            {
                // Fallback to simple execution without resilience
                logger.LogWarning("Resilience policy factory not found, executing without resilience patterns");
                await next();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while executing action with resilience patterns");
            throw;
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RetryAttribute : Attribute
{
    public int MaxAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
    public double MaxDelaySeconds { get; set; } = 30.0;
    public BackoffType BackoffType { get; set; } = BackoffType.Exponential;
    public double JitterFactor { get; set; } = 0.1;

    public RetryAttribute() { }

    public RetryAttribute(int maxAttempts, double baseDelaySeconds = 1.0)
    {
        MaxAttempts = maxAttempts;
        BaseDelaySeconds = baseDelaySeconds;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class TimeoutAttribute : Attribute
{
    public double TimeoutSeconds { get; set; } = 30.0;
    public TimeoutStrategy Strategy { get; set; } = TimeoutStrategy.Pessimistic;

    public TimeoutAttribute() { }

    public TimeoutAttribute(double timeoutSeconds)
    {
        TimeoutSeconds = timeoutSeconds;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class CircuitBreakerAttribute : Attribute
{
    public int FailureThreshold { get; set; } = 5;
    public double FailureRatio { get; set; } = 0.5;
    public int MinimumThroughput { get; set; } = 10;
    public double SamplingDurationSeconds { get; set; } = 30.0;
    public double BreakDurationSeconds { get; set; } = 60.0;

    public CircuitBreakerAttribute() { }

    public CircuitBreakerAttribute(int failureThreshold, double breakDurationSeconds = 60.0)
    {
        FailureThreshold = failureThreshold;
        BreakDurationSeconds = breakDurationSeconds;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class BulkheadAttribute : Attribute
{
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuedActions { get; set; } = 100;

    public BulkheadAttribute() { }

    public BulkheadAttribute(int maxParallelization, int maxQueuedActions = 100)
    {
        MaxParallelization = maxParallelization;
        MaxQueuedActions = maxQueuedActions;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    public int PermitLimit { get; set; } = 100;
    public double WindowSeconds { get; set; } = 60.0;
    public RateLimiterType Type { get; set; } = RateLimiterType.TokenBucket;
    public int QueueLimit { get; set; } = 0;

    public RateLimitAttribute() { }

    public RateLimitAttribute(int permitLimit, double windowSeconds = 60.0)
    {
        PermitLimit = permitLimit;
        WindowSeconds = windowSeconds;
    }
}