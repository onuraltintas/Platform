namespace Enterprise.Shared.Email.Models;

/// <summary>
/// Represents an email template
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Template unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name (used for lookup)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Email subject template
    /// </summary>
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body template content
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the template contains HTML
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// Template category for organization
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Template language/locale
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Template tags for filtering and searching
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Required template variables
    /// </summary>
    public List<TemplateVariable> Variables { get; set; } = new();

    /// <summary>
    /// Template creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template last modification date
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template author/creator
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Indicates if the template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// File path where template is stored (for file-based templates)
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Template metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets template variables from the content
    /// </summary>
    public List<string> GetTemplateVariables()
    {
        var variables = new HashSet<string>();
        
        // Match {{variable}} pattern
        var matches = Regex.Matches(Body + " " + Subject, @"\{\{([^}]+)\}\}");
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value.Trim());
        }

        return variables.ToList();
    }

    /// <summary>
    /// Validates the template
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Template name is required");
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            errors.Add("Template subject is required");
        }

        if (string.IsNullOrWhiteSpace(Body))
        {
            errors.Add("Template body is required");
        }

        // Validate template syntax
        try
        {
            var variables = GetTemplateVariables();
            // Additional validation logic can be added here
        }
        catch (Exception ex)
        {
            errors.Add($"Template syntax error: {ex.Message}");
        }

        return errors.Count == 0;
    }
}

/// <summary>
/// Represents a template variable definition
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// Variable name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Variable display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Variable description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Variable data type
    /// </summary>
    public TemplateVariableType Type { get; set; } = TemplateVariableType.String;

    /// <summary>
    /// Indicates if the variable is required
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Default value for the variable
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Variable validation rules
    /// </summary>
    public List<string> ValidationRules { get; set; } = new();
}

/// <summary>
/// Template variable data types
/// </summary>
public enum TemplateVariableType
{
    String,
    Number,
    Boolean,
    Date,
    DateTime,
    Array,
    Object
}