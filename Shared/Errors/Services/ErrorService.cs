using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Shared.Errors.Services;

public class ErrorService : IErrorService
{
    private readonly ILogger<ErrorService> _logger;
    
    public ErrorService(ILogger<ErrorService> logger)
    {
        _logger = logger;
    }
    
    public ErrorResult CreateError(string code, string message, string? detail = null)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        return new ErrorResult(code, message, detail!)
            .WithTraceId(traceId);
    }
    
    public ErrorResult CreateValidationError(string field, string message, object? attemptedValue = null)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        var error = new ErrorResult($"VALIDATION_{field.ToUpper()}", message)
            .WithTraceId(traceId)
            .WithMetadata("field", field);
            
        if (attemptedValue != null)
        {
            error.WithMetadata("attemptedValue", attemptedValue);
        }
        
        return error;
    }
    
    public List<ErrorResult> CreateValidationErrors(Dictionary<string, string> fieldErrors)
    {
        return fieldErrors.Select(kvp => CreateValidationError(kvp.Key, kvp.Value)).ToList();
    }
    
    public void LogError(Exception exception, string? additionalInfo = null)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        _logger.LogError(exception, 
            "Error occurred. TraceId: {TraceId}, AdditionalInfo: {AdditionalInfo}", 
            traceId, 
            additionalInfo ?? "None");
    }
    
    public void LogError(string errorCode, string message, string? additionalInfo = null)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        _logger.LogError(
            "Error occurred. ErrorCode: {ErrorCode}, Message: {Message}, TraceId: {TraceId}, AdditionalInfo: {AdditionalInfo}",
            errorCode,
            message,
            traceId,
            additionalInfo ?? "None");
    }
}