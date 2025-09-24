using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Middleware;
using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace Enterprise.Shared.Observability.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure settings
        var settings = new ObservabilitySettings();
        var configSection = configuration.GetSection("ObservabilitySettings");
        configSection.Bind(settings);
        services.AddSingleton(settings);
        services.Configure<ObservabilitySettings>(configSection);
        
        // Add core services
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddScoped<ITracingService, OpenTelemetryTracingService>();
        services.AddSingleton<IMetricsService, PrometheusMetricsService>();
        services.AddScoped<IAdvancedHealthChecks, AdvancedHealthChecksService>();
        services.AddScoped<IBusinessMetricsCollector, BusinessMetricsCollector>();
        services.AddSingleton<IBusinessMetricsRepository, InMemoryBusinessMetricsRepository>();
        
        // Add HttpClient factory for health checks
        services.AddHttpClient("HealthChecks");
        
        // Configure OpenTelemetry
        if (settings.EnableTracing)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(settings.ServiceName, settings.ServiceVersion)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = settings.Environment,
                        ["service.namespace"] = "Enterprise",
                        ["service.instance.id"] = Environment.MachineName
                    }))
                .WithTracing(tracing =>
                {
                    tracing.SetSampler(new TraceIdRatioBasedSampler(settings.SamplingRate));
                    
                    if (settings.Tracing.EnableHttpInstrumentation)
                        tracing.AddAspNetCoreInstrumentation()
                               .AddHttpClientInstrumentation();
                    
                    // SQL Client instrumentation temporarily disabled - requires stable package
                    // if (settings.Tracing.EnableSqlInstrumentation)
                    //     tracing.AddSqlClientInstrumentation();
                    
                    if (settings.Tracing.ConsoleExporter)
                        tracing.AddConsoleExporter();
                    
                    // Add custom activity source
                    tracing.AddSource("Enterprise.Platform");
                })
                .WithMetrics(metrics =>
                {
                    if (settings.Metrics.EnableRuntimeMetrics)
                        metrics.AddRuntimeInstrumentation();
                    
                    if (settings.Metrics.EnableHttpMetrics)
                        metrics.AddHttpClientInstrumentation();
                    
                    // Process instrumentation temporarily disabled - requires stable package
                    // if (settings.Metrics.EnableProcessMetrics)
                    //     metrics.AddProcessInstrumentation();
                    
                    metrics.AddMeter("Enterprise.Platform");
                });
        }
        
        // Configure Health Checks
        if (settings.EnableHealthChecks)
        {
            var healthChecksBuilder = services.AddHealthChecks();
            
            // Add custom health check
            healthChecksBuilder.AddTypeActivatedCheck<SystemHealthCheck>(
                "system",
                HealthStatus.Degraded,
                tags: new[] { "system" });
            
            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(5);
                options.Period = TimeSpan.FromSeconds(settings.HealthChecks.CheckIntervalSeconds);
            });
        }
        
        return services;
    }
    
    public static IApplicationBuilder UseSharedObservability(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<ObservabilitySettings>();
        
        // Add Correlation ID middleware
        app.UseMiddleware<CorrelationIdMiddleware>();
        
        // Add metrics middleware
        if (settings.EnableMetrics)
        {
            app.UseMiddleware<MetricsMiddleware>();
            
            // Configure Prometheus endpoint
            app.UseHttpMetrics();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics(settings.Metrics.PrometheusEndpoint);
            });
        }
        
        // Add health check endpoints
        if (settings.EnableHealthChecks)
        {
            app.UseHealthChecks(settings.HealthChecks.Endpoint);
            
            app.UseHealthChecks(settings.HealthChecks.LiveEndpoint, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            });
            
            app.UseHealthChecks(settings.HealthChecks.ReadyEndpoint, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            
            app.UseHealthChecks(settings.HealthChecks.DetailedEndpoint, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration.TotalMilliseconds,
                        entries = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration.TotalMilliseconds,
                            description = e.Value.Description,
                            data = e.Value.Data,
                            exception = e.Value.Exception?.Message
                        })
                    });
                    await context.Response.WriteAsync(json);
                }
            });
        }
        
        return app;
    }
}

// Custom health check implementation
public class SystemHealthCheck : IHealthCheck
{
    private readonly IAdvancedHealthChecks _healthChecks;
    
    public SystemHealthCheck(IAdvancedHealthChecks healthChecks)
    {
        _healthChecks = healthChecks;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var tasks = new[]
        {
            _healthChecks.CheckMemoryUsageAsync(cancellationToken),
            _healthChecks.CheckCpuUsageAsync(cancellationToken)
        };
        
        var results = await Task.WhenAll(tasks);
        
        if (results.All(r => r.Status == HealthStatus.Healthy))
            return HealthCheckResult.Healthy("System is healthy");
        
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthCheckResult.Unhealthy("System is unhealthy");
        
        return HealthCheckResult.Degraded("System is degraded");
    }
}