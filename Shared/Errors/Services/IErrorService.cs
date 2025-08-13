using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Shared.Errors.Services;

public interface IErrorService
{
    ErrorResult CreateError(string code, string message, string? detail = null);
    ErrorResult CreateValidationError(string field, string message, object? attemptedValue = null);
    List<ErrorResult> CreateValidationErrors(Dictionary<string, string> fieldErrors);
    void LogError(Exception exception, string? additionalInfo = null);
    void LogError(string errorCode, string message, string? additionalInfo = null);
}