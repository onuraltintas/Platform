using Enterprise.Shared.ErrorHandling.Exceptions;
using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public class ValidationExceptionFilter : IActionFilter
{
    private readonly ILogger<ValidationExceptionFilter> _logger;

    public ValidationExceptionFilter(ILogger<ValidationExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new ValidationError
                {
                    Field = x.Key,
                    Message = e.ErrorMessage,
                    AttemptedValue = x.Value.AttemptedValue
                }))
                .ToList();

            _logger.LogInformation("Model validation failed for action {ActionName} with {ErrorCount} errors", 
                context.ActionDescriptor.DisplayName, errors.Count);

            throw new Exceptions.ValidationException(errors);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No-op
    }
}