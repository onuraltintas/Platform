using Enterprise.Shared.ErrorHandling.Exceptions;
using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public class ErrorResponseFactory : IErrorResponseFactory
{
    private readonly ErrorHandlingSettings _settings;
    private readonly IStringLocalizer<ErrorResponseFactory>? _localizer;
    private readonly ILogger<ErrorResponseFactory> _logger;
    private readonly ITimeZoneProvider? _timeZoneProvider;

    public ErrorResponseFactory(
        IOptions<ErrorHandlingSettings> settings,
        ILogger<ErrorResponseFactory> logger,
        IStringLocalizer<ErrorResponseFactory>? localizer = null,
        ITimeZoneProvider? timeZoneProvider = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _localizer = localizer;
        _timeZoneProvider = timeZoneProvider;
    }

    public ErrorResponse CreateErrorResponse(Exception exception, HttpContext context)
    {
        var errorCode = GetErrorCode(exception);
        var message = GetLocalizedMessage(errorCode, context);
        
        return new ErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = context.TraceIdentifier,
            Timestamp = GetCurrentTime(),
            Path = context.Request.Path,
            Method = context.Request.Method,
            StatusCode = GetStatusCode(exception),
            Data = GetErrorData(exception)
        };
    }

    public ValidationErrorResponse CreateValidationErrorResponse(IEnumerable<ValidationError> errors, HttpContext? context = null)
    {
        return new ValidationErrorResponse
        {
            ErrorCode = "VALIDATION_FAILED",
            Message = "One or more validation errors occurred",
            Errors = errors.ToList(),
            Timestamp = GetCurrentTime(),
            CorrelationId = context?.TraceIdentifier ?? Guid.NewGuid().ToString(),
            Path = context?.Request.Path ?? string.Empty,
            Method = context?.Request.Method ?? string.Empty,
            StatusCode = 400
        };
    }

    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(Exception exception, string? correlationId = null)
    {
        if (exception is EnterpriseException enterpriseException)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                enterpriseException.CorrelationId = correlationId;
            }
            return enterpriseException.ToProblemDetails();
        }

        return CreateStandardProblemDetails(exception, correlationId);
    }

    private string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            EnterpriseException ee => ee.ErrorCode,
            ArgumentException => _settings.ErrorCodes.GetValueOrDefault("ValidationFailed", "ERR_VALIDATION_001"),
            _ => "UNKNOWN_ERROR"
        };
    }

    private int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            EnterpriseException ee => ee.HttpStatusCode,
            ArgumentException => 400,
            NotImplementedException => 501,
            TimeoutException => 408,
            OperationCanceledException => 499, // Client closed request
            _ => 500
        };
    }

    private Dictionary<string, object> GetErrorData(Exception exception)
    {
        var data = new Dictionary<string, object>();

        if (exception is EnterpriseException enterpriseException)
        {
            foreach (var item in enterpriseException.ErrorData)
            {
                data[item.Key] = item.Value;
            }
            data["severity"] = enterpriseException.Severity.ToString();
        }

        if (_settings.EnableDetailedErrors && exception.InnerException != null)
        {
            data["innerException"] = exception.InnerException.Message;
        }

        return data;
    }

    private string GetLocalizedMessage(string errorCode, HttpContext context)
    {
        if (!_settings.EnableLocalization || _localizer == null)
            return errorCode;

        try
        {
            var culture = GetRequestCulture(context);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            var localizedMessage = _localizer[errorCode];
            return localizedMessage.ResourceNotFound ? errorCode : localizedMessage.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to localize error message for code: {ErrorCode}", errorCode);
            return errorCode;
        }
    }

    private string GetRequestCulture(HttpContext context)
    {
        return context.Request.Headers["Accept-Language"].FirstOrDefault() 
            ?? _settings.DefaultLanguage;
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateStandardProblemDetails(Exception exception, string? correlationId = null)
    {
        var statusCode = GetStatusCode(exception);
        var detail = _settings.EnableDetailedErrors 
            ? exception.ToString() 
            : "An error occurred while processing your request";

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = GetTitleFromException(exception),
            Detail = SanitizeMessage(detail),
            Status = statusCode,
            Instance = correlationId,
            Extensions = new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = GetCurrentTime(),
                ["errorCode"] = GetErrorCode(exception)
            }
        };

        if (_settings.EnableDetailedErrors)
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = TruncateStackTrace(exception.StackTrace)
            };
        }

        return problemDetails;
    }

    private string GetTitleFromException(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "Invalid Request",
            ArgumentException => "Invalid Argument",
            InvalidOperationException => "Invalid Operation",
            NotImplementedException => "Not Implemented",
            TimeoutException => "Request Timeout",
            OperationCanceledException => "Operation Cancelled",
            _ => "Internal Server Error"
        };
    }

    private string SanitizeMessage(string message)
    {
        if (!_settings.EnableDetailedErrors)
        {
            // Remove sensitive data patterns
            foreach (var pattern in _settings.SensitiveDataPatterns)
            {
                message = Regex.Replace(message, pattern, "***", RegexOptions.IgnoreCase);
            }
        }
        return message;
    }

    private string? TruncateStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return null;
        if (stackTrace.Length <= _settings.MaxErrorStackTraceLength) return stackTrace;
        return stackTrace[.._settings.MaxErrorStackTraceLength] + "... (truncated)";
    }

    private DateTime GetCurrentTime()
    {
        return _timeZoneProvider?.GetCurrentTime() ?? DateTime.UtcNow;
    }
}