using FluentValidation;
using User.Core.DTOs;

namespace User.Application.Validators;

/// <summary>
/// Validator for UserPreferencesDto
/// </summary>
public class UserPreferencesDtoValidator : AbstractValidator<UserPreferencesDto>
{
    /// <summary>
    /// Constructor - Define validation rules
    /// </summary>
    public UserPreferencesDtoValidator()
    {
        RuleFor(x => x.ProfileVisibility)
            .NotEmpty()
            .WithMessage("Profile visibility is required")
            .Must(BeValidVisibility)
            .WithMessage("Profile visibility must be 'Public', 'Private', or 'Friends'");

        RuleFor(x => x.Theme)
            .Must(BeValidTheme)
            .WithMessage("Theme must be 'Light', 'Dark', or 'Auto'")
            .When(x => !string.IsNullOrWhiteSpace(x.Theme));

        RuleFor(x => x.DataProcessingConsent)
            .Equal(true)
            .WithMessage("Data processing consent is required and must be true");

        // Notification preferences - at least one must be enabled for user to receive any notifications
        RuleFor(x => x)
            .Must(HaveAtLeastOneNotificationMethod)
            .WithMessage("At least one notification method (Email, SMS, or Push) must be enabled")
            .When(x => x.EmailNotifications || x.SmsNotifications || x.PushNotifications);

        // GDPR compliance - if marketing consent is true, data processing must also be true
        RuleFor(x => x.MarketingConsent)
            .Equal(false)
            .WithMessage("Marketing consent cannot be true when data processing consent is false")
            .When(x => x.MarketingConsent && !x.DataProcessingConsent);

        // Analytics consent - if analytics consent is true, data processing must also be true
        RuleFor(x => x.AnalyticsConsent)
            .Equal(false)
            .WithMessage("Analytics consent cannot be true when data processing consent is false")
            .When(x => x.AnalyticsConsent && !x.DataProcessingConsent);
    }

    /// <summary>
    /// Check if profile visibility value is valid
    /// </summary>
    /// <param name="visibility">Visibility value</param>
    /// <returns>True if valid</returns>
    private static bool BeValidVisibility(string? visibility)
    {
        if (string.IsNullOrWhiteSpace(visibility))
            return false;

        var validVisibilities = new[] { "Public", "Private", "Friends" };
        return validVisibilities.Contains(visibility, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if theme value is valid
    /// </summary>
    /// <param name="theme">Theme value</param>
    /// <returns>True if valid</returns>
    private static bool BeValidTheme(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return true; // Theme is optional

        var validThemes = new[] { "Light", "Dark", "Auto" };
        return validThemes.Contains(theme, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if at least one notification method is enabled
    /// </summary>
    /// <param name="preferences">User preferences</param>
    /// <returns>True if at least one method is enabled</returns>
    private static bool HaveAtLeastOneNotificationMethod(UserPreferencesDto preferences)
    {
        return preferences.EmailNotifications || 
               preferences.SmsNotifications || 
               preferences.PushNotifications;
    }
}