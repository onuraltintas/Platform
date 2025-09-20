using Enterprise.Shared.Validation.Models;

namespace Enterprise.Shared.Validation.Interfaces;

/// <summary>
/// Generic validator interface for validation operations
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    ValidationResult Validate(T instance);
}

/// <summary>
/// Enhanced validator interface with context support
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IContextualValidator<T> : IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken = default);
    ValidationResult Validate(T instance, ValidationContext context);
}

/// <summary>
/// Conditional validator interface for complex validation scenarios
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IConditionalValidator<T> : IValidator<T>
{
    bool ShouldValidate(T instance, ValidationContext context);
}

/// <summary>
/// Business rule validator interface for domain-specific validations
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IBusinessRuleValidator<T> : IValidator<T>
{
    Task<ValidationResult> ValidateBusinessRulesAsync(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Async validator interface for operations that require external validation
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IAsyncValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pipeline validator interface for chaining multiple validators
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IPipelineValidator<T>
{
    IPipelineValidator<T> Add<TValidator>() where TValidator : class, FluentValidation.IValidator<T>;
    IPipelineValidator<T> Add<TValidator>(TValidator validator) where TValidator : class, FluentValidation.IValidator<T>;
    IPipelineValidator<T> AddWhen<TValidator>(Func<T, bool> condition) where TValidator : class, FluentValidation.IValidator<T>;
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation service interface for managing validation operations
/// </summary>
public interface IValidationService
{
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateAsync<T>(T instance, ValidationContext context, CancellationToken cancellationToken = default);
    ValidationResult Validate<T>(T instance);
    ValidationResult Validate<T>(T instance, ValidationContext context);
    bool TryValidate<T>(T instance, out ValidationResult result);
    Task<bool> TryValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation rule interface for custom validation rules
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IValidationRule<T>
{
    string RuleName { get; }
    string ErrorMessage { get; }
    bool IsValid(T value, ValidationContext? context = null);
    Task<bool> IsValidAsync(T value, ValidationContext? context = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cross-field validation interface for validating multiple properties
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface ICrossFieldValidator<T>
{
    Task<ValidationResult> ValidateCrossFieldAsync(T instance, CancellationToken cancellationToken = default);
    ValidationResult ValidateCrossField(T instance);
}

/// <summary>
/// Localized validator interface for multi-language validation
/// </summary>
public interface ILocalizedValidator
{
    string GetLocalizedMessage(string messageKey, params object[] args);
    void SetCulture(string culture);
    string CurrentCulture { get; }
}