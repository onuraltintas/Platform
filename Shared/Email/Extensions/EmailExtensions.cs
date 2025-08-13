using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Email.Configuration;
using EgitimPlatform.Shared.Email.Services;

namespace EgitimPlatform.Shared.Email.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Email")
    {
        // Configure options
        services.Configure<EmailOptions>(configuration.GetSection(configurationSection));

        // Validate configuration
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        // Register HTTP client for external API calls
        services.AddHttpClient<EmailValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register core services
        services.AddSingleton<IEmailTemplateService, HandlebarsTemplateService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>(); // Changed to Scoped

        return services;
    }

    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        Action<EmailOptions> configureOptions)
    {
        // Configure options using action
        services.Configure(configureOptions);

        // Validate configuration
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        // Register HTTP client for external API calls
        services.AddHttpClient<EmailValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register core services
        services.AddSingleton<IEmailTemplateService, HandlebarsTemplateService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>(); // Changed to Scoped

        return services;
    }

    public static IServiceCollection AddSmtpEmailService(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Email")
    {
        services.Configure<EmailOptions>(configuration.GetSection(configurationSection));
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddHttpClient<EmailValidationService>();
        
        services.AddSingleton<IEmailTemplateService, HandlebarsTemplateService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }

    public static IServiceCollection AddEmailTemplateService(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Email")
    {
        services.Configure<EmailOptions>(configuration.GetSection(configurationSection));
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddSingleton<IEmailTemplateService, HandlebarsTemplateService>();

        return services;
    }

    public static IServiceCollection AddEmailValidationService(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Email")
    {
        services.Configure<EmailOptions>(configuration.GetSection(configurationSection));
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddHttpClient<EmailValidationService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();

        return services;
    }

    public static IServiceCollection AddEmailQueueService(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Email")
    {
        services.Configure<EmailOptions>(configuration.GetSection(configurationSection));
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        // Email queue service requires email service
        services.AddSmtpEmailService(configuration, configurationSection);
        services.AddScoped<IEmailQueueService, EmailQueueService>(); // Changed to Scoped

        return services;
    }
}

public class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        // SMTP validation
        if (string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            failures.Add("SMTP Host is required");
        }

        if (options.Smtp.Port <= 0 || options.Smtp.Port > 65535)
        {
            failures.Add("SMTP Port must be between 1 and 65535");
        }

        // Settings validation
        if (string.IsNullOrWhiteSpace(options.Settings.DefaultFromEmail))
        {
            failures.Add("Default From Email is required");
        }
        else if (!IsValidEmail(options.Settings.DefaultFromEmail))
        {
            failures.Add("Default From Email is not a valid email address");
        }

        if (!string.IsNullOrWhiteSpace(options.Settings.DefaultReplyToEmail) &&
            !IsValidEmail(options.Settings.DefaultReplyToEmail))
        {
            failures.Add("Default Reply-To Email is not a valid email address");
        }

        // Template validation
        if (string.IsNullOrWhiteSpace(options.Templates.TemplatesPath))
        {
            failures.Add("Templates Path is required");
        }

        if (options.Templates.CacheExpirationMinutes < 0)
        {
            failures.Add("Cache Expiration Minutes cannot be negative");
        }

        // Throttling validation
        if (options.Throttling.MaxEmailsPerHour < 0)
        {
            failures.Add("Max Emails Per Hour cannot be negative");
        }

        if (options.Throttling.MaxEmailsPerDay < 0)
        {
            failures.Add("Max Emails Per Day cannot be negative");
        }

        if (options.Throttling.DelayBetweenEmailsMs < 0)
        {
            failures.Add("Delay Between Emails cannot be negative");
        }

        if (options.Throttling.BulkEmailBatchSize <= 0)
        {
            failures.Add("Bulk Email Batch Size must be greater than 0");
        }

        // Queue validation
        if (options.Queue.MaxRetryAttempts < 0)
        {
            failures.Add("Max Retry Attempts cannot be negative");
        }

        if (options.Queue.RetryDelayMinutes < 0)
        {
            failures.Add("Retry Delay Minutes cannot be negative");
        }

        if (options.Queue.ProcessingIntervalSeconds <= 0)
        {
            failures.Add("Processing Interval Seconds must be greater than 0");
        }

        if (options.Queue.MaxBatchSize <= 0)
        {
            failures.Add("Max Batch Size must be greater than 0");
        }

        if (options.Queue.DeliveryResultRetentionDays <= 0)
        {
            failures.Add("Delivery Result Retention Days must be greater than 0");
        }

        // Security validation
        if (options.Security.RequireSSL && !options.Smtp.EnableSsl && !options.Smtp.EnableStartTls)
        {
            failures.Add("SSL is required but neither EnableSsl nor EnableStartTls is configured");
        }

        if (options.Security.MaxAttachmentSizeMB < 0)
        {
            failures.Add("Max Attachment Size cannot be negative");
        }

        if (failures.Any())
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}