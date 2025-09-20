namespace Enterprise.Shared.Email.Extensions;

/// <summary>
/// Service collection extensions for email services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds email services with default configuration
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddEmailService(configuration.GetSection(EmailConfiguration.SectionName));
    }

    /// <summary>
    /// Adds email services with configuration section
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<EmailConfiguration>(configurationSection);
        return services.AddEmailServiceCore();
    }

    /// <summary>
    /// Adds email services with configuration options
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, Action<EmailConfiguration> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddEmailServiceCore();
    }

    /// <summary>
    /// Adds email services with pre-configured options
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, EmailConfiguration configuration)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.DefaultSender = configuration.DefaultSender;
            options.Smtp = configuration.Smtp;
            options.Templates = configuration.Templates;
            options.BulkProcessing = configuration.BulkProcessing;
            options.Retry = configuration.Retry;
            options.RateLimit = configuration.RateLimit;
            options.Logging = configuration.Logging;
        });

        return services.AddEmailServiceCore();
    }

    /// <summary>
    /// Adds SMTP email service with FluentEmail
    /// </summary>
    public static IServiceCollection AddSmtpEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSmtpEmailService(configuration.GetSection(EmailConfiguration.SectionName));
    }

    /// <summary>
    /// Adds SMTP email service with configuration section
    /// </summary>
    public static IServiceCollection AddSmtpEmailService(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        var emailConfig = new EmailConfiguration();
        configurationSection.Bind(emailConfig);

        services.Configure<EmailConfiguration>(configurationSection);

        services
            .AddFluentEmail(emailConfig.DefaultSender.Email, emailConfig.DefaultSender.Name)
            .AddSmtpSender(emailConfig.Smtp.Host, emailConfig.Smtp.Port, emailConfig.Smtp.Username, emailConfig.Smtp.Password)
            .AddRazorRenderer();

        return services.AddEmailServiceCore();
    }

    /// <summary>
    /// Adds SMTP email service with manual configuration
    /// </summary>
    public static IServiceCollection AddSmtpEmailService(
        this IServiceCollection services,
        string smtpHost,
        int smtpPort,
        string? username = null,
        string? password = null,
        bool enableSsl = true,
        string? defaultFromEmail = null,
        string? defaultFromName = null)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.Smtp.Host = smtpHost;
            options.Smtp.Port = smtpPort;
            options.Smtp.Username = username;
            options.Smtp.Password = password;
            options.Smtp.EnableSsl = enableSsl;
            
            if (!string.IsNullOrEmpty(defaultFromEmail))
            {
                options.DefaultSender.Email = defaultFromEmail;
                options.DefaultSender.Name = defaultFromName ?? "";
            }
        });

        services
            .AddFluentEmail(defaultFromEmail ?? "noreply@example.com", defaultFromName)
            .AddSmtpSender(smtpHost, smtpPort, username, password)
            .AddRazorRenderer();

        return services.AddEmailServiceCore();
    }

    /// <summary>
    /// Adds file system template provider
    /// </summary>
    public static IServiceCollection AddFileSystemTemplates(this IServiceCollection services, string templateDirectory)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.Templates.Provider = TemplateProvider.FileSystem;
            options.Templates.DirectoryPath = templateDirectory;
        });

        return services;
    }

    /// <summary>
    /// Adds memory cache template provider
    /// </summary>
    public static IServiceCollection AddMemoryTemplates(this IServiceCollection services, IEnumerable<EmailTemplate> templates)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.Templates.Provider = TemplateProvider.Memory;
        });

        services.AddSingleton<IEnumerable<EmailTemplate>>(templates);
        return services;
    }

    /// <summary>
    /// Configures bulk email processing options
    /// </summary>
    public static IServiceCollection ConfigureBulkProcessing(
        this IServiceCollection services,
        int defaultBatchSize = 50,
        int defaultMaxConcurrency = 10,
        int defaultDelayBetweenBatchesMs = 1000)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.BulkProcessing.DefaultBatchSize = defaultBatchSize;
            options.BulkProcessing.DefaultMaxConcurrency = defaultMaxConcurrency;
            options.BulkProcessing.DefaultDelayBetweenBatchesMs = defaultDelayBetweenBatchesMs;
        });

        return services;
    }

    /// <summary>
    /// Configures retry policy
    /// </summary>
    public static IServiceCollection ConfigureRetryPolicy(
        this IServiceCollection services,
        int maxAttempts = 3,
        int delayMs = 1000,
        bool useExponentialBackoff = true)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxAttempts = maxAttempts;
            options.Retry.DelayMs = delayMs;
            options.Retry.UseExponentialBackoff = useExponentialBackoff;
        });

        return services;
    }

    /// <summary>
    /// Configures rate limiting
    /// </summary>
    public static IServiceCollection ConfigureRateLimit(
        this IServiceCollection services,
        int emailsPerMinute = 100,
        int emailsPerHour = 1000,
        int emailsPerDay = 10000)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.RateLimit.Enabled = true;
            options.RateLimit.EmailsPerMinute = emailsPerMinute;
            options.RateLimit.EmailsPerHour = emailsPerHour;
            options.RateLimit.EmailsPerDay = emailsPerDay;
        });

        return services;
    }

    /// <summary>
    /// Adds development email service (console output)
    /// </summary>
    public static IServiceCollection AddDevelopmentEmailService(this IServiceCollection services)
    {
        services.Configure<EmailConfiguration>(options =>
        {
            options.DefaultSender.Email = "dev@localhost";
            options.DefaultSender.Name = "Development";
        });

        services
            .AddFluentEmail("dev@localhost", "Development")
            .AddSmtpSender("localhost", 25)
            .AddRazorRenderer();

        return services.AddEmailServiceCore();
    }

    private static IServiceCollection AddEmailServiceCore(this IServiceCollection services)
    {
        // Add required services
        services.AddMemoryCache();
        services.AddLogging();

        // Add FluentEmail if not already registered (with basic setup)
        if (!services.Any(x => x.ServiceType == typeof(IFluentEmail)))
        {
            services.AddFluentEmail("noreply@localhost", "Development")
                   .AddSmtpSender("localhost", 25);
        }

        // Add email services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<EmailServiceHealthCheck>("email_service");

        return services;
    }
}

/// <summary>
/// Health check for email service
/// </summary>
public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;

    public EmailServiceHealthCheck(IEmailService emailService)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _emailService.TestConnectionAsync(cancellationToken);
            
            return result.IsSuccess
                ? HealthCheckResult.Healthy("Email service is healthy")
                : HealthCheckResult.Unhealthy($"Email service is unhealthy: {result.Error}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Email service health check failed: {ex.Message}", ex);
        }
    }
}