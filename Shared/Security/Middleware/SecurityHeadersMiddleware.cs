using Microsoft.AspNetCore.Http;

namespace EgitimPlatform.Shared.Security.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Remove server information
        context.Response.Headers.Remove("Server");
        
        // Add security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        
        // Content Security Policy
        var csp = "default-src 'self'; " +
                 "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                 "style-src 'self' 'unsafe-inline'; " +
                 "img-src 'self' data: https:; " +
                 "font-src 'self'; " +
                 "connect-src 'self'; " +
                 "frame-ancestors 'none'";
        context.Response.Headers.Append("Content-Security-Policy", csp);
        
        // HTTP Strict Transport Security (HSTS)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }
        
        await _next(context);
    }
}