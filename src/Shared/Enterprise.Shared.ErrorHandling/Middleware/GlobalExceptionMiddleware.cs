using Enterprise.Shared.ErrorHandling.Exceptions;
using Enterprise.Shared.ErrorHandling.Handlers;
using Enterprise.Shared.ErrorHandling.Models;
using System.Text.Json;

namespace Enterprise.Shared.ErrorHandling.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly ErrorHandlingSettings _settings;
    private readonly IErrorResponseFactory _responseFactory;
    private readonly ICorrelationContextAccessor? _correlationContext;
    private readonly ITimeZoneProvider? _timeZoneProvider;

    public GlobalExceptionMiddleware(RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<ErrorHandlingSettings> settings,
        IErrorResponseFactory responseFactory,
        ICorrelationContextAccessor? correlationContext = null,
        ITimeZoneProvider? timeZoneProvider = null)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
        _responseFactory = responseFactory;
        _correlationContext = correlationContext;
        _timeZoneProvider = timeZoneProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = GetCorrelationId(context);

        // Log the exception
        LogException(exception, correlationId, context);

        // Create error response
        var problemDetails = CreateErrorResponse(exception, correlationId);

        // Set response
        context.Response.StatusCode = problemDetails.Status ?? 500;
        
        // Ensure we don't write to response if it has already started
        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/problem+json";
            var json = System.Text.Json.JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            await context.Response.WriteAsync(json);
        }
    }

    private string GetCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from correlation context
        var correlationId = _correlationContext?.CorrelationContext?.CorrelationId;
        
        // Fallback to trace identifier
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        // Fallback to new GUID
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        return correlationId;
    }

    private void LogException(Exception exception, string correlationId, HttpContext context)
    {
        var logLevel = GetLogLevel(exception);
        var requestInfo = GetRequestInfo(context);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = requestInfo.Path,
            ["RequestMethod"] = requestInfo.Method,
            ["UserAgent"] = requestInfo.UserAgent,
            ["RemoteIpAddress"] = requestInfo.RemoteIpAddress
        });

        _logger.Log(logLevel, exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
            correlationId, exception.GetType().Name, exception.Message);

        // Log additional details for critical errors
        if (logLevel >= LogLevel.Error && exception is EnterpriseException enterpriseException)
        {
            _logger.LogError("Enterprise exception details: ErrorCode={ErrorCode}, Severity={Severity}, Data={@Data}",
                enterpriseException.ErrorCode, enterpriseException.Severity, enterpriseException.ErrorData);
        }
    }

    private LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            EnterpriseException ee => GetLogLevelFromSeverity(ee.Severity),
            ArgumentException => LogLevel.Warning,
            OperationCanceledException => LogLevel.Information,
            TimeoutException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }

    private LogLevel GetLogLevelFromSeverity(Models.ErrorSeverity severity)
    {
        return severity switch
        {
            Models.ErrorSeverity.Low => LogLevel.Information,
            Models.ErrorSeverity.Medium => LogLevel.Warning,
            Models.ErrorSeverity.High => LogLevel.Error,
            Models.ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Warning
        };
    }

    private (string Path, string Method, string UserAgent, string RemoteIpAddress) GetRequestInfo(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        return (path, method, userAgent, remoteIp);
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateErrorResponse(Exception exception, string correlationId)
    {
        // Handle EnterpriseException
        if (exception is EnterpriseException enterpriseException)
        {
            enterpriseException.CorrelationId = correlationId;
            return enterpriseException.ToProblemDetails();
        }

        // Handle FluentValidation ValidationException
        if (exception is FluentValidationException fluentValidationException)
        {
            return CreateValidationProblemDetails(fluentValidationException, correlationId);
        }

        // Handle standard exceptions
        return exception switch
        {
            ArgumentNullException argNullEx => CreateProblemDetails(
                "Invalid Request", 
                $"Required parameter '{argNullEx.ParamName}' is missing", 
                400, correlationId),
            
            ArgumentException argEx => CreateProblemDetails(
                "Invalid Argument", 
                SanitizeMessage(argEx.Message), 
                400, correlationId),
            
            InvalidOperationException invOpEx => CreateProblemDetails(
                "Invalid Operation", 
                SanitizeMessage(invOpEx.Message), 
                500, correlationId),
            
            NotImplementedException => CreateProblemDetails(
                "Not Implemented", 
                "This feature is not yet implemented", 
                501, correlationId),
            
            TimeoutException => CreateProblemDetails(
                "Request Timeout", 
                "The operation timed out", 
                408, correlationId),
            
            OperationCanceledException => CreateProblemDetails(
                "Operation Cancelled", 
                "The operation was cancelled", 
                499, correlationId),
            
            _ => CreateGenericErrorResponse(exception, correlationId)
        };
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(string title, string detail, 
        int statusCode, string correlationId)
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = correlationId,
            Extensions = new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = GetCurrentTime()
            }
        };
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateGenericErrorResponse(Exception exception, string correlationId)
    {
        var detail = _settings.EnableDetailedErrors 
            ? exception.ToString() 
            : "An error occurred while processing your request";

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Internal Server Error",
            Detail = SanitizeMessage(detail),
            Status = 500,
            Instance = correlationId,
            Extensions = new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = GetCurrentTime()
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

    private Microsoft.AspNetCore.Mvc.ValidationProblemDetails CreateValidationProblemDetails(
        FluentValidationException exception, string correlationId)
    {
        var errors = exception.Errors.Select(e => new ValidationError
        {
            Field = e.PropertyName,
            Message = e.ErrorMessage,
            Code = e.ErrorCode,
            AttemptedValue = e.AttemptedValue
        }).ToList();

        return new Microsoft.AspNetCore.Mvc.ValidationProblemDetails
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred",
            Status = 400,
            Instance = correlationId,
            Errors = errors.GroupBy(e => e.Field)
                          .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
            Extensions = new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = GetCurrentTime(),
                ["validationErrors"] = errors
            }
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