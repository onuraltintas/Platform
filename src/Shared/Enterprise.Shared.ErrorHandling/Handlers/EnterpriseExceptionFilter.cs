using Enterprise.Shared.ErrorHandling.Exceptions;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public class EnterpriseExceptionFilter : IExceptionFilter
{
    private readonly ILogger<EnterpriseExceptionFilter> _logger;
    private readonly IErrorResponseFactory _responseFactory;

    public EnterpriseExceptionFilter(
        ILogger<EnterpriseExceptionFilter> logger,
        IErrorResponseFactory responseFactory)
    {
        _logger = logger;
        _responseFactory = responseFactory;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is EnterpriseException enterpriseException)
        {
            var problemDetails = enterpriseException.ToProblemDetails();
            
            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = enterpriseException.HttpStatusCode
            };

            context.ExceptionHandled = true;

            var logLevel = GetLogLevel(enterpriseException);
            _logger.Log(logLevel, enterpriseException, 
                "Handled enterprise exception: {ErrorCode} - {Message}", 
                enterpriseException.ErrorCode, enterpriseException.Message);
        }
    }

    private LogLevel GetLogLevel(EnterpriseException exception)
    {
        return exception.Severity switch
        {
            Models.ErrorSeverity.Low => LogLevel.Information,
            Models.ErrorSeverity.Medium => LogLevel.Warning,
            Models.ErrorSeverity.High => LogLevel.Error,
            Models.ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Warning
        };
    }
}