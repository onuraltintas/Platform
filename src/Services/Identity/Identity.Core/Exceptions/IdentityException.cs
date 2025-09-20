namespace Identity.Core.Exceptions;

public abstract class IdentityException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    protected IdentityException(string message, string errorCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    protected IdentityException(string message, string errorCode, Exception innerException, object? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

public class PermissionNotFoundException : IdentityException
{
    public PermissionNotFoundException(string permissionName)
        : base($"Permission '{permissionName}' not found", "PERMISSION_NOT_FOUND", new { PermissionName = permissionName })
    {
    }

    public PermissionNotFoundException(Guid permissionId)
        : base($"Permission with ID '{permissionId}' not found", "PERMISSION_NOT_FOUND", new { PermissionId = permissionId })
    {
    }
}

public class RoleNotFoundException : IdentityException
{
    public RoleNotFoundException(string roleName)
        : base($"Role '{roleName}' not found", "ROLE_NOT_FOUND", new { RoleName = roleName })
    {
    }

    public RoleNotFoundException(string roleId, bool isId)
        : base($"Role with ID '{roleId}' not found", "ROLE_NOT_FOUND", new { RoleId = roleId })
    {
    }
}

public class UserNotFoundException : IdentityException
{
    public UserNotFoundException(string userIdentifier)
        : base($"User '{userIdentifier}' not found", "USER_NOT_FOUND", new { UserIdentifier = userIdentifier })
    {
    }
}

public class GroupNotFoundException : IdentityException
{
    public GroupNotFoundException(string groupName)
        : base($"Group '{groupName}' not found", "GROUP_NOT_FOUND", new { GroupName = groupName })
    {
    }

    public GroupNotFoundException(Guid groupId)
        : base($"Group with ID '{groupId}' not found", "GROUP_NOT_FOUND", new { GroupId = groupId })
    {
    }
}

public class ServiceNotFoundException : IdentityException
{
    public ServiceNotFoundException(string serviceName)
        : base($"Service '{serviceName}' not found", "SERVICE_NOT_FOUND", new { ServiceName = serviceName })
    {
    }

    public ServiceNotFoundException(Guid serviceId)
        : base($"Service with ID '{serviceId}' not found", "SERVICE_NOT_FOUND", new { ServiceId = serviceId })
    {
    }
}

public class DuplicateResourceException : IdentityException
{
    public DuplicateResourceException(string resourceType, string resourceName)
        : base($"{resourceType} '{resourceName}' already exists", "DUPLICATE_RESOURCE",
            new { ResourceType = resourceType, ResourceName = resourceName })
    {
    }
}

public class InsufficientPermissionsException : IdentityException
{
    public InsufficientPermissionsException(string requiredPermission)
        : base($"Insufficient permissions. Required: {requiredPermission}", "INSUFFICIENT_PERMISSIONS",
            new { RequiredPermission = requiredPermission })
    {
    }

    public InsufficientPermissionsException(string[] requiredPermissions)
        : base($"Insufficient permissions. Required one of: {string.Join(", ", requiredPermissions)}",
            "INSUFFICIENT_PERMISSIONS", new { RequiredPermissions = requiredPermissions })
    {
    }
}

public class InvalidOperationException : IdentityException
{
    public InvalidOperationException(string operation, string reason)
        : base($"Invalid operation '{operation}': {reason}", "INVALID_OPERATION",
            new { Operation = operation, Reason = reason })
    {
    }
}

public class ValidationException : IdentityException
{
    public ValidationException(string fieldName, string validationError)
        : base($"Validation failed for '{fieldName}': {validationError}", "VALIDATION_ERROR",
            new { FieldName = fieldName, ValidationError = validationError })
    {
    }

    public ValidationException(Dictionary<string, string[]> validationErrors)
        : base("Multiple validation errors occurred", "VALIDATION_ERROR", new { ValidationErrors = validationErrors })
    {
    }
}

public class ExternalServiceException : IdentityException
{
    public ExternalServiceException(string serviceName, string error)
        : base($"External service '{serviceName}' error: {error}", "EXTERNAL_SERVICE_ERROR",
            new { ServiceName = serviceName, Error = error })
    {
    }

    public ExternalServiceException(string serviceName, Exception innerException)
        : base($"External service '{serviceName}' error", "EXTERNAL_SERVICE_ERROR", innerException,
            new { ServiceName = serviceName })
    {
    }
}