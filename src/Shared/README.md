# 📦 Shared Libraries Development Guide

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Shared Library Architecture](#shared-library-architecture)
3. [Email Service](#email-service)
4. [Auditing Service](#auditing-service)
5. [Error Handling](#error-handling)
6. [Caching Service](#caching-service)
7. [Security Service](#security-service)
8. [Events Service](#events-service)
9. [Common Utilities](#common-utilities)
10. [Development Guidelines](#development-guidelines)
11. [Testing Strategy](#testing-strategy)
12. [Package Management](#package-management)

---

## 🎯 Genel Bakış

Shared Libraries, mikroservisler arasında ortak kullanılan business logic, utilities ve cross-cutting concerns'leri barındırır. Her library kendi sorumluluklarına sahip, bağımsız geliştirilebilir ve test edilebilir modüller olarak tasarlanmıştır.

### Shared Libraries Listesi:
- **Enterprise.Shared.Email**: Email gönderimi ve template yönetimi
- **Enterprise.Shared.Auditing**: Audit logging ve security events
- **Enterprise.Shared.ErrorHandling**: Global exception handling ve error responses
- **Enterprise.Shared.Caching**: Distributed ve memory caching
- **Enterprise.Shared.Logging**: Structured logging ve enrichment
- **Enterprise.Shared.Security**: Encryption, hashing ve security utilities
- **Enterprise.Shared.Validation**: Input validation ve business rules
- **Enterprise.Shared.Events**: Event-driven communication infrastructure
- **Enterprise.Shared.Common**: Base entities, constants ve utilities
- **Enterprise.Shared.Storage**: File storage ve management
- **Enterprise.Shared.Notifications**: Push notifications, SMS ve Slack
- **Enterprise.Shared.Configuration**: Configuration management

### Design Principles:
- **Single Responsibility**: Her library tek bir sorumluluğa sahip
- **Dependency Inversion**: Interface-based design
- **Open/Closed**: Extension'a açık, modification'a kapalı
- **DRY (Don't Repeat Yourself)**: Code duplication önleme
- **SOLID Principles**: Clean architecture patterns

---

## 🏗️ Shared Library Architecture

### Package Structure Pattern

```
Enterprise.Shared.{LibraryName}/
├── 📄 Enterprise.Shared.{LibraryName}.csproj
├── 📁 Interfaces/
│   └── 📄 I{ServiceName}.cs
├── 📁 Models/
│   ├── 📄 {EntityName}.cs
│   └── 📄 {RequestName}.cs
├── 📁 Services/
│   └── 📄 {ServiceName}.cs
├── 📁 Extensions/
│   └── 📄 ServiceCollectionExtensions.cs
├── 📁 Configurations/
│   └── 📄 {LibraryName}Options.cs
└── 📄 README.md
```

### Base Package Configuration

```xml
<!-- Enterprise.Shared.Common.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Authors>Enterprise Team</Authors>
    <Company>Your Company</Company>
    <PackageProjectUrl>https://github.com/your-company/enterprise-microservices</PackageProjectUrl>
    <RepositoryUrl>https://github.com/your-company/enterprise-microservices</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
  </ItemGroup>
</Project>
```

---

## 📧 Email Service

### Interface Definition

```csharp
// Enterprise.Shared.Email/Interfaces/IEmailService.cs
public interface IEmailService
{
    Task<EmailResult> SendAsync(EmailMessage message);
    Task<EmailResult> SendTemplateAsync(string templateName, string to, object model);
    Task<EmailResult> SendBulkAsync(List<EmailMessage> messages);
    Task<EmailResult> SendWithAttachmentsAsync(EmailMessage message, List<EmailAttachment> attachments);
    Task<List<EmailTemplate>> GetTemplatesAsync();
    Task<EmailTemplate?> GetTemplateAsync(string templateName);
}

// Enterprise.Shared.Email/Interfaces/IEmailTemplateService.cs
public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object model);
    Task<bool> TemplateExistsAsync(string templateName);
    Task LoadTemplatesAsync();
}
```

### Models

```csharp
// Enterprise.Shared.Email/Models/EmailMessage.cs
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string[]? Cc { get; set; }
    public string[]? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? From { get; set; }
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public string TrackingId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Enterprise.Shared.Email/Models/EmailResult.cs
public class EmailResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? TrackingId { get; set; }
    public string? MessageId { get; set; }
    public DateTime SentAt { get; set; }
    public List<string> Errors { get; set; } = new();

    public static EmailResult Success(string trackingId, string? messageId = null)
        => new()
        {
            IsSuccess = true,
            TrackingId = trackingId,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        };

    public static EmailResult Failure(string message, string? trackingId = null)
        => new()
        {
            IsSuccess = false,
            Message = message,
            TrackingId = trackingId,
            Errors = new List<string> { message }
        };
}

public enum EmailPriority
{
    Low = 1,
    Normal = 2,
    High = 3
}
```

### Implementation

```csharp
// Enterprise.Shared.Email/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _options;
    private readonly IAuditService? _auditService;

    public EmailService(
        IEmailTemplateService templateService,
        ILogger<EmailService> logger,
        IOptions<EmailOptions> options,
        IAuditService? auditService = null)
    {
        _templateService = templateService;
        _logger = logger;
        _options = options.Value;
        _auditService = auditService;
    }

    public async Task<EmailResult> SendAsync(EmailMessage message)
    {
        try
        {
            ValidateEmailMessage(message);

            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(message);

            await smtpClient.SendMailAsync(mailMessage);

            await LogEmailEventAsync("EMAIL_SENT", message, "SUCCESS");

            return EmailResult.Success(message.TrackingId, mailMessage.Headers["Message-ID"]?.ToString());
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {To}. TrackingId: {TrackingId}", 
                message.To, message.TrackingId);
            
            await LogEmailEventAsync("EMAIL_SEND_FAILED", message, "SMTP_ERROR", ex.Message);
            
            return EmailResult.Failure($"SMTP Error: {ex.Message}", message.TrackingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {To}. TrackingId: {TrackingId}", 
                message.To, message.TrackingId);
            
            await LogEmailEventAsync("EMAIL_SEND_FAILED", message, "UNEXPECTED_ERROR", ex.Message);
            
            return EmailResult.Failure($"Unexpected error: {ex.Message}", message.TrackingId);
        }
    }

    public async Task<EmailResult> SendTemplateAsync(string templateName, string to, object model)
    {
        try
        {
            if (!await _templateService.TemplateExistsAsync(templateName))
            {
                return EmailResult.Failure($"Template '{templateName}' not found");
            }

            var body = await _templateService.RenderTemplateAsync(templateName, model);
            var subject = ExtractSubjectFromTemplate(body);

            var message = new EmailMessage
            {
                To = to,
                Subject = subject,
                Body = body,
                TrackingId = Guid.NewGuid().ToString()
            };

            return await SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template email. Template: {TemplateName}, To: {To}", 
                templateName, to);
            
            return EmailResult.Failure($"Template error: {ex.Message}");
        }
    }

    public async Task<EmailResult> SendBulkAsync(List<EmailMessage> messages)
    {
        var results = new List<EmailResult>();
        var semaphore = new SemaphoreSlim(_options.MaxConcurrentEmails, _options.MaxConcurrentEmails);

        var tasks = messages.Select(async message =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await SendAsync(message);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var allResults = await Task.WhenAll(tasks);
        results.AddRange(allResults);

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count - successCount;

        _logger.LogInformation("Bulk email completed. Success: {Success}, Failed: {Failed}", 
            successCount, failureCount);

        return new EmailResult
        {
            IsSuccess = failureCount == 0,
            Message = $"Bulk send completed. Success: {successCount}, Failed: {failureCount}",
            SentAt = DateTime.UtcNow
        };
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            Timeout = _options.TimeoutMs
        };

        return client;
    }

    private MailMessage CreateMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From ?? _options.DefaultFrom, message.FromName ?? _options.DefaultFromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
            Priority = message.Priority switch
            {
                EmailPriority.High => MailPriority.High,
                EmailPriority.Low => MailPriority.Low,
                _ => MailPriority.Normal
            }
        };

        mailMessage.To.Add(message.To);

        if (message.Cc != null)
        {
            foreach (var cc in message.Cc)
            {
                mailMessage.CC.Add(cc);
            }
        }

        if (message.Bcc != null)
        {
            foreach (var bcc in message.Bcc)
            {
                mailMessage.Bcc.Add(bcc);
            }
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            mailMessage.ReplyToList.Add(message.ReplyTo);
        }

        // Add custom headers
        foreach (var header in message.Headers)
        {
            mailMessage.Headers.Add(header.Key, header.Value);
        }

        // Add tracking header
        mailMessage.Headers.Add("X-Tracking-ID", message.TrackingId);

        return mailMessage;
    }

    private static void ValidateEmailMessage(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To))
            throw new ArgumentException("Recipient email is required", nameof(message.To));

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Email subject is required", nameof(message.Subject));

        if (string.IsNullOrWhiteSpace(message.Body))
            throw new ArgumentException("Email body is required", nameof(message.Body));

        if (!IsValidEmail(message.To))
            throw new ArgumentException("Invalid recipient email format", nameof(message.To));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractSubjectFromTemplate(string body)
    {
        // Simple subject extraction from HTML/template
        // In real implementation, this might be more sophisticated
        var match = Regex.Match(body, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "No Subject";
    }

    private async Task LogEmailEventAsync(string action, EmailMessage message, string result, string? error = null)
    {
        if (_auditService == null) return;

        try
        {
            await _auditService.LogEventAsync(new AuditEvent
            {
                Action = action,
                Resource = "Email",
                Result = result,
                Metadata = new Dictionary<string, object>
                {
                    ["TrackingId"] = message.TrackingId,
                    ["To"] = message.To,
                    ["Subject"] = message.Subject,
                    ["Priority"] = message.Priority.ToString(),
                    ["Error"] = error ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log email audit event");
        }
    }
}

// Enterprise.Shared.Email/Services/EmailTemplateService.cs
public class EmailTemplateService : IEmailTemplateService
{
    private readonly Dictionary<string, EmailTemplate> _templates = new();
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly EmailOptions _options;

    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        if (!_templates.TryGetValue(templateName.ToLower(), out var template))
        {
            throw new FileNotFoundException($"Email template '{templateName}' not found");
        }

        try
        {
            // Simple template rendering - in production, you might use a more sophisticated template engine
            var content = template.Content;
            var modelType = model.GetType();
            var properties = modelType.GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(model)?.ToString() ?? string.Empty;
                content = content.Replace($"{{{{{property.Name}}}}}", value);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw;
        }
    }

    public Task<bool> TemplateExistsAsync(string templateName)
    {
        return Task.FromResult(_templates.ContainsKey(templateName.ToLower()));
    }

    public async Task LoadTemplatesAsync()
    {
        try
        {
            var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");
            
            if (!Directory.Exists(templatesPath))
            {
                _logger.LogWarning("Email templates directory not found: {Path}", templatesPath);
                return;
            }

            var templateFiles = Directory.GetFiles(templatesPath, "*.html", SearchOption.AllDirectories);

            foreach (var file in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(file).ToLower();
                var content = await File.ReadAllTextAsync(file);

                _templates[templateName] = new EmailTemplate
                {
                    Name = templateName,
                    Content = content,
                    FilePath = file,
                    LastModified = File.GetLastWriteTime(file)
                };
            }

            _logger.LogInformation("Loaded {Count} email templates", _templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email templates");
            throw;
        }
    }
}
```

### Configuration & Registration

```csharp
// Enterprise.Shared.Email/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register options
        services.Configure<EmailOptions>(configuration.GetSection("Email"));

        // Register services
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Load templates on startup
        services.AddHostedService<EmailTemplateLoadingService>();

        return services;
    }
}

// Enterprise.Shared.Email/Configurations/EmailOptions.cs
public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DefaultFrom { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 30000;
    public int MaxConcurrentEmails { get; set; } = 10;
    public bool EnableTrackingPixel { get; set; } = false;
    public string TemplatesPath { get; set; } = "EmailTemplates";
}
```

---

## 🔍 Auditing Service

### Core Implementation

```csharp
// Enterprise.Shared.Auditing/Interfaces/IAuditService.cs
public interface IAuditService
{
    Task LogEventAsync(AuditEvent auditEvent);
    Task LogSecurityEventAsync(SecurityAuditEvent securityEvent);
    Task<List<AuditEvent>> SearchEventsAsync(AuditSearchCriteria criteria);
    Task<List<AuditEvent>> GetUserActivityAsync(string userId, DateTime? from = null, DateTime? to = null);
    Task LogDataChangeAsync<T>(T oldEntity, T newEntity, string action, string userId) where T : class;
    Task<AuditStatistics> GetAuditStatisticsAsync(DateTime from, DateTime to);
}

// Enterprise.Shared.Auditing/Models/AuditEvent.cs
public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string Result { get; set; } = string.Empty; // SUCCESS, FAILURE, UNAUTHORIZED
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ServiceName { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public TimeSpan? Duration { get; set; }

    // Helper methods
    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;
                
                if (value is JsonElement jsonElement)
                    return jsonElement.Deserialize<T>();
                
                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
}

// Enterprise.Shared.Auditing/Services/AuditService.cs
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IEventBus? _eventBus;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly AuditOptions _options;
    private readonly IAuditStore _auditStore;

    public async Task LogEventAsync(AuditEvent auditEvent)
    {
        try
        {
            // Enrich audit event with context information
            EnrichAuditEvent(auditEvent);

            // Validate required fields
            ValidateAuditEvent(auditEvent);

            // Store audit event
            await _auditStore.StoreAsync(auditEvent);

            // Log to structured logger
            LogToStructuredLogger(auditEvent);

            // Publish to event bus for real-time monitoring (if configured)
            if (_eventBus != null && _options.PublishEvents)
            {
                await _eventBus.PublishAsync(new AuditEventCreated
                {
                    AuditEventId = auditEvent.Id,
                    Action = auditEvent.Action,
                    UserId = auditEvent.UserId,
                    Result = auditEvent.Result,
                    Timestamp = auditEvent.Timestamp
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event. Action: {Action}, Resource: {Resource}", 
                auditEvent.Action, auditEvent.Resource);
            
            // Never throw exceptions from audit logging
            // This could break the main business flow
        }
    }

    public async Task LogSecurityEventAsync(SecurityAuditEvent securityEvent)
    {
        try
        {
            // Security events are critical - always log them
            EnrichAuditEvent(securityEvent);
            await _auditStore.StoreAsync(securityEvent);

            // Log with high priority
            _logger.LogWarning("Security Event: {EventType} - {Action} by {UserId} - {ThreatLevel}", 
                securityEvent.SecurityEventType, securityEvent.Action, securityEvent.UserId, securityEvent.ThreatLevel);

            // Immediate notification for high-threat events
            if (securityEvent.ThreatLevel == "HIGH" || securityEvent.ThreatLevel == "CRITICAL")
            {
                await NotifySecurityTeamAsync(securityEvent);
            }

            // Publish to event bus
            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new SecurityEventDetected
                {
                    SecurityEventId = securityEvent.Id,
                    ThreatLevel = securityEvent.ThreatLevel,
                    EventType = securityEvent.SecurityEventType,
                    UserId = securityEvent.UserId,
                    Timestamp = securityEvent.Timestamp
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
        }
    }

    public async Task LogDataChangeAsync<T>(T oldEntity, T newEntity, string action, string userId) where T : class
    {
        try
        {
            var changes = DetectChanges(oldEntity, newEntity);
            
            if (changes.Any())
            {
                var auditEvent = new AuditEvent
                {
                    UserId = userId,
                    Action = action,
                    Resource = typeof(T).Name,
                    ResourceId = GetEntityId(newEntity),
                    Result = "SUCCESS",
                    Metadata = new Dictionary<string, object>
                    {
                        ["EntityType"] = typeof(T).FullName!,
                        ["Changes"] = changes,
                        ["ChangeCount"] = changes.Count
                    }
                };

                await LogEventAsync(auditEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data change for entity type {EntityType}", typeof(T).Name);
        }
    }

    private void EnrichAuditEvent(AuditEvent auditEvent)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        
        if (httpContext != null)
        {
            auditEvent.IpAddress ??= GetClientIpAddress(httpContext);
            auditEvent.UserAgent ??= httpContext.Request.Headers["User-Agent"];
            auditEvent.CorrelationId ??= httpContext.Request.Headers["X-Correlation-ID"];
            
            if (string.IsNullOrEmpty(auditEvent.UserId))
            {
                auditEvent.UserId = httpContext.User?.FindFirst("sub")?.Value ?? 
                                  httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            
            if (string.IsNullOrEmpty(auditEvent.UserEmail))
            {
                auditEvent.UserEmail = httpContext.User?.FindFirst("email")?.Value ?? 
                                     httpContext.User?.FindFirst(ClaimTypes.Email)?.Value;
            }
        }

        auditEvent.ServiceName ??= Assembly.GetEntryAssembly()?.GetName().Name;
        
        if (auditEvent.Timestamp == default)
        {
            auditEvent.Timestamp = DateTime.UtcNow;
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers, proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static void ValidateAuditEvent(AuditEvent auditEvent)
    {
        if (string.IsNullOrWhiteSpace(auditEvent.Action))
            throw new ArgumentException("Action is required for audit events");
        
        if (string.IsNullOrWhiteSpace(auditEvent.Resource))
            throw new ArgumentException("Resource is required for audit events");
        
        if (string.IsNullOrWhiteSpace(auditEvent.Result))
            throw new ArgumentException("Result is required for audit events");
    }

    private void LogToStructuredLogger(AuditEvent auditEvent)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["AuditEventId"] = auditEvent.Id,
            ["UserId"] = auditEvent.UserId ?? "Unknown",
            ["Action"] = auditEvent.Action,
            ["Resource"] = auditEvent.Resource,
            ["Result"] = auditEvent.Result,
            ["CorrelationId"] = auditEvent.CorrelationId ?? "None"
        });

        var logLevel = auditEvent.Result == "SUCCESS" ? LogLevel.Information : LogLevel.Warning;
        
        _logger.Log(logLevel, "Audit Event: {Action} on {Resource} by {UserId} - {Result}", 
            auditEvent.Action, auditEvent.Resource, auditEvent.UserId ?? "Unknown", auditEvent.Result);
    }

    private static Dictionary<string, ChangeRecord> DetectChanges<T>(T oldEntity, T newEntity) where T : class
    {
        var changes = new Dictionary<string, ChangeRecord>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip properties marked with [NotAudited] attribute
            if (property.GetCustomAttribute<NotAuditedAttribute>() != null)
                continue;

            var oldValue = property.GetValue(oldEntity);
            var newValue = property.GetValue(newEntity);

            if (!Equals(oldValue, newValue))
            {
                changes[property.Name] = new ChangeRecord
                {
                    PropertyName = property.Name,
                    OldValue = oldValue,
                    NewValue = newValue,
                    PropertyType = property.PropertyType.Name
                };
            }
        }

        return changes;
    }

    private static string? GetEntityId<T>(T entity) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString();
    }

    private async Task NotifySecurityTeamAsync(SecurityAuditEvent securityEvent)
    {
        // Implementation would send immediate notifications
        // This could be email, Slack, SMS, etc.
        // Placeholder for security team notification
        await Task.CompletedTask;
    }
}
```

---

## ❌ Error Handling

### Exception Hierarchy

```csharp
// Enterprise.Shared.ErrorHandling/Exceptions/BusinessException.cs
public class BusinessException : Exception
{
    public string ErrorCode { get; }
    public object[] Parameters { get; }
    public Dictionary<string, object> Context { get; }

    public BusinessException(string errorCode, string message, params object[] parameters) 
        : base(message)
    {
        ErrorCode = errorCode;
        Parameters = parameters;
        Context = new Dictionary<string, object>();
    }

    public BusinessException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Parameters = Array.Empty<object>();
        Context = new Dictionary<string, object>();
    }

    public BusinessException AddContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }
}

// Enterprise.Shared.ErrorHandling/Exceptions/ValidationException.cs
public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<ValidationError>();
    }

    public ValidationException(string message, List<ValidationError> errors) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(List<ValidationError> errors) 
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public static ValidationException For<T>(Expression<Func<T, object>> property, string message)
    {
        var memberExpression = property.Body as MemberExpression ?? 
                              ((UnaryExpression)property.Body).Operand as MemberExpression;
        
        var propertyName = memberExpression?.Member.Name ?? "Unknown";

        return new ValidationException(new List<ValidationError>
        {
            new() { PropertyName = propertyName, ErrorMessage = message }
        });
    }
}
```

### Global Exception Middleware

```csharp
// Enterprise.Shared.ErrorHandling/Middleware/GlobalExceptionMiddleware.cs
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IAuditService? _auditService;
    private readonly ErrorHandlingOptions _options;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? 
                           Guid.NewGuid().ToString();
        
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        // Log the exception
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, TraceId: {TraceId}", 
            correlationId, traceId);

        // Log to audit service
        if (_auditService != null)
        {
            await _auditService.LogEventAsync(new AuditEvent
            {
                Action = "EXCEPTION_OCCURRED",
                Resource = context.Request.Path,
                Result = "FAILURE",
                CorrelationId = correlationId,
                Metadata = new Dictionary<string, object>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["ExceptionMessage"] = exception.Message,
                    ["StackTrace"] = _options.IncludeStackTrace ? exception.StackTrace : "Hidden",
                    ["RequestPath"] = context.Request.Path.ToString(),
                    ["RequestMethod"] = context.Request.Method
                }
            });
        }

        // Create error response
        var response = CreateErrorResponse(exception, correlationId, traceId);

        // Set response properties
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        // Write response
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _options.IndentJson
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string correlationId, string traceId)
    {
        return exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "One or more validation errors occurred.",
                Details = validationEx.Message,
                CorrelationId = correlationId,
                TraceId = traceId,
                ValidationErrors = validationEx.Errors,
                Timestamp = DateTime.UtcNow
            },
            
            BusinessException businessEx => new ErrorResponse
            {
                ErrorCode = businessEx.ErrorCode,
                Message = businessEx.Message,
                Details = _options.IncludeExceptionDetails ? businessEx.ToString() : null,
                CorrelationId = correlationId,
                TraceId = traceId,
                Context = businessEx.Context,
                Timestamp = DateTime.UtcNow
            },
            
            NotFoundException notFoundEx => new ErrorResponse
            {
                ErrorCode = "NOT_FOUND",
                Message = notFoundEx.Message,
                CorrelationId = correlationId,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            },
            
            UnauthorizedException unauthorizedEx => new ErrorResponse
            {
                ErrorCode = "UNAUTHORIZED",
                Message = "Authentication required",
                Details = unauthorizedEx.Message,
                CorrelationId = correlationId,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            },
            
            _ => new ErrorResponse
            {
                ErrorCode = "INTERNAL_SERVER_ERROR",
                Message = _options.ShowDetailedErrors 
                    ? exception.Message 
                    : "An internal server error occurred.",
                Details = _options.IncludeExceptionDetails ? exception.ToString() : null,
                CorrelationId = correlationId,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            }
        };
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ValidationException => 400,
        BusinessException => 422,
        NotFoundException => 404,
        UnauthorizedException => 401,
        ForbiddenException => 403,
        ConflictException => 409,
        _ => 500
    };
}
```

Bu comprehensive shared libraries guide, enterprise mikroservis mimarisinde kullanılacak tüm shared component'lerin detaylı implementasyonlarını içerir. Her library bağımsız olarak geliştirilebilir, test edilebilir ve deploy edilebilir şekilde tasarlanmıştır.