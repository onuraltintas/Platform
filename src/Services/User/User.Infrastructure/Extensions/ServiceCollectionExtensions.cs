using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using User.Core.Interfaces;
using User.Infrastructure.Configuration;
using User.Infrastructure.Repositories;
using User.Infrastructure.Services;

namespace User.Infrastructure.Extensions;

/// <summary>
/// Service collection extensions for User Infrastructure layer
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add User Infrastructure services to dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="rabbitMqConnectionString">RabbitMQ connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddUserInfrastructure(this IServiceCollection services, string? rabbitMqConnectionString = null)
    {
        // Add Repository Pattern
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        // Add Infrastructure Services
        services.AddScoped<IEventSerializationService, EventSerializationService>();
        services.AddScoped<IEventErrorHandlingService, EventErrorHandlingService>();

        // MassTransit is configured via AddSharedEvents in Program.cs
        // Removed duplicate registration to prevent conflicts

        return services;
    }
}