using Microsoft.EntityFrameworkCore;
using FluentValidation;
using EgitimPlatform.Services.IdentityService.Data;
using EgitimPlatform.Services.IdentityService.Services;
using EgitimPlatform.Services.IdentityService.Mappings;
using EgitimPlatform.Services.IdentityService.Validation;
using EgitimPlatform.Shared.Configuration.Extensions;
using EgitimPlatform.Shared.Email.Extensions;
using EgitimPlatform.Shared.Errors.Extensions;
using EgitimPlatform.Shared.Logging.Extensions;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Observability.Extensions;
using FluentValidation.AspNetCore;
using EgitimPlatform.Shared.Messaging.Extensions;
using EgitimPlatform.Shared.Security.Constants;

var builder = CreateBuilderWithEnvironment(args);

// Add Structured Logging
builder.UseStructuredLogging();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EgitimPlatform Identity Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Add Configuration
builder.Services.AddConfigurationOptions(builder.Configuration);

// Add Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateRoleRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Add Shared Services
builder.Services.AddStructuredLogging(builder.Configuration);
builder.Services.AddErrorHandling();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddEmailServices(builder.Configuration);

// Observability (Tracing/Metrics)
builder.Services.AddObservability(builder.Configuration, serviceName: "IdentityService");
builder.Services.UseObservabilityHealthChecks(builder.Configuration);

// Messaging (MassTransit/RabbitMQ)
builder.Services.AddMessaging(builder.Configuration, serviceName: "IdentityService");
builder.Services.UseMessagingHealthChecks(builder.Configuration);

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EgitimPlatform Identity Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// Ensure database and seed data exists
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Apply migrations
        logger.LogInformation("Applying database migrations");
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");

        // Seed all permissions from constants
        logger.LogInformation("Seeding permissions...");
        var existingPermissions = context.Permissions.Select(p => p.Name).ToList();
        var allPermissions = typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(c => c.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy))
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string?)x.GetRawConstantValue() ?? string.Empty)
            .ToList();

        var newPermissions = allPermissions
            .Where(p => !existingPermissions.Contains(p ?? string.Empty))
            .Select(p => new EgitimPlatform.Services.IdentityService.Models.Entities.Permission
            {
                Id = Guid.NewGuid().ToString(),
                Name = p ?? string.Empty,
                Description = $"Permission for {(p ?? string.Empty).Replace(".", " ", StringComparison.Ordinal)}",
                Group = (p ?? string.Empty).Split('.').FirstOrDefault() ?? "System",
            }).ToList();

        if (newPermissions.Any())
        {
            context.Permissions.AddRange(newPermissions);
            context.SaveChanges();
            logger.LogInformation("{Count} new permissions seeded.", newPermissions.Count);
        }
        else
        {
            logger.LogInformation("All permissions already exist in the database.");
        }

        // Ensure Admin role exists
        var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
        if (adminRole == null)
        {
            logger.LogInformation("Creating Admin role.");
            adminRole = new EgitimPlatform.Services.IdentityService.Models.Entities.Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                Description = "Administrator with all permissions."
            };
            context.Roles.Add(adminRole);
            context.SaveChanges();
        }

        // Grant all permissions to Admin role
        logger.LogInformation("Granting all permissions to Admin role.");
        var allPermissionIds = context.Permissions.Select(p => p.Id).ToList();
        var currentRolePermissionIds = context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToList();

        var permissionsToGrant = allPermissionIds
            .Where(pid => !currentRolePermissionIds.Contains(pid))
            .Select(pid => new EgitimPlatform.Services.IdentityService.Models.Entities.RolePermission
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = adminRole.Id,
                PermissionId = pid,
                AssignedBy = "System",
            }).ToList();

        if (permissionsToGrant.Any())
        {
            context.RolePermissions.AddRange(permissionsToGrant);
            context.SaveChanges();
            logger.LogInformation("{Count} new permissions granted to Admin role.", permissionsToGrant.Count);
        }
        else
        {
            logger.LogInformation("Admin role already has all permissions.");
        }

        // Create or update admin user
        var passwordService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Shared.Security.Services.IPasswordService>();
        var adminPassword = Environment.GetEnvironmentVariable("IDENTITY_ADMIN_PASSWORD") ?? "VForVan_40!";
        var correctPasswordHash = passwordService.HashPassword(adminPassword);

        var existingAdmin = context.Users.FirstOrDefault(u => u.UserName == "admin");
        if (existingAdmin != null)
        {
            logger.LogInformation("Updating existing admin user password");
            existingAdmin.PasswordHash = correctPasswordHash;
            existingAdmin.AccessFailedCount = 0;
            existingAdmin.LockoutEnd = null;
            existingAdmin.IsLocked = false;
            context.SaveChanges();
            logger.LogInformation("Admin user updated successfully");
        }
        else
        {
            logger.LogInformation("Creating admin user");

            // Create admin user
            var adminUser = new EgitimPlatform.Services.IdentityService.Models.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin",
                Email = "admin@egitimplatform.com",
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = correctPasswordHash,
                IsEmailConfirmed = true,
                IsActive = true
            };
            context.Users.Add(adminUser);

            // Assign admin role
            var userRole = new EgitimPlatform.Services.IdentityService.Models.Entities.UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedBy = "System"
            };
            context.UserRoles.Add(userRole);

            context.SaveChanges();
            logger.LogInformation("Admin user created successfully");
        }

        logger.LogInformation("Database setup completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating the database");
    }
}

app.UseGlobalExceptionHandler();
app.UseSecurityHeaders();

app.UseCors("AllowAll");
// app.UseHttpsRedirection(); // Disabled for Docker deployment

app.UseRequestLogging();

app.UseRouting(); // Explicitly add routing middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseObservability(builder.Configuration);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "IdentityService", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.Run();

static WebApplicationBuilder CreateBuilderWithEnvironment(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    return builder;
}

public partial class Program { }
