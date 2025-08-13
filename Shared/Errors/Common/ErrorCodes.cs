namespace EgitimPlatform.Shared.Errors.Common;

public static class ErrorCodes
{
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string NOT_FOUND = "NOT_FOUND";
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string CONFLICT = "CONFLICT";
    public const string INTERNAL_SERVER_ERROR = "INTERNAL_SERVER_ERROR";
    public const string BAD_REQUEST = "BAD_REQUEST";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string TIMEOUT = "TIMEOUT";
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    
    public static class Authentication
    {
        public const string INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
        public const string TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
        public const string INVALID_TOKEN = "AUTH_INVALID_TOKEN";
        public const string ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
        public const string EMAIL_NOT_CONFIRMED = "AUTH_EMAIL_NOT_CONFIRMED";
    }
    
    public static class User
    {
        public const string EMAIL_ALREADY_EXISTS = "USER_EMAIL_ALREADY_EXISTS";
        public const string USERNAME_ALREADY_EXISTS = "USER_USERNAME_ALREADY_EXISTS";
        public const string INVALID_PASSWORD = "USER_INVALID_PASSWORD";
        public const string PROFILE_NOT_FOUND = "USER_PROFILE_NOT_FOUND";
    }
    
    public static class Course
    {
        public const string COURSE_NOT_FOUND = "COURSE_NOT_FOUND";
        public const string COURSE_ACCESS_DENIED = "COURSE_ACCESS_DENIED";
        public const string COURSE_ENROLLMENT_FULL = "COURSE_ENROLLMENT_FULL";
        public const string ALREADY_ENROLLED = "COURSE_ALREADY_ENROLLED";
    }
    
    public static class System
    {
        public const string DATABASE_CONNECTION_FAILED = "SYS_DATABASE_CONNECTION_FAILED";
        public const string EXTERNAL_SERVICE_UNAVAILABLE = "SYS_EXTERNAL_SERVICE_UNAVAILABLE";
        public const string CONFIGURATION_ERROR = "SYS_CONFIGURATION_ERROR";
    }
}