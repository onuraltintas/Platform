using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Errors.Exceptions;

namespace EgitimPlatform.Shared.Errors.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", traceId);
        
        var response = CreateErrorResponse(exception, traceId);
        var statusCode = GetStatusCode(exception);
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
    
    private static ApiResponse CreateErrorResponse(Exception exception, string traceId)
    {
        return exception switch
        {
            BaseException baseEx => ApiResponse.Fail(new ErrorResult(baseEx.ErrorCode, baseEx.Message)
                .WithTraceId(traceId)),
            
            FluentValidation.ValidationException validationEx => ApiResponse.Fail(
                validationEx.Errors.Select(ve => 
                    new ErrorResult($"VALIDATION_{ve.PropertyName?.ToUpper()}", ve.ErrorMessage)
                        .WithTraceId(traceId)
                        .WithMetadata("field", ve.PropertyName!)
                        .WithMetadata("attemptedValue", ve.AttemptedValue?.ToString() ?? ""))
                .ToList()),
            
            ArgumentNullException argNullEx => ApiResponse.Fail(
                new ErrorResult(ErrorCodes.BAD_REQUEST, $"Required parameter '{argNullEx.ParamName}' is missing.")
                    .WithTraceId(traceId)),
            
            ArgumentException argEx => ApiResponse.Fail(
                new ErrorResult(ErrorCodes.BAD_REQUEST, argEx.Message)
                    .WithTraceId(traceId)),
            
            UnauthorizedAccessException => ApiResponse.Fail(
                new ErrorResult(ErrorCodes.UNAUTHORIZED, "Unauthorized access.")
                    .WithTraceId(traceId)),
            
            TimeoutException => ApiResponse.Fail(
                new ErrorResult(ErrorCodes.TIMEOUT, "The operation timed out.")
                    .WithTraceId(traceId)),
            
            _ => ApiResponse.Fail(
                new ErrorResult(ErrorCodes.INTERNAL_SERVER_ERROR, "An internal server error occurred.")
                    .WithTraceId(traceId))
        };
    }
    
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            BaseException baseEx => baseEx.StatusCode,
            FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}