namespace EgitimPlatform.Shared.Errors.Types;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    InternalServerError = 6,
    BadRequest = 7,
    ServiceUnavailable = 8,
    Timeout = 9,
    RateLimitExceeded = 10
}

public enum BusinessErrorType
{
    None = 0,
    DuplicateEntry = 1,
    InvalidOperation = 2,
    ResourceNotFound = 3,
    BusinessRuleViolation = 4,
    AccessDenied = 5,
    QuotaExceeded = 6,
    InvalidState = 7,
    DependencyFailure = 8,
    ConfigurationError = 9,
    ExternalServiceError = 10
}