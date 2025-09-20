namespace Enterprise.Shared.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new[] { message };
    }

    public ValidationException(IEnumerable<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new[] { message };
    }
}

/// <summary>
/// Exception thrown when security operations fail
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message)
    {
    }

    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when business rules are violated
/// </summary>
public class BusinessException : Exception
{
    public string? ErrorCode { get; }

    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public BusinessException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public string? ResourceName { get; }
    public object? ResourceId { get; }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceName, object resourceId) 
        : base($"{resourceName} with id '{resourceId}' was not found")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}