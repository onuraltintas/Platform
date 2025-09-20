namespace Enterprise.Shared.Auditing.Extensions;

/// <summary>
/// Extension methods for registering audit services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds audit services with configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration root</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditing(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddAuditing(configuration.GetSection(AuditConfiguration.SectionName));
    }

    /// <summary>
    /// Adds audit services with configuration section
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configurationSection">The configuration section</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditing(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<AuditConfiguration>(configurationSection);
        return services.AddAuditingCore();
    }

    /// <summary>
    /// Adds audit services with configuration action
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditing(this IServiceCollection services, Action<AuditConfiguration> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddAuditingCore();
    }

    /// <summary>
    /// Adds audit services with default configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditing(this IServiceCollection services)
    {
        services.Configure<AuditConfiguration>(options => { });
        return services.AddAuditingCore();
    }

    /// <summary>
    /// Adds in-memory audit store (for testing)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInMemoryAuditStore(this IServiceCollection services)
    {
        services.AddSingleton<IAuditStore, InMemoryAuditStore>();
        return services;
    }

    /// <summary>
    /// Adds custom audit store implementation
    /// </summary>
    /// <typeparam name="TImplementation">The audit store implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditStore<TImplementation>(
        this IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TImplementation : class, IAuditStore
    {
        services.Add(ServiceDescriptor.Describe(typeof(IAuditStore), typeof(TImplementation), lifetime));
        return services;
    }

    /// <summary>
    /// Adds custom audit context provider
    /// </summary>
    /// <typeparam name="TImplementation">The context provider implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditContextProvider<TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TImplementation : class, IAuditContextProvider
    {
        services.Add(ServiceDescriptor.Describe(typeof(IAuditContextProvider), typeof(TImplementation), lifetime));
        return services;
    }

    /// <summary>
    /// Adds audit interceptors for automatic auditing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditInterceptors(this IServiceCollection services)
    {
        // Add Castle DynamicProxy for interceptors
        services.AddSingleton<IProxyGenerator>(new ProxyGenerator());
        services.AddScoped<AuditInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds health checks for audit services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<AuditHealthCheck>("audit", tags: new[] { "audit", "ready" });

        return services;
    }

    /// <summary>
    /// Core audit services registration
    /// </summary>
    private static IServiceCollection AddAuditingCore(this IServiceCollection services)
    {
        // Add required services
        services.AddLogging();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Add audit services
        services.TryAddScoped<IAuditService, AuditService>();
        services.TryAddScoped<IAuditContextProvider, HttpAuditContextProvider>();

        // Add default in-memory store if no store is registered
        services.TryAddSingleton<IAuditStore, InMemoryAuditStore>();

        return services;
    }
}

/// <summary>
/// Extension methods for application builder
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds audit middleware to the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseAuditing(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditMiddleware>();
    }

    /// <summary>
    /// Adds correlation ID middleware for audit tracking
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="headerName">The correlation ID header name</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app, string headerName = "X-Correlation-ID")
    {
        return app.UseMiddleware<CorrelationIdMiddleware>(headerName);
    }
}

/// <summary>
/// Extension methods for host builder
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures audit services for the host
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <param name="configureAudit">Configuration action</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder ConfigureAuditing(
        this IHostBuilder builder, 
        Action<AuditConfiguration> configureAudit)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddAuditing(configureAudit);
        });
    }

    /// <summary>
    /// Configures audit services with configuration section
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <param name="sectionName">Configuration section name</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder ConfigureAuditing(
        this IHostBuilder builder, 
        string sectionName = AuditConfiguration.SectionName)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddAuditing(context.Configuration.GetSection(sectionName));
        });
    }
}

/// <summary>
/// Extension methods for web application builder
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds comprehensive audit services to the web application
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <param name="configureAudit">Optional configuration action</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplicationBuilder AddAuditServices(
        this WebApplicationBuilder builder, 
        Action<AuditConfiguration>? configureAudit = null)
    {
        // Add audit services
        if (configureAudit != null)
        {
            builder.Services.AddAuditing(configureAudit);
        }
        else
        {
            builder.Services.AddAuditing(builder.Configuration);
        }

        // Add interceptors and health checks
        builder.Services.AddAuditInterceptors();
        builder.Services.AddAuditHealthChecks();

        return builder;
    }

    /// <summary>
    /// Adds audit services with in-memory store (for development)
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplicationBuilder AddDevelopmentAuditing(this WebApplicationBuilder builder)
    {
        return builder.AddAuditServices(options =>
        {
            options.DefaultEnvironment = "Development";
            options.EnableBatchProcessing = false;
            options.Retention.EnableAutoPurge = false;
            options.Performance.UseAsyncProcessing = false;
        });
    }

    /// <summary>
    /// Adds production-ready audit services
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplicationBuilder AddProductionAuditing(this WebApplicationBuilder builder)
    {
        return builder.AddAuditServices(options =>
        {
            options.DefaultEnvironment = "Production";
            options.EnableBatchProcessing = true;
            options.Retention.EnableAutoPurge = true;
            options.Performance.UseAsyncProcessing = true;
            options.Security.EnableAlerting = true;
            options.Security.EncryptSensitiveData = true;
        });
    }
}