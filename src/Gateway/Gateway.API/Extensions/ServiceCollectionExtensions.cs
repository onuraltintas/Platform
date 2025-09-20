using Gateway.Core.Interfaces;
using Gateway.Core.Configuration;
using Gateway.Core.Services;
using Gateway.API.Transforms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Enterprise.Shared.Security.Extensions;
using Enterprise.Shared.Events.Extensions;
using Enterprise.Shared.Observability.Extensions;

namespace Gateway.API.Extensions;

/// <summary>
/// Service collection extensions for Gateway
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Enterprise Shared Services
    /// </summary>
    public static IServiceCollection AddEnterpriseSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure enterprise settings from environment variables
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Security Settings
            ["Security:JwtSecretKey"] = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "default-gateway-secret-key-256-bits-minimum",
            ["Security:JwtIssuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "https://gateway.platform.com",
            ["Security:JwtAudience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "platform-services",
            ["Security:JwtAccessTokenExpirationMinutes"] = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY") ?? "15",
            ["Security:RefreshTokenExpirationDays"] = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY") ?? "7",
            ["Security:JwtClockSkewMinutes"] = "0",
            ["Security:EncryptionKey"] = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "ThisIsAVerySecureEncryptionKey32",
            ["Security:EncryptionIV"] = Environment.GetEnvironmentVariable("ENCRYPTION_IV") ?? "ThisIsAnIV16Char",

            // Observability Settings
            ["ObservabilitySettings:ServiceName"] = "Gateway.API",
            ["ObservabilitySettings:ServiceVersion"] = "1.0.0",
            ["ObservabilitySettings:Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            ["ObservabilitySettings:EnableTracing"] = "true",
            ["ObservabilitySettings:EnableMetrics"] = "true",
            ["ObservabilitySettings:EnableHealthChecks"] = "true",
            ["ObservabilitySettings:SamplingRate"] = "0.2",
            ["ObservabilitySettings:Tracing:ConsoleExporter"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "true" : "false",
            ["ObservabilitySettings:Tracing:EnableHttpInstrumentation"] = "true",
            ["ObservabilitySettings:Tracing:EnableSqlInstrumentation"] = "false",
            ["ObservabilitySettings:Metrics:PrometheusEndpoint"] = "/metrics",
            ["ObservabilitySettings:Metrics:EnableRuntimeMetrics"] = "true",
            ["ObservabilitySettings:Metrics:EnableHttpMetrics"] = "true",
            ["ObservabilitySettings:HealthChecks:Endpoint"] = "/health",
            ["ObservabilitySettings:HealthChecks:DetailedEndpoint"] = "/health/detailed",
            ["ObservabilitySettings:HealthChecks:ReadyEndpoint"] = "/health/ready",
            ["ObservabilitySettings:HealthChecks:LiveEndpoint"] = "/health/live"
        });
        var enterpriseConfig = configBuilder.Build();

        // Add Enterprise Security
        services.AddEnterpriseSecurity(enterpriseConfig, options =>
        {
            options.EnableTokenService = true;
            options.EnableHashing = true;
            options.EnableEncryption = true;
            options.EnableSecurityAudit = true;
            options.EnableApiKeyService = true;
        });

        // Add Enterprise Observability
        services.AddSharedObservability(enterpriseConfig);

        // Add Enterprise Events
        services.AddSharedEvents(enterpriseConfig, typeof(ServiceCollectionExtensions).Assembly);

        // Add Enterprise Caching
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });

            services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
            services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.DistributedCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
            services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Add Gateway-specific services
    /// </summary>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        // Register core gateway services
        services.AddSingleton<IServiceDiscoveryService, ServiceDiscoveryService>();
        services.AddHttpClient<ServiceDiscoveryService>();
        
        // Register authentication services
        services.AddScoped<IGatewayAuthenticationService, GatewayAuthenticationService>();
        services.AddSingleton<IApiKeyAuthenticationService, ApiKeyAuthenticationService>();
        
        // TODO: Register remaining Gateway services when implemented
        // services.AddScoped<IGatewayService, GatewayService>();
        // services.AddScoped<IServiceHealthService, ServiceHealthService>();
        
        return services;
    }

    /// <summary>
    /// Add JWT Authentication for Gateway
    /// </summary>
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";

        if (!string.IsNullOrEmpty(jwtSecret))
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        }

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Add Rate Limiting for Gateway
    /// </summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        // Rate limiting will be implemented using custom middleware for now
        // since AspNetCore.RateLimiting package is not available in .NET 8
        // TODO: Implement custom rate limiting middleware
        return services;
    }

    /// <summary>
    /// Add CORS for Gateway
    /// </summary>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services)
    {
        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',') ?? new[] { "http://localhost:4200", "https://localhost:3000" };

        // Log CORS origins for debugging
        Console.WriteLine($"🌐 CORS Origins configured: {string.Join(", ", corsOrigins)}");

        services.AddCors(options =>
        {
            options.AddPolicy("GatewayPolicy", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });

        return services;
    }

    /// <summary>
    /// Add Health Checks for Gateway
    /// </summary>
    public static IServiceCollection AddGatewayHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("gateway_health", () => 
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Gateway is running"));

        // TODO: Add downstream service health checks when implementing service health service
        return services;
    }

    /// <summary>
    /// Add YARP with custom transforms
    /// </summary>
    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms<GatewayRequestTransformProvider>();
            
        return services;
    }
}