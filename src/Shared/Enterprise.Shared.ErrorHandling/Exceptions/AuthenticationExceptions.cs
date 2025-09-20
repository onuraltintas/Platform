namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class UnauthorizedException : EnterpriseException
{
    public UnauthorizedException(string message = "User is not authenticated")
        : base(message, "UNAUTHORIZED", 401)
    {
        Severity = Models.ErrorSeverity.Medium;
    }

    protected override string GetTitle() => "Unauthorized";
}

public class ForbiddenException : EnterpriseException
{
    public string? RequiredPermission { get; }

    public ForbiddenException(string message = "User does not have permission to perform this action", 
        string? requiredPermission = null)
        : base(message, "FORBIDDEN", 403)
    {
        RequiredPermission = requiredPermission;
        if (requiredPermission != null)
        {
            ErrorData["requiredPermission"] = requiredPermission;
        }
        Severity = Models.ErrorSeverity.Medium;
    }

    protected override string GetTitle() => "Forbidden";
}