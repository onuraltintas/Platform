using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using EgitimPlatform.Shared.Logging.Configuration;
using EgitimPlatform.Shared.Logging.Middleware;
using EgitimPlatform.Shared.Logging.Services;

namespace EgitimPlatform.Shared.Logging.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));
        services.AddScoped<IStructuredLogger, StructuredLogger>();
        
        return services;
    }
    
    public static IHostBuilder UseStructuredLogging(this IHostBuilder hostBuilder, IConfiguration? configuration = null)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var config = configuration ?? context.Configuration;
            ConfigureSerilog(loggerConfiguration, config);
        });
    }
    
    public static WebApplicationBuilder UseStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            ConfigureSerilog(loggerConfiguration, context.Configuration);
        });
        
        return builder;
    }
    
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
    
    private static void ConfigureSerilog(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var loggingOptions = configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();
        
        loggerConfiguration
            .MinimumLevel.Is(ParseLogLevel(loggingOptions.MinimumLevel))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
        
        ConfigureEnrichment(loggerConfiguration, loggingOptions.Enrichment);
        ConfigureSinks(loggerConfiguration, loggingOptions);
    }
    
    private static void ConfigureEnrichment(LoggerConfiguration loggerConfiguration, EnrichmentOptions enrichment)
    {
        if (enrichment.WithMachineName)
            loggerConfiguration.Enrich.WithMachineName();
            
        if (enrichment.WithEnvironmentUserName)
            loggerConfiguration.Enrich.WithEnvironmentUserName();
            
        if (enrichment.WithProcessId)
            loggerConfiguration.Enrich.WithProcessId();
            
        if (enrichment.WithProcessName)
            loggerConfiguration.Enrich.WithProcessName();
            
        if (enrichment.WithThreadId)
            loggerConfiguration.Enrich.WithThreadId();
            
        if (enrichment.WithThreadName)
            loggerConfiguration.Enrich.WithThreadName();
            
        loggerConfiguration.Enrich.FromLogContext();
    }
    
    private static void ConfigureSinks(LoggerConfiguration loggerConfiguration, LoggingOptions loggingOptions)
    {
        if (loggingOptions.Console.Enabled)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: loggingOptions.Console.OutputTemplate);
        }
        
        if (loggingOptions.File.Enabled)
        {
            loggerConfiguration.WriteTo.File(
                path: loggingOptions.File.Path,
                rollingInterval: Enum.Parse<RollingInterval>(loggingOptions.File.RollingInterval),
                fileSizeLimitBytes: loggingOptions.File.FileSizeLimitBytes,
                retainedFileCountLimit: loggingOptions.File.RetainedFileCountLimit,
                outputTemplate: loggingOptions.File.OutputTemplate);
        }
        
        if (loggingOptions.Elasticsearch.Enabled)
        {
            var elasticsearchOptions = new ElasticsearchSinkOptions(new Uri(loggingOptions.Elasticsearch.Uri))
            {
                IndexFormat = loggingOptions.Elasticsearch.IndexFormat,
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
            };
            
            if (!string.IsNullOrEmpty(loggingOptions.Elasticsearch.Username))
            {
                elasticsearchOptions.ModifyConnectionSettings = conn =>
                    conn.BasicAuthentication(loggingOptions.Elasticsearch.Username, loggingOptions.Elasticsearch.Password);
            }
            
            loggerConfiguration.WriteTo.Elasticsearch(elasticsearchOptions);
        }
        
        if (loggingOptions.Seq.Enabled)
        {
            loggerConfiguration.WriteTo.Seq(
                serverUrl: loggingOptions.Seq.ServerUrl,
                apiKey: string.IsNullOrEmpty(loggingOptions.Seq.ApiKey) ? null : loggingOptions.Seq.ApiKey);
        }
    }
    
    private static LogEventLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" or "critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}