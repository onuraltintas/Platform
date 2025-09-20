using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Enterprise.Shared.Validation.Models;
using Enterprise.Shared.Validation.Interfaces;
using Enterprise.Shared.Validation.Services;
using System.Reflection;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// Service collection extensions for Enterprise Shared Validation
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Enterprise Shared Validation services with configuration
    /// </summary>
    public static IServiceCollection AddEnterpriseValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure validation settings
        services.Configure<ValidationSettings>(
            configuration.GetSection("ValidationSettings"));

        return services.AddEnterpriseValidationCore();
    }

    /// <summary>
    /// Adds Enterprise Shared Validation services with action configuration
    /// </summary>
    public static IServiceCollection AddEnterpriseValidation(
        this IServiceCollection services,
        Action<ValidationSettings> configureSettings)
    {
        // Configure validation settings
        services.Configure(configureSettings);

        return services.AddEnterpriseValidationCore();
    }

    /// <summary>
    /// Adds Enterprise Shared Validation services with default configuration
    /// </summary>
    public static IServiceCollection AddEnterpriseValidation(
        this IServiceCollection services)
    {
        // Configure default validation settings
        services.Configure<ValidationSettings>(settings =>
        {
            settings.Culture = "tr-TR";
            settings.TimeZone = "Turkey Standard Time";
            settings.EnableDetailedErrors = true;
            settings.EnableLocalization = true;
            settings.MaxValidationErrors = 50;
        });

        return services.AddEnterpriseValidationCore();
    }

    private static IServiceCollection AddEnterpriseValidationCore(this IServiceCollection services)
    {
        // Set Turkish culture globally
        SetTurkishCulture();

        // Register core validation services
        services.AddScoped<IValidationService, ValidationService>();
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Configure FluentValidation with Turkish messages
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
        {
            // Convert property names to Turkish display names
            return ConvertToTurkishDisplayName(member?.Name ?? string.Empty);
        };

        // Add localization services
        services.AddLocalization(options =>
        {
            options.ResourcesPath = "Resources";
        });

        // Configure Turkish localization (if ASP.NET Core is available)
        try 
        {
            services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { new CultureInfo("tr-TR") };
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }
        catch 
        {
            // Ignore if ASP.NET Core is not available
        }

        return services;
    }

    /// <summary>
    /// Adds validators from specified assembly
    /// </summary>
    public static IServiceCollection AddValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.BaseType?.IsGenericType == true &&
                       t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var interfaceType = validatorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(FluentValidation.IValidator<>));
                               
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, validatorType);
                services.AddScoped(validatorType);
            }
        }

        return services;
    }

    /// <summary>
    /// Adds custom validator
    /// </summary>
    public static IServiceCollection AddValidator<TValidator, TModel>(
        this IServiceCollection services)
        where TValidator : class, FluentValidation.IValidator<TModel>
    {
        services.AddScoped<FluentValidation.IValidator<TModel>, TValidator>();
        services.AddScoped<TValidator>();
        return services;
    }

    /// <summary>
    /// Adds validation pipeline
    /// </summary>
    public static IServiceCollection AddValidationPipeline<T>(
        this IServiceCollection services,
        Action<IPipelineValidator<T>> configurePipeline)
    {
        var pipeline = new PipelineValidator<T>(services.BuildServiceProvider());
        configurePipeline(pipeline);
        
        services.AddSingleton<IPipelineValidator<T>>(pipeline);
        return services;
    }

    private static void SetTurkishCulture()
    {
        var turkishCulture = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
        CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;
        
        // Set Turkish validation messages
        ConfigureTurkishValidationMessages();
    }

    private static void ConfigureTurkishValidationMessages()
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr-TR");
        
        // Configure Turkish language for FluentValidation
        // Note: Custom Turkish translations are handled via resource files or custom validators
    }

    private static string ConvertToTurkishDisplayName(string propertyName)
    {
        // Convert common English property names to Turkish
        var turkishNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Email"] = "E-posta",
            ["EmailAddress"] = "E-posta Adresi",
            ["Password"] = "Şifre",
            ["PasswordConfirmation"] = "Şifre Tekrarı",
            ["FirstName"] = "Ad",
            ["LastName"] = "Soyad",
            ["FullName"] = "Ad Soyad",
            ["PhoneNumber"] = "Telefon Numarası",
            ["Phone"] = "Telefon",
            ["BirthDate"] = "Doğum Tarihi",
            ["Age"] = "Yaş",
            ["Address"] = "Adres",
            ["City"] = "Şehir",
            ["Country"] = "Ülke",
            ["PostalCode"] = "Posta Kodu",
            ["CompanyName"] = "Şirket Adı",
            ["TaxNumber"] = "Vergi Numarası",
            ["TCNumber"] = "T.C. Kimlik No",
            ["Name"] = "Ad",
            ["Description"] = "Açıklama",
            ["Title"] = "Başlık",
            ["Price"] = "Fiyat",
            ["Quantity"] = "Miktar",
            ["Category"] = "Kategori",
            ["Status"] = "Durum",
            ["CreatedAt"] = "Oluşturulma Tarihi",
            ["UpdatedAt"] = "Güncellenme Tarihi",
            ["IsActive"] = "Aktif mi",
            ["Username"] = "Kullanıcı Adı",
            ["Website"] = "Web Sitesi",
            ["Notes"] = "Notlar",
            ["Comments"] = "Yorumlar"
        };

        return turkishNames.TryGetValue(propertyName, out var turkishName) ? turkishName : propertyName;
    }
}

/// <summary>
/// Configuration extensions for validation
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets validation settings from configuration
    /// </summary>
    public static ValidationSettings GetValidationSettings(this IConfiguration configuration)
    {
        var section = configuration.GetSection("ValidationSettings");
        var settings = section.Get<ValidationSettings>() ?? new ValidationSettings();
        
        return settings;
    }

    /// <summary>
    /// Validates configuration section exists
    /// </summary>
    public static bool HasValidationSettings(this IConfiguration configuration)
    {
        return configuration.GetSection("ValidationSettings").Exists();
    }
}