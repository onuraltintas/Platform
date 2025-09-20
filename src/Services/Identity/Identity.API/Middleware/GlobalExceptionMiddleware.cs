using System.Net;
using System.Text.Json;
using Identity.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred during request processing");
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = CreateErrorResponse(exception);
        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response.Body, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private (int StatusCode, object Body) CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            PermissionNotFoundException ex => (
                StatusCode: (int)HttpStatusCode.NotFound,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Permission Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            RoleNotFoundException ex => (
                StatusCode: (int)HttpStatusCode.NotFound,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Role Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            UserNotFoundException ex => (
                StatusCode: (int)HttpStatusCode.NotFound,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "User Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            GroupNotFoundException ex => (
                StatusCode: (int)HttpStatusCode.NotFound,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Group Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            ServiceNotFoundException ex => (
                StatusCode: (int)HttpStatusCode.NotFound,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Service Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            DuplicateResourceException ex => (
                StatusCode: (int)HttpStatusCode.Conflict,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Duplicate Resource",
                    Status = (int)HttpStatusCode.Conflict,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            InsufficientPermissionsException ex => (
                StatusCode: (int)HttpStatusCode.Forbidden,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Insufficient Permissions",
                    Status = (int)HttpStatusCode.Forbidden,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            ValidationException ex => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Error",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            System.InvalidOperationException ex => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Invalid Operation",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = "INVALID_OPERATION", ["details"] = ex.Data }
                }
            ),

            ExternalServiceException ex => (
                StatusCode: (int)HttpStatusCode.BadGateway,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    Title = "External Service Error",
                    Status = (int)HttpStatusCode.BadGateway,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = ex.ErrorCode, ["details"] = ex.Details }
                }
            ),

            ArgumentException ex => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = ex.Message,
                    Extensions = { ["errorCode"] = "INVALID_ARGUMENT" }
                }
            ),

            UnauthorizedAccessException ex => (
                StatusCode: (int)HttpStatusCode.Unauthorized,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = (int)HttpStatusCode.Unauthorized,
                    Detail = "Authentication is required to access this resource",
                    Extensions = { ["errorCode"] = "UNAUTHORIZED" }
                }
            ),

            NotImplementedException ex => (
                StatusCode: (int)HttpStatusCode.NotImplemented,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2",
                    Title = "Not Implemented",
                    Status = (int)HttpStatusCode.NotImplemented,
                    Detail = "This functionality is not yet implemented",
                    Extensions = { ["errorCode"] = "NOT_IMPLEMENTED" }
                }
            ),

            TimeoutException ex => (
                StatusCode: (int)HttpStatusCode.RequestTimeout,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7",
                    Title = "Request Timeout",
                    Status = (int)HttpStatusCode.RequestTimeout,
                    Detail = "The request timed out",
                    Extensions = { ["errorCode"] = "REQUEST_TIMEOUT" }
                }
            ),

            _ => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred",
                    Extensions = _environment.IsDevelopment()
                        ? new Dictionary<string, object?>
                        {
                            ["errorCode"] = "INTERNAL_ERROR",
                            ["stackTrace"] = exception.StackTrace,
                            ["exceptionType"] = exception.GetType().Name
                        }
                        : new Dictionary<string, object?> { ["errorCode"] = "INTERNAL_ERROR" }
                }
            )
        };
    }
}