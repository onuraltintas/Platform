using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Shared.Errors.Exceptions;

public class ValidationException : BaseException
{
    public List<ValidationError> ValidationErrors { get; }
    
    public ValidationException(List<ValidationError> validationErrors) 
        : base(ErrorCodes.VALIDATION_ERROR, "One or more validation errors occurred.", 400)
    {
        ValidationErrors = validationErrors;
    }
    
    public ValidationException(string field, string message) 
        : base(ErrorCodes.VALIDATION_ERROR, "Validation error occurred.", 400)
    {
        ValidationErrors = new List<ValidationError> 
        { 
            new ValidationError(field, message) 
        };
    }
    
    public ValidationException(string message) 
        : base(ErrorCodes.VALIDATION_ERROR, message, 400)
    {
        ValidationErrors = new List<ValidationError>();
    }
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
    public object? AttemptedValue { get; set; }
    
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
    
    public ValidationError(string field, string message, object attemptedValue) : this(field, message)
    {
        AttemptedValue = attemptedValue;
    }
}