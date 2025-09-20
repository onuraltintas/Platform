namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class ConflictException : EnterpriseException
{
    public string ConflictType { get; }

    public ConflictException(string message, string conflictType = "DATA_CONFLICT")
        : base(message, "CONFLICT", 409)
    {
        ConflictType = conflictType;
        ErrorData["conflictType"] = conflictType;
        Severity = Models.ErrorSeverity.Medium;
    }

    protected override string GetTitle() => "Conflict";
}