using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Hedging;
// using Polly.RateLimiting; // Not available in Polly v8
using Polly.Retry;
using Polly.Timeout;
using System.Threading.RateLimiting;
using EgitimPlatform.Shared.Resilience.Configuration;

namespace EgitimPlatform.Shared.Resilience.Policies;

public interface IResiliencePolicyFactory
{
    ResiliencePipeline CreateHttpPipeline(string? serviceName = null);
    ResiliencePipeline<T> CreateHttpPipeline<T>(string? serviceName = null);
    ResiliencePipeline CreateDatabasePipeline(string? serviceName = null);
    ResiliencePipeline<T> CreateDatabasePipeline<T>(string? serviceName = null);
    ResiliencePipeline CreateMessagingPipeline(string? serviceName = null);
    ResiliencePipeline<T> CreateMessagingPipeline<T>(string? serviceName = null);
    ResiliencePipeline CreateCustomPipeline(string serviceName);
    ResiliencePipeline<T> CreateCustomPipeline<T>(string serviceName);
}

public class ResiliencePolicyFactory : IResiliencePolicyFactory
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResiliencePolicyFactory> _logger;

    public ResiliencePolicyFactory(IOptions<ResilienceOptions> options, ILogger<ResiliencePolicyFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ResiliencePipeline CreateHttpPipeline(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();
        var serviceOptions = GetServiceOptions(serviceName);

        AddStrategies(pipelineBuilder, serviceOptions, "HTTP");
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline<T> CreateHttpPipeline<T>(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<T>();
        var serviceOptions = GetServiceOptions(serviceName);

        AddStrategies(pipelineBuilder, serviceOptions, "HTTP");
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline CreateDatabasePipeline(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();
        var serviceOptions = GetServiceOptions(serviceName);

        // Database-specific configurations (typically less aggressive)
        AddDatabaseStrategies(pipelineBuilder, serviceOptions);
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline<T> CreateDatabasePipeline<T>(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<T>();
        var serviceOptions = GetServiceOptions(serviceName);

        AddDatabaseStrategies(pipelineBuilder, serviceOptions);
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline CreateMessagingPipeline(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();
        var serviceOptions = GetServiceOptions(serviceName);

        AddMessagingStrategies(pipelineBuilder, serviceOptions);
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline<T> CreateMessagingPipeline<T>(string? serviceName = null)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<T>();
        var serviceOptions = GetServiceOptions(serviceName);

        AddMessagingStrategies(pipelineBuilder, serviceOptions);
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline CreateCustomPipeline(string serviceName)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();
        var serviceOptions = GetServiceOptions(serviceName);

        AddStrategies(pipelineBuilder, serviceOptions, "Custom");
        
        return pipelineBuilder.Build();
    }

    public ResiliencePipeline<T> CreateCustomPipeline<T>(string serviceName)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<T>();
        var serviceOptions = GetServiceOptions(serviceName);

        AddStrategies(pipelineBuilder, serviceOptions, "Custom");
        
        return pipelineBuilder.Build();
    }

    private ServiceResilienceOptions GetServiceOptions(string? serviceName)
    {
        if (!string.IsNullOrEmpty(serviceName) && _options.Services.TryGetValue(serviceName, out var serviceOptions))
        {
            return serviceOptions;
        }

        // Return default options
        return new ServiceResilienceOptions
        {
            ServiceName = serviceName ?? "Default",
            Retry = _options.Retry,
            CircuitBreaker = _options.CircuitBreaker,
            Timeout = _options.Timeout,
            Bulkhead = _options.Bulkhead,
            RateLimiter = _options.RateLimiter,
            Fallback = _options.Fallback,
            Hedging = _options.Hedging
        };
    }

    private void AddStrategies(ResiliencePipelineBuilderBase pipelineBuilder, ServiceResilienceOptions options, string context)
    {
        // Order matters: Timeout -> Retry -> Circuit Breaker -> Rate Limiter -> Bulkhead -> Hedging -> Fallback

        // 1. Timeout (outermost)
        if (options.Timeout?.Enabled == true)
        {
            AddTimeoutStrategy(pipelineBuilder, options.Timeout, context);
        }

        // 2. Retry
        if (options.Retry?.Enabled == true)
        {
            AddRetryStrategy(pipelineBuilder, options.Retry, context);
        }

        // 3. Circuit Breaker
        if (options.CircuitBreaker?.Enabled == true)
        {
            AddCircuitBreakerStrategy(pipelineBuilder, options.CircuitBreaker, context);
        }

        // Note: Rate Limiter, Bulkhead, Hedging, and Fallback strategies are commented out
        // due to extensive API changes in Polly v8. These can be re-implemented later.
        
        // TODO: Implement Rate Limiter with Polly v8 API
        // TODO: Implement Bulkhead with Polly v8 API
        // TODO: Implement Hedging with Polly v8 API
        // TODO: Implement Fallback with Polly v8 API
    }

    private void AddDatabaseStrategies(ResiliencePipelineBuilderBase pipelineBuilder, ServiceResilienceOptions options)
    {
        // Database-specific: less aggressive retry, longer timeout
        if (options.Timeout?.Enabled == true)
        {
            var dbTimeout = new TimeoutOptions
            {
                Enabled = true,
                TimeoutSeconds = Math.Max(options.Timeout.TimeoutSeconds, 60.0), // Minimum 60s for DB
                // Strategy = Polly.Timeout.TimeoutStrategy.Pessimistic // Commented out due to type mismatch
            };
            AddTimeoutStrategy(pipelineBuilder, dbTimeout, "Database");
        }

        if (options.Retry?.Enabled == true)
        {
            var dbRetry = new RetryOptions
            {
                Enabled = true,
                MaxRetryAttempts = Math.Min(options.Retry.MaxRetryAttempts, 2), // Max 2 retries for DB
                BaseDelaySeconds = Math.Max(options.Retry.BaseDelaySeconds, 2.0), // Minimum 2s delay
                BackoffType = BackoffType.Linear,
                RetryableExceptions = new List<string>
                {
                    "System.Data.SqlClient.SqlException",
                    "Microsoft.Data.SqlClient.SqlException",
                    "System.TimeoutException"
                }
            };
            AddRetryStrategy(pipelineBuilder, dbRetry, "Database");
        }

        if (options.CircuitBreaker?.Enabled == true)
        {
            AddCircuitBreakerStrategy(pipelineBuilder, options.CircuitBreaker, "Database");
        }
    }

    private void AddMessagingStrategies(ResiliencePipelineBuilderBase pipelineBuilder, ServiceResilienceOptions options)
    {
        // Messaging-specific: more aggressive retry, shorter timeout
        if (options.Timeout?.Enabled == true)
        {
            var msgTimeout = new TimeoutOptions
            {
                Enabled = true,
                TimeoutSeconds = Math.Min(options.Timeout.TimeoutSeconds, 15.0), // Max 15s for messaging
                // Strategy = Polly.Timeout.TimeoutStrategy.Optimistic // Commented out due to type mismatch
            };
            AddTimeoutStrategy(pipelineBuilder, msgTimeout, "Messaging");
        }

        if (options.Retry?.Enabled == true)
        {
            var msgRetry = new RetryOptions
            {
                Enabled = true,
                MaxRetryAttempts = Math.Max(options.Retry.MaxRetryAttempts, 5), // More retries for messaging
                BaseDelaySeconds = Math.Min(options.Retry.BaseDelaySeconds, 0.5), // Faster retries
                BackoffType = BackoffType.ExponentialWithJitter,
                RetryableExceptions = new List<string>
                {
                    "MassTransit.RequestTimeoutException",
                    "RabbitMQ.Client.Exceptions.ConnectFailureException",
                    "System.Net.Sockets.SocketException",
                    "System.TimeoutException"
                }
            };
            AddRetryStrategy(pipelineBuilder, msgRetry, "Messaging");
        }

        if (options.CircuitBreaker?.Enabled == true)
        {
            AddCircuitBreakerStrategy(pipelineBuilder, options.CircuitBreaker, "Messaging");
        }
    }

    private void AddTimeoutStrategy(ResiliencePipelineBuilderBase pipelineBuilder, TimeoutOptions options, string context)
    {
        if (pipelineBuilder is ResiliencePipelineBuilder builder)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogWarning("Timeout occurred in {Context} after {Timeout}s", context, options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            });
        }
        else if (pipelineBuilder is ResiliencePipelineBuilder<object> genericBuilder)
        {
            genericBuilder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogWarning("Timeout occurred in {Context} after {Timeout}s", context, options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            });
        }
    }

    private void AddRetryStrategy(ResiliencePipelineBuilderBase pipelineBuilder, RetryOptions options, string context)
    {
        if (pipelineBuilder is ResiliencePipelineBuilder builder)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                DelayGenerator = args =>
                {
                    var delay = options.BackoffType switch
                    {
                        BackoffType.Constant => TimeSpan.FromSeconds(options.BaseDelaySeconds),
                        BackoffType.Linear => TimeSpan.FromSeconds(options.BaseDelaySeconds * (args.AttemptNumber + 1)),
                        BackoffType.Exponential => TimeSpan.FromSeconds(options.BaseDelaySeconds * Math.Pow(2, args.AttemptNumber)),
                        BackoffType.ExponentialWithJitter => TimeSpan.FromSeconds(
                            options.BaseDelaySeconds * Math.Pow(2, args.AttemptNumber) *
                            (1 + Random.Shared.NextDouble() * options.JitterFactor)),
                        _ => TimeSpan.FromSeconds(options.BaseDelaySeconds)
                    };

                    if (delay.TotalSeconds > options.MaxDelaySeconds)
                    {
                        delay = TimeSpan.FromSeconds(options.MaxDelaySeconds);
                    }

                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry attempt {Attempt} in {Context}. Exception: {Exception}",
                        args.AttemptNumber, context, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            });
        }
        else if (pipelineBuilder is ResiliencePipelineBuilder<object> genericBuilder)
        {
            genericBuilder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry attempt {Attempt} in {Context}", args.AttemptNumber, context);
                    return ValueTask.CompletedTask;
                }
            });
        }
    }

    private void AddCircuitBreakerStrategy(ResiliencePipelineBuilderBase pipelineBuilder, CircuitBreakerOptions options, string context)
    {
        if (pipelineBuilder is ResiliencePipelineBuilder builder)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.FailureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit breaker opened in {Context}. Failure ratio: {FailureRatio}",
                        context, options.FailureRatio);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed in {Context}", context);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-opened in {Context}", context);
                    return ValueTask.CompletedTask;
                }
            });
        }
        else if (pipelineBuilder is ResiliencePipelineBuilder<object> genericBuilder)
        {
            genericBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.FailureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit breaker opened in {Context}", context);
                    return ValueTask.CompletedTask;
                }
            });
        }
    }

    // Rate Limiter implementation commented out due to Polly v8 API changes
    // TODO: Re-implement with correct Polly v8 rate limiter API
    private void AddRateLimiterStrategy(ResiliencePipelineBuilderBase pipelineBuilder, RateLimiterOptions options, string context)
    {
        _logger.LogWarning("Rate limiter strategy not implemented for Polly v8 in {Context}", context);
        // Implementation will be added in future version
    }

    // Bulkhead implementation commented out due to Polly v8 API changes
    // TODO: Re-implement with correct Polly v8 bulkhead API
    private void AddBulkheadStrategy(ResiliencePipelineBuilderBase pipelineBuilder, BulkheadOptions options, string context)
    {
        _logger.LogWarning("Bulkhead strategy not implemented for Polly v8 in {Context}", context);
        // Implementation will be added in future version
    }

    // Hedging implementation commented out due to Polly v8 API changes
    // TODO: Re-implement with correct Polly v8 hedging API
    private void AddHedgingStrategy(ResiliencePipelineBuilderBase pipelineBuilder, HedgingOptions options, string context)
    {
        _logger.LogWarning("Hedging strategy not implemented for Polly v8 in {Context}", context);
        // Implementation will be added in future version
    }

    // Fallback implementation commented out due to Polly v8 API changes
    // TODO: Re-implement with correct Polly v8 fallback API
    private void AddFallbackStrategy(ResiliencePipelineBuilderBase pipelineBuilder, FallbackOptions options, string context)
    {
        _logger.LogWarning("Fallback strategy not implemented for Polly v8 in {Context}", context);
        // Implementation will be added in future version
    }

    private HttpResponseMessage GetCachedResponse()
    {
        // Implement cached response logic
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"source\":\"cache\",\"message\":\"Cached response\"}")
        };
    }

    private HttpResponseMessage GetAlternativeResponse()
    {
        // Implement alternative response logic
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"source\":\"fallback\",\"message\":\"Alternative response\"}")
        };
    }
}