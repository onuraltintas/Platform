namespace Enterprise.Shared.Email.Services;

/// <summary>
/// Email template service implementation using Scriban for templating
/// </summary>
public class EmailTemplateService : IEmailTemplateService, IDisposable
{
    private readonly EmailConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly Dictionary<string, EmailTemplate> _templates;
    private FileSystemWatcher? _fileWatcher;

    public EmailTemplateService(
        IOptions<EmailConfiguration> configuration,
        IMemoryCache cache,
        ILogger<EmailTemplateService> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templates = new Dictionary<string, EmailTemplate>();

        if (_configuration.Templates.Provider == TemplateProvider.FileSystem && 
            _configuration.Templates.WatchFileChanges)
        {
            InitializeFileWatcher();
        }

        // Load initial templates
        LoadTemplatesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a template by name
    /// </summary>
    public async Task<EmailTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var cacheKey = $"email_template_{name}";
        
        if (_configuration.Templates.EnableCaching && _cache.TryGetValue(cacheKey, out EmailTemplate? cachedTemplate))
        {
            return cachedTemplate;
        }

        await LoadTemplatesAsync();

        if (_templates.TryGetValue(name, out var template) && template.IsActive)
        {
            if (_configuration.Templates.EnableCaching)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_configuration.Templates.CacheExpirationMinutes)
                };
                _cache.Set(cacheKey, template, cacheOptions);
            }

            return template;
        }

        return null;
    }

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    public async Task<EmailTemplate?> GetTemplateByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await LoadTemplatesAsync();
        return _templates.Values.FirstOrDefault(t => t.Id == id && t.IsActive);
    }

    /// <summary>
    /// Gets all templates
    /// </summary>
    public async Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await LoadTemplatesAsync();
        return _templates.Values.Where(t => t.IsActive).ToList();
    }

    /// <summary>
    /// Gets templates by category
    /// </summary>
    public async Task<IEnumerable<EmailTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        await LoadTemplatesAsync();
        return _templates.Values
            .Where(t => t.IsActive && string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Searches templates by tags
    /// </summary>
    public async Task<IEnumerable<EmailTemplate>> SearchTemplatesAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        await LoadTemplatesAsync();
        var tagList = tags.ToList();
        
        return _templates.Values
            .Where(t => t.IsActive && t.Tags.Any(tag => tagList.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Creates a new template
    /// </summary>
    public async Task<Result> CreateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        if (template == null)
            return Result.Failure("Template cannot be null");

        if (!template.IsValid(out var errors))
            return Result.Failure($"Template validation failed: {string.Join("; ", errors)}");

        if (_templates.ContainsKey(template.Name))
            return Result.Failure($"Template with name '{template.Name}' already exists");

        try
        {
            template.Id = Guid.NewGuid().ToString();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _templates[template.Name] = template;

            if (_configuration.Templates.Provider == TemplateProvider.FileSystem)
            {
                await SaveTemplateToFileAsync(template);
            }

            _logger.LogInformation("Template '{Name}' created successfully", template.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template '{Name}'", template.Name);
            return Result.Failure($"Failed to create template: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing template
    /// </summary>
    public async Task<Result> UpdateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        if (template == null)
            return Result.Failure("Template cannot be null");

        if (!template.IsValid(out var errors))
            return Result.Failure($"Template validation failed: {string.Join("; ", errors)}");

        if (!_templates.ContainsKey(template.Name))
            return Result.Failure($"Template with name '{template.Name}' does not exist");

        try
        {
            template.UpdatedAt = DateTime.UtcNow;
            _templates[template.Name] = template;

            if (_configuration.Templates.Provider == TemplateProvider.FileSystem)
            {
                await SaveTemplateToFileAsync(template);
            }

            // Clear cache
            if (_configuration.Templates.EnableCaching)
            {
                _cache.Remove($"email_template_{template.Name}");
            }

            _logger.LogInformation("Template '{Name}' updated successfully", template.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template '{Name}'", template.Name);
            return Result.Failure($"Failed to update template: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a template by name
    /// </summary>
    public Task<Result> DeleteTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(Result.Failure("Template name cannot be empty"));

        if (!_templates.ContainsKey(name))
            return Task.FromResult(Result.Failure($"Template with name '{name}' does not exist"));

        try
        {
            _templates.Remove(name);

            if (_configuration.Templates.Provider == TemplateProvider.FileSystem)
            {
                var filePath = GetTemplateFilePath(name);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            // Clear cache
            if (_configuration.Templates.EnableCaching)
            {
                _cache.Remove($"email_template_{name}");
            }

            _logger.LogInformation("Template '{Name}' deleted successfully", name);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template '{Name}'", name);
            return Task.FromResult(Result.Failure($"Failed to delete template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates a template
    /// </summary>
    public Task<ValidationResult> ValidateTemplateAsync(EmailTemplate template)
    {
        if (template == null)
            return Task.FromResult(ValidationResult.Failure(new[] { "Template cannot be null" }));

        if (!template.IsValid(out var errors))
            return Task.FromResult(ValidationResult.Failure(errors));

        var syntaxValidation = ValidateTemplateSyntax(template.Subject + " " + template.Body);
        if (!syntaxValidation.IsSuccess)
            return Task.FromResult(syntaxValidation);

        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Renders a template with provided data
    /// </summary>
    public async Task<TemplateRenderResult> RenderTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateName, cancellationToken);
        if (template == null)
            return TemplateRenderResult.Failure($"Template '{templateName}' not found");

        return await RenderTemplateContentAsync(template.Subject, template.Body, data, template.IsHtml);
    }

    /// <summary>
    /// Renders template content with provided data
    /// </summary>
    public async Task<TemplateRenderResult> RenderTemplateContentAsync(string subject, string body, object data, bool isHtml = true)
    {
        try
        {
            var subjectTemplate = Scriban.Template.Parse(subject);
            var bodyTemplate = Scriban.Template.Parse(body);

            if (subjectTemplate.HasErrors)
                return TemplateRenderResult.Failure($"Subject template errors: {string.Join("; ", subjectTemplate.Messages)}");

            if (bodyTemplate.HasErrors)
                return TemplateRenderResult.Failure($"Body template errors: {string.Join("; ", bodyTemplate.Messages)}");

            var scriptObject = new ScriptObject();
            if (data != null)
            {
                scriptObject.Import(data);
            }

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var renderedSubject = await subjectTemplate.RenderAsync(context);
            var renderedBody = await bodyTemplate.RenderAsync(context);

            var usedVariables = ExtractTemplateVariables(subject + " " + body);

            return TemplateRenderResult.Success(renderedSubject, renderedBody, isHtml, usedVariables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template");
            return TemplateRenderResult.Failure($"Template rendering failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets template variables from content
    /// </summary>
    public List<string> ExtractTemplateVariables(string content)
    {
        var variables = new HashSet<string>();
        
        // Match {{variable}} pattern for Scriban templates
        var matches = Regex.Matches(content, @"\{\{\s*([^}]+)\s*\}\}");
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value.Trim());
        }

        return variables.ToList();
    }

    /// <summary>
    /// Validates template syntax
    /// </summary>
    public ValidationResult ValidateTemplateSyntax(string content)
    {
        try
        {
            var template = Scriban.Template.Parse(content);
            if (template.HasErrors)
            {
                var errors = template.Messages.Select(m => m.ToString()).ToList();
                return ValidationResult.Failure(errors);
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(new[] { $"Template syntax validation failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Imports templates from file system
    /// </summary>
    public async Task<Result> ImportTemplatesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            return Result.Failure("Invalid directory path");

        try
        {
            var templateFiles = Directory.GetFiles(directoryPath, $"*{_configuration.Templates.FileExtension}", SearchOption.TopDirectoryOnly);
            var importedCount = 0;

            foreach (var filePath in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(filePath);
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);

                // Simple template format: first line is subject, rest is body
                var lines = content.Split('\n', 2);
                var subject = lines.Length > 0 ? lines[0].Trim() : "";
                var body = lines.Length > 1 ? lines[1].Trim() : "";

                var template = new EmailTemplate
                {
                    Name = templateName,
                    DisplayName = templateName,
                    Subject = subject,
                    Body = body,
                    FilePath = filePath
                };

                var result = await CreateTemplateAsync(template, cancellationToken);
                if (result.IsSuccess)
                {
                    importedCount++;
                }
            }

            _logger.LogInformation("Imported {Count} templates from {Directory}", importedCount, directoryPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import templates from {Directory}", directoryPath);
            return Result.Failure($"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports templates to file system
    /// </summary>
    public async Task<Result> ExportTemplatesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var templates = await GetAllTemplatesAsync(cancellationToken);
            var exportedCount = 0;

            foreach (var template in templates)
            {
                var filePath = Path.Combine(directoryPath, $"{template.Name}{_configuration.Templates.FileExtension}");
                var content = $"{template.Subject}\n{template.Body}";
                
                await File.WriteAllTextAsync(filePath, content, cancellationToken);
                exportedCount++;
            }

            _logger.LogInformation("Exported {Count} templates to {Directory}", exportedCount, directoryPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export templates to {Directory}", directoryPath);
            return Result.Failure($"Export failed: {ex.Message}");
        }
    }

    private async Task LoadTemplatesAsync()
    {
        if (_configuration.Templates.Provider == TemplateProvider.FileSystem)
        {
            await LoadTemplatesFromFileSystemAsync();
        }
    }

    private async Task LoadTemplatesFromFileSystemAsync()
    {
        var directoryPath = _configuration.Templates.DirectoryPath;
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            return;
        }

        var templateFiles = Directory.GetFiles(directoryPath, $"*{_configuration.Templates.FileExtension}");
        
        foreach (var filePath in templateFiles)
        {
            try
            {
                var templateName = Path.GetFileNameWithoutExtension(filePath);
                
                if (_templates.ContainsKey(templateName))
                    continue;

                var content = await File.ReadAllTextAsync(filePath);
                var lines = content.Split('\n', 2);
                var subject = lines.Length > 0 ? lines[0].Trim() : "";
                var body = lines.Length > 1 ? lines[1].Trim() : "";

                var template = new EmailTemplate
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = templateName,
                    DisplayName = templateName,
                    Subject = subject,
                    Body = body,
                    FilePath = filePath,
                    CreatedAt = File.GetCreationTimeUtc(filePath),
                    UpdatedAt = File.GetLastWriteTimeUtc(filePath)
                };

                _templates[templateName] = template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load template from file {FilePath}", filePath);
            }
        }
    }

    private async Task SaveTemplateToFileAsync(EmailTemplate template)
    {
        var filePath = GetTemplateFilePath(template.Name);
        var content = $"{template.Subject}\n{template.Body}";
        
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        await File.WriteAllTextAsync(filePath, content);
        template.FilePath = filePath;
    }

    private string GetTemplateFilePath(string templateName)
    {
        return Path.Combine(_configuration.Templates.DirectoryPath, $"{templateName}{_configuration.Templates.FileExtension}");
    }

    private void InitializeFileWatcher()
    {
        try
        {
            var directoryPath = _configuration.Templates.DirectoryPath;
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _fileWatcher = new FileSystemWatcher(directoryPath, $"*{_configuration.Templates.FileExtension}")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnTemplateFileChanged;
            _fileWatcher.Created += OnTemplateFileChanged;
            _fileWatcher.Deleted += OnTemplateFileDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize file watcher for templates");
        }
    }

    private void OnTemplateFileChanged(object sender, FileSystemEventArgs e)
    {
        var templateName = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(templateName)) return;
        
        if (_configuration.Templates.EnableCaching)
        {
            _cache.Remove($"email_template_{templateName}");
        }

        _templates.Remove(templateName);
        _logger.LogInformation("Template file changed: {TemplateName}", templateName);
    }

    private void OnTemplateFileDeleted(object sender, FileSystemEventArgs e)
    {
        var templateName = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(templateName)) return;
        
        if (_configuration.Templates.EnableCaching)
        {
            _cache.Remove($"email_template_{templateName}");
        }

        _templates.Remove(templateName);
        _logger.LogInformation("Template file deleted: {TemplateName}", templateName);
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }
}