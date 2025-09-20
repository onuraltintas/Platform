namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class BusinessRuleException : EnterpriseException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message, string? errorCode = null)
        : base(message, errorCode ?? "BUSINESS_RULE_VIOLATION", 400)
    {
        RuleName = ruleName;
        ErrorData["ruleName"] = ruleName;
        Severity = Models.ErrorSeverity.Medium;
    }

    protected override string GetTitle() => "Business Rule Violation";
}