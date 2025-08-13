using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using EgitimPlatform.Shared.Errors.Middleware;
using EgitimPlatform.Shared.Errors.Services;

namespace EgitimPlatform.Shared.Errors.Extensions;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddScoped<IErrorService, ErrorService>();
        return services;
    }
    
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}