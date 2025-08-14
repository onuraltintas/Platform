using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EgitimPlatform.Shared.Security.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Security.Middleware;
using EgitimPlatform.Shared.Security.Services;

namespace EgitimPlatform.Shared.Security.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Add JWT Authentication
        services.AddJwtAuthentication(configuration);
        
        // Add Authorization with custom policies
        services.AddCustomAuthorization();
        
        // Add security services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICategoryRoleService, CategoryRoleService>();
        
        // Add authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CategoryAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AnyCategoryAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RoleAndCategoryAuthorizationHandler>();
        
        return services;
    }
    
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("Jwt");
        var secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtConfig["Issuer"];
        var audience = jwtConfig["Audience"];
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized" });
                        return context.Response.WriteAsync(result, context.HttpContext.RequestAborted);
                    }
                    return Task.CompletedTask;
                }
            };
        });
        
        return services;
    }
    
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authenticated user
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                
            // User management permissions
            options.AddPolicy(Permissions.Users.Read, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Users.Read)));
                
            options.AddPolicy(Permissions.Users.Write, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Users.Write)));
                
            options.AddPolicy(Permissions.Users.Delete, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Users.Delete)));
                
            // Course management permissions
            options.AddPolicy(Permissions.Courses.Read, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Courses.Read)));
                
            options.AddPolicy(Permissions.Courses.Write, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Courses.Write)));
                
            options.AddPolicy(Permissions.Courses.Delete, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.Courses.Delete)));
                
            // System permissions
            options.AddPolicy(Permissions.System.Admin, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.System.Admin)));

            // Speed Reading permissions
            options.AddPolicy(Permissions.SpeedReading.ContentManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.SpeedReading.ContentManage)));
            options.AddPolicy(Permissions.SpeedReading.ProfileManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.SpeedReading.ProfileManage)));
            options.AddPolicy(Permissions.SpeedReading.ProgressReadAll, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.SpeedReading.ProgressReadAll)));
            options.AddPolicy(Permissions.SpeedReading.ProgressExport, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.SpeedReading.ProgressExport)));
        });
        
        return services;
    }
    
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
    
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
        app.UseSecurityHeaders();
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }
}