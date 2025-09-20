namespace Enterprise.Shared.Common.Constants;

/// <summary>
/// Common application constants
/// </summary>
public static class CommonConstants
{
    #region Application Constants

    /// <summary>
    /// Default application name
    /// </summary>
    public const string ApplicationName = "Enterprise Platform";

    /// <summary>
    /// Default API version
    /// </summary>
    public const string ApiVersion = "v1";

    /// <summary>
    /// Default page size for pagination
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Maximum page size for pagination
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Default timeout in seconds
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    #endregion

    #region HTTP Constants

    /// <summary>
    /// Content type for JSON
    /// </summary>
    public const string JsonContentType = "application/json";

    /// <summary>
    /// Content type for XML
    /// </summary>
    public const string XmlContentType = "application/xml";

    /// <summary>
    /// Content type for form data
    /// </summary>
    public const string FormContentType = "application/x-www-form-urlencoded";

    /// <summary>
    /// Content type for multipart form data
    /// </summary>
    public const string MultipartFormContentType = "multipart/form-data";

    #endregion

    #region Header Names

    /// <summary>
    /// Correlation ID header name
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Request ID header name
    /// </summary>
    public const string RequestIdHeader = "X-Request-ID";

    /// <summary>
    /// User ID header name
    /// </summary>
    public const string UserIdHeader = "X-User-ID";

    /// <summary>
    /// Tenant ID header name
    /// </summary>
    public const string TenantIdHeader = "X-Tenant-ID";

    /// <summary>
    /// API version header name
    /// </summary>
    public const string ApiVersionHeader = "X-API-Version";

    /// <summary>
    /// Authorization header name
    /// </summary>
    public const string AuthorizationHeader = "Authorization";

    /// <summary>
    /// Bearer token prefix
    /// </summary>
    public const string BearerTokenPrefix = "Bearer ";

    #endregion

    #region Cache Constants

    /// <summary>
    /// Default cache expiration time in minutes
    /// </summary>
    public const int DefaultCacheExpirationMinutes = 60;

    /// <summary>
    /// Short cache expiration time in minutes
    /// </summary>
    public const int ShortCacheExpirationMinutes = 5;

    /// <summary>
    /// Long cache expiration time in minutes
    /// </summary>
    public const int LongCacheExpirationMinutes = 24 * 60; // 24 hours

    /// <summary>
    /// Cache key separator
    /// </summary>
    public const string CacheKeySeparator = ":";

    #endregion

    #region Validation Constants

    /// <summary>
    /// Minimum password length
    /// </summary>
    public const int MinPasswordLength = 8;

    /// <summary>
    /// Maximum password length
    /// </summary>
    public const int MaxPasswordLength = 128;

    /// <summary>
    /// Maximum string length for names
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Maximum string length for descriptions
    /// </summary>
    public const int MaxDescriptionLength = 1000;

    /// <summary>
    /// Maximum string length for emails
    /// </summary>
    public const int MaxEmailLength = 254;

    /// <summary>
    /// Maximum string length for URLs
    /// </summary>
    public const int MaxUrlLength = 2048;

    #endregion

    #region Date/Time Constants

    /// <summary>
    /// ISO 8601 date format
    /// </summary>
    public const string Iso8601DateFormat = "yyyy-MM-ddTHH:mm:ss.fffK";

    /// <summary>
    /// Short date format
    /// </summary>
    public const string ShortDateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Time format
    /// </summary>
    public const string TimeFormat = "HH:mm:ss";

    /// <summary>
    /// File name date format
    /// </summary>
    public const string FileNameDateFormat = "yyyy-MM-dd_HH-mm-ss";

    #endregion

    #region TimeZone Constants

    /// <summary>
    /// Turkey Standard Time zone identifier (cross-platform)
    /// </summary>
    public const string TurkeyTimeZoneId = "Europe/Istanbul";

    /// <summary>
    /// Default application timezone
    /// </summary>
    public const string DefaultTimeZoneId = TurkeyTimeZoneId;

    #endregion

    #region File Constants

    /// <summary>
    /// Maximum file size in bytes (10MB)
    /// </summary>
    public const long MaxFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum image file size in bytes (5MB)
    /// </summary>
    public const long MaxImageFileSize = 5 * 1024 * 1024;

    /// <summary>
    /// Allowed image file extensions
    /// </summary>
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    /// <summary>
    /// Allowed document file extensions
    /// </summary>
    public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };

    #endregion

    #region Regular Expressions

    /// <summary>
    /// Email validation regex pattern
    /// </summary>
    public const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    /// <summary>
    /// Phone number validation regex pattern (international format)
    /// </summary>
    public const string PhoneRegexPattern = @"^\+?[1-9]\d{6,14}$";

    /// <summary>
    /// Strong password regex pattern
    /// </summary>
    public const string StrongPasswordRegexPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    /// <summary>
    /// URL validation regex pattern
    /// </summary>
    public const string UrlRegexPattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$";

    /// <summary>
    /// GUID validation regex pattern
    /// </summary>
    public const string GuidRegexPattern = @"^[{(]?[0-9A-Fa-f]{8}[-]?([0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?$";

    #endregion

    #region Error Messages

    /// <summary>
    /// Generic error message for validation failures
    /// </summary>
    public const string ValidationErrorMessage = "One or more validation errors occurred.";

    /// <summary>
    /// Generic error message for not found
    /// </summary>
    public const string NotFoundErrorMessage = "The requested resource was not found.";

    /// <summary>
    /// Generic error message for unauthorized access
    /// </summary>
    public const string UnauthorizedErrorMessage = "You are not authorized to access this resource.";

    /// <summary>
    /// Generic error message for forbidden access
    /// </summary>
    public const string ForbiddenErrorMessage = "You do not have permission to access this resource.";

    /// <summary>
    /// Generic error message for internal server error
    /// </summary>
    public const string InternalServerErrorMessage = "An internal server error occurred. Please try again later.";

    /// <summary>
    /// Generic error message for bad request
    /// </summary>
    public const string BadRequestErrorMessage = "The request is invalid.";

    /// <summary>
    /// Generic error message for conflict
    /// </summary>
    public const string ConflictErrorMessage = "The request conflicts with the current state of the resource.";

    #endregion

    #region Configuration Keys

    /// <summary>
    /// Database connection string key
    /// </summary>
    public const string DatabaseConnectionStringKey = "ConnectionStrings:DefaultConnection";

    /// <summary>
    /// Redis connection string key
    /// </summary>
    public const string RedisConnectionStringKey = "ConnectionStrings:Redis";

    /// <summary>
    /// JWT secret key configuration key
    /// </summary>
    public const string JwtSecretKey = "Jwt:Secret";

    /// <summary>
    /// JWT issuer configuration key
    /// </summary>
    public const string JwtIssuerKey = "Jwt:Issuer";

    /// <summary>
    /// JWT audience configuration key
    /// </summary>
    public const string JwtAudienceKey = "Jwt:Audience";

    /// <summary>
    /// Environment configuration key
    /// </summary>
    public const string EnvironmentKey = "ASPNETCORE_ENVIRONMENT";

    #endregion

    #region Environment Names

    /// <summary>
    /// Development environment name
    /// </summary>
    public const string DevelopmentEnvironment = "Development";

    /// <summary>
    /// Staging environment name
    /// </summary>
    public const string StagingEnvironment = "Staging";

    /// <summary>
    /// Production environment name
    /// </summary>
    public const string ProductionEnvironment = "Production";

    /// <summary>
    /// Testing environment name
    /// </summary>
    public const string TestingEnvironment = "Testing";

    #endregion

    #region Claim Types

    /// <summary>
    /// User ID claim type
    /// </summary>
    public const string UserIdClaimType = "user_id";

    /// <summary>
    /// Username claim type
    /// </summary>
    public const string UsernameClaimType = "username";

    /// <summary>
    /// Email claim type
    /// </summary>
    public const string EmailClaimType = "email";

    /// <summary>
    /// Role claim type
    /// </summary>
    public const string RoleClaimType = "role";

    /// <summary>
    /// Tenant ID claim type
    /// </summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>
    /// Permission claim type
    /// </summary>
    public const string PermissionClaimType = "permission";

    #endregion

    #region Roles

    /// <summary>
    /// Administrator role name
    /// </summary>
    public const string AdministratorRole = "Administrator";

    /// <summary>
    /// Manager role name
    /// </summary>
    public const string ManagerRole = "Manager";

    /// <summary>
    /// User role name
    /// </summary>
    public const string UserRole = "User";

    /// <summary>
    /// Guest role name
    /// </summary>
    public const string GuestRole = "Guest";

    #endregion
}