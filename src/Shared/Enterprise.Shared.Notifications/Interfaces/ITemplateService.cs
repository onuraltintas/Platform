using Enterprise.Shared.Notifications.Models;

namespace Enterprise.Shared.Notifications.Interfaces;

/// <summary>
/// Template service interface
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Render template with data
    /// </summary>
    /// <param name="templateKey">Template key</param>
    /// <param name="data">Template data</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rendered template</returns>
    Task<RenderedTemplate> RenderAsync(string templateKey, Dictionary<string, object> data, string language = "en-US", CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template by key and language
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification template</returns>
    Task<NotificationTemplate?> GetTemplateAsync(string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update template
    /// </summary>
    /// <param name="template">Notification template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CreateOrUpdateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete template
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeleteTemplateAsync(string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All templates</returns>
    Task<IEnumerable<NotificationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by key
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates for all languages</returns>
    Task<IEnumerable<NotificationTemplate>> GetTemplatesByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate template
    /// </summary>
    /// <param name="template">Template to validate</param>
    /// <param name="sampleData">Sample data for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<TemplateValidationResult> ValidateTemplateAsync(NotificationTemplate template, Dictionary<string, object>? sampleData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview template
    /// </summary>
    /// <param name="templateKey">Template key</param>
    /// <param name="data">Template data</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template preview</returns>
    Task<TemplatePreview> PreviewTemplateAsync(string templateKey, Dictionary<string, object> data, string language = "en-US", CancellationToken cancellationToken = default);

    /// <summary>
    /// Clone template to another language
    /// </summary>
    /// <param name="sourceKey">Source template key</param>
    /// <param name="sourceLanguage">Source language</param>
    /// <param name="targetLanguage">Target language</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CloneTemplateAsync(string sourceKey, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template statistics</returns>
    Task<TemplateStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear template cache
    /// </summary>
    /// <param name="templateKey">Template key (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ClearCacheAsync(string? templateKey = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Template statistics
/// </summary>
public class TemplateStatistics
{
    /// <summary>
    /// Total templates
    /// </summary>
    public int TotalTemplates { get; set; }

    /// <summary>
    /// Templates by language
    /// </summary>
    public Dictionary<string, int> ByLanguage { get; set; } = new();

    /// <summary>
    /// Templates by category
    /// </summary>
    public Dictionary<string, int> ByCategory { get; set; } = new();

    /// <summary>
    /// Active templates
    /// </summary>
    public int ActiveTemplates { get; set; }

    /// <summary>
    /// Inactive templates
    /// </summary>
    public int InactiveTemplates { get; set; }

    /// <summary>
    /// Most used templates
    /// </summary>
    public Dictionary<string, int> MostUsed { get; set; } = new();

    /// <summary>
    /// Recent templates
    /// </summary>
    public Dictionary<string, DateTime> RecentlyUpdated { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Template repository interface
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// Get template by key and language
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification template</returns>
    Task<NotificationTemplate?> GetByKeyAndLanguageAsync(string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create template
    /// </summary>
    /// <param name="template">Template to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created template</returns>
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update template
    /// </summary>
    /// <param name="template">Template to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<NotificationTemplate> UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete template
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeleteAsync(string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All templates</returns>
    Task<IEnumerable<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by key
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates for key</returns>
    Task<IEnumerable<NotificationTemplate>> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search templates
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="language">Language filter</param>
    /// <param name="category">Category filter</param>
    /// <param name="isActive">Active filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching templates</returns>
    Task<IEnumerable<NotificationTemplate>> SearchAsync(string? searchTerm = null, string? language = null, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if template exists
    /// </summary>
    /// <param name="key">Template key</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template count
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template count</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates in category</returns>
    Task<IEnumerable<NotificationTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all languages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All languages</returns>
    Task<IEnumerable<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);
}