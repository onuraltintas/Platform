using EgitimPlatform.Shared.Configuration.Extensions;
using EgitimPlatform.Services.UserService.Data;
using EgitimPlatform.Services.UserService.Mappings;
using EgitimPlatform.Services.UserService.Services;
using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Logging.Extensions;
using EgitimPlatform.Shared.Errors.Extensions;
using EgitimPlatform.Shared.Messaging.Extensions;
using EgitimPlatform.Shared.Observability.Extensions;

namespace EgitimPlatform.Services.UserService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = CreateBuilderWithEnvironment(args);

        // Structured Logging
        builder.UseStructuredLogging();

        // Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EgitimPlatform User Service", Version = "v1" });
        });

        // Configuration & Db
        builder.Services.AddConfigurationOptions(builder.Configuration);
        builder.Services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // AutoMapper
        builder.Services.AddAutoMapper(typeof(UserMappingProfile));

        // Shared
        builder.Services.AddStructuredLogging(builder.Configuration);
        builder.Services.AddErrorHandling();
        builder.Services.AddSecurity(builder.Configuration);

        // Messaging
        builder.Services.AddMessaging(builder.Configuration, serviceName: "UserService");
        builder.Services.UseMessagingHealthChecks(builder.Configuration);

        // Observability
        builder.Services.AddObservability(builder.Configuration, serviceName: "UserService");
        builder.Services.UseObservabilityHealthChecks(builder.Configuration);

        // Application Services
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "EgitimPlatform User Service v1");
                c.RoutePrefix = string.Empty;
            });
        }

        // app.UseHttpsRedirection(); // Disabled for Docker
        app.UseObservability(builder.Configuration);

        app.UseGlobalExceptionHandler();

        app.UseRequestLogging();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Health check
        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "UserService", Timestamp = DateTime.UtcNow }))
           .WithName("HealthCheck")
           .WithOpenApi();

        // Ensure DB
        using (var scope = app.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            try
            {
                ctx.Database.Migrate();

                // Backward-compat: add missing Preferences column if DB was created before it existed
                const string addPreferencesSql = @"
IF COL_LENGTH('dbo.UserSettings','Preferences') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD Preferences NVARCHAR(MAX) NULL;
END";
                ctx.Database.ExecuteSqlRaw(addPreferencesSql);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "UserDbContext migration skipped");
            }
        }

        app.Run();
    }

    private static WebApplicationBuilder CreateBuilderWithEnvironment(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        return builder;
    }
}
