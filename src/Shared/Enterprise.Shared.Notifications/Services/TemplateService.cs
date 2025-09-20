using Enterprise.Shared.Notifications.Interfaces;
using Enterprise.Shared.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotLiquid;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Notifications.Services;

public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly NotificationSettings _settings;
    private readonly ConcurrentDictionary<string, Template> _templateCache = new();
    private readonly ConcurrentDictionary<string, NotificationTemplate> _templates = new();

    public TemplateService(ILogger<TemplateService> logger, IOptions<NotificationSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        InitializeDefaultTemplates();
    }

    public async Task<RenderedTemplate> RenderAsync(string templateKey, Dictionary<string, object> data, string language = "en-US", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentNullException.ThrowIfNull(data);

        _logger.LogDebug("Rendering template {TemplateKey} for language {Language}", templateKey, language);

        var template = await GetTemplateAsync(templateKey, language, cancellationToken);
        if (template == null)
        {
            _logger.LogWarning("Template {TemplateKey} not found for language {Language}", templateKey, language);
            throw new InvalidOperationException($"Template '{templateKey}' not found for language '{language}'");
        }

        try
        {
            var liquidData = Hash.FromDictionary(data);
            var rendered = new RenderedTemplate
            {
                Subject = RenderTemplateContent(template.SubjectTemplate, liquidData),
                HtmlContent = RenderTemplateContent(template.HtmlTemplate, liquidData),
                TextContent = RenderTemplateContent(template.TextTemplate, liquidData),
                SmsContent = RenderTemplateContent(template.SmsTemplate ?? template.TextTemplate, liquidData),
                PushTitle = RenderTemplateContent(template.PushTitleTemplate ?? template.SubjectTemplate, liquidData),
                PushBody = RenderTemplateContent(template.PushBodyTemplate ?? template.TextTemplate, liquidData),
                Language = language,
                RenderedAt = DateTime.UtcNow,
                TemplateKey = templateKey,
                Data = data
            };

            _logger.LogDebug("Successfully rendered template {TemplateKey} for language {Language}", templateKey, language);
            return rendered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateKey} for language {Language}", templateKey, language);
            throw new InvalidOperationException($"Failed to render template '{templateKey}': {ex.Message}", ex);
        }
    }

    public Task<NotificationTemplate?> GetTemplateAsync(string templateKey, string language = "en-US", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var key = $"{templateKey}_{language}";
        if (_templates.TryGetValue(key, out var template))
        {
            return Task.FromResult<NotificationTemplate?>(template);
        }

        // Try fallback to default language (en-US)
        if (language != "en-US")
        {
            var fallbackKey = $"{templateKey}_en-US";
            if (_templates.TryGetValue(fallbackKey, out var fallbackTemplate))
            {
                _logger.LogDebug("Using fallback template {TemplateKey} (en-US) for language {Language}", templateKey, language);
                return Task.FromResult<NotificationTemplate?>(fallbackTemplate);
            }
        }

        return Task.FromResult<NotificationTemplate?>(null);
    }

    public Task CreateOrUpdateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(template.Key);

        var key = $"{template.Key}_{template.Language}";
        _templates.AddOrUpdate(key, template, (_, _) => template);

        // Clear compiled template cache
        var cacheKeys = _templateCache.Keys.Where(k => k.StartsWith($"{template.Key}_")).ToList();
        foreach (var cacheKey in cacheKeys)
        {
            _templateCache.TryRemove(cacheKey, out _);
        }

        _logger.LogInformation("Created/Updated template {TemplateKey} for language {Language}", template.Key, template.Language);
        return Task.CompletedTask;
    }

    public Task DeleteTemplateAsync(string templateKey, string language = "en-US", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var key = $"{templateKey}_{language}";
        if (_templates.TryRemove(key, out _))
        {
            // Clear compiled template cache
            var cacheKeys = _templateCache.Keys.Where(k => k.StartsWith($"{templateKey}_")).ToList();
            foreach (var cacheKey in cacheKeys)
            {
                _templateCache.TryRemove(cacheKey, out _);
            }

            _logger.LogInformation("Deleted template {TemplateKey} for language {Language}", templateKey, language);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<NotificationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_templates.Values.OrderBy(t => t.Key).ThenBy(t => t.Language).AsEnumerable());
    }

    public Task<IEnumerable<NotificationTemplate>> GetTemplatesByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        return Task.FromResult(_templates.Values.Where(t => t.Key == key).OrderBy(t => t.Language).AsEnumerable());
    }

    public Task<IEnumerable<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType notificationType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_templates.Values
            .Where(t => t.Category == notificationType.ToString())
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Language).AsEnumerable());
    }

    public Task<TemplateValidationResult> ValidateTemplateAsync(NotificationTemplate template, Dictionary<string, object>? sampleData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Validate template content
            ValidateTemplateContent(template.SubjectTemplate ?? "");
            ValidateTemplateContent(template.HtmlTemplate ?? "");
            ValidateTemplateContent(template.TextTemplate ?? "");
            ValidateTemplateContent(template.SmsTemplate ?? "");
            ValidateTemplateContent(template.PushTitleTemplate ?? "");
            ValidateTemplateContent(template.PushBodyTemplate ?? "");

            // Test render with sample data if provided
            if (sampleData != null)
            {
                var liquidData = Hash.FromDictionary(sampleData);
                RenderTemplateContent(template.SubjectTemplate ?? "", liquidData);
                RenderTemplateContent(template.HtmlTemplate ?? "", liquidData);
                RenderTemplateContent(template.TextTemplate ?? "", liquidData);
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return Task.FromResult(new TemplateValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            ValidatedAt = DateTime.UtcNow
        });
    }

    public async Task<TemplatePreview> PreviewTemplateAsync(string templateKey, Dictionary<string, object> data, string language = "en-US", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentNullException.ThrowIfNull(data);

        var template = await GetTemplateAsync(templateKey, language, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateKey}' not found for language '{language}'");
        }

        var liquidData = Hash.FromDictionary(data);
        return new TemplatePreview
        {
            SubjectPreview = RenderTemplateContent(template.SubjectTemplate ?? "", liquidData),
            HtmlPreview = RenderTemplateContent(template.HtmlTemplate ?? "", liquidData),
            TextPreview = RenderTemplateContent(template.TextTemplate ?? "", liquidData),
            SmsPreview = RenderTemplateContent(template.SmsTemplate ?? template.TextTemplate, liquidData),
            PushPreview = $"{RenderTemplateContent(template.PushTitleTemplate ?? template.SubjectTemplate, liquidData)} - {RenderTemplateContent(template.PushBodyTemplate ?? template.TextTemplate, liquidData)}",
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task CloneTemplateAsync(string sourceKey, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguage);

        var sourceTemplate = await GetTemplateAsync(sourceKey, sourceLanguage, cancellationToken);
        if (sourceTemplate == null)
        {
            throw new InvalidOperationException($"Source template '{sourceKey}' not found for language '{sourceLanguage}'");
        }

        var clonedTemplate = new NotificationTemplate
        {
            Key = sourceTemplate.Key,
            Language = targetLanguage,
            Name = sourceTemplate.Name,
            Description = sourceTemplate.Description,
            SubjectTemplate = sourceTemplate.SubjectTemplate,
            HtmlTemplate = sourceTemplate.HtmlTemplate,
            TextTemplate = sourceTemplate.TextTemplate,
            SmsTemplate = sourceTemplate.SmsTemplate,
            PushTitleTemplate = sourceTemplate.PushTitleTemplate,
            PushBodyTemplate = sourceTemplate.PushBodyTemplate,
            IsActive = sourceTemplate.IsActive,
            Category = sourceTemplate.Category,
            Version = 1,
            RequiredFields = new List<string>(sourceTemplate.RequiredFields),
            OptionalFields = new List<string>(sourceTemplate.OptionalFields),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await CreateOrUpdateTemplateAsync(clonedTemplate, cancellationToken);
        _logger.LogInformation("Cloned template {TemplateKey} from {SourceLanguage} to {TargetLanguage}", 
            sourceKey, sourceLanguage, targetLanguage);
    }

    public async Task<TemplateStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var templates = _templates.Values.ToList();
        var statistics = new TemplateStatistics
        {
            TotalTemplates = templates.Count,
            ByLanguage = templates.GroupBy(t => t.Language).ToDictionary(g => g.Key, g => g.Count()),
            ByCategory = templates.GroupBy(t => t.Category ?? "Default").ToDictionary(g => g.Key, g => g.Count()),
            ActiveTemplates = templates.Count(t => t.IsActive),
            InactiveTemplates = templates.Count(t => !t.IsActive),
            MostUsed = new Dictionary<string, int>(), // Would track usage in production
            RecentlyUpdated = templates
                .OrderByDescending(t => t.UpdatedAt)
                .Take(10)
                .ToDictionary(t => t.Key, t => t.UpdatedAt),
            GeneratedAt = DateTime.UtcNow
        };

        return statistics;
    }

    public async Task ClearCacheAsync(string? templateKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(templateKey))
        {
            _templateCache.Clear();
            _logger.LogInformation("Cleared all template cache");
        }
        else
        {
            var keysToRemove = _templateCache.Keys.Where(k => k.StartsWith($"{templateKey}_")).ToList();
            foreach (var key in keysToRemove)
            {
                _templateCache.TryRemove(key, out _);
            }
            _logger.LogInformation("Cleared template cache for {TemplateKey}", templateKey);
        }
    }

    public async Task ImportTemplatesAsync(IEnumerable<NotificationTemplate> templates, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templates);

        var importedCount = 0;
        var skippedCount = 0;

        foreach (var template in templates)
        {
            var key = $"{template.Key}_{template.Language}";
            if (_templates.ContainsKey(key) && !overwrite)
            {
                skippedCount++;
                continue;
            }

            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _templates.AddOrUpdate(key, template, (_, _) => template);
            importedCount++;
        }

        _logger.LogInformation("Imported {ImportedCount} templates, skipped {SkippedCount} existing templates", 
            importedCount, skippedCount);
    }

    public async Task<IEnumerable<NotificationTemplate>> ExportTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return _templates.Values.ToList();
    }

    private string RenderTemplateContent(string? templateContent, Hash data)
    {
        if (string.IsNullOrEmpty(templateContent))
            return string.Empty;

        var cacheKey = $"{templateContent.GetHashCode()}";
        var template = _templateCache.GetOrAdd(cacheKey, _ => Template.Parse(templateContent));

        return template.Render(data);
    }

    private void ValidateTemplateContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return;

        Template.Parse(content);
    }

    private void InitializeDefaultTemplates()
    {
        var defaultTemplates = new[]
        {
            new NotificationTemplate
            {
                Key = "welcome",
                Language = "en-US",
                Name = "Welcome Message",
                Description = "Welcome new users to the platform",
                SubjectTemplate = "Welcome to {{ company_name }}!",
                HtmlTemplate = "<h1>Welcome {{ user.first_name }}!</h1><p>Thank you for joining {{ company_name }}. We're excited to have you on board.</p>",
                TextTemplate = "Welcome {{ user.first_name }}! Thank you for joining {{ company_name }}. We're excited to have you on board.",
                PushTitleTemplate = "Welcome to {{ company_name }}!",
                PushBodyTemplate = "Hi {{ user.first_name }}, welcome aboard!",
                SmsTemplate = "Welcome to {{ company_name }}, {{ user.first_name }}!",
                IsActive = true,
                Category = "Welcome",
                RequiredFields = new List<string> { "company_name", "user.first_name" }
            },
            new NotificationTemplate
            {
                Key = "email_verification",
                Language = "en-US",
                Name = "Email Verification",
                Description = "Email address verification message",
                SubjectTemplate = "Verify your email address",
                HtmlTemplate = "<p>Please verify your email address by clicking <a href=\"{{ verification_url }}\">here</a>.</p>",
                TextTemplate = "Please verify your email address by visiting: {{ verification_url }}",
                SmsTemplate = "Your verification code is: {{ verification_code }}",
                IsActive = true,
                Category = "Authentication",
                RequiredFields = new List<string> { "verification_url", "verification_code" }
            },
            new NotificationTemplate
            {
                Key = "password_reset",
                Language = "en-US",
                Name = "Password Reset",
                Description = "Password reset instructions",
                SubjectTemplate = "Reset your password",
                HtmlTemplate = "<p>Click <a href=\"{{ reset_url }}\">here</a> to reset your password. This link expires in {{ expires_in }} minutes.</p>",
                TextTemplate = "Reset your password by visiting: {{ reset_url }}. This link expires in {{ expires_in }} minutes.",
                IsActive = true,
                Category = "Authentication",
                RequiredFields = new List<string> { "reset_url", "expires_in" }
            },
            new NotificationTemplate
            {
                Key = "order_confirmation",
                Language = "en-US",
                Name = "Order Confirmation",
                Description = "Order confirmation message",
                SubjectTemplate = "Order Confirmation #{{ order.number }}",
                HtmlTemplate = "<h1>Order Confirmed!</h1><p>Your order #{{ order.number }} for {{ order.total | currency }} has been confirmed.</p>",
                TextTemplate = "Your order #{{ order.number }} for {{ order.total | currency }} has been confirmed.",
                PushTitleTemplate = "Order Confirmed",
                PushBodyTemplate = "Order #{{ order.number }} confirmed!",
                SmsTemplate = "Order #{{ order.number }} confirmed. Total: {{ order.total | currency }}",
                IsActive = true,
                Category = "Orders",
                RequiredFields = new List<string> { "order.number", "order.total" }
            }
        };

        foreach (var template in defaultTemplates)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            var key = $"{template.Key}_{template.Language}";
            _templates.TryAdd(key, template);
        }

        _logger.LogInformation("Initialized {Count} default templates", defaultTemplates.Length);
    }
}