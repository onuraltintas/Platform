using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EgitimPlatform.Shared.Configuration.ConfigurationOptions;

namespace EgitimPlatform.Shared.Configuration.Extensions;

public static class ConfigurationExtensions
{
    // Bu metot artık doğrudan Program.cs içinde çağrıldığı için burada tutmaya gerek kalmadı.
    // Ancak merkezi bir mantık istenirse tekrar aktif edilebilir. Şimdilik sadeleştiriyoruz.
    // public static IConfigurationBuilder AddDotEnv(this IConfigurationBuilder builder)
    // {
    //     builder.AddEnvironmentVariables();
    //     return builder;
    // }
    
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ApiGatewayOptions>(configuration.GetSection(ApiGatewayOptions.SectionName));
        
        return services;
    }
    
    public static T GetOptions<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var options = new T();
        configuration.GetSection(sectionName).Bind(options);
        return options;
    }
    
    public static void ValidateConfiguration(this IConfiguration configuration)
    {
        var requiredSections = new[]
        {
            DatabaseOptions.SectionName,
            JwtOptions.SectionName
        };
        
        foreach (var section in requiredSections)
        {
            if (!configuration.GetSection(section).Exists())
            {
                throw new InvalidOperationException($"Required configuration section '{section}' is missing.");
            }
        }
    }
}
