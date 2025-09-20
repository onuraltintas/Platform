using Enterprise.Shared.ErrorHandling.Middleware;
using Enterprise.Shared.ErrorHandling.Models;
using Microsoft.AspNetCore.Builder;

namespace Enterprise.Shared.ErrorHandling.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSharedErrorHandling(this IApplicationBuilder app)
    {
        // Use custom global exception middleware
        app.UseMiddleware<GlobalExceptionMiddleware>();
        
        return app;
    }

    public static IApplicationBuilder UseSharedErrorHandling(
        this IApplicationBuilder app, 
        Action<ErrorHandlingSettings> configureSettings)
    {
        // Configure settings if needed
        var settings = new ErrorHandlingSettings();
        configureSettings(settings);

        // Use developer exception page if enabled and in development
        if (settings.EnableDeveloperExceptionPage)
        {
            app.UseDeveloperExceptionPage();
        }

        // Use custom global exception middleware
        app.UseMiddleware<GlobalExceptionMiddleware>();
        
        return app;
    }

    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }
}