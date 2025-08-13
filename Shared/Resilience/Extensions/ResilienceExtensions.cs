using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EgitimPlatform.Shared.Resilience.Configuration;
using EgitimPlatform.Shared.Resilience.Policies;
using EgitimPlatform.Shared.Resilience.Services;

namespace EgitimPlatform.Shared.Resilience.Extensions;

public static class ResilienceExtensions
{
    public static IServiceCollection AddResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resilienceOptions = new ResilienceOptions();
        configuration.GetSection(ResilienceOptions.SectionName).Bind(resilienceOptions);
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));

        // Register core services
        services.AddSingleton<IResiliencePolicyFactory, ResiliencePolicyFactory>();
        services.AddScoped<IResilientDatabaseService, ResilientDatabaseService>();
        services.AddScoped<IResilientMessagingService, ResilientMessagingService>();

        return services;
    }

    // Note: HTTP client resilience methods have been temporarily commented out
    // due to Polly v8 API changes. These will be re-implemented in a future version.
    
    // TODO: Implement AddResilientHttpClient methods with Polly v8 API
    // TODO: Implement AddResilienceHandler methods with Polly v8 API
}