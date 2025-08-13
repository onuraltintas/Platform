using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using EgitimPlatform.Services.FeatureFlagService.Data;
using EgitimPlatform.Services.FeatureFlagService.Services;
using EgitimPlatform.Services.FeatureFlagService.Repositories;
using EgitimPlatform.Services.FeatureFlagService.Mappings;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Errors.Extensions;
using EgitimPlatform.Shared.Logging.Extensions;
using EgitimPlatform.Shared.Configuration.Extensions;
using EgitimPlatform.Shared.Messaging.Extensions;
using EgitimPlatform.Shared.Observability.Extensions;

var builder = CreateBuilderWithEnvironment(args);

// Structured Logging
builder.UseStructuredLogging();

// Add services to the container
builder.Services.AddControllers();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer();

// Database
builder.Services.AddDbContext<FeatureFlagDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(FeatureFlagMappingProfile));

// Memory Cache
builder.Services.AddMemoryCache();

// Redis Cache (optional)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}

// Application Services
builder.Services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddScoped<IFeatureFlagEvaluationEngine, FeatureFlagEvaluationEngine>();

// Shared libraries
builder.Services.AddConfigurationOptions(builder.Configuration);
builder.Services.AddStructuredLogging(builder.Configuration);
builder.Services.AddErrorHandling();
builder.Services.AddSecurity(builder.Configuration);

// Messaging
builder.Services.AddMessaging(builder.Configuration, serviceName: "FeatureFlagService");
builder.Services.UseMessagingHealthChecks(builder.Configuration);

// Observability
builder.Services.AddObservability(builder.Configuration, serviceName: "FeatureFlagService");
builder.Services.UseObservabilityHealthChecks(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FeatureFlagDbContext>()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "FeatureFlag Service API", 
        Version = "v1",
        Description = "API for managing feature flags"
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, 
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FeatureFlag Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
    });
}

// app.UseHttpsRedirection(); // Disabled for Docker deployment

app.UseCors("AllowAll");
app.UseObservability(builder.Configuration);

app.UseGlobalExceptionHandler();
app.UseRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Database migration on startup (skip if DB not available)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FeatureFlagDbContext>();
    try
    {
        // Test connection with short timeout
        using var connection = context.Database.GetDbConnection();
        connection.ConnectionString += ";Connection Timeout=5;";
        await connection.OpenAsync();
        connection.Close();
        
        context.Database.Migrate();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database not available, skipping migration. Service will continue without database");
    }
}

app.Logger.LogInformation("FeatureFlag Service started successfully");

app.Run();

static WebApplicationBuilder CreateBuilderWithEnvironment(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Load .env file (best-effort)
    try
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    if (Environment.GetEnvironmentVariable(key) is null)
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
            }
        }
    }
    catch { }

    // Configure JWT secret from env if available (no hard-coded fallback)
    var jwtSecretFromEnv = Environment.GetEnvironmentVariable("JwtSettings__SecretKey")
        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? Environment.GetEnvironmentVariable("FEATURE_FLAG_JWT_SECRET");
    if (!string.IsNullOrWhiteSpace(jwtSecretFromEnv))
    {
        builder.Configuration["JwtSettings:SecretKey"] = jwtSecretFromEnv;
        builder.Configuration["Jwt:SecretKey"] = jwtSecretFromEnv; // compatibility with shared security
    }

    // Do not override ConnectionStrings here; rely on appsettings and Docker env
    return builder;
}

public partial class Program { }
