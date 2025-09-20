using FluentValidation;
using Enterprise.Shared.Validation.Extensions;

namespace Enterprise.Shared.Validation.Validators;

/// <summary>
/// Base validator with Turkish localization support
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
        // Set Turkish culture for validation messages  
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
    }

    /// <summary>
    /// Validates Turkish phone number
    /// </summary>
    protected static bool BeValidTurkishPhone(string? phone)
    {
        return !string.IsNullOrEmpty(phone) && phone.IsValidTurkishPhone();
    }

    /// <summary>
    /// Validates Turkish TC number
    /// </summary>
    protected static bool BeValidTCNumber(string? tcNo)
    {
        return !string.IsNullOrEmpty(tcNo) && tcNo.IsValidTCNumber();
    }

    /// <summary>
    /// Validates Turkish tax number
    /// </summary>
    protected static bool BeValidTurkishTaxNumber(string? vkn)
    {
        return !string.IsNullOrEmpty(vkn) && vkn.IsValidTurkishTaxNumber();
    }

    /// <summary>
    /// Validates Turkish IBAN
    /// </summary>
    protected static bool BeValidTurkishIban(string? iban)
    {
        return !string.IsNullOrEmpty(iban) && iban.IsValidTurkishIban();
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    protected static bool BeValidEmailAddress(string? email)
    {
        return !string.IsNullOrEmpty(email) && email.IsValidEmail();
    }

    /// <summary>
    /// Checks if date is not in future
    /// </summary>
    protected static bool NotBeInFuture(DateTime? date)
    {
        if (!date.HasValue) return true;
        return date.Value <= DateTimeExtensions.GetTurkeyNow();
    }

    /// <summary>
    /// Checks if date is not in past
    /// </summary>
    protected static bool NotBeInPast(DateTime? date)
    {
        if (!date.HasValue) return true;
        return date.Value >= DateTimeExtensions.GetTurkeyNow().Date;
    }

    /// <summary>
    /// Validates minimum age
    /// </summary>
    protected static bool BeValidAge(DateTime? birthDate, int minimumAge)
    {
        if (!birthDate.HasValue) return false;
        return birthDate.Value.Age() >= minimumAge;
    }

    /// <summary>
    /// Validates strong password
    /// </summary>
    protected static bool BeStrongPassword(string? password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        if (password.Length < 8) return false;
        
        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
        
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    /// <summary>
    /// Validates Turkish text (allows Turkish characters)
    /// </summary>
    protected static bool BeTurkishText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        
        return text.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || 
                            "çÇğĞıİöÖşŞüÜ".Contains(c));
    }

    /// <summary>
    /// Validates file size in MB
    /// </summary>
    protected static bool BeValidFileSize(long? fileSizeBytes, int maxSizeMB)
    {
        if (!fileSizeBytes.HasValue) return true;
        var maxSizeBytes = maxSizeMB * 1024 * 1024;
        return fileSizeBytes.Value <= maxSizeBytes;
    }

    /// <summary>
    /// Validates allowed file extensions
    /// </summary>
    protected static bool HaveAllowedExtension(string? fileName, string[] allowedExtensions)
    {
        if (string.IsNullOrEmpty(fileName)) return true;
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
}