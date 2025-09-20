namespace Enterprise.Shared.Validation.Models;

/// <summary>
/// Represents a result of an operation that can succeed or fail
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = new();

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    
    public static Result Failure(string error) => new(false, error);
    
    public static Result Failure(IEnumerable<string> errors) => new(false, string.Join("; ", errors))
    {
        Errors = errors.ToList()
    };

    public static implicit operator Result(string error) => Failure(error);
}

/// <summary>
/// Represents a result of an operation that can succeed or fail with a value
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
public class Result<T> : Result
{
    public T Value { get; private set; }

    protected Result(bool isSuccess, T value, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    
    public static new Result<T> Failure(string error) => new(false, default!, error);
    
    public static new Result<T> Failure(IEnumerable<string> errors) => 
        new(false, default!, string.Join("; ", errors)) { Errors = errors.ToList() };

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);
}