namespace Enterprise.Shared.Resilience.Models;

public class ServiceUnavailableException : Exception
{
    public string ServiceName { get; }

    public ServiceUnavailableException(string serviceName) 
        : base($"Service {serviceName} is currently unavailable")
    {
        ServiceName = serviceName;
    }

    public ServiceUnavailableException(string serviceName, string message) 
        : base(message)
    {
        ServiceName = serviceName;
    }

    public ServiceUnavailableException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

public class BulkheadRejectedException : Exception
{
    public string BulkheadKey { get; }

    public BulkheadRejectedException(string bulkheadKey) 
        : base($"Operation rejected by bulkhead: {bulkheadKey}")
    {
        BulkheadKey = bulkheadKey;
    }

    public BulkheadRejectedException(string bulkheadKey, string message) 
        : base(message)
    {
        BulkheadKey = bulkheadKey;
    }

    public BulkheadRejectedException(string bulkheadKey, string message, Exception innerException) 
        : base(message, innerException)
    {
        BulkheadKey = bulkheadKey;
    }
}

public class RateLimitExceededException : Exception
{
    public string RateLimitKey { get; }
    public TimeSpan RetryAfter { get; }

    public RateLimitExceededException(string rateLimitKey, TimeSpan retryAfter) 
        : base($"Rate limit exceeded for {rateLimitKey}. Retry after {retryAfter}")
    {
        RateLimitKey = rateLimitKey;
        RetryAfter = retryAfter;
    }

    public RateLimitExceededException(string rateLimitKey, TimeSpan retryAfter, string message) 
        : base(message)
    {
        RateLimitKey = rateLimitKey;
        RetryAfter = retryAfter;
    }
}

public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public int? StatusCode { get; }

    public ExternalServiceException(string serviceName) 
        : base($"External service {serviceName} error")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, int statusCode) 
        : base($"External service {serviceName} returned status code {statusCode}")
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }

    public ExternalServiceException(string serviceName, string message) 
        : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

public class BusinessRuleException : Exception
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName) 
        : base($"Business rule violation: {ruleName}")
    {
        RuleName = ruleName;
    }

    public BusinessRuleException(string ruleName, string message) 
        : base(message)
    {
        RuleName = ruleName;
    }
}

public class ValidationException : Exception
{
    public string Field { get; }

    public ValidationException(string field) 
        : base($"Validation failed for field: {field}")
    {
        Field = field;
    }

    public ValidationException(string field, string message) 
        : base(message)
    {
        Field = field;
    }
}