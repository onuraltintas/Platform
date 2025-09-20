using Enterprise.Shared.ErrorHandling.Handlers;
using Enterprise.Shared.ErrorHandling.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Enterprise.Shared.ErrorHandling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedErrorHandling(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure settings
        services.Configure<ErrorHandlingSettings>(
            configuration.GetSection(ErrorHandlingSettings.SectionName));

        // Register core services
        services.AddSingleton<ITimeZoneProvider, TimeZoneProvider>();
        services.AddSingleton<IErrorResponseFactory, ErrorResponseFactory>();
        services.AddSingleton<RetryPolicyFactory>();
        services.AddSingleton<CircuitBreakerFactory>();
        services.AddSingleton<IErrorMonitoringService, ErrorMonitoringService>();

        // Register filters
        services.AddScoped<EnterpriseExceptionFilter>();
        services.AddScoped<ValidationExceptionFilter>();

        // Add localization support if enabled
        var settings = configuration.GetSection(ErrorHandlingSettings.SectionName)
            .Get<ErrorHandlingSettings>() ?? new ErrorHandlingSettings();

        if (settings.EnableLocalization)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            ConfigureTurkishCulture(services, settings);
        }

        return services;
    }

    public static IServiceCollection AddSharedErrorHandling(
        this IServiceCollection services, 
        Action<ErrorHandlingSettings> configureSettings)
    {
        // Configure settings using action
        services.Configure(configureSettings);

        // Register core services
        services.AddSingleton<ITimeZoneProvider, TimeZoneProvider>();
        services.AddSingleton<IErrorResponseFactory, ErrorResponseFactory>();
        services.AddSingleton<RetryPolicyFactory>();
        services.AddSingleton<CircuitBreakerFactory>();
        services.AddSingleton<IErrorMonitoringService, ErrorMonitoringService>();

        // Register filters
        services.AddScoped<EnterpriseExceptionFilter>();
        services.AddScoped<ValidationExceptionFilter>();

        // Configure localization based on settings
        var settings = new ErrorHandlingSettings();
        configureSettings(settings);

        if (settings.EnableLocalization)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            ConfigureTurkishCulture(services, settings);
        }

        return services;
    }

    public static IServiceCollection AddErrorHandlingFilters(this IServiceCollection services)
    {
        // Register required dependencies for filters
        services.TryAddSingleton<IErrorResponseFactory, ErrorResponseFactory>();
        services.TryAddSingleton<ITimeZoneProvider, TimeZoneProvider>();
        
        // Add default settings if not configured
        services.TryAddSingleton(Options.Create(new ErrorHandlingSettings()));
        
        services.AddScoped<EnterpriseExceptionFilter>();
        services.AddScoped<ValidationExceptionFilter>();
        
        return services;
    }

    public static IServiceCollection AddRetryPolicies(this IServiceCollection services)
    {
        // Add default settings if not configured
        services.TryAddSingleton(Options.Create(new ErrorHandlingSettings()));
        
        services.AddSingleton<RetryPolicyFactory>();
        services.AddSingleton<CircuitBreakerFactory>();
        
        return services;
    }

    private static void ConfigureTurkishCulture(IServiceCollection services, ErrorHandlingSettings settings)
    {
        // Set Turkish culture as default
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var turkishCulture = new CultureInfo(settings.DefaultCulture);
            
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(turkishCulture);
            options.SupportedCultures = new List<CultureInfo> { turkishCulture };
            options.SupportedUICultures = new List<CultureInfo> { turkishCulture };

            // Configure request culture providers to prioritize Turkish
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
        });

        // Set thread culture for the application
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(settings.DefaultCulture);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(settings.DefaultCulture);
    }
}