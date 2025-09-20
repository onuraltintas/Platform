namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Categories of audit events
/// </summary>
public enum AuditEventCategory
{
    /// <summary>
    /// General application events
    /// </summary>
    Application = 0,

    /// <summary>
    /// Security-related events
    /// </summary>
    Security = 1,

    /// <summary>
    /// Data access and modification events
    /// </summary>
    DataAccess = 2,

    /// <summary>
    /// Administrative actions
    /// </summary>
    Administration = 3,

    /// <summary>
    /// System events
    /// </summary>
    System = 4,

    /// <summary>
    /// User activity events
    /// </summary>
    UserActivity = 5,

    /// <summary>
    /// Business process events
    /// </summary>
    BusinessProcess = 6,

    /// <summary>
    /// Integration and external system events
    /// </summary>
    Integration = 7,

    /// <summary>
    /// Performance-related events
    /// </summary>
    Performance = 8,

    /// <summary>
    /// Compliance and regulatory events
    /// </summary>
    Compliance = 9
}

/// <summary>
/// Severity levels for audit events
/// </summary>
public enum AuditSeverity
{
    /// <summary>
    /// Verbose information
    /// </summary>
    Verbose = 0,

    /// <summary>
    /// Debug information
    /// </summary>
    Debug = 1,

    /// <summary>
    /// General information
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning conditions
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error conditions
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical conditions
    /// </summary>
    Critical = 5,

    /// <summary>
    /// Fatal conditions
    /// </summary>
    Fatal = 6
}

/// <summary>
/// Types of security events
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// User authentication events
    /// </summary>
    Authentication = 0,

    /// <summary>
    /// Authorization and access control events
    /// </summary>
    Authorization = 1,

    /// <summary>
    /// Account management events
    /// </summary>
    AccountManagement = 2,

    /// <summary>
    /// Session management events
    /// </summary>
    SessionManagement = 3,

    /// <summary>
    /// Password-related events
    /// </summary>
    PasswordManagement = 4,

    /// <summary>
    /// Multi-factor authentication events
    /// </summary>
    MultiFactorAuthentication = 5,

    /// <summary>
    /// Data access security events
    /// </summary>
    DataAccess = 6,

    /// <summary>
    /// System security events
    /// </summary>
    SystemSecurity = 7,

    /// <summary>
    /// Privilege escalation events
    /// </summary>
    PrivilegeEscalation = 8,

    /// <summary>
    /// Suspicious activity events
    /// </summary>
    SuspiciousActivity = 9,

    /// <summary>
    /// Security policy violations
    /// </summary>
    PolicyViolation = 10,

    /// <summary>
    /// Cryptographic operations
    /// </summary>
    Cryptographic = 11,

    /// <summary>
    /// External authentication (OAuth, SAML, etc.)
    /// </summary>
    ExternalAuthentication = 12,

    /// <summary>
    /// API security events
    /// </summary>
    ApiSecurity = 13,

    /// <summary>
    /// Compliance-related security events
    /// </summary>
    Compliance = 14
}

/// <summary>
/// Security event outcomes
/// </summary>
public enum SecurityOutcome
{
    /// <summary>
    /// Operation succeeded
    /// </summary>
    Success = 0,

    /// <summary>
    /// Operation failed
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Operation was blocked/denied
    /// </summary>
    Blocked = 2,

    /// <summary>
    /// Operation timed out
    /// </summary>
    Timeout = 3,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Operation resulted in an error
    /// </summary>
    Error = 5,

    /// <summary>
    /// Operation is pending
    /// </summary>
    Pending = 6,

    /// <summary>
    /// Operation requires additional verification
    /// </summary>
    RequiresVerification = 7
}

/// <summary>
/// Common security actions for auditing
/// </summary>
public static class SecurityActions
{
    // Authentication
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountUnlocked = "ACCOUNT_UNLOCKED";

    // Authorization
    public const string AccessGranted = "ACCESS_GRANTED";
    public const string AccessDenied = "ACCESS_DENIED";
    public const string PermissionChanged = "PERMISSION_CHANGED";
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRemoved = "ROLE_REMOVED";

    // Account Management
    public const string AccountCreated = "ACCOUNT_CREATED";
    public const string AccountUpdated = "ACCOUNT_UPDATED";
    public const string AccountDeleted = "ACCOUNT_DELETED";
    public const string AccountEnabled = "ACCOUNT_ENABLED";
    public const string AccountDisabled = "ACCOUNT_DISABLED";

    // Password Management
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string PasswordExpired = "PASSWORD_EXPIRED";

    // MFA
    public const string MfaEnabled = "MFA_ENABLED";
    public const string MfaDisabled = "MFA_DISABLED";
    public const string MfaChallenge = "MFA_CHALLENGE";
    public const string MfaSuccess = "MFA_SUCCESS";
    public const string MfaFailed = "MFA_FAILED";

    // Sessions
    public const string SessionCreated = "SESSION_CREATED";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string SessionTerminated = "SESSION_TERMINATED";
    public const string SessionRefreshed = "SESSION_REFRESHED";

    // Data Access
    public const string DataAccessed = "DATA_ACCESSED";
    public const string DataCreated = "DATA_CREATED";
    public const string DataUpdated = "DATA_UPDATED";
    public const string DataDeleted = "DATA_DELETED";
    public const string DataExported = "DATA_EXPORTED";

    // Suspicious Activity
    public const string SuspiciousLogin = "SUSPICIOUS_LOGIN";
    public const string BruteForceDetected = "BRUTE_FORCE_DETECTED";
    public const string UnusualActivity = "UNUSUAL_ACTIVITY";
    public const string SecurityViolation = "SECURITY_VIOLATION";
}

/// <summary>
/// Common audit resources
/// </summary>
public static class AuditResources
{
    public const string User = "User";
    public const string Session = "Session";
    public const string Role = "Role";
    public const string Permission = "Permission";
    public const string Token = "Token";
    public const string Api = "API";
    public const string System = "System";
    public const string Database = "Database";
    public const string File = "File";
    public const string Configuration = "Configuration";
}