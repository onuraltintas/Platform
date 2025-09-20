using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Reflection;
using FluentValidation.AspNetCore;
using FluentValidation;
using SpeedReading.Infrastructure.Data;
using SpeedReading.Application.Services;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Repositories;
using SpeedReading.Infrastructure.Repositories;
using SpeedReading.Application.Services.ExerciseTypes;
using Enterprise.Shared.Security.Extensions;
using Enterprise.Shared.Events.Extensions;
using Enterprise.Shared.Observability.Extensions;
using Enterprise.Shared.Email.Extensions;
using SpeedReading.API.Middleware;
using SpeedReading.API.Filters;

// Production-ready configuration - uses environment variables directly
// Container deployments provide environment variables via docker-compose or orchestrator

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/speedreading-service-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container with validation filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Speed Reading Service API",
        Version = "v1",
        Description = "Comprehensive speed reading and comprehension training service",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@platform.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configure JWT authentication for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
var connectionString = Environment.GetEnvironmentVariable("SPEEDREADING_DB_CONNECTION")
    ?? Environment.GetEnvironmentVariable("IDENTITY_DB_CONNECTION")
    ?? throw new InvalidOperationException("SPEEDREADING_DB_CONNECTION or IDENTITY_DB_CONNECTION environment variable not found.");

builder.Services.AddDbContext<SpeedReadingDbContext>(options =>
{
    options.UseNpgsql(connectionString, b =>
    {
        b.MigrationsAssembly(typeof(SpeedReadingDbContext).Assembly.FullName);
        b.MigrationsHistoryTable("__EFMigrationsHistory", "speedreading");
        b.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
});

// Environment variables mapping for Enterprise services
var jwtSecretEarly = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT secret not found");
var jwtIssuerEarly = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? throw new InvalidOperationException("JWT issuer not found");
var jwtAudienceEarly = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? throw new InvalidOperationException("JWT audience not found");

// Add Security configuration to builder.Configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Security:JwtSecretKey"] = jwtSecretEarly,
    ["Security:JwtIssuer"] = jwtIssuerEarly,
    ["Security:JwtAudience"] = jwtAudienceEarly,
    ["Security:JwtAccessTokenExpirationMinutes"] = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY") ?? "15",
    ["Security:RefreshTokenExpirationDays"] = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY") ?? "7",
    ["Security:JwtClockSkewMinutes"] = "0",
    ["Security:EncryptionKey"] = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "ThisIsAVerySecureEncryptionKey32",
    ["Security:EncryptionIV"] = Environment.GetEnvironmentVariable("ENCRYPTION_IV") ?? "ThisIsAnIV16Char",

    // Observability Settings
    ["ObservabilitySettings:ServiceName"] = "SpeedReading.API",
    ["ObservabilitySettings:ServiceVersion"] = "1.0.0",
    ["ObservabilitySettings:Environment"] = builder.Environment.EnvironmentName,
    ["ObservabilitySettings:EnableTracing"] = "true",
    ["ObservabilitySettings:EnableMetrics"] = "true",
    ["ObservabilitySettings:EnableHealthChecks"] = "true",
    ["ObservabilitySettings:SamplingRate"] = "0.1",
    ["ObservabilitySettings:Tracing:ConsoleExporter"] = builder.Environment.IsDevelopment().ToString(),
    ["ObservabilitySettings:Tracing:EnableHttpInstrumentation"] = "true",
    ["ObservabilitySettings:Tracing:EnableSqlInstrumentation"] = "true",
    ["ObservabilitySettings:Metrics:PrometheusEndpoint"] = "/metrics",
    ["ObservabilitySettings:Metrics:EnableRuntimeMetrics"] = "true",
    ["ObservabilitySettings:Metrics:EnableHttpMetrics"] = "true",
    ["ObservabilitySettings:HealthChecks:Endpoint"] = "/health",
    ["ObservabilitySettings:HealthChecks:DetailedEndpoint"] = "/health/detailed",
    ["ObservabilitySettings:HealthChecks:ReadyEndpoint"] = "/health/ready",
    ["ObservabilitySettings:HealthChecks:LiveEndpoint"] = "/health/live"
});

// JWT Authentication - Enterprise Standard
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretEarly)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuerEarly,
        ValidateAudience = true,
        ValidAudience = jwtAudienceEarly,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Enterprise Security Services
builder.Services.AddEnterpriseSecurity(builder.Configuration, options =>
{
    options.EnableTokenService = true;
    options.EnableHashing = true;
    options.EnableEncryption = true;
    options.EnableSecurityAudit = true;
    options.EnableApiKeyService = true;
});

// Add Enterprise Shared Services
builder.Services.AddSharedEvents(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddSharedObservability(builder.Configuration);

// Email Service configuration
var smtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
var smtpPortStr = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
var smtpUser = Environment.GetEnvironmentVariable("EMAIL_SMTP_USER");
var smtpPass = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD");
var smtpEnableSsl = (Environment.GetEnvironmentVariable("EMAIL_SMTP_ENABLE_SSL") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
if (!string.IsNullOrWhiteSpace(smtpHost) && int.TryParse(smtpPortStr, out var smtpPort))
{
    builder.Services.AddSmtpEmailService(
        smtpHost,
        smtpPort,
        username: smtpUser,
        password: smtpPass,
        enableSsl: smtpEnableSsl,
        defaultFromEmail: smtpUser,
        defaultFromName: "Speed Reading Platform"
    );
}

builder.Services.AddAuthorization();

// Add Caching Services (Enterprise.Shared.Caching)
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Register Redis connection multiplexer as singleton
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });

    // Register ICacheMetricsService required by DistributedCacheService
    builder.Services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.DistributedCacheService>();
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.MemoryCacheService>();
}

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',')
            ?? new[] { "http://localhost:3000", "https://localhost:3001" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health checks
var healthCheckRedisConnection = redisConnectionString ?? "platform-redis:6379,password=Redis_Password_2024!";
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, tags: new[] { "db", "postgres" })
    .AddRedis(healthCheckRedisConnection, tags: new[] { "cache", "redis" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "self" });

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Application Services
builder.Services.AddScoped<ITextAnalysisService, TurkishTextAnalyzer>();
builder.Services.AddScoped<TurkishTextAnalyzer>();
builder.Services.AddScoped<ComprehensionScoringService>();
builder.Services.AddScoped<ComprehensionAnalyticsService>();
builder.Services.AddScoped<ExerciseScoreCalculatorService>();
builder.Services.AddScoped<AutomaticQuestionGenerationService>();

// Exercise Type Handlers
builder.Services.AddScoped<ReadingComprehensionHandler>();
builder.Services.AddScoped<VocabularyTestHandler>();
builder.Services.AddScoped<SpeedReadingHandler>();

// Repository Services
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IReadingTextRepository, ReadingTextRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseAttemptRepository, ExerciseAttemptRepository>();

// Additional Repository Services
builder.Services.AddScoped<SpeedReading.Application.Interfaces.ICityRepository, SpeedReading.Infrastructure.Repositories.CityRepository>();
builder.Services.AddScoped<SpeedReading.Application.Interfaces.IDistrictRepository, SpeedReading.Infrastructure.Repositories.DistrictRepository>();
builder.Services.AddScoped<SpeedReading.Application.Interfaces.IEducationLevelRepository, SpeedReading.Infrastructure.Repositories.EducationLevelRepository>();

// HttpClient factory
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Speed Reading Service API v1");
    if (app.Environment.IsDevelopment())
    {
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger in dev
    }
    else
    {
        c.RoutePrefix = "docs"; // Swagger UI at /docs in production
    }
    c.DocumentTitle = "Speed Reading Service API Documentation";
});

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

// Routing must be added before endpoints
app.UseRouting();

// Shared Observability (includes metrics, correlation ID, health checks)
app.UseSharedObservability();

// Global Exception Handling
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Request logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());

        var userId = httpContext.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    };
});

// Map controllers
app.MapControllers();

// Simple health check endpoint
app.MapGet("/health", () => Results.Ok(new {
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    service = "Speed Reading Service",
    version = "1.0.0"
}));

// Support HEAD method for health endpoint
app.MapMethods("/health", new[] { "HEAD" }, () => Results.Ok());

// Database migrations for container deployment
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SpeedReadingDbContext>();
    try
    {
        var applyMigrations = (Environment.GetEnvironmentVariable("APPLY_DATABASE_MIGRATIONS") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        if (applyMigrations)
        {
            try
            {
                var hasPending = (await dbContext.Database.GetPendingMigrationsAsync()).Any();
                if (hasPending)
                {
                    Log.Information("Applying Speed Reading Service database migrations...");
                    await dbContext.Database.MigrateAsync();
                    Log.Information("Speed Reading Service database migrations applied");
                }
                else
                {
                    var canConnect = dbContext.Database.CanConnect();
                    if (!canConnect)
                    {
                        Log.Information("No migrations found and cannot connect. Ensuring Speed Reading DB is created...");
                        await dbContext.Database.EnsureCreatedAsync();
                        Log.Information("Speed Reading DB ensured (created)");
                    }
                    else
                    {
                        Log.Information("No pending Speed Reading DB migrations. Database is reachable.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while applying/ensuring Speed Reading Service database");
                throw;
            }
        }
        else
        {
            Log.Information("APPLY_DATABASE_MIGRATIONS is false. Skipping Speed Reading DB migrations.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate Speed Reading Service database");
        throw;
    }
}

Log.Information("Speed Reading Service starting up...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("CORS origins configured: {Origins}",
    Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "localhost default");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Speed Reading Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Program class for Speed Reading API service
/// </summary>
public partial class Program { }