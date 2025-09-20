using Enterprise.Shared.Resilience.Bulkhead;
using Enterprise.Shared.Resilience.CircuitBreaker;
using Enterprise.Shared.Resilience.Interfaces;
using Enterprise.Shared.Resilience.Models;
using Enterprise.Shared.Resilience.RateLimit;
using Enterprise.Shared.Resilience.Retry;
using Enterprise.Shared.Resilience.Services;
using Enterprise.Shared.Resilience.Timeout;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Resilience.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResilience(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure settings
        services.Configure<ResilienceSettings>(
            configuration.GetSection("Resilience"));
        
        // Validate settings
        services.AddSingleton<IValidateOptions<ResilienceSettings>, ResilienceSettingsValidator>();

        // Register services
        services.AddSingleton<ICircuitBreakerService, PollyCircuitBreakerService>();
        services.AddSingleton<IRetryService, PollyRetryService>();
        services.AddSingleton<IBulkheadService, BulkheadService>();
        services.AddSingleton<ITimeoutService, TimeoutService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IResilienceMonitoringService, ResilienceMonitoringService>();

        return services;
    }

    public static IServiceCollection AddResilience(this IServiceCollection services, Action<ResilienceSettings> configureSettings)
    {
        // Configure settings with action
        services.Configure(configureSettings);
        
        // Validate settings
        services.AddSingleton<IValidateOptions<ResilienceSettings>, ResilienceSettingsValidator>();

        // Register services
        services.AddSingleton<ICircuitBreakerService, PollyCircuitBreakerService>();
        services.AddSingleton<IRetryService, PollyRetryService>();
        services.AddSingleton<IBulkheadService, BulkheadService>();
        services.AddSingleton<ITimeoutService, TimeoutService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IResilienceMonitoringService, ResilienceMonitoringService>();

        return services;
    }

    public static IServiceCollection AddResilienceHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ResilienceHealthCheck>("resilience");

        return services;
    }

    public static IServiceCollection AddCircuitBreaker(this IServiceCollection services)
    {
        services.AddSingleton<ICircuitBreakerService, PollyCircuitBreakerService>();
        return services;
    }

    public static IServiceCollection AddRetryPolicy(this IServiceCollection services)
    {
        services.AddSingleton<IRetryService, PollyRetryService>();
        return services;
    }

    public static IServiceCollection AddBulkhead(this IServiceCollection services)
    {
        services.AddSingleton<IBulkheadService, BulkheadService>();
        return services;
    }

    public static IServiceCollection AddTimeout(this IServiceCollection services)
    {
        services.AddSingleton<ITimeoutService, TimeoutService>();
        return services;
    }

    public static IServiceCollection AddRateLimit(this IServiceCollection services)
    {
        services.AddSingleton<IRateLimitService, RateLimitService>();
        return services;
    }
}

public class ResilienceSettingsValidator : IValidateOptions<ResilienceSettings>
{
    public ValidateOptionsResult Validate(string? name, ResilienceSettings options)
    {
        var errors = new List<string>();

        // Circuit Breaker validation
        if (options.CircuitBreaker.FailureThreshold <= 0)
            errors.Add("CircuitBreaker.FailureThreshold must be greater than 0");
        
        if (options.CircuitBreaker.MinimumThroughput <= 0)
            errors.Add("CircuitBreaker.MinimumThroughput must be greater than 0");
        
        if (options.CircuitBreaker.SamplingDuration <= TimeSpan.Zero)
            errors.Add("CircuitBreaker.SamplingDuration must be greater than zero");
        
        if (options.CircuitBreaker.BreakDuration <= TimeSpan.Zero)
            errors.Add("CircuitBreaker.BreakDuration must be greater than zero");

        // Retry validation
        if (options.Retry.MaxRetryAttempts < 0)
            errors.Add("Retry.MaxRetryAttempts must be greater than or equal to 0");
        
        if (options.Retry.BaseDelayMs <= 0)
            errors.Add("Retry.BaseDelayMs must be greater than 0");
        
        if (options.Retry.MaxDelayMs < options.Retry.BaseDelayMs)
            errors.Add("Retry.MaxDelayMs must be greater than or equal to BaseDelayMs");

        // Bulkhead validation
        if (options.Bulkhead.MaxParallelization <= 0)
            errors.Add("Bulkhead.MaxParallelization must be greater than 0");
        
        if (options.Bulkhead.MaxQueuedActions < 0)
            errors.Add("Bulkhead.MaxQueuedActions must be greater than or equal to 0");

        // Timeout validation
        if (options.Timeout.DefaultTimeoutMs <= 0)
            errors.Add("Timeout.DefaultTimeoutMs must be greater than 0");
        
        if (options.Timeout.HttpTimeoutMs <= 0)
            errors.Add("Timeout.HttpTimeoutMs must be greater than 0");
        
        if (options.Timeout.DatabaseTimeoutMs <= 0)
            errors.Add("Timeout.DatabaseTimeoutMs must be greater than 0");

        // Rate Limit validation
        if (options.RateLimit.PermitLimit <= 0)
            errors.Add("RateLimit.PermitLimit must be greater than 0");
        
        if (options.RateLimit.Window <= TimeSpan.Zero)
            errors.Add("RateLimit.Window must be greater than zero");
        
        if (options.RateLimit.QueueLimit < 0)
            errors.Add("RateLimit.QueueLimit must be greater than or equal to 0");

        return errors.Any()
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}