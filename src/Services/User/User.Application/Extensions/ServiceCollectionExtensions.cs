using FluentValidation;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using User.Application.Mapping;
using User.Application.Services;
using User.Core.Interfaces;

namespace User.Application.Extensions;

/// <summary>
/// Service collection extensions for User Application layer
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add User Application services to dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        // Add AutoMapper
        services.AddAutoMapper(typeof(UserMappingProfile));

        // Add Application Services
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add MediatR for domain events
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}