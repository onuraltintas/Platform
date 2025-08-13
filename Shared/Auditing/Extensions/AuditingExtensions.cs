using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Auditing.Configuration;
using EgitimPlatform.Shared.Auditing.Services;
using EgitimPlatform.Shared.Auditing.Interceptors;

namespace EgitimPlatform.Shared.Auditing.Extensions;

public static class AuditingExtensions
{
    public static IServiceCollection AddAuditing(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Auditing")
    {
        // Configure options
        services.Configure<AuditingOptions>(configuration.GetSection(configurationSection));

        // Validate configuration
        services.AddSingleton<IValidateOptions<AuditingOptions>, AuditingOptionsValidator>();

        // Register core services
        services.AddScoped<IAuditService, DatabaseAuditService>();
        services.AddScoped<IAuditContextProvider, HttpAuditContextProvider>();
        services.AddScoped<AuditingInterceptor>();

        // Add HTTP context accessor for web applications
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddAuditing(
        this IServiceCollection services,
        Action<AuditingOptions> configureOptions)
    {
        // Configure options using action
        services.Configure(configureOptions);

        // Validate configuration
        services.AddSingleton<IValidateOptions<AuditingOptions>, AuditingOptionsValidator>();

        // Register core services
        services.AddScoped<IAuditService, DatabaseAuditService>();
        services.AddScoped<IAuditContextProvider, HttpAuditContextProvider>();
        services.AddScoped<AuditingInterceptor>();

        // Add HTTP context accessor for web applications
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddAuditingDbContext(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        services.AddDbContextFactory<AuditDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

            configureOptions?.Invoke(options);
        });

        return services;
    }

    public static IServiceCollection AddSystemAuditContext(
        this IServiceCollection services,
        string systemUserId = "SYSTEM",
        string systemUserName = "System")
    {
        services.AddScoped<IAuditContextProvider>(provider => 
            new SystemAuditContextProvider(systemUserId, systemUserName));

        return services;
    }

    public static DbContextOptionsBuilder AddAuditingInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<AuditingInterceptor>();
        return optionsBuilder.AddInterceptors(interceptor);
    }

    public static DbContextOptionsBuilder AddAuditingInterceptor<TInterceptor>(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
        where TInterceptor : class, Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor
    {
        var interceptor = serviceProvider.GetRequiredService<TInterceptor>();
        return optionsBuilder.AddInterceptors(interceptor);
    }

    public static void EnsureAuditDatabaseCreated(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        using var context = contextFactory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    public static async Task EnsureAuditDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public static async Task MigrateAuditDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}

public class AuditingOptionsValidator : IValidateOptions<AuditingOptions>
{
    public ValidateOptionsResult Validate(string? name, AuditingOptions options)
    {
        var failures = new List<string>();

        // Validate retention settings
        if (options.AuditRetentionDays <= 0)
        {
            failures.Add("Audit retention days must be greater than 0");
        }

        if (options.MaxAuditRecords <= 0)
        {
            failures.Add("Max audit records must be greater than 0");
        }

        // Validate performance settings
        if (options.SlowQueryThresholdMs < 0)
        {
            failures.Add("Slow query threshold cannot be negative");
        }

        // Validate database settings
        if (string.IsNullOrWhiteSpace(options.Database.ConnectionStringName))
        {
            failures.Add("Database connection string name is required");
        }

        if (options.Database.BatchSize <= 0)
        {
            failures.Add("Database batch size must be greater than 0");
        }

        if (options.Database.FlushIntervalSeconds <= 0)
        {
            failures.Add("Database flush interval must be greater than 0");
        }

        // Validate file settings if enabled
        if (options.File.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.File.LogPath))
            {
                failures.Add("File log path is required when file auditing is enabled");
            }

            if (options.File.MaxFileSizeMB <= 0)
            {
                failures.Add("Max file size must be greater than 0");
            }

            if (options.File.MaxFileCount <= 0)
            {
                failures.Add("Max file count must be greater than 0");
            }
        }

        // Validate external settings if enabled
        if (options.External.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.External.ApiEndpoint))
            {
                failures.Add("External API endpoint is required when external auditing is enabled");
            }

            if (options.External.TimeoutSeconds <= 0)
            {
                failures.Add("External API timeout must be greater than 0");
            }

            if (options.External.RetryCount < 0)
            {
                failures.Add("External API retry count cannot be negative");
            }
        }

        if (failures.Any())
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}