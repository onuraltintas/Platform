using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Enterprise.Shared.Validation.Interfaces;
using Enterprise.Shared.Validation.Models;

namespace Enterprise.Shared.Validation.Services;

/// <summary>
/// Implementation of validation service
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ValidationSettings _settings;

    public ValidationService(IServiceProvider serviceProvider, IOptions<ValidationSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        try
        {
            var validator = _serviceProvider.GetService<FluentValidation.IValidator<T>>();
            if (validator == null)
            {
                return ValidationResult.Success();
            }

            var fluentResult = await validator.ValidateAsync(instance, cancellationToken);
            return ConvertToValidationResult(fluentResult);
        }
        catch (Exception ex)
        {
            var error = new ValidationError("General", $"Doğrulama sırasında hata oluştu: {ex.Message}", "VALIDATION_ERROR");
            return ValidationResult.Failed(error);
        }
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance, ValidationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if validator supports context
            var contextualValidator = _serviceProvider.GetService<IContextualValidator<T>>();
            if (contextualValidator != null)
            {
                return await contextualValidator.ValidateAsync(instance, context, cancellationToken);
            }

            // Fallback to regular validation
            return await ValidateAsync(instance, cancellationToken);
        }
        catch (Exception ex)
        {
            var error = new ValidationError("General", $"Doğrulama sırasında hata oluştu: {ex.Message}", "VALIDATION_ERROR");
            return ValidationResult.Failed(error);
        }
    }

    public ValidationResult Validate<T>(T instance)
    {
        try
        {
            var validator = _serviceProvider.GetService<FluentValidation.IValidator<T>>();
            if (validator == null)
            {
                return ValidationResult.Success();
            }

            var fluentResult = validator.Validate(instance);
            return ConvertToValidationResult(fluentResult);
        }
        catch (Exception ex)
        {
            var error = new ValidationError("General", $"Doğrulama sırasında hata oluştu: {ex.Message}", "VALIDATION_ERROR");
            return ValidationResult.Failed(error);
        }
    }

    public ValidationResult Validate<T>(T instance, ValidationContext context)
    {
        try
        {
            // Check if validator supports context
            var contextualValidator = _serviceProvider.GetService<IContextualValidator<T>>();
            if (contextualValidator != null)
            {
                return contextualValidator.Validate(instance, context);
            }

            // Fallback to regular validation
            return Validate(instance);
        }
        catch (Exception ex)
        {
            var error = new ValidationError("General", $"Doğrulama sırasında hata oluştu: {ex.Message}", "VALIDATION_ERROR");
            return ValidationResult.Failed(error);
        }
    }

    public bool TryValidate<T>(T instance, out ValidationResult result)
    {
        try
        {
            result = Validate(instance);
            return result.IsValid;
        }
        catch
        {
            var error = new ValidationError("General", "Doğrulama sırasında beklenmeyen hata oluştu.", "VALIDATION_ERROR");
            result = ValidationResult.Failed(error);
            return false;
        }
    }

    public async Task<bool> TryValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ValidateAsync(instance, cancellationToken);
            return result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    private ValidationResult ConvertToValidationResult(FluentValidation.Results.ValidationResult fluentResult)
    {
        if (fluentResult.IsValid)
        {
            return ValidationResult.Success();
        }

        var errors = fluentResult.Errors
            .Take(_settings.MaxValidationErrors)
            .Select(error => new ValidationError(
                error.PropertyName,
                error.ErrorMessage,
                error.ErrorCode,
                error.AttemptedValue))
            .ToList();

        return ValidationResult.Failed(errors);
    }
}

/// <summary>
/// Pipeline validator implementation
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public class PipelineValidator<T> : IPipelineValidator<T>
{
    private readonly List<Func<T, Task<ValidationResult>>> _validators = new();
    private readonly IServiceProvider _serviceProvider;

    public PipelineValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPipelineValidator<T> Add<TValidator>() where TValidator : class, FluentValidation.IValidator<T>
    {
        _validators.Add(async instance =>
        {
            var validator = _serviceProvider.GetRequiredService<TValidator>();
            var fluentResult = await validator.ValidateAsync(instance);
            return ConvertToValidationResult(fluentResult);
        });
        
        return this;
    }

    public IPipelineValidator<T> Add<TValidator>(TValidator validator) where TValidator : class, FluentValidation.IValidator<T>
    {
        _validators.Add(async instance =>
        {
            var fluentResult = await validator.ValidateAsync(instance);
            return ConvertToValidationResult(fluentResult);
        });
        
        return this;
    }

    public IPipelineValidator<T> AddWhen<TValidator>(Func<T, bool> condition) where TValidator : class, FluentValidation.IValidator<T>
    {
        _validators.Add(async instance =>
        {
            if (!condition(instance))
            {
                return ValidationResult.Success();
            }

            var validator = _serviceProvider.GetRequiredService<TValidator>();
            var fluentResult = await validator.ValidateAsync(instance);
            return ConvertToValidationResult(fluentResult);
        });
        
        return this;
    }

    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var allErrors = new List<ValidationError>();

        foreach (var validatorFunc in _validators)
        {
            var result = await validatorFunc(instance);
            if (!result.IsValid)
            {
                allErrors.AddRange(result.Errors);
            }
        }

        return allErrors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failed(allErrors);
    }

    private static ValidationResult ConvertToValidationResult(FluentValidation.Results.ValidationResult fluentResult)
    {
        if (fluentResult.IsValid)
        {
            return ValidationResult.Success();
        }

        var errors = fluentResult.Errors.Select(error => new ValidationError(
            error.PropertyName,
            error.ErrorMessage,
            error.ErrorCode,
            error.AttemptedValue)).ToList();

        return ValidationResult.Failed(errors);
    }
}

/// <summary>
/// Contextual validator base implementation
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public abstract class ContextualValidator<T> : IContextualValidator<T>
{
    public abstract Task<ValidationResult> ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken = default);
    public abstract ValidationResult Validate(T instance, ValidationContext context);

    public virtual async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var context = new ValidationContext(instance!);
        return await ValidateAsync(instance, context, cancellationToken);
    }

    public virtual ValidationResult Validate(T instance)
    {
        var context = new ValidationContext(instance!);
        return Validate(instance, context);
    }
}

/// <summary>
/// Business rule validator base implementation
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public abstract class BusinessRuleValidator<T> : IBusinessRuleValidator<T>
{
    public abstract Task<ValidationResult> ValidateBusinessRulesAsync(T instance, CancellationToken cancellationToken = default);

    public virtual async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        return await ValidateBusinessRulesAsync(instance, cancellationToken);
    }

    public virtual ValidationResult Validate(T instance)
    {
        return ValidateAsync(instance).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Conditional validator base implementation
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public abstract class ConditionalValidator<T> : IConditionalValidator<T>
{
    public abstract bool ShouldValidate(T instance, ValidationContext context);
    protected abstract Task<ValidationResult> ValidateWhenConditionMet(T instance, ValidationContext context, CancellationToken cancellationToken = default);

    public virtual async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var context = new ValidationContext(instance!);
        
        if (!ShouldValidate(instance, context))
        {
            return ValidationResult.Success();
        }

        return await ValidateWhenConditionMet(instance, context, cancellationToken);
    }

    public virtual ValidationResult Validate(T instance)
    {
        return ValidateAsync(instance).GetAwaiter().GetResult();
    }
}