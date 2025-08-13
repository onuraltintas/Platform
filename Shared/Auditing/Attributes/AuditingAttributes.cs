namespace EgitimPlatform.Shared.Auditing.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AuditableAttribute : Attribute
{
    public string? TableName { get; set; }
    public string? Description { get; set; }
    public bool IncludeAllProperties { get; set; } = true;
    public bool AuditSoftDeletes { get; set; } = true;
    
    public AuditableAttribute()
    {
    }

    public AuditableAttribute(string description)
    {
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class NoAuditAttribute : Attribute
{
    public string? Reason { get; set; }

    public NoAuditAttribute()
    {
    }

    public NoAuditAttribute(string reason)
    {
        Reason = reason;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AuditIgnoreAttribute : Attribute
{
    public string? Reason { get; set; }

    public AuditIgnoreAttribute()
    {
    }

    public AuditIgnoreAttribute(string reason)
    {
        Reason = reason;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SensitiveDataAttribute : Attribute
{
    public string? MaskingPattern { get; set; } = "***SENSITIVE***";
    public bool LogHash { get; set; } = false;

    public SensitiveDataAttribute()
    {
    }

    public SensitiveDataAttribute(string maskingPattern)
    {
        MaskingPattern = maskingPattern;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AuditDisplayNameAttribute : Attribute
{
    public string DisplayName { get; }

    public AuditDisplayNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class AuditActionAttribute : Attribute
{
    public string ActionName { get; }
    public string? Description { get; set; }
    public bool LogParameters { get; set; } = true;
    public bool LogResult { get; set; } = true;
    public bool LogExecutionTime { get; set; } = true;

    public AuditActionAttribute(string actionName)
    {
        ActionName = actionName;
    }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class AuditParameterIgnoreAttribute : Attribute
{
    public string? Reason { get; set; }

    public AuditParameterIgnoreAttribute()
    {
    }

    public AuditParameterIgnoreAttribute(string reason)
    {
        Reason = reason;
    }
}