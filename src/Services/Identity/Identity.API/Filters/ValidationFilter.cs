using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.API.Filters;

public class ValidationFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest,
                Detail = "See the errors property for details",
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["errorCode"] = "VALIDATION_ERROR";
            problemDetails.Extensions["errors"] = errors;

            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }

        base.OnActionExecuting(context);
    }
}