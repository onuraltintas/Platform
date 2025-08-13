namespace EgitimPlatform.Shared.Errors.Models;

public static class InternalServerError
{
    public static string DatabaseError = "An internal database error occurred";
    public static string GeneralError = "An internal server error occurred";
    public static string ServiceUnavailable = "Service is temporarily unavailable";
}

public static class NotFoundError
{
    public static string UserNotFound = "User not found";
    public static string RoleNotFound = "Role not found";
    public static string ResourceNotFound = "Resource not found";
    public static string PermissionNotFound = "Permission not found";
    public static string TokenNotFound = "Token not found";
}

public static class ConflictError
{
    public static string UserAlreadyExists = "User already exists";
    public static string RoleAlreadyExists = "Role already exists";
    public static string PermissionAlreadyExists = "Permission already exists";
    public static string DuplicateEntry = "Duplicate entry detected";
}

public static class ValidationError
{
    public static string InvalidInput = "Invalid input provided";
    public static string RequiredField = "Required field is missing";
    public static string InvalidFormat = "Invalid format";
    public static string InvalidLength = "Invalid length";
}

public static class UnauthorizedError
{
    public static string InvalidCredentials = "Invalid credentials";
    public static string TokenExpired = "Token has expired";
    public static string AccessDenied = "Access denied";
    public static string InsufficientPermissions = "Insufficient permissions";
}

public static class BadRequestError
{
    public static string InvalidRequest = "Invalid request";
    public static string MissingParameter = "Missing required parameter";
    public static string InvalidParameter = "Invalid parameter value";
}