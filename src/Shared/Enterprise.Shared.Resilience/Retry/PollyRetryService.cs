using System.Collections.Concurrent;
using System.Net;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Enterprise.Shared.Resilience.Retry;

public class PollyRetryService : IRetryService
{
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _retryPipelines = new();
    private readonly ResilienceSettings _settings;
    private readonly ILogger<PollyRetryService> _logger;

    public PollyRetryService(
        IOptions<ResilienceSettings> settings,
        ILogger<PollyRetryService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string? retryKey = null, 
        CancellationToken cancellationToken = default)
    {
        var key = retryKey ?? "default";
        var pipeline = GetOrCreateRetryPipeline(key);

        var startTime = DateTime.UtcNow;
        var attemptCount = 0;

        try
        {
            var result = await pipeline.ExecuteAsync(async (context) =>
            {
                attemptCount++;
                
                if (_settings.Retry.EnableRetryLogging && attemptCount > 1)
                {
                    _logger.LogDebug("Retry attempt {AttemptCount} for operation {RetryKey}", 
                        attemptCount, key);
                }

                return await operation().ConfigureAwait(false);
            }, cancellationToken);

            if (_settings.Retry.EnableRetryLogging)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Retry operation {RetryKey} completed successfully in {AttemptCount} attempts, duration: {Duration}ms",
                    key, attemptCount, duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (_settings.Retry.EnableRetryLogging)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Retry operation {RetryKey} failed after {AttemptCount} attempts, duration: {Duration}ms",
                    key, attemptCount, duration.TotalMilliseconds);
            }
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string? retryKey = null, 
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, retryKey, cancellationToken);
    }

    public async Task<T> ExecuteWithCustomPolicyAsync<T>(Func<Task<T>> operation, RetryPolicy customPolicy,
        CancellationToken cancellationToken = default)
    {
        var pipeline = CreateCustomRetryPipeline(customPolicy);
        var startTime = DateTime.UtcNow;
        var attemptCount = 0;

        try
        {
            var result = await pipeline.ExecuteAsync(async (context) =>
            {
                attemptCount++;
                
                if (_settings.Retry.EnableRetryLogging && attemptCount > 1)
                {
                    _logger.LogDebug("Custom retry attempt {AttemptCount}", attemptCount);
                }

                return await operation().ConfigureAwait(false);
            }, cancellationToken);

            if (_settings.Retry.EnableRetryLogging)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Custom retry operation completed successfully in {AttemptCount} attempts, duration: {Duration}ms",
                    attemptCount, duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (_settings.Retry.EnableRetryLogging)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Custom retry operation failed after {AttemptCount} attempts, duration: {Duration}ms",
                    attemptCount, duration.TotalMilliseconds);
            }
            throw;
        }
        finally
        {
            // Custom pipeline cleanup is handled automatically
        }
    }

    public async Task ExecuteWithCustomPolicyAsync(Func<Task> operation, RetryPolicy customPolicy,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithCustomPolicyAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, customPolicy, cancellationToken);
    }

    private ResiliencePipeline GetOrCreateRetryPipeline(string key)
    {
        return _retryPipelines.GetOrAdd(key, CreateDefaultRetryPipeline);
    }

    private ResiliencePipeline CreateDefaultRetryPipeline(string key)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = _settings.Retry.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_settings.Retry.BaseDelayMs),
                BackoffType = _settings.Retry.BackoffType switch
                {
                    "Linear" => DelayBackoffType.Linear,
                    "Exponential" => DelayBackoffType.Exponential,
                    _ => DelayBackoffType.Constant
                },
                UseJitter = _settings.Retry.UseJitter,
                MaxDelay = TimeSpan.FromMilliseconds(_settings.Retry.MaxDelayMs),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ShouldRetryException),
                OnRetry = args =>
                {
                    if (_settings.Retry.EnableRetryLogging)
                    {
                        _logger.LogWarning("Retry {AttemptNumber} for {RetryKey} after {Delay}ms due to: {Exception}",
                            args.AttemptNumber, key, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    }
                    return default;
                }
            })
            .Build();

        if (_settings.Retry.EnableRetryLogging)
        {
            _logger.LogDebug("Created retry pipeline for key: {RetryKey}", key);
        }

        return pipeline;
    }

    private ResiliencePipeline CreateCustomRetryPipeline(RetryPolicy policy)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = policy.MaxAttempts,
                Delay = policy.BaseDelay,
                BackoffType = policy.BackoffType switch
                {
                    "Linear" => DelayBackoffType.Linear,
                    "Exponential" => DelayBackoffType.Exponential,
                    _ => DelayBackoffType.Constant
                },
                UseJitter = policy.UseJitter,
                MaxDelay = policy.MaxDelay,
                ShouldHandle = policy.ShouldRetry != null 
                    ? new PredicateBuilder().Handle<Exception>(policy.ShouldRetry)
                    : new PredicateBuilder().Handle<Exception>(ShouldRetryException),
                OnRetry = args =>
                {
                    if (_settings.Retry.EnableRetryLogging)
                    {
                        _logger.LogWarning("Custom retry {AttemptNumber} after {Delay}ms due to: {Exception}",
                            args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    }
                    return default;
                }
            })
            .Build();
    }

    private bool ShouldRetryException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            DbException dbEx => IsTransientDbError(dbEx),
            ExternalServiceException => true,
            BusinessRuleException => false,
            ValidationException => false,
            _ => false
        };
    }

    private bool ShouldRetryHttpStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            _ => false
        };
    }

    private static bool IsTransientDbError(DbException ex)
    {
        // Provider-agnostic basic detection by message keywords
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        if (message.Contains("timeout") || message.Contains("temporarily unavailable") || message.Contains("deadlock"))
            return true;
        return false;
    }
}