namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class ResourceNotFoundException : EnterpriseException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ResourceNotFoundException(string resourceType, string resourceId, string? message = null)
        : base(message ?? $"{resourceType} with ID '{resourceId}' was not found", 
               "RESOURCE_NOT_FOUND", 404)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        ErrorData["resourceType"] = resourceType;
        ErrorData["resourceId"] = resourceId;
        Severity = Models.ErrorSeverity.Low;
    }

    protected override string GetTitle() => "Resource Not Found";
}