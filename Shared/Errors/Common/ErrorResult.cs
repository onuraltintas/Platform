namespace EgitimPlatform.Shared.Errors.Common;

public class ErrorResult
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public ErrorResult() { }
    
    public ErrorResult(string code, string message)
    {
        Code = code;
        Message = message;
    }
    
    public ErrorResult(string code, string message, string detail) : this(code, message)
    {
        Detail = detail;
    }
    
    public ErrorResult WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
        return this;
    }
    
    public ErrorResult WithTraceId(string traceId)
    {
        TraceId = traceId;
        return this;
    }
}