using Enterprise.Shared.Logging.Enrichers;
using Enterprise.Shared.Logging.Extensions.Interceptors;
using Enterprise.Shared.Logging.Extensions.Middleware;
using Enterprise.Shared.Logging.Interfaces;
using Enterprise.Shared.Logging.Models;
using Enterprise.Shared.Logging.Services;
using Serilog;

namespace Enterprise.Shared.Logging.Extensions;

/// <summary>
/// Dependency injection extensions for Enterprise Logging
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds enterprise logging services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEnterpriseLogging(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register logging settings
        services.Configure<LoggingSettings>(configuration.GetSection(LoggingSettings.SectionName));

        // Register core services
        services.TryAddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.TryAddSingleton<IEnterpriseLoggerFactory, EnterpriseLoggerFactory>();
        services.TryAddTransient(typeof(IEnterpriseLogger<>), typeof(Services.EnterpriseLogger<>));

        // Register interceptor for AOP logging
        services.TryAddTransient<LoggingInterceptor>();

        // Register HTTP context accessor if not already registered
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }

    /// <summary>
    /// Adds enterprise logging services with custom settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureSettings">Settings configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEnterpriseLogging(
        this IServiceCollection services,
        Action<LoggingSettings> configureSettings)
    {
        services.Configure(configureSettings);

        // Register core services
        services.TryAddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.TryAddSingleton<IEnterpriseLoggerFactory, EnterpriseLoggerFactory>();
        services.TryAddTransient(typeof(IEnterpriseLogger<>), typeof(Services.EnterpriseLogger<>));

        // Register interceptor for AOP logging
        services.TryAddTransient<LoggingInterceptor>();

        // Register HTTP context accessor if not already registered
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }

    /// <summary>
    /// Configures Serilog with enterprise-specific enrichers
    /// </summary>
    /// <param name="configuration">Serilog logger configuration</param>
    /// <param name="services">Service provider for enrichers</param>
    /// <param name="settings">Logging settings</param>
    /// <returns>Logger configuration for chaining</returns>
    public static LoggerConfiguration ConfigureEnterpriseEnrichers(
        this LoggerConfiguration configuration,
        IServiceProvider services,
        LoggingSettings settings)
    {
        if (settings.EnableCorrelationId)
        {
            var correlationContextAccessor = services.GetService<ICorrelationContextAccessor>();
            configuration.Enrich.With(new CorrelationIdEnricher(correlationContextAccessor));
        }

        if (settings.EnableUserEnrichment)
        {
            var httpContextAccessor = services.GetService<IHttpContextAccessor>();
            configuration.Enrich.With(new UserEnricher(httpContextAccessor));
        }

        if (settings.EnableEnvironmentEnrichment)
        {
            configuration.Enrich.With(new ServiceEnricher(
                settings.ServiceName,
                settings.ServiceVersion,
                settings.Environment));
        }

        return configuration;
    }

    /// <summary>
    /// Creates a Serilog logger configuration with enterprise defaults
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <param name="services">Service provider</param>
    /// <returns>Configured Serilog logger</returns>
    public static Serilog.ILogger CreateEnterpriseLogger(
        IConfiguration configuration,
        IServiceProvider? services = null)
    {
        var settings = configuration.GetSection(LoggingSettings.SectionName)
            .Get<LoggingSettings>() ?? new LoggingSettings();

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        // Add enterprise enrichers if services are available
        if (services != null)
        {
            loggerConfig.ConfigureEnterpriseEnrichers(services, settings);
        }

        return loggerConfig.CreateLogger();
    }
}

/// <summary>
/// Application builder extensions for Enterprise Logging middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds performance logging middleware to the application pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEnterpriseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceLoggingMiddleware>();
    }

    /// <summary>
    /// Adds correlation ID middleware that ensures every request has a correlation ID
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationContextAccessor = context.RequestServices
                .GetRequiredService<ICorrelationContextAccessor>();

            // Create or get correlation ID
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                               context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
                               Guid.NewGuid().ToString();

            // Set correlation context
            var correlationContext = new CorrelationContext
            {
                CorrelationId = correlationId,
                RequestId = context.TraceIdentifier,
                UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                SessionId = context.Session?.Id
            };

            correlationContextAccessor.SetCorrelationContext(correlationContext);

            // Add to response headers
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            try
            {
                await next();
            }
            finally
            {
                correlationContextAccessor.ClearCorrelationContext();
            }
        });
    }
}

/// <summary>
/// Host builder extensions for Enterprise Logging
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog with enterprise logging settings
    /// </summary>
    /// <param name="hostBuilder">Host builder</param>
    /// <returns>Host builder for chaining</returns>
    public static IHostBuilder UseEnterpriseSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);

            var settings = context.Configuration.GetSection(LoggingSettings.SectionName)
                .Get<LoggingSettings>() ?? new LoggingSettings();

            configuration.ConfigureEnterpriseEnrichers(services, settings);
        });
    }

    /// <summary>
    /// Configures Serilog with custom configuration action
    /// </summary>
    /// <param name="hostBuilder">Host builder</param>
    /// <param name="configureLogger">Logger configuration action</param>
    /// <returns>Host builder for chaining</returns>
    public static IHostBuilder UseEnterpriseSerilog(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IServiceProvider, LoggerConfiguration> configureLogger)
    {
        return hostBuilder.UseSerilog(configureLogger);
    }
}