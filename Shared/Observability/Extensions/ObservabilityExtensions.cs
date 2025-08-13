using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using EgitimPlatform.Shared.Observability.Configuration;
using EgitimPlatform.Shared.Observability.Metrics;
using EgitimPlatform.Shared.Observability.Middleware;
using EgitimPlatform.Shared.Observability.Tracing;

namespace EgitimPlatform.Shared.Observability.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string? serviceVersion = null)
    {
        var observabilityOptions = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(observabilityOptions);
        
        // Override service name and version if provided
        if (!string.IsNullOrEmpty(serviceName))
        {
            observabilityOptions.ServiceName = serviceName;
        }
        
        if (!string.IsNullOrEmpty(serviceVersion))
        {
            observabilityOptions.ServiceVersion = serviceVersion;
        }

        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));
        services.AddSingleton<ApplicationMetrics>();

        // Add OpenTelemetry
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(observabilityOptions.ServiceName, observabilityOptions.ServiceVersion, observabilityOptions.ServiceInstanceId)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", observabilityOptions.Environment),
                new KeyValuePair<string, object>("service.instance.id", observabilityOptions.ServiceInstanceId),
                new KeyValuePair<string, object>("telemetry.sdk.name", "opentelemetry"),
                new KeyValuePair<string, object>("telemetry.sdk.language", "dotnet"),
                new KeyValuePair<string, object>("telemetry.sdk.version", "1.7.0")
            });

        // Add Tracing
        if (observabilityOptions.Tracing.Enabled)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource(ApplicationActivitySource.SourceName)
                        .SetSampler(new TraceIdRatioBasedSampler(observabilityOptions.Tracing.SamplingRatio));

                    // Add instrumentations
                    if (observabilityOptions.Tracing.AspNetCore.Enabled)
                    {
                        tracerProviderBuilder.AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = observabilityOptions.Tracing.AspNetCore.RecordException;
                            options.Filter = (httpContext) =>
                            {
                                var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
                                return !observabilityOptions.Tracing.AspNetCore.IgnorePatterns.Any(pattern => 
                                    path.Contains(pattern.ToLowerInvariant()));
                            };
                        });
                    }

                    if (observabilityOptions.Tracing.Http.Enabled)
                    {
                        tracerProviderBuilder.AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = observabilityOptions.Tracing.Http.RecordException;
                            options.FilterHttpRequestMessage = (httpRequestMessage) =>
                            {
                                var uri = httpRequestMessage.RequestUri?.ToString().ToLowerInvariant() ?? "";
                                return !observabilityOptions.Tracing.Http.IgnorePatterns.Any(pattern => 
                                    uri.Contains(pattern.ToLowerInvariant()));
                            };
                        });
                    }

                    if (observabilityOptions.Tracing.Sql.Enabled)
                    {
                        tracerProviderBuilder.AddSqlClientInstrumentation(options =>
                        {
                            options.SetDbStatementForText = observabilityOptions.Tracing.Sql.SetDbStatementForText;
                            options.SetDbStatementForStoredProcedure = observabilityOptions.Tracing.Sql.SetDbStatementForStoredProcedure;
                            options.RecordException = observabilityOptions.Tracing.Sql.RecordException;
                            options.EnableConnectionLevelAttributes = observabilityOptions.Tracing.Sql.EnableConnectionLevelAttributes;
                        });
                    }

                    if (observabilityOptions.Tracing.EntityFramework.Enabled)
                    {
                        tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation(options =>
                        {
                            options.SetDbStatementForText = observabilityOptions.Tracing.EntityFramework.SetDbStatementForText;
                            options.SetDbStatementForStoredProcedure = observabilityOptions.Tracing.EntityFramework.SetDbStatementForStoredProcedure;
                        });
                    }

                    // Add exporters
                    if (observabilityOptions.Tracing.TraceConsoleExporter)
                    {
                        tracerProviderBuilder.AddConsoleExporter();
                    }

                    if (observabilityOptions.Exporters.Jaeger.Enabled)
                    {
                        tracerProviderBuilder.AddJaegerExporter(options =>
                        {
                            options.Endpoint = new Uri(observabilityOptions.Exporters.Jaeger.Endpoint);
                        });
                    }

                    if (observabilityOptions.Exporters.Zipkin.Enabled)
                    {
                        tracerProviderBuilder.AddZipkinExporter(options =>
                        {
                            options.Endpoint = new Uri(observabilityOptions.Exporters.Zipkin.Endpoint);
                        });
                    }

                    if (observabilityOptions.Exporters.Otlp.Enabled)
                    {
                        tracerProviderBuilder.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(observabilityOptions.Exporters.Otlp.Endpoint);
                            options.Protocol = observabilityOptions.Exporters.Otlp.Protocol.ToLowerInvariant() switch
                            {
                                "grpc" => OtlpExportProtocol.Grpc,
                                "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
                                _ => OtlpExportProtocol.Grpc
                            };
                            
                            if (observabilityOptions.Exporters.Otlp.Headers.Any())
                            {
                                options.Headers = string.Join(",", observabilityOptions.Exporters.Otlp.Headers.Select(h => $"{h.Key}={h.Value}"));
                            }
                        });
                    }
                });
        }

        // Add Metrics
        if (observabilityOptions.Metrics.Enabled)
        {
            services.AddOpenTelemetry()
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .SetResourceBuilder(resourceBuilder)
                        .AddMeter(observabilityOptions.ServiceName);

                    if (observabilityOptions.Metrics.IncludeDefaultMetrics)
                    {
                        meterProviderBuilder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation();
                    }

                    if (observabilityOptions.Metrics.ConsoleExporter)
                    {
                        meterProviderBuilder.AddConsoleExporter();
                    }

                    if (observabilityOptions.Metrics.PrometheusEnabled)
                    {
                        meterProviderBuilder.AddPrometheusExporter();
                    }

                    if (observabilityOptions.Exporters.Otlp.Enabled)
                    {
                        meterProviderBuilder.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(observabilityOptions.Exporters.Otlp.Endpoint);
                            options.Protocol = observabilityOptions.Exporters.Otlp.Protocol.ToLowerInvariant() switch
                            {
                                "grpc" => OtlpExportProtocol.Grpc,
                                "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
                                _ => OtlpExportProtocol.Grpc
                            };
                        });
                    }
                });
        }

        return services;
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app, IConfiguration configuration)
    {
        var observabilityOptions = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(observabilityOptions);

        // Add tracing middleware
        if (observabilityOptions.Tracing.Enabled)
        {
            app.UseMiddleware<TracingMiddleware>();
        }

        // Add metrics middleware
        if (observabilityOptions.Metrics.Enabled)
        {
            app.UseMiddleware<MetricsMiddleware>();
        }

        // Add Prometheus metrics endpoint
        if (observabilityOptions.Metrics.PrometheusEnabled)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics(observabilityOptions.Metrics.PrometheusEndpoint);
            });
        }

        return app;
    }

    public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ApplicationMetrics>();
        
        // Start default metrics collectors
        var server = new MetricServer(port: 9090);
        server.Start();

        return services;
    }

    public static IHost UsePrometheusMetrics(this IHost host)
    {
        // Start collecting default .NET metrics
        var defaultCollectors = new IMetricServer[]
        {
            new MetricServer(hostname: "*", port: 9090)
        };

        foreach (var collector in defaultCollectors)
        {
            collector.Start();
        }

        return host;
    }

    public static void UseObservabilityHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var observabilityOptions = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(observabilityOptions);

        var healthCheckBuilder = services.AddHealthChecks();

        // Note: URL health checks require AspNetCore.HealthChecks.Uris package
        // These are commented out to avoid dependency issues
        /*
        if (observabilityOptions.Exporters.Jaeger.Enabled)
        {
            healthCheckBuilder.AddUrlGroup(new Uri(observabilityOptions.Exporters.Jaeger.Endpoint), 
                name: "jaeger", tags: new[] { "observability", "tracing" });
        }

        if (observabilityOptions.Exporters.Zipkin.Enabled)
        {
            healthCheckBuilder.AddUrlGroup(new Uri(observabilityOptions.Exporters.Zipkin.Endpoint), 
                name: "zipkin", tags: new[] { "observability", "tracing" });
        }

        if (observabilityOptions.Exporters.Otlp.Enabled)
        {
            healthCheckBuilder.AddUrlGroup(new Uri(observabilityOptions.Exporters.Otlp.Endpoint), 
                name: "otlp", tags: new[] { "observability", "telemetry" });
        }
        */
    }
}