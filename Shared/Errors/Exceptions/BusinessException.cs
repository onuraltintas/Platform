using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Shared.Errors.Exceptions;

public class BusinessException : BaseException
{
    public BusinessException(string errorCode, string message, int statusCode = 400) 
        : base(errorCode, message, statusCode)
    {
    }
    
    public BusinessException(string errorCode, string message, Exception innerException, int statusCode = 400) 
        : base(errorCode, message, innerException, statusCode)
    {
    }
    
    public static BusinessException NotFound(string resource, string identifier)
    {
        return new BusinessException(
            ErrorCodes.NOT_FOUND, 
            $"{resource} with identifier '{identifier}' was not found.",
            404);
    }
    
    public static BusinessException Conflict(string message)
    {
        return new BusinessException(ErrorCodes.CONFLICT, message, 409);
    }
    
    public static BusinessException Unauthorized(string message = "Unauthorized access.")
    {
        return new BusinessException(ErrorCodes.UNAUTHORIZED, message, 401);
    }
    
    public static BusinessException Forbidden(string message = "Access forbidden.")
    {
        return new BusinessException(ErrorCodes.FORBIDDEN, message, 403);
    }
    
    public static BusinessException BadRequest(string message)
    {
        return new BusinessException(ErrorCodes.BAD_REQUEST, message, 400);
    }
}