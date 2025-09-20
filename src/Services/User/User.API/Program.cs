using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using User.Application.Extensions;
using User.Infrastructure.Data;
using User.Infrastructure.Extensions;
using Enterprise.Shared.Security.Extensions;
using Enterprise.Shared.Events.Extensions;
using Enterprise.Shared.Observability.Extensions;
using Enterprise.Shared.Email.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using Enterprise.Shared.Permissions;
using User.API.Middleware;
using User.API.Filters;

// Production-ready configuration - uses environment variables directly
// Container deployments provide environment variables via docker-compose or orchestrator

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/user-service-.txt", rollingInterval: RollingInterval.Day)
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
        Title = "User Service API",
        Version = "v1",
        Description = "Comprehensive user management service with GDPR compliance, event integration, and advanced features",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@company.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

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
var connectionString = Environment.GetEnvironmentVariable("USER_DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("USER_DB_CONNECTION_STRING environment variable or DefaultConnection not found.");

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Add User Application and Infrastructure layers
builder.Services.AddUserApplication();

// RabbitMQ configuration for MassTransit
var rabbitMqConnectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING");
builder.Services.AddUserInfrastructure(rabbitMqConnectionString);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT_SECRET not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings["Issuer"],
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Handle authentication events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("JWT token validated for user: {UserId}", 
                context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
            return Task.CompletedTask;
        }
    };
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
    ["ObservabilitySettings:ServiceName"] = "User.API",
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
        defaultFromName: "User Platform"
    );
}

builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Global rate limiting
    rateLimiterOptions.AddFixedWindowLimiter("GlobalPolicy", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    // API-specific rate limiting
    rateLimiterOptions.AddFixedWindowLimiter("ApiPolicy", options =>
    {
        options.PermitLimit = 50;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    // GDPR export rate limiting (stricter)
    rateLimiterOptions.AddFixedWindowLimiter("GdprPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromHours(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    // Email verification rate limiting
    rateLimiterOptions.AddFixedWindowLimiter("EmailPolicy", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromHours(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 3;
    });

    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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

// Health checks
var healthCheckRedisConnection = redisConnectionString ?? "platform-redis:6379,password=Redis_Password_2024!";
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "UserServiceDb", tags: new[] { "db", "postgres" })
    .AddRedis(healthCheckRedisConnection, name: "UserServiceRedis", tags: new[] { "cache", "redis" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "self" });

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[]
    {
        "application/json",
        "application/xml",
        "text/csv",
        "text/plain"
    };
});

// HttpClient factory for background syncs
builder.Services.AddHttpClient();

builder.Services.AddHostedService<PermissionsSyncHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    if (app.Environment.IsDevelopment())
    {
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger in dev
    }
    else
    {
        c.RoutePrefix = "docs"; // Swagger UI at /docs in production
    }
    c.DocumentTitle = "User Service API Documentation";
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

// Routing (required for endpoints)
app.UseRouting();

// Shared Observability (includes metrics, correlation ID, health checks)
app.UseSharedObservability();

// Global Exception Handling
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// Rate limiting middleware
app.UseRateLimiter();

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

// Map controllers with rate limiting policies
app.MapControllers().RequireRateLimiting("ApiPolicy");

// Specific rate limiting for GDPR endpoints
app.MapControllerRoute(
    name: "gdpr",
    pattern: "api/v1/gdpr/{action}",
    defaults: new { controller = "Gdpr" })
    .RequireRateLimiting("GdprPolicy");

// Specific rate limiting for email verification endpoints
app.MapControllerRoute(
    name: "email",
    pattern: "api/v1/emailverification/{action}",
    defaults: new { controller = "EmailVerification" })
    .RequireRateLimiting("EmailPolicy");

// Simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "Healthy", 
    timestamp = DateTime.UtcNow,
    service = "User Service",
    version = "1.0.0"
}));

// Support HEAD method for health endpoint (return 200 without body)
app.MapMethods("/health", new[] { "HEAD" }, () => Results.Ok());

// Additional health check endpoints disabled for simplicity

// Database migrations for container deployment (controlled by APPLY_DATABASE_MIGRATIONS)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
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
                    Log.Information("Applying User Service database migrations...");
                    await dbContext.Database.MigrateAsync();
                    Log.Information("User Service database migrations applied");
                }
                else
                {
                    var canConnect = dbContext.Database.CanConnect();
                    if (!canConnect)
                    {
                        Log.Information("No migrations found and cannot connect. Ensuring User DB is created...");
                        await dbContext.Database.EnsureCreatedAsync();
                        Log.Information("User DB ensured (created)");
                    }
                    else
                    {
                        Log.Information("No pending User DB migrations. Database is reachable.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while applying/ensuring User Service database");
                throw;
            }
        }
        else
        {
            Log.Information("APPLY_DATABASE_MIGRATIONS is false. Skipping User DB migrations.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate User Service database");
        throw;
    }
}

// Global exception handling is now handled by GlobalExceptionMiddleware

Log.Information("User Service starting up...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("CORS origins configured: {Origins}", 
    Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "localhost default");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "User Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Program class for User API service
/// </summary>
public partial class Program { }

public class PermissionsSyncHostedService : BackgroundService
{
    private readonly ILogger<PermissionsSyncHostedService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public PermissionsSyncHostedService(ILogger<PermissionsSyncHostedService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay a bit to allow Identity to start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        try
        {
            var identityUrl = Environment.GetEnvironmentVariable("IDENTITY_INTERNAL_URL") ?? "http://identity";
            var syncKey = Environment.GetEnvironmentVariable("PERMISSIONS_SYNC_KEY") ?? string.Empty;

            // fetch local manifest from service DNS name within docker network
            using var manifestClient = new HttpClient();
            var manifestResp = await manifestClient.GetAsync("http://user/.well-known/permissions-manifest", stoppingToken);
            if (!manifestResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Permissions manifest could not be fetched: {Status}", manifestResp.StatusCode);
                return;
            }
            var manifest = await manifestResp.Content.ReadFromJsonAsync<PermissionManifest>(cancellationToken: stoppingToken);
            if (manifest is null)
            {
                _logger.LogWarning("Permissions manifest body was empty or invalid");
                return;
            }

            using var identityClient = new HttpClient { BaseAddress = new Uri(identityUrl) };
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/permissions/sync");
            if (!string.IsNullOrEmpty(syncKey)) req.Headers.Add("X-Permissions-Sync-Key", syncKey);
            req.Content = JsonContent.Create(new { manifest, autoAssignNewToAdmin = true });
            var syncResp = await identityClient.SendAsync(req, stoppingToken);
            if (syncResp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Permissions synced successfully for User service");
            }
            else
            {
                _logger.LogWarning("Permissions sync failed: {Status}", syncResp.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permissions sync");
        }
    }
}