using Enterprise.Shared.Configuration.Interfaces;
using Enterprise.Shared.Configuration.Models;
using Enterprise.Shared.Configuration.Services;
using Enterprise.Shared.Configuration.Validators;

namespace Enterprise.Shared.Configuration.Extensions;

/// <summary>
/// Extension methods for configuring shared configuration services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared configuration services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure strongly typed configuration sections
        ConfigureStronglyTypedSettings(services, configuration);

        // Register configuration services
        RegisterConfigurationServices(services);

        // Register validators
        RegisterValidators(services);

        return services;
    }

    /// <summary>
    /// Adds shared configuration services with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configureOptions">Action to configure additional options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ConfigurationOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new ConfigurationOptions();
        configureOptions(options);

        // Configure strongly typed settings
        ConfigureStronglyTypedSettings(services, configuration);

        // Register configuration services based on options
        if (options.EnableConfigurationService)
        {
            services.AddSingleton<IConfigurationService, ConfigurationService>();
        }

        if (options.EnableFeatureFlags)
        {
            services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        }

        if (options.EnableChangeTracking)
        {
            services.AddSingleton<IConfigurationChangeTracker, ConfigurationChangeTracker>();
        }

        // Register user context service
        if (options.UserContextServiceType is not null)
        {
            services.AddScoped(typeof(IUserContextService), options.UserContextServiceType);
        }
        else
        {
            services.AddSingleton<IUserContextService, DefaultUserContextService>();
        }

        // Register validators if enabled
        if (options.EnableValidation)
        {
            RegisterValidators(services);
        }

        return services;
    }

    /// <summary>
    /// Adds feature flag services only
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure configuration settings
        services.Configure<ConfigurationSettings>(configuration.GetSection(ConfigurationSettings.SectionName));

        // Register minimal services for feature flags
        services.AddMemoryCache();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<IUserContextService, DefaultUserContextService>();

        return services;
    }

    /// <summary>
    /// Validates configuration on application startup
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ValidateConfigurationOnStartup(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add options validation
        services.AddOptions<ConfigurationSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RabbitMQSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static void ConfigureStronglyTypedSettings(IServiceCollection services, IConfiguration configuration)
    {
        // Configure strongly typed configuration objects
        services.Configure<ConfigurationSettings>(configuration.GetSection(ConfigurationSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));

        // Add memory cache if not already registered
        services.AddMemoryCache();
    }

    private static void RegisterConfigurationServices(IServiceCollection services)
    {
        // Register core configuration services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<IConfigurationChangeTracker, ConfigurationChangeTracker>();

        // Register default user context service (can be overridden)
        services.TryAddSingleton<IUserContextService, DefaultUserContextService>();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        // Register configuration validators
        services.AddSingleton<IValidateOptions<ConfigurationSettings>, ConfigurationSettingsValidator>();
        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        services.AddSingleton<IValidateOptions<RedisSettings>, RedisSettingsValidator>();
        services.AddSingleton<IValidateOptions<RabbitMQSettings>, RabbitMQSettingsValidator>();
    }
}

/// <summary>
/// Configuration options for shared configuration services
/// </summary>
public sealed class ConfigurationOptions
{
    /// <summary>
    /// Whether to enable the configuration service
    /// </summary>
    public bool EnableConfigurationService { get; set; } = true;

    /// <summary>
    /// Whether to enable feature flags service
    /// </summary>
    public bool EnableFeatureFlags { get; set; } = true;

    /// <summary>
    /// Whether to enable configuration change tracking
    /// </summary>
    public bool EnableChangeTracking { get; set; } = true;

    /// <summary>
    /// Whether to enable configuration validation
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Custom user context service type (optional)
    /// </summary>
    public Type? UserContextServiceType { get; set; }

    /// <summary>
    /// Custom configuration change tracker type (optional)
    /// </summary>
    public Type? ChangeTrackerType { get; set; }
}