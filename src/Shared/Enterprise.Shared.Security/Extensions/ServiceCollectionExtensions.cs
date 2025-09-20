using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Enterprise.Shared.Security.Models;
using Enterprise.Shared.Security.Services;
using Enterprise.Shared.Security.Interfaces;

namespace Enterprise.Shared.Security.Extensions;

/// <summary>
/// Extension methods for configuring security services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Enterprise security services
    /// </summary>
    public static IServiceCollection AddEnterpriseSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SecurityOptions>? configureOptions = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var options = new SecurityOptions();
        configureOptions?.Invoke(options);

        // Configure settings
        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));

        // Register core security services
        services.AddMemoryCache();
        
        if (options.EnableEncryption)
        {
            services.AddSingleton<IEncryptionService, EncryptionService>();
        }

        if (options.EnableHashing)
        {
            services.AddSingleton<IHashingService, HashingService>();
        }

        if (options.EnableTokenService)
        {
            services.AddSingleton<ITokenService, TokenService>();
        }

        if (options.EnableSecurityValidator)
        {
            services.AddSingleton<ISecurityValidator, SecurityValidator>();
        }

        if (options.EnableApiKeyService)
        {
            services.AddScoped<IApiKeyService, ApiKeyService>();
        }

        if (options.EnableSecurityAudit)
        {
            services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        }

        // Configure JWT authentication if enabled
        if (options.EnableJwtAuthentication)
        {
            var securitySettings = configuration.GetSection(SecuritySettings.SectionName).Get<SecuritySettings>();
            if (securitySettings == null)
                throw new InvalidOperationException("Security settings are not configured");

            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = securitySettings.JwtIssuer,
                    ValidAudience = securitySettings.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(securitySettings.JwtSecretKey ?? throw new InvalidOperationException("JWT secret key is not configured"))),
                    ClockSkew = TimeSpan.FromMinutes(securitySettings.JwtClockSkewMinutes ?? 5)
                };

                opts.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Additional token validation logic can be added here
                        return Task.CompletedTask;
                    }
                };

                if (options.RequireHttps)
                {
                    opts.RequireHttpsMetadata = true;
                }
            });
        }

        // Configure authorization if enabled
        if (options.EnableAuthorization)
        {
            services.AddAuthorization(opts =>
            {
                // Add default policies
                opts.AddPolicy("RequireAuthenticatedUser", policy =>
                    policy.RequireAuthenticatedUser());

                opts.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireRole("Admin"));

                // Add custom policies from options
                foreach (var policy in options.AuthorizationPolicies)
                {
                    opts.AddPolicy(policy.Key, policy.Value);
                }
            });
        }

        return services;
    }

    /// <summary>
    /// Adds JWT authentication only
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddEnterpriseSecurity(configuration, opts =>
        {
            opts.EnableJwtAuthentication = true;
            opts.EnableTokenService = true;
            opts.EnableEncryption = false;
            opts.EnableHashing = false;
            opts.EnableSecurityValidator = false;
            opts.EnableApiKeyService = false;
            opts.EnableSecurityAudit = false;
            opts.EnableAuthorization = false;
        });
    }

    /// <summary>
    /// Adds API key authentication services
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddEnterpriseSecurity(configuration, opts =>
        {
            opts.EnableApiKeyService = true;
            opts.EnableHashing = true;
            opts.EnableSecurityAudit = true;
            opts.EnableJwtAuthentication = false;
            opts.EnableTokenService = false;
            opts.EnableEncryption = false;
            opts.EnableSecurityValidator = false;
            opts.EnableAuthorization = false;
        });
    }
}

/// <summary>
/// Options for configuring security services
/// </summary>
public class SecurityOptions
{
    public bool EnableEncryption { get; set; } = true;
    public bool EnableHashing { get; set; } = true;
    public bool EnableTokenService { get; set; } = true;
    public bool EnableSecurityValidator { get; set; } = true;
    public bool EnableApiKeyService { get; set; } = true;
    public bool EnableSecurityAudit { get; set; } = true;
    public bool EnableJwtAuthentication { get; set; } = false;
    public bool EnableAuthorization { get; set; } = false;
    public bool RequireHttps { get; set; } = true;
    public Dictionary<string, Action<Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder>> AuthorizationPolicies { get; set; } = new();
}