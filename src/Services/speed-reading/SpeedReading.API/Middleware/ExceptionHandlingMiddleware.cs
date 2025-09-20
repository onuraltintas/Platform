using SpeedReading.API.Models;
using System.Net;
using System.Text.Json;

namespace SpeedReading.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ArgumentNullException nullEx => new ApiResponse
            {
                Success = false,
                Error = "Required parameter is missing",
                ErrorDetails = new { Message = nullEx.Message, Parameter = nullEx.ParamName }
            },
            ArgumentException argEx => new ApiResponse
            {
                Success = false,
                Error = "Invalid input parameter",
                ErrorDetails = new { Message = argEx.Message, Parameter = argEx.ParamName }
            },
            InvalidOperationException opEx => new ApiResponse
            {
                Success = false,
                Error = "Invalid operation",
                ErrorDetails = new { Message = opEx.Message }
            },
            UnauthorizedAccessException => new ApiResponse
            {
                Success = false,
                Error = "Access denied",
                ErrorDetails = new { Message = "You do not have permission to perform this action" }
            },
            KeyNotFoundException => new ApiResponse
            {
                Success = false,
                Error = "Resource not found",
                ErrorDetails = new { Message = "The requested resource was not found" }
            },
            TimeoutException => new ApiResponse
            {
                Success = false,
                Error = "Operation timeout",
                ErrorDetails = new { Message = "The operation took too long to complete" }
            },
            _ => new ApiResponse
            {
                Success = false,
                Error = "An internal error occurred",
                ErrorDetails = new { Message = "Please try again later or contact support if the problem persists" }
            }
        };

        context.Response.StatusCode = exception switch
        {
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(jsonResponse);
    }
}