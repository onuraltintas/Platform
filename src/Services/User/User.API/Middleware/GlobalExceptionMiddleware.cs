using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace User.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, body) = exception switch
        {
            ArgumentNullException ex => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions = { ["errorCode"] = "ARGUMENT_NULL", ["parameterName"] = ex.ParamName }
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
                    Instance = context.Request.Path,
                    Extensions = { ["errorCode"] = "INVALID_ARGUMENT", ["parameterName"] = ex.ParamName }
                }
            ),

            UnauthorizedAccessException ex => (
                StatusCode: (int)HttpStatusCode.Unauthorized,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = (int)HttpStatusCode.Unauthorized,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions = { ["errorCode"] = "UNAUTHORIZED" }
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
                    Instance = context.Request.Path,
                    Extensions = { ["errorCode"] = "INVALID_OPERATION", ["details"] = ex.Data }
                }
            ),

            TaskCanceledException ex when ex.InnerException is TimeoutException => (
                StatusCode: (int)HttpStatusCode.RequestTimeout,
                Body: new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7",
                    Title = "Request Timeout",
                    Status = (int)HttpStatusCode.RequestTimeout,
                    Detail = "The request timed out",
                    Instance = context.Request.Path,
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
                    Detail = "An internal server error occurred",
                    Instance = context.Request.Path,
                    Extensions = { ["errorCode"] = "INTERNAL_ERROR" }
                }
            )
        };

        // Add correlation ID if available
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            body.Extensions["correlationId"] = correlationId.ToString();
            context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
        }

        // Add timestamp
        body.Extensions["timestamp"] = DateTime.UtcNow;

        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var response = JsonSerializer.Serialize(body, jsonOptions);
        await context.Response.WriteAsync(response);
    }
}