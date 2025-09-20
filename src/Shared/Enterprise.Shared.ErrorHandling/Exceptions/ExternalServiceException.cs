namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class ExternalServiceException : EnterpriseException
{
    public string ServiceName { get; }
    public int? ServiceStatusCode { get; }

    public ExternalServiceException(string serviceName, string message, 
        int? serviceStatusCode = null, Exception? innerException = null)
        : base(message, "EXTERNAL_SERVICE_ERROR", 502, innerException)
    {
        ServiceName = serviceName;
        ServiceStatusCode = serviceStatusCode;
        ErrorData["serviceName"] = serviceName;
        if (serviceStatusCode.HasValue)
        {
            ErrorData["serviceStatusCode"] = serviceStatusCode.Value;
        }
        Severity = Models.ErrorSeverity.High;
    }

    protected override string GetTitle() => "External Service Error";
}