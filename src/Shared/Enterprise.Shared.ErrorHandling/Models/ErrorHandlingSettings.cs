namespace Enterprise.Shared.ErrorHandling.Models;

public class ErrorHandlingSettings
{
    public const string SectionName = "ErrorHandlingSettings";

    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableDeveloperExceptionPage { get; set; } = false;
    public bool EnableProblemDetails { get; set; } = true;
    public bool EnableCorrelationId { get; set; } = true;
    public bool EnableLocalization { get; set; } = true;
    public string DefaultLanguage { get; set; } = "tr-TR";
    public string DefaultCulture { get; set; } = "tr-TR";
    public string TimeZoneId { get; set; } = "Turkey Standard Time";
    public int MaxErrorStackTraceLength { get; set; } = 5000;
    public List<string> SensitiveDataPatterns { get; set; } = new()
    {
        "password", "token", "secret", "key"
    };

    public RetryPolicySettings RetryPolicy { get; set; } = new();
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
    public Dictionary<string, string> ErrorCodes { get; set; } = new()
    {
        ["ValidationFailed"] = "ERR_VALIDATION_001",
        ["ResourceNotFound"] = "ERR_NOTFOUND_001",
        ["Unauthorized"] = "ERR_AUTH_001",
        ["Forbidden"] = "ERR_AUTH_002",
        ["Conflict"] = "ERR_CONFLICT_001",
        ["BusinessRule"] = "ERR_BUSINESS_001",
        ["ExternalService"] = "ERR_EXTERNAL_001",
        ["Database"] = "ERR_DATABASE_001"
    };
}

public class RetryPolicySettings
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 30000;
    public double BackoffMultiplier { get; set; } = 2;
}

public class CircuitBreakerSettings
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int MinimumThroughput { get; set; } = 10;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}