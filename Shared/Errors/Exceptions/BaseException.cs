namespace EgitimPlatform.Shared.Errors.Exceptions;

public abstract class BaseException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public Dictionary<string, object>? Metadata { get; set; }
    
    protected BaseException(string errorCode, string message, int statusCode = 500) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
    
    protected BaseException(string errorCode, string message, Exception innerException, int statusCode = 500) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
    
    public BaseException WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
        return this;
    }
}