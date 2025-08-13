using EgitimPlatform.Gateway.Extensions;
using EgitimPlatform.Gateway.Middleware;
using EgitimPlatform.Shared.Configuration.Extensions;
using EgitimPlatform.Shared.Errors.Extensions;
using EgitimPlatform.Shared.Logging.Extensions;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel based on environment
if (builder.Environment.EnvironmentName == "Docker")
{
    // Completely override Kestrel configuration for Docker
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Listen(System.Net.IPAddress.Any, 80);
        // Clear all certificates and HTTPS configuration
        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = null;
        });
    });
    
    // Remove all Kestrel certificate configurations from appsettings
    builder.Configuration["Kestrel:Certificates:Default:Path"] = null;
    builder.Configuration["Kestrel:Certificates:Default:Password"] = null;
    builder.Configuration["Kestrel:Certificates"] = null;
}
else
{
    // Configure Kestrel for local development
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(80);
        serverOptions.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}

// Add Structured Logging
builder.UseStructuredLogging();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EgitimPlatform API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Add Configuration
builder.Services.AddConfigurationOptions(builder.Configuration);

// Add Shared Services
builder.Services.AddErrorHandling();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, serviceName: "ApiGateway");
builder.Services.UseObservabilityHealthChecks(builder.Configuration);

// Add Gateway Services
builder.Services.AddGatewayServices(builder.Configuration);
builder.Services.AddGatewayCors(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EgitimPlatform API Gateway v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseGlobalExceptionHandler();
app.UseSecurityHeaders();

// app.UseHttpsRedirection(); // Disabled for Docker deployment

// Use custom request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Use gateway middleware (includes CORS, Authentication, Authorization, Rate Limiting, Reverse Proxy)
app.UseGatewayMiddleware();

// Observability middlewares (tracing/metrics)
app.UseObservability(builder.Configuration);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "API Gateway", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

// Gateway info endpoint
app.MapGet("/info", () => new
{
    Service = "EgitimPlatform API Gateway",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Features = new[]
    {
        "YARP Reverse Proxy",
        "JWT Authentication", 
        "Rate Limiting",
        "Health Checks",
        "Request Logging",
        "CORS Support",
        "API Versioning"
    }
})
.WithName("GatewayInfo")
.WithOpenApi();

// Fallback route for unmatched requests
app.MapFallback(() => Results.NotFound(new
{
    error = "Endpoint not found",
    message = "The requested endpoint does not exist or has been moved",
    statusCode = 404,
    timestamp = DateTime.UtcNow
}));

app.Run();

public partial class Program { }
