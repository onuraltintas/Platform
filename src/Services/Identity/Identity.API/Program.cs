using Identity.Infrastructure.Data;
using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DotNetEnv;
using Serilog;
using FluentValidation.AspNetCore;
using FluentValidation;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Enterprise.Shared.Security.Extensions;
using Enterprise.Shared.Events.Extensions;
using Enterprise.Shared.Email.Extensions;
using Enterprise.Shared.Observability.Extensions;

// Load environment variables from multiple .env files (Priority: shared → secrets → local)
// Skip .env loading in containerized environment - use environment variables directly
var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

if (!isRunningInContainer)
{
    var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
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

// Map JWT environment variables to Security configuration for Enterprise.Shared.Security
// This must be done early before AddEnterpriseSecurity is called
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
    ["ObservabilitySettings:ServiceName"] = "Identity.API",
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

// Bridge RabbitMQ env -> EventSettings for MassTransit (used by AddSharedEvents)
var rabbitMqConnectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
{
    try
    {
        var uri = new Uri(rabbitMqConnectionString);
        var userInfoParts = (uri.UserInfo ?? string.Empty).Split(':');
        var rabbitUser = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty;
        var rabbitPass = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;
        var virtualHost = uri.AbsolutePath?.Trim('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(virtualHost))
        {
            virtualHost = "/";
        }

        var eventSettings = new Dictionary<string, string?>
        {
            ["EventSettings:RabbitMQ:Host"] = uri.Host,
            ["EventSettings:RabbitMQ:Port"] = uri.Port.ToString(),
            ["EventSettings:RabbitMQ:Username"] = rabbitUser,
            ["EventSettings:RabbitMQ:Password"] = rabbitPass,
            ["EventSettings:RabbitMQ:VirtualHost"] = virtualHost,
            ["EventSettings:RabbitMQ:ConnectionRetryCount"] = "5",
            ["EventSettings:RabbitMQ:PrefetchCount"] = "16",
            ["EventSettings:RabbitMQ:ConnectionTimeout"] = "30",
            ["EventSettings:RabbitMQ:UseSsl"] = uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase) ? "true" : "false"
        };

        builder.Configuration.AddInMemoryCollection(eventSettings!);
    }
    catch
    {
        // Fall back silently; AddSharedEvents will use defaults if binding fails
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/identity-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Identity.API.Filters.ValidationFilter>();
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Add Session support for Google OAuth callback
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Google Auth configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["GoogleAuth:ClientId"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
    ["GoogleAuth:ClientSecret"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"),
    ["GoogleAuth:RedirectUri"] = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI") ?? "http://localhost:5001/api/v1/auth/google/callback"
});

// Database
var connectionString = Environment.GetEnvironmentVariable("IDENTITY_DB_CONNECTION")
    ?? throw new InvalidOperationException("Identity database connection string not found");

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = bool.Parse(Environment.GetEnvironmentVariable("PASSWORD_REQUIRE_DIGIT") ?? "true");
    options.Password.RequireLowercase = bool.Parse(Environment.GetEnvironmentVariable("PASSWORD_REQUIRE_LOWERCASE") ?? "true");
    options.Password.RequireUppercase = bool.Parse(Environment.GetEnvironmentVariable("PASSWORD_REQUIRE_UPPERCASE") ?? "true");
    options.Password.RequireNonAlphanumeric = bool.Parse(Environment.GetEnvironmentVariable("PASSWORD_REQUIRE_SPECIAL") ?? "true");
    options.Password.RequiredLength = int.Parse(Environment.GetEnvironmentVariable("PASSWORD_MIN_LENGTH") ?? "8");

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable("LOCKOUT_DURATION") ?? "30"));
    options.Lockout.MaxFailedAccessAttempts = int.Parse(Environment.GetEnvironmentVariable("MAX_FAILED_ATTEMPTS") ?? "5");
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_EMAIL_CONFIRMATION") ?? "true");
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// Repositories
builder.Services.AddScoped<IRefreshTokenRepository, Identity.Infrastructure.Repositories.RefreshTokenRepository>();

// JWT Authentication - use the early defined variables

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

// Google Authentication
if (bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GOOGLE_AUTH") ?? "false"))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
            options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
        });
}

// Authorization with Dynamic Policy Provider
builder.Services.AddAuthorization(options =>
{
    // Continue invoking handlers even if one fails
    options.InvokeHandlersAfterFailure = false;
});
builder.Services.AddHttpContextAccessor();

// Register SuperAdmin Bypass Handler (MUST BE FIRST for priority)
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.SuperAdminBypassHandler>();

// Register Authorization Handlers (Basic)
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.PermissionAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.MultiplePermissionsAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.ResourceOwnerAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.GroupMemberAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.TimeBasedAuthorizationHandler>();

// Register Advanced Authorization Handlers
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.HierarchicalPermissionAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.OptimizedMultiplePermissionsAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Identity.Application.Authorization.Handlers.ConditionalPermissionAuthorizationHandler>();

// Register Dynamic Policy Provider
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Identity.Application.Authorization.Providers.DynamicPolicyProvider>();

// Configure Security Settings
builder.Services.Configure<Enterprise.Shared.Security.Models.SecuritySettings>(
    builder.Configuration.GetSection("Security"));

// Add Enterprise Security Services
builder.Services.AddEnterpriseSecurity(builder.Configuration, options =>
{
    options.EnableTokenService = true;
    options.EnableHashing = true;
    options.EnableEncryption = true;
    options.EnableSecurityAudit = true;
    options.EnableApiKeyService = true;
});

// Add Enterprise Events Services (RabbitMQ via EventSettings)
builder.Services.AddSharedEvents(builder.Configuration, Assembly.GetExecutingAssembly());

// Email Service wiring (SMTP via env)
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
        defaultFromName: "Platform"
    );
}

// Bridge Identity IEmailService -> Shared Email service
builder.Services.AddScoped<Identity.Core.Interfaces.IEmailService, Identity.Application.Services.IdentityEmailService>();

// Add Caching Services with Redis best practices
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Lazy<ConnectionMultiplexer> pattern for thread-safe singleton (best practice)
    builder.Services.AddSingleton<Lazy<StackExchange.Redis.IConnectionMultiplexer>>(provider =>
        new Lazy<StackExchange.Redis.IConnectionMultiplexer>(() =>
        {
            var logger = provider.GetRequiredService<ILogger<Program>>();
            try
            {
                logger.LogInformation("Connecting to Redis...");
                var multiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
                logger.LogInformation("Successfully connected to Redis");
                return multiplexer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        }));

    // Register IConnectionMultiplexer using the lazy instance
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
        provider.GetRequiredService<Lazy<StackExchange.Redis.IConnectionMultiplexer>>().Value);

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });

    // Register ICacheMetricsService required by DistributedCacheService
    builder.Services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.DistributedCacheService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.IBulkCacheService>(provider =>
        (Enterprise.Shared.Caching.Interfaces.IBulkCacheService)provider.GetRequiredService<Enterprise.Shared.Caching.Interfaces.ICacheService>());
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService, Enterprise.Shared.Caching.Services.CacheMetricsService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.ICacheService, Enterprise.Shared.Caching.Services.MemoryCacheService>();
    builder.Services.AddScoped<Enterprise.Shared.Caching.Interfaces.IBulkCacheService>(provider =>
        (Enterprise.Shared.Caching.Interfaces.IBulkCacheService)provider.GetRequiredService<Enterprise.Shared.Caching.Interfaces.ICacheService>());
}

// CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',') ?? new[] { "http://localhost:4200", "https://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global policy: 100 istek / 1 dakika
    options.AddFixedWindowLimiter("GlobalPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 50;
    });

    // Auth policy: login/refresh için daha sıkı limitler
    options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Email policy: e-posta işlemleri için düşük trafik
    options.AddFixedWindowLimiter("EmailPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program), typeof(Identity.Application.Mappings.IdentityMappingProfile));

// Repository and Unit of Work
builder.Services.AddScoped<Identity.Infrastructure.UnitOfWork.IUnitOfWork, Identity.Infrastructure.UnitOfWork.UnitOfWork>();
builder.Services.AddScoped<IPermissionRepository, Identity.Infrastructure.Repositories.PermissionRepository>();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
// Register GroupService - now uses shared Enterprise caching internally
// Gateway cache invalidation client
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGatewayCacheInvalidationClient, Identity.Infrastructure.Services.GatewayCacheInvalidationClient>();
builder.Services.AddScoped<IGroupService, Identity.Application.Services.GroupService>();
builder.Services.AddScoped<IGroupRepository, Identity.Infrastructure.Repositories.GroupRepository>();
// Temporarily comment out ServiceRegistryService to avoid complex dependencies
// builder.Services.AddScoped<IServiceRegistryService, ServiceRegistryService>();
// builder.Services.AddScoped<IServiceRepository, Identity.Infrastructure.Repositories.ServiceRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Advanced Permission Services
builder.Services.AddScoped<Identity.Application.Services.IPermissionHierarchyService, Identity.Application.Services.PermissionHierarchyService>();
builder.Services.AddScoped<Identity.Application.Services.IPermissionQueryOptimizer, Identity.Application.Services.PermissionQueryOptimizer>();
builder.Services.AddScoped<Identity.Application.Services.IConditionalPermissionService, Identity.Application.Services.ConditionalPermissionService>();
builder.Services.AddScoped<Identity.Application.Authorization.Services.IWildcardPermissionResolver, Identity.Application.Authorization.Services.WildcardPermissionResolver>();
builder.Services.AddScoped<IPermissionAuditService, PermissionAuditService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();

// Permission Discovery and Seeding Services
builder.Services.AddScoped<IPermissionDiscoveryService, PermissionDiscoveryService>();
builder.Services.AddScoped<RoleSeedingService>();
builder.Services.AddScoped<ModernPermissionSeedingService>(); // New modern service
builder.Services.AddScoped<PermissionDataMigrationService>(); // Data migration for legacy permission codes
builder.Services.AddScoped<DemoUserSeedingService>();
builder.Services.AddScoped<GroupSeedingService>();
builder.Services.AddScoped<ZeroTrustSeedingService>();
builder.Services.AddScoped<AuditSeedingService>();

// Service Permission Providers
builder.Services.AddScoped<IServicePermissionProvider, IdentityServicePermissionProvider>();
builder.Services.AddScoped<IServicePermissionProvider, UserServicePermissionProvider>();
builder.Services.AddScoped<IServicePermissionProvider, SpeedReadingServicePermissionProvider>();

// Zero Trust Security Architecture
builder.Services.AddScoped<Identity.Core.ZeroTrust.IZeroTrustService, Identity.Application.Services.ZeroTrustService>();

// Advanced Audit & Monitoring
builder.Services.AddScoped<Identity.Core.Audit.IAuditService, Identity.Application.Services.AuditService>();

// Advanced Permission Caching
builder.Services.AddScoped<Identity.Core.Caching.IPermissionCacheService, Identity.Application.Services.PermissionCacheService>();

// Enterprise Shared Observability
builder.Services.AddSharedObservability(builder.Configuration);

// Add HttpContextAccessor for conditional permissions
builder.Services.AddHttpContextAccessor();

// Add Shared Services (these would be configured based on your shared libraries)
// builder.Services.AddSharedServices(builder.Configuration);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Identity Service API", 
        Version = "v1",
        Description = "Enterprise Identity and Access Management Service"
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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API V1");
    if (app.Environment.IsDevelopment())
    {
        c.RoutePrefix = string.Empty; // Swagger UI at root in dev
    }
    else
    {
        c.RoutePrefix = "docs"; // Swagger UI at /docs in production
        c.DocumentTitle = "Identity Service API Documentation";
    }
});

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy (basic, environment-aware)
    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
    {
        var isDev = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;
        var csp = isDev
            ? string.Join("; ", new[]
            {
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' localhost:*",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: https:",
                "font-src 'self' https:",
                "connect-src 'self' https: ws: wss: localhost:*",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "upgrade-insecure-requests"
            })
            : string.Join("; ", new[]
            {
                "default-src 'self'",
                "script-src 'self'",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: https:",
                "font-src 'self' https:",
                "connect-src 'self' https:",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "upgrade-insecure-requests"
            });
        context.Response.Headers.Append("Content-Security-Policy", csp);
    }

    // Permissions-Policy
    if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
    {
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()");
    }

    // Cross-Origin isolation headers
    if (!context.Response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
    if (!context.Response.Headers.ContainsKey("Cross-Origin-Opener-Policy"))
        context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    if (!context.Response.Headers.ContainsKey("Cross-Origin-Resource-Policy"))
        context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");

    // HSTS (HTTPS ise)
    if (context.Request.IsHttps && !context.Response.Headers.ContainsKey("Strict-Transport-Security"))
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    }

    await next();
});

// Routing (required before UseEndpoints)
app.UseRouting();

// Shared Observability - Temporarily disabled
// app.UseSharedObservability();

// Global Exception Handling
app.UseMiddleware<Identity.API.Middleware.GlobalExceptionMiddleware>();

// Request Logging
app.UseSerilogRequestLogging();

// CORS
app.UseCors("DefaultPolicy");

// Rate Limiting
app.UseRateLimiter();

// Session (for Google OAuth callback)
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Group Context (must run after authentication so we can attach claims)
app.UseMiddleware<Identity.API.Middleware.GroupContextMiddleware>();

// Controllers
app.MapControllers();

// Health Checks
app.MapGet("/health", () => Results.Ok(new { 
    status = "Healthy", 
    timestamp = DateTime.UtcNow,
    service = "Identity Service",
    version = "1.0.0"
}));

// Support HEAD method for health endpoint (return 200 without body)
app.MapMethods("/health", new[] { "HEAD" }, () => Results.Ok());

// Database Migration (in development)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    try
    {
        var applyMigrations = (Environment.GetEnvironmentVariable("APPLY_DATABASE_MIGRATIONS") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        if (applyMigrations)
        {
            try
            {
                var hasPending = (await context.Database.GetPendingMigrationsAsync()).Any();
                if (hasPending)
                {
                    Log.Information("Applying Identity database migrations...");
                    await context.Database.MigrateAsync();
                    Log.Information("Identity database migrations applied");
                }
                else
                {
                    // Ensure database exists if no migrations are present
                    var canConnect = context.Database.CanConnect();
                    if (!canConnect)
                    {
                        Log.Information("No migrations found and cannot connect. Ensuring database is created...");
                        await context.Database.EnsureCreatedAsync();
                        Log.Information("Identity database ensured (created)");
                    }
                    else
                    {
                        Log.Information("No pending Identity migrations. Database is reachable.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while applying/ensuring Identity database");
                throw;
            }

            // Seed standard roles first
            var roleSeedingService = scope.ServiceProvider.GetRequiredService<RoleSeedingService>();
            await roleSeedingService.SeedStandardRolesAsync();
            Log.Information("Standard roles seeding completed");

            // Seed default groups and group-scoped roles/permissions
            var groupSeedingService = scope.ServiceProvider.GetRequiredService<GroupSeedingService>();
            await groupSeedingService.SeedDefaultGroupsAsync();
            await groupSeedingService.SeedGroupRolesAndPermissionsAsync();
            Log.Information("Group seeding completed");

            // Seed environment admin
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                const string adminRoleName = "SuperAdmin"; // Use SuperAdmin role for environment admin

                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var adminUser = new ApplicationUser
                    {
                        UserName = "superadmin", // Set a simple username for superadmin
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = "Environment",
                        LastName = "Administrator",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        About = "Environment administrator account configured from deployment settings"
                    };
                    var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRoleName);
                        Log.Information("Environment admin user '{Email}' created and added to '{Role}' role", adminEmail, adminRoleName);
                    }
                    else
                    {
                        Log.Error("Failed to create environment admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(existingAdmin, adminRoleName))
                    {
                        await userManager.AddToRoleAsync(existingAdmin, adminRoleName);
                        Log.Information("Existing admin user '{Email}' added to '{Role}' role", adminEmail, adminRoleName);
                    }

                    // In Development, ensure admin password matches env for easier local testing
                    if (app.Environment.IsDevelopment())
                    {
                        try
                        {
                            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
                            var resetResult = await userManager.ResetPasswordAsync(existingAdmin, resetToken, adminPassword);
                            if (resetResult.Succeeded)
                            {
                                Log.Information("Environment admin password reset from env (Development)");
                            }
                            else
                            {
                                Log.Warning("Environment admin password reset failed: {Errors}", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Environment admin password reset error in Development");
                        }
                    }
                }

                // Seed default services if missing
                await SeedDefaultServicesAsync(context);

                // Data migration: normalize legacy permission codes/patterns
                var permissionDataMigration = scope.ServiceProvider.GetRequiredService<PermissionDataMigrationService>();
                await permissionDataMigration.MigrateAsync();

                // Discover and seed permissions using the new automatic system
                var permissionDiscoveryService = scope.ServiceProvider.GetRequiredService<IPermissionDiscoveryService>();
                await SeedDiscoveredPermissionsAsync(permissionDiscoveryService, context);

                // Use Modern Permission Seeding Service
                var modernPermissionSeedingService = scope.ServiceProvider.GetRequiredService<ModernPermissionSeedingService>();
                await modernPermissionSeedingService.SeedAllPermissionsAsync();
                Log.Information("Modern permission seeding completed");

                // Seed demo users in Development environment
                var demoUserSeedingService = scope.ServiceProvider.GetRequiredService<DemoUserSeedingService>();
                await demoUserSeedingService.SeedDemoUsersAsync();
                Log.Information("Demo user seeding completed");
            }
            else
            {
                Log.Information("ADMIN_EMAIL or ADMIN_PASSWORD not provided. Skipping admin seeding.");
            }
        }
        else
        {
            Log.Information("APPLY_DATABASE_MIGRATIONS is false. Skipping migrations.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating/seeding the Identity database");
    }
}

try
{
    Log.Information("Starting Identity Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Helper methods for seeding
static async Task SeedDefaultServicesAsync(IdentityDbContext context)
{
    var servicesToSeed = new[]
    {
        new Service { Id = Guid.NewGuid(), Name = "Identity", DisplayName = "Identity Service", Endpoint = "/", Type = ServiceType.Internal, RegisteredAt = DateTime.UtcNow, Status = ServiceStatus.Healthy, IsActive = true },
        new Service { Id = Guid.NewGuid(), Name = "User", DisplayName = "User Service", Endpoint = "/", Type = ServiceType.Internal, RegisteredAt = DateTime.UtcNow, Status = ServiceStatus.Healthy, IsActive = true },
        new Service { Id = Guid.NewGuid(), Name = "SpeedReading", DisplayName = "SpeedReading Service", Endpoint = "/", Type = ServiceType.Internal, RegisteredAt = DateTime.UtcNow, Status = ServiceStatus.Healthy, IsActive = true }
    };

    foreach (var service in servicesToSeed)
    {
        if (!await context.Services.AnyAsync(s => s.Name == service.Name))
        {
            context.Services.Add(service);
            Log.Information("Added service: {ServiceName}", service.Name);
        }
    }

    await context.SaveChangesAsync();
}

static async Task SeedDiscoveredPermissionsAsync(IPermissionDiscoveryService permissionDiscoveryService, IdentityDbContext context)
{
    try
    {
        var discoveredPermissions = await permissionDiscoveryService.DiscoverAllPermissionsAsync();
        var now = DateTime.UtcNow;

        foreach (var permissionDto in discoveredPermissions)
        {
            // Find the service ID
            var service = await context.Services.FirstOrDefaultAsync(s => s.Name == permissionDto.ServiceName);
            if (service == null)
            {
                Log.Warning("Service {ServiceName} not found for permission {PermissionName}", permissionDto.ServiceName, permissionDto.Name);
                continue;
            }

            var existingPermission = await context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionDto.Name);
            if (existingPermission == null)
            {
                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = permissionDto.Name,
                    DisplayName = permissionDto.DisplayName,
                    Description = permissionDto.Description,
                    Resource = permissionDto.Resource,
                    Action = permissionDto.Action,
                    ServiceId = service.Id,
                    Type = permissionDto.Type,
                    Priority = permissionDto.Priority,
                    IsActive = true,
                    CreatedAt = now
                };
                context.Permissions.Add(permission);
            }
            else
            {
                // Update existing permission
                existingPermission.DisplayName = permissionDto.DisplayName;
                existingPermission.Description = permissionDto.Description;
                existingPermission.Resource = permissionDto.Resource;
                existingPermission.Action = permissionDto.Action;
                existingPermission.Type = permissionDto.Type;
                existingPermission.Priority = permissionDto.Priority;
                existingPermission.IsActive = true;
                existingPermission.LastModifiedAt = now;
            }
        }

        await context.SaveChangesAsync();
        Log.Information("Discovered and seeded {Count} permissions", discoveredPermissions.Count());
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed discovered permissions");
    }
}


// Make Program class accessible for integration tests
public partial class Program { }
