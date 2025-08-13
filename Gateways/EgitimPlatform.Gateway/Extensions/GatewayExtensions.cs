using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using EgitimPlatform.Gateway.Middleware;

namespace EgitimPlatform.Gateway.Extensions;

public static class GatewayExtensions
{
    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add YARP reverse proxy
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        // Add rate limiting
        services.AddRateLimiter(options =>
        {
            var rateLimitConfig = configuration.GetSection("RateLimiting");
            
            // Global policy for anonymous users
            options.AddFixedWindowLimiter("GlobalPolicy", policyOptions =>
            {
                var globalConfig = rateLimitConfig.GetSection("GlobalPolicy");
                policyOptions.PermitLimit = globalConfig.GetValue<int>("PermitLimit", 100);
                policyOptions.Window = TimeSpan.Parse(globalConfig.GetValue<string>("Window") ?? "00:01:00");
                policyOptions.QueueLimit = globalConfig.GetValue<int>("QueueLimit", 0);
                policyOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Policy for authenticated users
            options.AddFixedWindowLimiter("AuthenticatedPolicy", policyOptions =>
            {
                var authConfig = rateLimitConfig.GetSection("AuthenticatedPolicy");
                policyOptions.PermitLimit = authConfig.GetValue<int>("PermitLimit", 200);
                policyOptions.Window = TimeSpan.Parse(authConfig.GetValue<string>("Window") ?? "00:01:00");
                policyOptions.QueueLimit = authConfig.GetValue<int>("QueueLimit", 10);
                policyOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Policy for admin users
            options.AddFixedWindowLimiter("AdminPolicy", policyOptions =>
            {
                var adminConfig = rateLimitConfig.GetSection("AdminPolicy");
                policyOptions.PermitLimit = adminConfig.GetValue<int>("PermitLimit", 500);
                policyOptions.Window = TimeSpan.Parse(adminConfig.GetValue<string>("Window") ?? "00:01:00");
                policyOptions.QueueLimit = adminConfig.GetValue<int>("QueueLimit", 20);
                policyOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Default policy for requests without specific policy
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var user = httpContext.User;
                
                if (user.Identity?.IsAuthenticated == true)
                {
                    if (user.IsInRole("Admin") || user.HasClaim("permission", "admin.access"))
                    {
                        return RateLimitPartition.GetFixedWindowLimiter("admin", key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 500,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 20,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                    }
                    
                    return RateLimitPartition.GetFixedWindowLimiter("authenticated", key => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
                }

                return RateLimitPartition.GetFixedWindowLimiter("anonymous", key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.RejectionStatusCode = 429;
        });

        // Add health checks
        services.AddHealthChecks()
            .AddCheck("gateway-health", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Gateway is healthy"));

        // Add health checks UI (DEV only: endpoints from configuration)
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(10);
            setup.AddHealthCheckEndpoint("Gateway", "/health");

            var env = services.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                var devEndpoints = configuration.GetSection("HealthChecks:UI:HealthCheckEndpoints").GetChildren();
                foreach (var ep in devEndpoints)
                {
                    var name = ep.GetValue<string>("Name");
                    var uri = ep.GetValue<string>("Uri");
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(uri))
                    {
                        setup.AddHealthCheckEndpoint(name, uri);
                    }
                }
            }
        })
        .AddInMemoryStorage();

        return services;
    }

    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("GatewayPolicy", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                policy.AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("X-Pagination", "X-Rate-Limit-Remaining", "X-Rate-Limit-Reset");

                if (allowedOrigins.Length > 0)
                {
                    policy.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static WebApplication UseGatewayMiddleware(this WebApplication app)
    {
        // Use API versioning middleware (must be early in pipeline)
        app.UseMiddleware<ApiVersioningMiddleware>();
        
        // Use rate limiting
        app.UseRateLimiter();
        
        // Use custom rate limiting middleware for better error handling
        app.UseMiddleware<RateLimitingMiddleware>();

        // Use CORS
        app.UseCors("GatewayPolicy");

        // Use authentication & authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Use reverse proxy
        app.MapReverseProxy();

        // Map health checks
        app.MapHealthChecks("/health");
        
        // Map health checks UI
        app.MapHealthChecksUI();

        return app;
    }
}