using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public interface IErrorResponseFactory
{
    ErrorResponse CreateErrorResponse(Exception exception, HttpContext context);
    ValidationErrorResponse CreateValidationErrorResponse(IEnumerable<ValidationError> errors, HttpContext? context = null);
    Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(Exception exception, string? correlationId = null);
}