using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Shared.Errors.Exceptions;

public class SystemException : BaseException
{
    public SystemException(string errorCode, string message, int statusCode = 500) 
        : base(errorCode, message, statusCode)
    {
    }
    
    public SystemException(string errorCode, string message, Exception innerException, int statusCode = 500) 
        : base(errorCode, message, innerException, statusCode)
    {
    }
    
    public static SystemException DatabaseConnectionFailed(Exception innerException)
    {
        return new SystemException(
            ErrorCodes.System.DATABASE_CONNECTION_FAILED,
            "Database connection failed.",
            innerException,
            503);
    }
    
    public static SystemException ExternalServiceUnavailable(string serviceName, Exception? innerException = null)
    {
        return new SystemException(
            ErrorCodes.System.EXTERNAL_SERVICE_UNAVAILABLE,
            $"External service '{serviceName}' is unavailable.",
            innerException!,
            503);
    }
    
    public static SystemException ConfigurationError(string message)
    {
        return new SystemException(
            ErrorCodes.System.CONFIGURATION_ERROR,
            $"Configuration error: {message}",
            500);
    }
    
    public static SystemException Timeout(string operation)
    {
        return new SystemException(
            ErrorCodes.TIMEOUT,
            $"Operation '{operation}' timed out.",
            408);
    }
}