using EgitimPlatform.Shared.Configuration.Extensions;
using EgitimPlatform.Services.NotificationService.Data;
using EgitimPlatform.Services.NotificationService.Services;
using EgitimPlatform.Services.NotificationService.Mappings;
using EgitimPlatform.Shared.Email.Extensions;
using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Shared.Messaging.Extensions;
using EgitimPlatform.Shared.Observability.Extensions;
using EgitimPlatform.Shared.Logging.Extensions;
 
 namespace EgitimPlatform.Services.NotificationService;
 
 public class Program
 {
     public static void Main(string[] args)
     {
         var builder = CreateBuilderWithEnvironment(args);
         builder.UseStructuredLogging();
 
         // Add services to the container
         builder.Services.AddControllers();
         
         // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
         builder.Services.AddEndpointsApiExplorer();
         builder.Services.AddSwaggerGen();
         
         // Database Configuration
         builder.Services.AddDbContext<NotificationDbContext>(options =>
             options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
             
         // AutoMapper Configuration
         builder.Services.AddAutoMapper(typeof(NotificationMappingProfile));
         
         // Application Services
         builder.Services.AddEmailServices(builder.Configuration);
         builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
         builder.Services.AddScoped<IUserDeviceService, UserDeviceService>();

         // Structured logging
         builder.Services.AddStructuredLogging(builder.Configuration);

         // Messaging (MassTransit/RabbitMQ)
         builder.Services.AddMessaging(builder.Configuration, serviceName: "NotificationService");
         builder.Services.UseMessagingHealthChecks(builder.Configuration);

         // Observability (OpenTelemetry/Prometheus)
         builder.Services.AddObservability(builder.Configuration, serviceName: "NotificationService");
         builder.Services.UseObservabilityHealthChecks(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.UseHttpsRedirection(); // Disabled for Docker deployment
        app.UseRequestLogging();
        app.UseObservability(builder.Configuration);
        app.UseAuthorization();
        app.MapControllers();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "NotificationService", Timestamp = DateTime.UtcNow }))
           .WithName("HealthCheck")
           .WithOpenApi();

        app.Run();
    }

    private static WebApplicationBuilder CreateBuilderWithEnvironment(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        return builder;
    }
}