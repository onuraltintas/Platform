namespace Enterprise.Shared.Email.Interfaces;

/// <summary>
/// Service interface for managing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Gets a template by name
    /// </summary>
    Task<EmailTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    Task<EmailTemplate?> GetTemplateByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates
    /// </summary>
    Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by category
    /// </summary>
    Task<IEnumerable<EmailTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches templates by tags
    /// </summary>
    Task<IEnumerable<EmailTemplate>> SearchTemplatesAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template
    /// </summary>
    Task<Result> CreateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task<Result> UpdateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template by name
    /// </summary>
    Task<Result> DeleteTemplateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a template
    /// </summary>
    Task<ValidationResult> ValidateTemplateAsync(EmailTemplate template);

    /// <summary>
    /// Renders a template with provided data
    /// </summary>
    Task<TemplateRenderResult> RenderTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders template content with provided data
    /// </summary>
    Task<TemplateRenderResult> RenderTemplateContentAsync(string subject, string body, object data, bool isHtml = true);

    /// <summary>
    /// Gets template variables from content
    /// </summary>
    List<string> ExtractTemplateVariables(string content);

    /// <summary>
    /// Validates template syntax
    /// </summary>
    ValidationResult ValidateTemplateSyntax(string content);

    /// <summary>
    /// Imports templates from file system
    /// </summary>
    Task<Result> ImportTemplatesAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports templates to file system
    /// </summary>
    Task<Result> ExportTemplatesAsync(string directoryPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of template rendering operation
/// </summary>
public class TemplateRenderResult : Result
{
    /// <summary>
    /// Rendered subject
    /// </summary>
    public string RenderedSubject { get; set; } = string.Empty;

    /// <summary>
    /// Rendered body content
    /// </summary>
    public string RenderedBody { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the rendered content is HTML
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// Variables used in the template
    /// </summary>
    public List<string> UsedVariables { get; set; } = new();

    /// <summary>
    /// Missing variables that were not provided
    /// </summary>
    public List<string> MissingVariables { get; set; } = new();

    protected TemplateRenderResult(bool isSuccess, string error, OperationStatus status = OperationStatus.Success) 
        : base(isSuccess, error, status)
    {
    }

    /// <summary>
    /// Creates a successful render result
    /// </summary>
    public static TemplateRenderResult Success(string subject, string body, bool isHtml = true, List<string>? usedVariables = null, List<string>? missingVariables = null)
    {
        return new TemplateRenderResult(true, string.Empty)
        {
            RenderedSubject = subject,
            RenderedBody = body,
            IsHtml = isHtml,
            UsedVariables = usedVariables ?? new List<string>(),
            MissingVariables = missingVariables ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failed render result
    /// </summary>
    public static TemplateRenderResult Failure(string message)
    {
        return new TemplateRenderResult(false, message, OperationStatus.Failed);
    }
}