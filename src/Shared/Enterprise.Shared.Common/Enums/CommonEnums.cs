namespace Enterprise.Shared.Common.Enums;

/// <summary>
/// Sort direction enumeration
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending sort order
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Descending sort order
    /// </summary>
    Descending = 1
}

/// <summary>
/// Entity status enumeration
/// </summary>
public enum EntityStatus
{
    /// <summary>
    /// Entity is active
    /// </summary>
    Active = 0,

    /// <summary>
    /// Entity is inactive
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Entity is pending approval
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Entity is archived
    /// </summary>
    Archived = 3,

    /// <summary>
    /// Entity is deleted (soft delete)
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// Entity is suspended
    /// </summary>
    Suspended = 5
}

/// <summary>
/// Operation result status enumeration
/// </summary>
public enum OperationStatus
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
    /// Operation is pending
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Operation timed out
    /// </summary>
    Timeout = 4,

    /// <summary>
    /// Operation is in progress
    /// </summary>
    InProgress = 5,

    /// <summary>
    /// Validation failed
    /// </summary>
    ValidationFailed = 6,

    /// <summary>
    /// Unauthorized access
    /// </summary>
    Unauthorized = 7,

    /// <summary>
    /// Resource not found
    /// </summary>
    NotFound = 8,

    /// <summary>
    /// Rate limit exceeded
    /// </summary>
    RateLimitExceeded = 9
}

/// <summary>
/// Log level enumeration (matching Microsoft.Extensions.Logging)
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Logs that are used for interactive investigation during development
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Logs that track the general flow of the application
    /// </summary>
    Information = 2,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure
    /// </summary>
    Error = 4,

    /// <summary>
    /// Logs that describe an unrecoverable application or system crash
    /// </summary>
    Critical = 5,

    /// <summary>
    /// Not used for writing log messages. Specifies that a logging category should not write any messages
    /// </summary>
    None = 6
}

/// <summary>
/// Priority levels enumeration
/// </summary>
public enum Priority
{
    /// <summary>
    /// Lowest priority
    /// </summary>
    Lowest = 0,

    /// <summary>
    /// Low priority
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority
    /// </summary>
    High = 3,

    /// <summary>
    /// Highest priority
    /// </summary>
    Highest = 4,

    /// <summary>
    /// Critical priority
    /// </summary>
    Critical = 5
}

/// <summary>
/// File types enumeration
/// </summary>
public enum FileType
{
    /// <summary>
    /// Unknown file type
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Image file
    /// </summary>
    Image = 1,

    /// <summary>
    /// Document file
    /// </summary>
    Document = 2,

    /// <summary>
    /// Video file
    /// </summary>
    Video = 3,

    /// <summary>
    /// Audio file
    /// </summary>
    Audio = 4,

    /// <summary>
    /// Archive file
    /// </summary>
    Archive = 5,

    /// <summary>
    /// Text file
    /// </summary>
    Text = 6,

    /// <summary>
    /// Executable file
    /// </summary>
    Executable = 7
}

/// <summary>
/// Gender enumeration
/// </summary>
public enum Gender
{
    /// <summary>
    /// Not specified
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    /// Male
    /// </summary>
    Male = 1,

    /// <summary>
    /// Female
    /// </summary>
    Female = 2,

    /// <summary>
    /// Other
    /// </summary>
    Other = 3,

    /// <summary>
    /// Prefer not to say
    /// </summary>
    PreferNotToSay = 4
}

/// <summary>
/// Contact method enumeration
/// </summary>
public enum ContactMethod
{
    /// <summary>
    /// Email contact
    /// </summary>
    Email = 0,

    /// <summary>
    /// Phone contact
    /// </summary>
    Phone = 1,

    /// <summary>
    /// SMS contact
    /// </summary>
    Sms = 2,

    /// <summary>
    /// Push notification
    /// </summary>
    PushNotification = 3,

    /// <summary>
    /// Mail contact
    /// </summary>
    Mail = 4,

    /// <summary>
    /// In-app notification
    /// </summary>
    InApp = 5
}

/// <summary>
/// Language codes enumeration (ISO 639-1)
/// </summary>
public enum LanguageCode
{
    /// <summary>
    /// English
    /// </summary>
    En = 0,

    /// <summary>
    /// Turkish
    /// </summary>
    Tr = 1,

    /// <summary>
    /// German
    /// </summary>
    De = 2,

    /// <summary>
    /// French
    /// </summary>
    Fr = 3,

    /// <summary>
    /// Spanish
    /// </summary>
    Es = 4,

    /// <summary>
    /// Italian
    /// </summary>
    It = 5,

    /// <summary>
    /// Portuguese
    /// </summary>
    Pt = 6,

    /// <summary>
    /// Russian
    /// </summary>
    Ru = 7,

    /// <summary>
    /// Chinese
    /// </summary>
    Zh = 8,

    /// <summary>
    /// Japanese
    /// </summary>
    Ja = 9,

    /// <summary>
    /// Korean
    /// </summary>
    Ko = 10,

    /// <summary>
    /// Arabic
    /// </summary>
    Ar = 11
}

/// <summary>
/// Time zone enumeration
/// </summary>
public enum TimeZone
{
    /// <summary>
    /// Coordinated Universal Time
    /// </summary>
    Utc = 0,

    /// <summary>
    /// Turkey Time
    /// </summary>
    TurkeyTime = 1,

    /// <summary>
    /// Central European Time
    /// </summary>
    CentralEuropeanTime = 2,

    /// <summary>
    /// Eastern Standard Time
    /// </summary>
    EasternStandardTime = 3,

    /// <summary>
    /// Pacific Standard Time
    /// </summary>
    PacificStandardTime = 4,

    /// <summary>
    /// Greenwich Mean Time
    /// </summary>
    GreenwichMeanTime = 5
}

/// <summary>
/// Data format enumeration
/// </summary>
public enum DataFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 1,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv = 2,

    /// <summary>
    /// Excel format
    /// </summary>
    Excel = 3,

    /// <summary>
    /// PDF format
    /// </summary>
    Pdf = 4,

    /// <summary>
    /// Plain text format
    /// </summary>
    Text = 5
}

/// <summary>
/// Comparison operator enumeration
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// Equal to
    /// </summary>
    Equal = 0,

    /// <summary>
    /// Not equal to
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// Greater than
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// Greater than or equal to
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// Less than
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// Less than or equal to
    /// </summary>
    LessThanOrEqual = 5,

    /// <summary>
    /// Contains
    /// </summary>
    Contains = 6,

    /// <summary>
    /// Starts with
    /// </summary>
    StartsWith = 7,

    /// <summary>
    /// Ends with
    /// </summary>
    EndsWith = 8,

    /// <summary>
    /// In list
    /// </summary>
    In = 9,

    /// <summary>
    /// Not in list
    /// </summary>
    NotIn = 10
}

/// <summary>
/// HTTP methods enumeration
/// </summary>
public enum HttpMethod
{
    /// <summary>
    /// GET method
    /// </summary>
    Get = 0,

    /// <summary>
    /// POST method
    /// </summary>
    Post = 1,

    /// <summary>
    /// PUT method
    /// </summary>
    Put = 2,

    /// <summary>
    /// DELETE method
    /// </summary>
    Delete = 3,

    /// <summary>
    /// PATCH method
    /// </summary>
    Patch = 4,

    /// <summary>
    /// HEAD method
    /// </summary>
    Head = 5,

    /// <summary>
    /// OPTIONS method
    /// </summary>
    Options = 6
}

/// <summary>
/// Cache invalidation strategies enumeration
/// </summary>
public enum CacheInvalidationStrategy
{
    /// <summary>
    /// No invalidation
    /// </summary>
    None = 0,

    /// <summary>
    /// Time-based expiration
    /// </summary>
    TimeToLive = 1,

    /// <summary>
    /// Least recently used
    /// </summary>
    LeastRecentlyUsed = 2,

    /// <summary>
    /// Least frequently used
    /// </summary>
    LeastFrequentlyUsed = 3,

    /// <summary>
    /// First in, first out
    /// </summary>
    FirstInFirstOut = 4,

    /// <summary>
    /// Manual invalidation
    /// </summary>
    Manual = 5
}

/// <summary>
/// Serialization format enumeration
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// JSON serialization
    /// </summary>
    Json = 0,

    /// <summary>
    /// Binary serialization
    /// </summary>
    Binary = 1,

    /// <summary>
    /// XML serialization
    /// </summary>
    Xml = 2,

    /// <summary>
    /// MessagePack serialization
    /// </summary>
    MessagePack = 3,

    /// <summary>
    /// Protocol Buffers serialization
    /// </summary>
    ProtocolBuffers = 4
}

/// <summary>
/// Configuration provider types enumeration
/// </summary>
public enum ConfigurationProviderType
{
    /// <summary>
    /// File-based configuration (appsettings.json)
    /// </summary>
    File = 0,

    /// <summary>
    /// Environment variables configuration
    /// </summary>
    EnvironmentVariables = 1,

    /// <summary>
    /// Azure Key Vault configuration
    /// </summary>
    AzureKeyVault = 2,

    /// <summary>
    /// Database configuration
    /// </summary>
    Database = 3,

    /// <summary>
    /// Consul configuration
    /// </summary>
    Consul = 4,

    /// <summary>
    /// Azure App Configuration
    /// </summary>
    AzureAppConfiguration = 5,

    /// <summary>
    /// In-memory configuration
    /// </summary>
    InMemory = 6
}

/// <summary>
/// Validation mode enumeration
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// No validation
    /// </summary>
    None = 0,

    /// <summary>
    /// Lenient validation (warnings only)
    /// </summary>
    Lenient = 1,

    /// <summary>
    /// Strict validation (errors for invalid values)
    /// </summary>
    Strict = 2
}