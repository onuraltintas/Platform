namespace Enterprise.Shared.Validation.Models;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; private set; }
    public List<ValidationError> Errors { get; private set; } = new();

    public ValidationResult(bool isValid)
    {
        IsValid = isValid;
    }

    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        IsValid = false;
        Errors = errors.ToList();
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failed(params ValidationError[] errors) => new(errors);
    public static ValidationResult Failed(IEnumerable<ValidationError> errors) => new(errors);
}

/// <summary>
/// Represents a validation error with Turkish localization support
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public Dictionary<string, object> PlaceholderValues { get; set; } = new();

    public ValidationError()
    {
    }

    public ValidationError(string propertyName, string errorMessage, string errorCode = "", object? attemptedValue = null)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        AttemptedValue = attemptedValue;
    }

    public ValidationError WithPlaceholder(string key, object value)
    {
        PlaceholderValues[key] = value;
        return this;
    }

    public override string ToString()
    {
        return $"{PropertyName}: {ErrorMessage}";
    }
}

/// <summary>
/// Validation settings for Turkish localization
/// </summary>
public class ValidationSettings
{
    public string Culture { get; set; } = "tr-TR";
    public string TimeZone { get; set; } = "Turkey Standard Time";
    public bool EnableDetailedErrors { get; set; } = true;
    public bool EnableLocalization { get; set; } = true;
    public int MaxValidationErrors { get; set; } = 50;
    public Dictionary<string, string> CustomMessages { get; set; } = new();
}

/// <summary>
/// Validation context for enhanced validation scenarios
/// </summary>
public class ValidationContext
{
    public object Instance { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public object? PropertyValue { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? UserId { get; set; }
    public string? UserRole { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;

    public ValidationContext(object instance)
    {
        Instance = instance;
    }

    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }
}