using Enterprise.Shared.Privacy.Anonymization;
using Enterprise.Shared.Privacy.Consent;
using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Enterprise.Shared.Privacy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Privacy.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrivacy(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure settings
        services.Configure<PrivacySettings>(
            configuration.GetSection("Privacy"));
        
        // Validate settings
        services.AddSingleton<IValidateOptions<PrivacySettings>, PrivacySettingsValidator>();

        // Register services
        services.AddSingleton<IDataAnonymizationService, DataAnonymizationService>();
        services.AddSingleton<IConsentManagementService, ConsentManagementService>();
        services.AddSingleton<IPrivacyAuditService, PrivacyAuditService>();

        return services;
    }

    public static IServiceCollection AddPrivacy(this IServiceCollection services, Action<PrivacySettings> configureSettings)
    {
        // Configure settings with action
        services.Configure(configureSettings);
        
        // Validate settings
        services.AddSingleton<IValidateOptions<PrivacySettings>, PrivacySettingsValidator>();

        // Register services
        services.AddSingleton<IDataAnonymizationService, DataAnonymizationService>();
        services.AddSingleton<IConsentManagementService, ConsentManagementService>();
        services.AddSingleton<IPrivacyAuditService, PrivacyAuditService>();

        return services;
    }

    public static IServiceCollection AddDataAnonymization(this IServiceCollection services)
    {
        services.AddSingleton<IDataAnonymizationService, DataAnonymizationService>();
        return services;
    }

    public static IServiceCollection AddConsentManagement(this IServiceCollection services)
    {
        services.AddSingleton<IConsentManagementService, ConsentManagementService>();
        return services;
    }

    public static IServiceCollection AddPrivacyAudit(this IServiceCollection services)
    {
        services.AddSingleton<IPrivacyAuditService, PrivacyAuditService>();
        return services;
    }
}

public class PrivacySettingsValidator : IValidateOptions<PrivacySettings>
{
    public ValidateOptionsResult Validate(string? name, PrivacySettings options)
    {
        var errors = new List<string>();

        // Data Anonymization validation
        if (options.Anonymization.EnableAnonymization)
        {
            if (string.IsNullOrEmpty(options.Anonymization.HashingSalt))
                errors.Add("Anonymization.HashingSalt is required when anonymization is enabled");
            
            if (options.Anonymization.HashingIterations <= 0)
                errors.Add("Anonymization.HashingIterations must be greater than 0");
            
            if (string.IsNullOrEmpty(options.Anonymization.EncryptionKey))
                errors.Add("Anonymization.EncryptionKey is required when anonymization is enabled");
        }

        // Consent Management validation
        if (options.ConsentManagement.EnableConsentTracking)
        {
            if (options.ConsentManagement.ConsentExpirationDays <= 0)
                errors.Add("ConsentManagement.ConsentExpirationDays must be greater than 0");
        }

        // Data Retention validation
        if (options.DataRetention.EnableAutomaticDeletion)
        {
            if (options.DataRetention.DefaultRetentionDays <= 0)
                errors.Add("DataRetention.DefaultRetentionDays must be greater than 0");
        }

        // GDPR Compliance validation
        if (options.GdprCompliance.EnableGdprCompliance)
        {
            if (string.IsNullOrEmpty(options.GdprCompliance.DataControllerName))
                errors.Add("GdprCompliance.DataControllerName is required when GDPR compliance is enabled");
            
            if (options.GdprCompliance.ResponseTimeLimit <= 0)
                errors.Add("GdprCompliance.ResponseTimeLimit must be greater than 0");
        }

        // Audit Logging validation
        if (options.AuditLogging.EnableAuditLogging)
        {
            if (options.AuditLogging.AuditLogRetentionDays <= 0)
                errors.Add("AuditLogging.AuditLogRetentionDays must be greater than 0");
        }

        return errors.Any()
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}