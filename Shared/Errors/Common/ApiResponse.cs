namespace EgitimPlatform.Shared.Errors.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ErrorResult? Error { get; set; }
    public List<ErrorResult>? Errors { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
    
    public static ApiResponse<T> Fail(ErrorResult error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
    
    public static ApiResponse<T> Fail(List<ErrorResult> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors
        };
    }
    
    public static ApiResponse<T> Fail(string code, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ErrorResult(code, message)
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }
    
    public new static ApiResponse Fail(ErrorResult error)
    {
        return new ApiResponse
        {
            Success = false,
            Error = error
        };
    }
    
    public new static ApiResponse Fail(List<ErrorResult> errors)
    {
        return new ApiResponse
        {
            Success = false,
            Errors = errors
        };
    }
    
    public new static ApiResponse Fail(string code, string message)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ErrorResult(code, message)
        };
    }
}