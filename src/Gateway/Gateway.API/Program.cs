using Gateway.Core.Configuration;
using Gateway.API.Extensions;
using Gateway.API.Middleware;
using DotNetEnv;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Enterprise.Shared.Observability.Extensions;

// Load environment variables from multiple .env files (Priority: shared → secrets → local)
// Skip .env loading in containerized environment - use environment variables directly
var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

if (!isRunningInContainer)
{
    var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
    var envSharedPath = Path.Combine(projectRoot, "config", "env", "shared.env");
    var envSecretsPath = Path.Combine(projectRoot, "config", "env", "secrets.env");  
    var envLocalPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");

    // Load in priority order (later files override earlier ones)
    if (File.Exists(envSharedPath))
    {
        Env.Load(envSharedPath);
        Console.WriteLine($"✅ Loaded: {envSharedPath}");
    }

    if (File.Exists(envSecretsPath))
    {
        Env.Load(envSecretsPath);
        Console.WriteLine($"✅ Loaded: {envSecretsPath}");
    }

    if (File.Exists(envLocalPath))
    {
        Env.Load(envLocalPath);
        Console.WriteLine($"✅ Loaded: {envLocalPath}");
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Gateway Options
builder.Services.Configure<GatewayOptions>(options =>
{
    options.Environment = Environment.GetEnvironmentVariable("GATEWAY_ENVIRONMENT") ?? "Development";
    options.Port = int.Parse(Environment.GetEnvironmentVariable("GATEWAY_PORT") ?? "5000");
    
    // Security configuration (allow defaults for development)
    options.Security.JwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
        ?? "development-secret-key-minimum-256-bits-for-hs256-algorithm";
    options.Security.JwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
        ?? "https://localhost:5000";
    options.Security.JwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
        ?? "gateway-api";
    
    // Rate limiting configuration
    options.RateLimiting.RequestsPerMinute = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_REQUESTS_PER_MINUTE") ?? "100");
    options.RateLimiting.RequestsPerHour = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_REQUESTS_PER_HOUR") ?? "1000");
    
    // CORS configuration
    var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',') ?? new[] { "http://localhost:4200", "https://localhost:3000" };
    options.Cors.AllowedOrigins = corsOrigins.ToList();
    
    // Downstream services
    options.DownstreamServices.Identity.BaseUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") 
        ?? "https://localhost:5001";
    options.DownstreamServices.Identity.HealthEndpoint = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_HEALTH_URL") ?? "/health";
    
    // User service
    options.DownstreamServices.User = new IdentityServiceOptions
    {
        BaseUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "https://localhost:5002",
        HealthEndpoint = "/health",
        TimeoutSeconds = 30,
        RetryCount = 3,
        EnableCircuitBreaker = true,
        FailureThreshold = 0.5,
        CircuitBreakerTimeoutSeconds = 60
    };
    
    // Notification service
    options.DownstreamServices.Notification = new IdentityServiceOptions
    {
        BaseUrl = Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL") ?? "https://localhost:5003",
        HealthEndpoint = "/health",
        TimeoutSeconds = 25,
        RetryCount = 2,
        EnableCircuitBreaker = true,
        FailureThreshold = 0.3,
        CircuitBreakerTimeoutSeconds = 45
    };
});

// Add controllers with validation filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Gateway.API.Filters.ValidationFilter>();
});

// Add YARP with custom transforms
builder.Services.AddGatewayReverseProxy(builder.Configuration);

// Add Enterprise Shared Services
builder.Services.AddEnterpriseSharedServices(builder.Configuration);

// Add Gateway Services
builder.Services.AddGatewayServices();

// Add Authentication & Authorization
builder.Services.AddGatewayAuthentication();

// Add Rate Limiting
builder.Services.AddGatewayRateLimiting();

// Add CORS
builder.Services.AddGatewayCors();

// Add Health Checks
builder.Services.AddGatewayHealthChecks();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "API Gateway", 
        Version = "v1",
        Description = "Enterprise API Gateway - Centralized entry point for all microservices"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway V1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// Security Headers
app.UseSecurityHeaders();

// Routing (required for endpoints)
app.UseRouting();

// Shared Observability (includes metrics, correlation ID, health checks)
app.UseSharedObservability();

// Global Exception Handling
app.UseMiddleware<Gateway.API.Middleware.GlobalExceptionMiddleware>();

// Request Logging
app.UseSerilogRequestLogging();

// CORS
app.UseCors("GatewayPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate Limiting (after authentication to allow per-user limits)
app.UseGatewayRateLimiting();

// Circuit Breaker Middleware
app.UseMiddleware<Gateway.API.Middleware.CircuitBreakerMiddleware>();

// Custom Gateway Middleware
app.UseGatewayLogging();

// Controllers (for health checks, admin endpoints)
app.MapControllers();

// YARP Reverse Proxy
app.MapReverseProxy();

// Health Checks
app.MapHealthChecks("/health");
// Support HEAD method for health endpoint exposed by controllers
app.MapMethods("/health", new[] { "HEAD" }, () => Results.Ok());

try
{
    Log.Information("Starting API Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}