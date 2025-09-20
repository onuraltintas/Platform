namespace Enterprise.Shared.ErrorHandling.Models;

public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ValidationErrorResponse : ErrorResponse
{
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public object? AttemptedValue { get; set; }
}

public enum ErrorSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<int, int> ErrorsByStatusCode { get; set; } = new();
    public List<TopErrorInfo> TopErrors { get; set; } = new();
    public double ErrorRate { get; set; }
}

public class TopErrorInfo
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
}