using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public class RetryPolicyFactory
{
    private readonly RetryPolicySettings _settings;
    private readonly ILogger<RetryPolicyFactory> _logger;

    public RetryPolicyFactory(IOptions<ErrorHandlingSettings> options, ILogger<RetryPolicyFactory> logger)
    {
        _settings = options.Value.RetryPolicy;
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode && ShouldRetryHttpStatusCode(msg.StatusCode))
            .WaitAndRetryAsync(
                _settings.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    Math.Min(_settings.InitialDelayMs * Math.Pow(_settings.BackoffMultiplier, retryAttempt - 1), 
                            _settings.MaxDelayMs)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetValueOrDefault("logger") as ILogger ?? _logger;
                    
                    if (outcome.Exception != null)
                    {
                        logger.LogWarning(outcome.Exception,
                            "HTTP retry {RetryCount}/{MaxRetries} after {Delay}ms due to exception", 
                            retryCount, _settings.MaxRetryAttempts, timespan.TotalMilliseconds);
                    }
                    else
                    {
                        logger.LogWarning(
                            "HTTP retry {RetryCount}/{MaxRetries} after {Delay}ms due to status code {StatusCode}", 
                            retryCount, _settings.MaxRetryAttempts, timespan.TotalMilliseconds, outcome.Result.StatusCode);
                    }
                });
    }

    public IAsyncPolicy CreateDatabaseRetryPolicy()
    {
        return Policy
            .Handle<System.Data.Common.DbException>(ex => IsTransientDbError(ex))
            .Or<TimeoutException>()
            .Or<InvalidOperationException>(ex => ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(
                _settings.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    Math.Min(_settings.InitialDelayMs * Math.Pow(_settings.BackoffMultiplier, retryAttempt - 1), 
                            _settings.MaxDelayMs)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    var logger = context.GetValueOrDefault("logger") as ILogger ?? _logger;
                        
                    logger.LogWarning(exception, 
                        "Database retry {RetryCount}/{MaxRetries} after {Delay}ms", 
                        retryCount, _settings.MaxRetryAttempts, timespan.TotalMilliseconds);
                });
    }

    public IAsyncPolicy<T> CreateGenericRetryPolicy<T>()
    {
        return Policy<T>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _settings.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    Math.Min(_settings.InitialDelayMs * Math.Pow(_settings.BackoffMultiplier, retryAttempt - 1), 
                            _settings.MaxDelayMs)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    var logger = context.GetValueOrDefault("logger") as ILogger ?? _logger;
                        
                    logger.LogWarning(exception.Exception, 
                        "Generic retry {RetryCount}/{MaxRetries} after {Delay}ms", 
                        retryCount, _settings.MaxRetryAttempts, timespan.TotalMilliseconds);
                });
    }

    private bool IsTransientDbError(System.Data.Common.DbException ex)
    {
        var msg = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return msg.Contains("timeout") || msg.Contains("temporarily unavailable") || msg.Contains("deadlock");
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
}