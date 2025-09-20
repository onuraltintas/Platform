using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class ValidationException : EnterpriseException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation errors occurred", "VALIDATION_FAILED", 400)
    {
        Errors = errors.ToList();
        ErrorData["errors"] = Errors;
        Severity = Models.ErrorSeverity.Low;
    }

    public ValidationException(string field, string message)
        : this(new[] { new ValidationError { Field = field, Message = message } })
    {
    }

    public ValidationException(string field, string message, string? code, object? attemptedValue = null)
        : this(new[] { new ValidationError 
        { 
            Field = field, 
            Message = message, 
            Code = code, 
            AttemptedValue = attemptedValue 
        } })
    {
    }

    protected override string GetTitle() => "Validation Failed";

    public static ValidationException FromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => new ValidationError
            {
                Field = x.Key,
                Message = e.ErrorMessage,
                AttemptedValue = x.Value.AttemptedValue
            }))
            .ToList();

        return new ValidationException(errors);
    }
}