using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using EgitimPlatform.Shared.Email.Configuration;
using EgitimPlatform.Shared.Email.Services;

namespace EgitimPlatform.Shared.Email.Services;

public class HandlebarsTemplateService : IEmailTemplateService
{
    private readonly EmailOptions _options;
    private readonly ILogger<HandlebarsTemplateService> _logger;
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _templateCache;
    private readonly IHandlebars _handlebars;

    public HandlebarsTemplateService(IOptions<EmailOptions> options, ILogger<HandlebarsTemplateService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _templateCache = new ConcurrentDictionary<string, HandlebarsTemplate<object, object>>();
        _handlebars = Handlebars.Create();
        
        RegisterHelpers();
    }

    public async Task<string> RenderTemplateAsync(string templateName, object templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Rendering template: {TemplateName}", templateName);

            var template = await GetCompiledTemplateAsync(templateName, cancellationToken);
            if (template == null)
            {
                throw new FileNotFoundException($"Template '{templateName}' not found");
            }

            var mergedData = MergeWithGlobalVariables(templateData);
            var result = template(mergedData);

            _logger.LogDebug("Successfully rendered template: {TemplateName}", templateName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template: {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<string> RenderTemplateFromContentAsync(string templateContent, object templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Rendering inline template content");

            var template = _handlebars.Compile(templateContent);
            var mergedData = MergeWithGlobalVariables(templateData);
            var result = template(mergedData);

            _logger.LogDebug("Successfully rendered inline template");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render inline template");
            throw;
        }
    }

    public async Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var templatePath = GetTemplatePath(templateName);
        return File.Exists(templatePath);
    }

    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templatesPath = _options.Templates.TemplatesPath;
        
        if (!Directory.Exists(templatesPath))
        {
            _logger.LogWarning("Templates directory does not exist: {TemplatesPath}", templatesPath);
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(templatesPath, "*.hbs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>();
    }

    public async Task ClearTemplateCacheAsync(string? templateName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            _templateCache.Clear();
            _logger.LogInformation("Cleared all template cache");
        }
        else if (_templateCache.TryRemove(templateName, out _))
        {
            _logger.LogInformation("Cleared template cache for: {TemplateName}", templateName);
        }
    }

    private async Task<HandlebarsTemplate<object, object>?> GetCompiledTemplateAsync(string templateName, CancellationToken cancellationToken)
    {
        // Check if template is allowed
        if (_options.Templates.AllowedTemplates.Any() && 
            !_options.Templates.AllowedTemplates.Contains(templateName))
        {
            _logger.LogWarning("Template not in allowed list: {TemplateName}", templateName);
            return null;
        }

        // Check cache first
        if (_options.Templates.CacheTemplates && _templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        // Load and compile template
        var templatePath = GetTemplatePath(templateName);
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template file not found: {TemplatePath}", templatePath);
            return null;
        }

        var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
        
        // Apply layout if specified
        if (!string.IsNullOrEmpty(_options.Templates.DefaultLayout))
        {
            templateContent = await ApplyLayoutAsync(templateContent, _options.Templates.DefaultLayout, cancellationToken);
        }

        var compiledTemplate = _handlebars.Compile(templateContent);

        // Cache if enabled
        if (_options.Templates.CacheTemplates)
        {
            _templateCache.TryAdd(templateName, compiledTemplate);
            
            // Schedule cache expiration
            if (_options.Templates.CacheExpirationMinutes > 0)
            {
                _ = Task.Delay(TimeSpan.FromMinutes(_options.Templates.CacheExpirationMinutes), cancellationToken)
                    .ContinueWith(_ => _templateCache.TryRemove(templateName, out var _), TaskScheduler.Default);
            }
        }

        return compiledTemplate;
    }

    private async Task<string> ApplyLayoutAsync(string templateContent, string layoutName, CancellationToken cancellationToken)
    {
        var layoutPath = GetTemplatePath($"layouts/{layoutName}");
        if (!File.Exists(layoutPath))
        {
            _logger.LogWarning("Layout file not found: {LayoutPath}", layoutPath);
            return templateContent;
        }

        var layoutContent = await File.ReadAllTextAsync(layoutPath, cancellationToken);
        
        // Replace {{{body}}} placeholder with template content
        return layoutContent.Replace("{{{body}}}", templateContent);
    }

    private string GetTemplatePath(string templateName)
    {
        var templatesPath = Path.IsPathRooted(_options.Templates.TemplatesPath)
            ? _options.Templates.TemplatesPath
            : Path.Combine(AppContext.BaseDirectory, _options.Templates.TemplatesPath);

        var templateFileName = templateName.EndsWith(".hbs") ? templateName : $"{templateName}.hbs";
        return Path.Combine(templatesPath, templateFileName);
    }

    private object MergeWithGlobalVariables(object templateData)
    {
        var result = new Dictionary<string, object>();

        // Add global variables first
        foreach (var globalVar in _options.Templates.GlobalVariables)
        {
            result[globalVar.Key] = globalVar.Value;
        }

        // Add template data (can override global variables)
        if (templateData is Dictionary<string, object> dict)
        {
            foreach (var item in dict)
            {
                result[item.Key] = item.Value;
            }
        }
        else
        {
            var properties = templateData.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(templateData);
                if (value != null)
                {
                    result[property.Name] = value;
                }
            }
        }

        // Add system variables
        result["currentDate"] = DateTime.UtcNow;
        result["currentYear"] = DateTime.UtcNow.Year;
        result["applicationName"] = "Eğitim Platform";

        return result;
    }

    private void RegisterHelpers()
    {
        // Date formatting helper
        _handlebars.RegisterHelper("formatDate", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0] is DateTime date)
            {
                var format = parameters.Length > 1 ? parameters[1]?.ToString() : "yyyy-MM-dd";
                writer.WriteSafeString(date.ToString(format));
            }
        });

        // Currency formatting helper
        _handlebars.RegisterHelper("formatCurrency", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && decimal.TryParse(parameters[0]?.ToString(), out var amount))
            {
                var culture = parameters.Length > 1 ? parameters[1]?.ToString() : "tr-TR";
                var formatted = amount.ToString("C", new System.Globalization.CultureInfo(culture));
                writer.WriteSafeString(formatted);
            }
        });

        // Conditional helper
        _handlebars.RegisterHelper("if_equals", (writer, options, context, parameters) =>
        {
            if (parameters.Length >= 2 && 
                string.Equals(parameters[0]?.ToString(), parameters[1]?.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                options.Template(writer, context);
            }
            else
            {
                options.Inverse(writer, context);
            }
        });

        // URL helper
        _handlebars.RegisterHelper("url", (writer, context, parameters) =>
        {
            if (parameters.Length > 0)
            {
                var baseUrl = _options.Templates.GlobalVariables.GetValueOrDefault("baseUrl", "https://egitimplatform.com");
                var path = parameters[0]?.ToString()?.TrimStart('/');
                var fullUrl = $"{baseUrl.ToString().TrimEnd('/')}/{path}";
                writer.WriteSafeString(fullUrl);
            }
        });

        // Truncate helper
        _handlebars.RegisterHelper("truncate", (writer, context, parameters) =>
        {
            if (parameters.Length >= 2 && 
                parameters[0] is string text && 
                int.TryParse(parameters[1]?.ToString(), out var length))
            {
                var truncated = text.Length <= length ? text : text.Substring(0, length) + "...";
                writer.WriteSafeString(truncated);
            }
        });

        // Loop index helper
        _handlebars.RegisterHelper("add", (writer, context, parameters) =>
        {
            if (parameters.Length >= 2 && 
                int.TryParse(parameters[0]?.ToString(), out var num1) &&
                int.TryParse(parameters[1]?.ToString(), out var num2))
            {
                writer.WriteSafeString((num1 + num2).ToString());
            }
        });
    }
}