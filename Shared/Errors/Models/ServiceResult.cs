namespace EgitimPlatform.Shared.Errors.Models;

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage, string? errorCode = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    public static ServiceResult<T> Failure(List<string> errors, string? errorCode = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = errors,
            ErrorCode = errorCode
        };
    }
}

public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Success()
    {
        return new ServiceResult
        {
            IsSuccess = true
        };
    }

    public static ServiceResult Failure(string errorMessage, string? errorCode = null)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    public static ServiceResult Failure(List<string> errors, string? errorCode = null)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Errors = errors,
            ErrorCode = errorCode
        };
    }
}