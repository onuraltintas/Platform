using Enterprise.Shared.Common.Enums;

namespace Enterprise.Shared.Common.Models;

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the primary error message
    /// </summary>
    public string Error { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the list of all error messages
    /// </summary>
    public List<string> Errors { get; protected set; } = [];

    /// <summary>
    /// Gets the operation status
    /// </summary>
    public OperationStatus Status { get; protected set; } = OperationStatus.Success;

    /// <summary>
    /// Gets additional metadata about the result
    /// </summary>
    public Dictionary<string, object> Metadata { get; protected set; } = [];

    /// <summary>
    /// Gets the timestamp when the result was created
    /// </summary>
    public DateTime Timestamp { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the Result class
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="error">The primary error message</param>
    /// <param name="status">The operation status</param>
    protected Result(bool isSuccess, string error, OperationStatus status = OperationStatus.Success)
    {
        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
        Status = isSuccess ? OperationStatus.Success : status;
        
        if (!string.IsNullOrEmpty(error))
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    /// <summary>
    /// Creates a successful result with metadata
    /// </summary>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>A successful result with metadata</returns>
    public static Result Success(Dictionary<string, object> metadata)
    {
        var result = new Result(true, string.Empty);
        result.Metadata = metadata;
        return result;
    }

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    /// <param name="error">The error message</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string error, OperationStatus status = OperationStatus.Failed)
    {
        return new Result(false, error, status);
    }

    /// <summary>
    /// Creates a failed result with multiple error messages
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static Result Failure(IEnumerable<string> errors, OperationStatus status = OperationStatus.Failed)
    {
        var errorList = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
        var primaryError = errorList.FirstOrDefault() ?? "Operation failed";
        
        var result = new Result(false, primaryError, status);
        result.Errors = errorList;
        return result;
    }

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static Result Failure(Exception exception, OperationStatus status = OperationStatus.Failed)
    {
        var result = new Result(false, exception.Message, status);
        result.Metadata["ExceptionType"] = exception.GetType().Name;
        result.Metadata["StackTrace"] = exception.StackTrace ?? string.Empty;
        
        if (exception.InnerException != null)
        {
            result.Metadata["InnerException"] = exception.InnerException.Message;
        }
        
        return result;
    }

    /// <summary>
    /// Adds metadata to the result
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    /// <returns>The result instance for method chaining</returns>
    public Result WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata entries to the result
    /// </summary>
    /// <param name="metadata">The metadata dictionary</param>
    /// <returns>The result instance for method chaining</returns>
    public Result WithMetadata(Dictionary<string, object> metadata)
    {
        foreach (var kvp in metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Implicit conversion from string to failed Result
    /// </summary>
    /// <param name="error">The error message</param>
    public static implicit operator Result(string error) => Failure(error);

    /// <summary>
    /// Implicit conversion from Exception to failed Result
    /// </summary>
    /// <param name="exception">The exception</param>
    public static implicit operator Result(Exception exception) => Failure(exception);

    /// <summary>
    /// Combines multiple results into a single result
    /// </summary>
    /// <param name="results">The results to combine</param>
    /// <returns>A combined result</returns>
    public static Result Combine(params Result[] results)
    {
        var allErrors = new List<string>();
        var allMetadata = new Dictionary<string, object>();

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                allErrors.AddRange(result.Errors);
            }

            foreach (var metadata in result.Metadata)
            {
                allMetadata[metadata.Key] = metadata.Value;
            }
        }

        if (allErrors.Any())
        {
            var combinedResult = Failure(allErrors);
            combinedResult.Metadata = allMetadata;
            return combinedResult;
        }

        var successResult = Success();
        successResult.Metadata = allMetadata;
        return successResult;
    }

    /// <summary>
    /// Returns a string representation of the result
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return IsSuccess 
            ? "Success" 
            : $"Failure: {Error}";
    }
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
/// <typeparam name="T">The type of the return value</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value returned by the operation
    /// </summary>
    public T? Value { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the result has a value
    /// </summary>
    public bool HasValue => IsSuccess && Value != null;

    /// <summary>
    /// Initializes a new instance of the Result{T} class
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="value">The return value</param>
    /// <param name="error">The primary error message</param>
    /// <param name="status">The operation status</param>
    protected Result(bool isSuccess, T? value, string error, OperationStatus status = OperationStatus.Success) 
        : base(isSuccess, error, status)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <param name="value">The return value</param>
    /// <returns>A successful result with value</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    /// <summary>
    /// Creates a successful result with a value and metadata
    /// </summary>
    /// <param name="value">The return value</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>A successful result with value and metadata</returns>
    public static Result<T> Success(T value, Dictionary<string, object> metadata)
    {
        var result = new Result<T>(true, value, string.Empty);
        result.Metadata = metadata;
        return result;
    }

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    /// <param name="error">The error message</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static new Result<T> Failure(string error, OperationStatus status = OperationStatus.Failed)
    {
        return new Result<T>(false, default, error, status);
    }

    /// <summary>
    /// Creates a failed result with multiple error messages
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static new Result<T> Failure(IEnumerable<string> errors, OperationStatus status = OperationStatus.Failed)
    {
        var errorList = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
        var primaryError = errorList.FirstOrDefault() ?? "Operation failed";
        
        var result = new Result<T>(false, default, primaryError, status);
        result.Errors = errorList;
        return result;
    }

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="status">The operation status</param>
    /// <returns>A failed result</returns>
    public static new Result<T> Failure(Exception exception, OperationStatus status = OperationStatus.Failed)
    {
        var result = new Result<T>(false, default, exception.Message, status);
        result.Metadata["ExceptionType"] = exception.GetType().Name;
        result.Metadata["StackTrace"] = exception.StackTrace ?? string.Empty;
        
        if (exception.InnerException != null)
        {
            result.Metadata["InnerException"] = exception.InnerException.Message;
        }
        
        return result;
    }

    /// <summary>
    /// Maps the result value to another type
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A result with the mapped value</returns>
    public Result<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        if (IsFailure)
        {
            return Result<TOutput>.Failure(Errors, Status);
        }

        try
        {
            var mappedValue = mapper(Value!);
            var mappedResult = Result<TOutput>.Success(mappedValue);
            mappedResult.Metadata = Metadata;
            return mappedResult;
        }
        catch (Exception ex)
        {
            return Result<TOutput>.Failure(ex);
        }
    }

    /// <summary>
    /// Binds the result to another operation
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="binder">The binding function</param>
    /// <returns>The result of the bound operation</returns>
    public Result<TOutput> Bind<TOutput>(Func<T, Result<TOutput>> binder)
    {
        if (IsFailure)
        {
            return Result<TOutput>.Failure(Errors, Status);
        }

        try
        {
            var boundResult = binder(Value!);
            
            // Combine metadata
            foreach (var metadata in Metadata)
            {
                if (!boundResult.Metadata.ContainsKey(metadata.Key))
                {
                    boundResult.Metadata[metadata.Key] = metadata.Value;
                }
            }
            
            return boundResult;
        }
        catch (Exception ex)
        {
            return Result<TOutput>.Failure(ex);
        }
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result for method chaining</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value != null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result for method chaining</returns>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the default value
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The value or default value</returns>
    public T? GetValueOrDefault(T? defaultValue = default)
    {
        return IsSuccess ? Value : defaultValue;
    }

    /// <summary>
    /// Implicit conversion from T to successful Result{T}
    /// </summary>
    /// <param name="value">The value</param>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from string to failed Result{T}
    /// </summary>
    /// <param name="error">The error message</param>
    public static implicit operator Result<T>(string error) => Failure(error);

    /// <summary>
    /// Implicit conversion from Exception to failed Result{T}
    /// </summary>
    /// <param name="exception">The exception</param>
    public static implicit operator Result<T>(Exception exception) => Failure(exception);

    /// <summary>
    /// Returns a string representation of the result
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return IsSuccess 
            ? $"Success: {Value}" 
            : $"Failure: {Error}";
    }
}