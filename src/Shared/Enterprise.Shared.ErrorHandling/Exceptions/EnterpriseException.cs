using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Exceptions;

public abstract class EnterpriseException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }
    public Dictionary<string, object> ErrorData { get; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Medium;

    protected EnterpriseException(string message, string errorCode = "UNKNOWN", 
        int httpStatusCode = 500, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
        ErrorData = new Dictionary<string, object>();
    }

    public virtual Microsoft.AspNetCore.Mvc.ProblemDetails ToProblemDetails()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = GetTitle(),
            Detail = Message,
            Status = HttpStatusCode,
            Type = $"https://enterprise.com/errors/{ErrorCode}",
            Instance = CorrelationId,
            Extensions = new Dictionary<string, object?>
            {
                ["errorCode"] = ErrorCode,
                ["occurredAt"] = OccurredAt,
                ["severity"] = Severity.ToString(),
                ["data"] = ErrorData
            }
        };
    }

    protected abstract string GetTitle();

    public EnterpriseException WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    public EnterpriseException WithSeverity(ErrorSeverity severity)
    {
        Severity = severity;
        return this;
    }

    public EnterpriseException WithData(string key, object value)
    {
        ErrorData[key] = value;
        return this;
    }
}