using Gateway.API.Middleware;

namespace Gateway.API.Extensions;

/// <summary>
/// Application builder extensions for Gateway middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use security headers middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Use Gateway request/response logging middleware
    /// </summary>
    public static IApplicationBuilder UseGatewayLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }

    /// <summary>
    /// Use Gateway rate limiting middleware
    /// </summary>
    public static IApplicationBuilder UseGatewayRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Use Gateway authorization middleware
    /// </summary>
    public static IApplicationBuilder UseGatewayAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GatewayAuthorizationMiddleware>();
    }

    /// <summary>
    /// Use Gateway error handling middleware
    /// </summary>
    public static IApplicationBuilder UseGatewayErrorHandling(this IApplicationBuilder app)
    {
        // TODO: Implement custom error handling middleware
        return app;
    }
}