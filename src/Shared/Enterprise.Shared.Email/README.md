# Enterprise.Shared.Email

**Versiyon:** 1.0.0  
**Hedef Framework:** .NET 8.0  
**Geliştirici:** Enterprise Platform Team

## 📋 Proje Amacı

Enterprise.Shared.Email, Enterprise mikroservis platformu için geliştirilmiş kapsamlı bir email hizmet kütüphanesidir. SMTP tabanlı email gönderimi, HTML/text template engine, toplu email işlemleri, email tracking, bounce handling ve delivery status monitoring ile enterprise-grade email çözümleri sunar.

## 🌟 Ana Özellikler

### Email Delivery
- **SMTP Support**: Güvenli SMTP sunucu entegrasyonu
- **FluentEmail Integration**: Modern email API desteği
- **Multiple Recipients**: To, CC, BCC desteği
- **Attachment Support**: Dosya ekleri ve inline images
- **HTML/Text Content**: Hem HTML hem de plain text desteği

### Template Engine
- **Razor Templates**: ASP.NET Core Razor template desteği
- **Scriban Templates**: Liquid-like template syntax
- **Dynamic Content**: Variable substitution ve conditional rendering
- **Template Caching**: Yüksek performans için template cache
- **Multi-language**: Çoklu dil template desteği

### Bulk Operations
- **Bulk Email**: Toplu email gönderimi
- **Batch Processing**: Configurable batch size ile işleme
- **Rate Limiting**: Email gönderim hız sınırlaması
- **Concurrent Processing**: Paralel email işleme
- **Progress Tracking**: Bulk operasyon takibi

### Monitoring & Analytics
- **Delivery Tracking**: Email delivery status monitoring
- **Health Checks**: Email service sağlık kontrolü
- **Retry Logic**: Otomatik hata yönetimi ve retry
- **Performance Metrics**: Email gönderim metrikleri
- **Validation**: Email format ve content validation

## 🛠 Kullanılan Teknolojiler

### Ana Bağımlılıklar
- **FluentEmail.Core 3.0.2**: Modern email API
- **FluentEmail.Smtp 3.0.2**: SMTP transport provider
- **FluentEmail.Razor 3.0.2**: Razor template engine
- **Scriban 5.10.0**: Advanced template engine

### Microsoft Extensions
- **Microsoft.Extensions.DependencyInjection.Abstractions 8.0.1**: DI support
- **Microsoft.Extensions.Logging.Abstractions 8.0.1**: Structured logging
- **Microsoft.Extensions.Options 8.0.2**: Configuration options
- **Microsoft.Extensions.Configuration.Abstractions 8.0.0**: Configuration
- **Microsoft.Extensions.Hosting.Abstractions 8.0.0**: Background services
- **Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0**: Config binding

### Additional Dependencies
- **Microsoft.AspNetCore.Http.Abstractions 2.2.0**: HTTP context
- **System.ComponentModel.Annotations 5.0.0**: Data annotations

## ⚙️ Kurulum ve Konfigürasyon

### 1. NuGet Paketi Yükleme
```bash
dotnet add package Enterprise.Shared.Email
```

### 2. Dependency Injection Konfigürasyonu

#### Basic Setup (Program.cs)
```csharp
using Enterprise.Shared.Email.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Basic email services
builder.Services.AddEmailService(builder.Configuration);

// SMTP configuration
builder.Services.AddSmtpEmailService(options =>
{
    options.Host = "smtp.gmail.com";
    options.Port = 587;
    options.Username = "your-email@gmail.com";
    options.Password = "your-app-password";
    options.EnableSsl = true;
});

var app = builder.Build();

app.Run();
```

#### Development Setup
```csharp
// Development-specific email configuration
builder.Services.AddDevelopmentEmailService();

// File system templates for development
builder.Services.AddFileSystemTemplates(options =>
{
    options.TemplatesDirectory = Path.Combine(builder.Environment.ContentRootPath, "Templates", "Email");
    options.FileExtension = ".html";
});
```

#### Production Setup
```csharp
// Production email configuration
builder.Services.AddEmailService(builder.Configuration.GetSection("EmailService"));

// Configure retry policy
builder.Services.ConfigureRetryPolicy(options =>
{
    options.MaxAttempts = 3;
    options.DelayMs = 5000;
});

// Configure rate limiting
builder.Services.ConfigureRateLimit(options =>
{
    options.EmailsPerMinute = 100;
});

// Configure bulk processing
builder.Services.ConfigureBulkProcessing(options =>
{
    options.DefaultBatchSize = 100;
    options.DefaultMaxConcurrency = 10;
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<EmailServiceHealthCheck>("email_service");
```

#### Advanced Configuration
```csharp
// Comprehensive email service configuration
builder.Services.AddEmailService(options =>
{
    // Default sender
    options.DefaultSender.Email = "noreply@enterprise.com";
    options.DefaultSender.Name = "Enterprise Platform";
    
    // SMTP settings
    options.Smtp.Host = "smtp.enterprise.com";
    options.Smtp.Port = 587;
    options.Smtp.Username = "smtp-user";
    options.Smtp.Password = "smtp-password";
    options.Smtp.EnableSsl = true;
    
    // Template settings
    options.Templates.Provider = TemplateProvider.FileSystem;
    options.Templates.TemplatesDirectory = "Templates/Email";
    options.Templates.DefaultLanguage = "tr-TR";
    
    // Bulk processing
    options.BulkProcessing.DefaultBatchSize = 50;
    options.BulkProcessing.DefaultMaxConcurrency = 5;
    
    // Retry policy
    options.Retry.MaxAttempts = 5;
    options.Retry.DelayMs = 10000;
    
    // Rate limiting
    options.RateLimit.EmailsPerMinute = 200;
    
    // Logging
    options.Logging.LogSuccessfulSends = true;
    options.Logging.LogFailedSends = true;
    options.Logging.LogTemplateRenders = true;
});
```

### 3. appsettings.json Konfigürasyonu

```json
{
  "EmailService": {
    "DefaultSender": {
      "Email": "noreply@enterprise.com",
      "Name": "Enterprise Platform"
    },
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSsl": true,
      "Timeout": 30000
    },
    "Templates": {
      "Provider": "FileSystem",
      "TemplatesDirectory": "Templates/Email",
      "FileExtension": ".html",
      "DefaultLanguage": "tr-TR",
      "CacheTemplates": true,
      "CacheDuration": "00:30:00"
    },
    "BulkProcessing": {
      "DefaultBatchSize": 100,
      "DefaultMaxConcurrency": 10
    },
    "Retry": {
      "MaxAttempts": 3,
      "DelayMs": 5000
    },
    "RateLimit": {
      "EmailsPerMinute": 100
    },
    "Logging": {
      "LogSuccessfulSends": true,
      "LogFailedSends": true,
      "LogTemplateRenders": true,
      "LogBulkOperations": true,
      "LogValidation": false
    }
  },
  
  "Logging": {
    "LogLevel": {
      "Enterprise.Shared.Email": "Information",
      "FluentEmail": "Warning"
    }
  }
}
```

## 📖 Kullanım Kılavuzu

### 1. Basic Email Sending

#### Simple Email
```csharp
public class NotificationController : ControllerBase
{
    private readonly IEmailService _emailService;
    
    public NotificationController(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    [HttpPost("send-notification")]
    public async Task<IActionResult> SendNotification(SendNotificationRequest request)
    {
        var message = EmailMessage.Create(
            to: request.Email,
            subject: "Bildirim",
            body: "Bu bir test bildirimi mesajıdır."
        );
        
        var result = await _emailService.SendAsync(message);
        
        if (result.IsSuccess)
        {
            return Ok(new { 
                success = true, 
                messageId = result.MessageId,
                trackingId = result.TrackingId 
            });
        }
        
        return BadRequest(new { 
            success = false, 
            errors = result.Errors 
        });
    }
}
```

#### HTML Email with Attachments
```csharp
public async Task SendWelcomeEmailAsync(string userEmail, string userName, byte[] pdfAttachment)
{
    var message = new EmailMessage
    {
        To = userEmail,
        Subject = "Hoş Geldiniz!",
        Body = $@"
            <html>
                <body>
                    <h1>Merhaba {userName}!</h1>
                    <p>Enterprise platformumuza hoş geldiniz.</p>
                    <p>Ekli dosyada kullanım kılavuzunu bulabilirsiniz.</p>
                    <br>
                    <p>İyi günler,<br>Enterprise Ekibi</p>
                </body>
            </html>",
        IsHtml = true,
        Priority = EmailPriority.High
    };
    
    // Add attachment
    var attachment = EmailAttachment.FromBytes(
        content: pdfAttachment,
        fileName: "kullanim-kilavuzu.pdf",
        contentType: "application/pdf"
    );
    message.AddAttachment(attachment);
    
    // Add metadata for tracking
    message.AddMetadata("campaign", "welcome");
    message.AddMetadata("userType", "new");
    message.AddTag("welcome");
    message.AddTag("onboarding");
    
    var result = await _emailService.SendAsync(message);
    
    if (result.IsSuccess)
    {
        _logger.LogInformation("Welcome email sent to {Email} with tracking ID {TrackingId}", 
            userEmail, result.TrackingId);
    }
    else
    {
        _logger.LogError("Failed to send welcome email to {Email}: {Errors}", 
            userEmail, string.Join(", ", result.Errors));
    }
}
```

#### CC and BCC Recipients
```csharp
public async Task SendProjectUpdateAsync(string[] teamMembers, string[] managers, string projectUpdate)
{
    var message = new EmailMessage
    {
        To = teamMembers[0], // Primary recipient
        Cc = teamMembers.Skip(1).ToArray(), // Other team members
        Bcc = managers, // Managers get BCC
        Subject = "Proje Güncellemesi",
        Body = projectUpdate,
        IsHtml = true
    };
    
    message.AddHeader("X-Priority", "1"); // High priority
    message.AddTag("project-update");
    
    var result = await _emailService.SendAsync(message);
    return result;
}
```

### 2. Template-Based Email

#### Create and Use Templates
```csharp
public class EmailTemplateManager
{
    private readonly IEmailTemplateService _templateService;
    private readonly IEmailService _emailService;
    
    public EmailTemplateManager(IEmailTemplateService templateService, IEmailService emailService)
    {
        _templateService = templateService;
        _emailService = emailService;
    }
    
    // Create a template
    public async Task CreateWelcomeTemplateAsync()
    {
        var template = new EmailTemplate
        {
            Name = "welcome-user",
            Subject = "Hoş Geldiniz {{user.name}}!",
            Body = @"
                <html>
                    <body>
                        <div style='font-family: Arial, sans-serif;'>
                            <h1>Merhaba {{user.name}}!</h1>
                            <p>{{company.name}} platformuna hoş geldiniz.</p>
                            
                            <div style='background: #f5f5f5; padding: 20px; margin: 20px 0;'>
                                <h3>Hesap Bilgileriniz:</h3>
                                <ul>
                                    <li><strong>Email:</strong> {{user.email}}</li>
                                    <li><strong>Kayıt Tarihi:</strong> {{user.registrationDate | date: 'dd/MM/yyyy'}}</li>
                                    <li><strong>Kullanıcı Tipi:</strong> {{user.type}}</li>
                                </ul>
                            </div>
                            
                            {{if features}}
                            <h3>Kullanabileceğiniz Özellikler:</h3>
                            <ul>
                                {{for feature in features}}
                                <li>{{feature.name}} - {{feature.description}}</li>
                                {{end}}
                            </ul>
                            {{end}}
                            
                            <p>Herhangi bir sorunuz olursa, <a href='mailto:destek@enterprise.com'>destek ekibimiz</a> ile iletişime geçebilirsiniz.</p>
                            
                            <hr style='margin: 30px 0;'>
                            <p style='color: #666; font-size: 12px;'>
                                Bu email {{company.name}} tarafından gönderilmiştir.<br>
                                {{company.address}}
                            </p>
                        </div>
                    </body>
                </html>",
            Language = "tr-TR",
            Category = "Onboarding",
            IsHtml = true
        };
        
        var result = await _templateService.CreateTemplateAsync(template);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Template oluşturulamadı: {result.Errors}");
        }
    }
    
    // Send email using template
    public async Task SendWelcomeEmailAsync(User user, List<Feature> availableFeatures)
    {
        var templateData = new
        {
            user = new
            {
                name = user.Name,
                email = user.Email,
                registrationDate = user.CreatedAt,
                type = user.UserType
            },
            company = new
            {
                name = "Enterprise Platform",
                address = "İstanbul, Türkiye"
            },
            features = availableFeatures.Select(f => new
            {
                name = f.Name,
                description = f.Description
            }).ToList()
        };
        
        var result = await _emailService.SendTemplateAsync(
            templateName: "welcome-user",
            to: user.Email,
            data: templateData
        );
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Welcome email sent to {Email} using template", user.Email);
        }
    }
}
```

#### Template Variables and Validation
```csharp
public async Task ValidateAndPreviewTemplateAsync(string templateName)
{
    // Get template
    var template = await _templateService.GetTemplateAsync(templateName);
    if (template == null)
    {
        throw new ArgumentException($"Template '{templateName}' bulunamadı");
    }
    
    // Validate template syntax
    var syntaxResult = await _templateService.ValidateTemplateSyntax(template.Body);
    if (!syntaxResult.IsSuccess)
    {
        _logger.LogError("Template syntax error: {Errors}", string.Join(", ", syntaxResult.Errors));
        return;
    }
    
    // Extract template variables
    var variables = await _templateService.ExtractTemplateVariables(template.Body);
    _logger.LogInformation("Template variables: {Variables}", 
        string.Join(", ", variables.Select(v => $"{v.Name}:{v.Type}")));
    
    // Preview with sample data
    var sampleData = new
    {
        user = new { name = "Test User", email = "test@example.com" },
        company = new { name = "Test Company" }
    };
    
    var previewResult = await _templateService.RenderTemplateAsync(templateName, sampleData);
    if (previewResult.IsSuccess)
    {
        _logger.LogInformation("Template preview generated successfully");
        // Save preview to file or display in admin interface
    }
}
```

### 3. Bulk Email Operations

#### Send Bulk Emails
```csharp
public class NewsletterService
{
    private readonly IEmailService _emailService;
    
    public NewsletterService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task SendNewsletterAsync(NewsletterCampaign campaign, List<Subscriber> subscribers)
    {
        var recipients = subscribers
            .Where(s => s.IsActive && !s.IsUnsubscribed)
            .Select(s => new BulkEmailRecipient
            {
                Email = s.Email,
                Data = new
                {
                    firstName = s.FirstName,
                    lastName = s.LastName,
                    subscriberType = s.SubscriptionType,
                    preferences = s.Preferences
                }
            })
            .ToList();
        
        var bulkRequest = new BulkEmailRequest
        {
            TemplateName = "newsletter-template",
            Subject = campaign.Subject,
            Recipients = recipients,
            
            // Performance settings
            BatchSize = 50, // Process 50 emails at a time
            MaxConcurrency = 5, // Maximum 5 concurrent batches
            DelayBetweenBatches = TimeSpan.FromSeconds(10), // Wait 10 seconds between batches
            
            // Common template data
            CommonData = new
            {
                campaign = new
                {
                    name = campaign.Name,
                    date = campaign.SendDate,
                    unsubscribeUrl = $"https://enterprise.com/unsubscribe?campaign={campaign.Id}"
                },
                company = new
                {
                    name = "Enterprise",
                    logo = "https://enterprise.com/images/logo.png",
                    address = "İstanbul, Türkiye"
                }
            }
        };
        
        // Add tracking tags
        bulkRequest.AddTag("newsletter");
        bulkRequest.AddTag($"campaign-{campaign.Id}");
        bulkRequest.AddMetadata("campaignId", campaign.Id.ToString());
        bulkRequest.AddMetadata("sendDate", DateTime.UtcNow.ToString("O"));
        
        // Send bulk emails
        var result = await _emailService.SendBulkAsync(bulkRequest);
        
        // Update campaign status
        campaign.TotalSent = result.TotalSent;
        campaign.TotalFailed = result.TotalFailed;
        campaign.CompletedAt = result.CompletedAt;
        
        _logger.LogInformation("Newsletter campaign {CampaignId} completed. " +
            "Sent: {Sent}, Failed: {Failed}, Success Rate: {SuccessRate:P2}",
            campaign.Id, result.TotalSent, result.TotalFailed, result.SuccessRate);
    }
}
```

#### Bulk Email with Dynamic Content
```csharp
public async Task SendPersonalizedOffersAsync(List<Customer> customers, OfferCampaign campaign)
{
    var recipients = customers.Select(customer => new BulkEmailRecipient
    {
        Email = customer.Email,
        Data = new
        {
            customer = new
            {
                name = customer.Name,
                loyaltyLevel = customer.LoyaltyLevel,
                lastPurchase = customer.LastPurchaseDate,
                totalSpent = customer.TotalSpent
            },
            offer = new
            {
                discountPercent = CalculatePersonalizedDiscount(customer),
                validUntil = campaign.ValidUntil,
                products = GetRecommendedProducts(customer)
            }
        }
    }).ToList();
    
    var bulkRequest = new BulkEmailRequest
    {
        TemplateName = "personalized-offer",
        Recipients = recipients,
        BatchSize = 25, // Smaller batches for personalized content
        MaxConcurrency = 3,
        DelayBetweenBatches = TimeSpan.FromSeconds(15)
    };
    
    var result = await _emailService.SendBulkAsync(bulkRequest);
    
    // Track results by customer segment
    var segmentResults = result.Results
        .GroupBy(r => ((dynamic)r.RecipientData).customer.loyaltyLevel)
        .ToDictionary(g => g.Key, g => new
        {
            Total = g.Count(),
            Successful = g.Count(r => r.IsSuccess),
            Failed = g.Count(r => !r.IsSuccess)
        });
    
    foreach (var segment in segmentResults)
    {
        _logger.LogInformation("Segment {Segment}: {Successful}/{Total} emails sent successfully",
            segment.Key, segment.Value.Successful, segment.Value.Total);
    }
}
```

### 4. Email Validation and Testing

#### Email Format Validation
```csharp
public async Task<ValidationResult> ValidateEmailAddressAsync(string email)
{
    var testMessage = new EmailMessage
    {
        To = email,
        Subject = "Test",
        Body = "Test"
    };
    
    var result = await _emailService.ValidateEmailAsync(testMessage);
    
    if (!result.IsSuccess)
    {
        _logger.LogWarning("Email validation failed for {Email}: {Errors}", 
            email, string.Join(", ", result.Errors));
    }
    
    return result;
}
```

#### Connection Testing
```csharp
public async Task<bool> TestEmailConfigurationAsync()
{
    try
    {
        var result = await _emailService.TestConnectionAsync();
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Email service connection test successful");
            return true;
        }
        else
        {
            _logger.LogError("Email service connection test failed: {Errors}", 
                string.Join(", ", result.Errors));
            return false;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Email service connection test threw exception");
        return false;
    }
}
```

#### Email Preview and Testing
```csharp
public async Task<string> PreviewEmailTemplateAsync(string templateName, object sampleData)
{
    var renderResult = await _templateService.RenderTemplateAsync(templateName, sampleData);
    
    if (!renderResult.IsSuccess)
    {
        throw new InvalidOperationException($"Template render failed: {renderResult.Errors}");
    }
    
    return renderResult.RenderedContent;
}

public async Task SendTestEmailAsync(string templateName, string testEmail, object testData)
{
    // First validate the template
    var template = await _templateService.GetTemplateAsync(templateName);
    if (template == null)
    {
        throw new ArgumentException($"Template '{templateName}' not found");
    }
    
    // Render template
    var renderResult = await _templateService.RenderTemplateAsync(templateName, testData);
    if (!renderResult.IsSuccess)
    {
        throw new InvalidOperationException($"Template render failed: {renderResult.Errors}");
    }
    
    // Create test message
    var message = new EmailMessage
    {
        To = testEmail,
        Subject = $"[TEST] {renderResult.Subject}",
        Body = $"<div style='background: #ffe6e6; padding: 10px; margin-bottom: 20px; border: 1px solid #ff9999;'>" +
               $"<strong>⚠️ BU BİR TEST EMAİL'İDIR</strong><br>" +
               $"Template: {templateName}<br>" +
               $"Gönderim Zamanı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
               $"</div>" +
               renderResult.RenderedContent,
        IsHtml = true
    };
    
    message.AddTag("test");
    message.AddTag($"template-{templateName}");
    message.AddMetadata("isTest", "true");
    
    var result = await _emailService.SendAsync(message);
    
    if (result.IsSuccess)
    {
        _logger.LogInformation("Test email sent successfully to {Email} for template {Template}", 
            testEmail, templateName);
    }
    
    return result;
}
```

### 5. Email Tracking and Analytics

#### Delivery Status Tracking
```csharp
public class EmailTrackingService
{
    private readonly IEmailService _emailService;
    
    public async Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string trackingId)
    {
        try
        {
            var status = await _emailService.GetDeliveryStatusAsync(trackingId);
            
            _logger.LogInformation("Delivery status for {TrackingId}: {Status}", trackingId, status);
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery status for {TrackingId}", trackingId);
            return EmailDeliveryStatus.Unknown;
        }
    }
    
    public async Task ProcessDeliveryWebhookAsync(EmailWebhookEvent webhookEvent)
    {
        switch (webhookEvent.EventType)
        {
            case "delivered":
                await HandleEmailDeliveredAsync(webhookEvent);
                break;
                
            case "bounced":
                await HandleEmailBouncedAsync(webhookEvent);
                break;
                
            case "opened":
                await HandleEmailOpenedAsync(webhookEvent);
                break;
                
            case "clicked":
                await HandleEmailClickedAsync(webhookEvent);
                break;
                
            case "complained":
                await HandleEmailComplainedAsync(webhookEvent);
                break;
                
            case "unsubscribed":
                await HandleEmailUnsubscribedAsync(webhookEvent);
                break;
                
            default:
                _logger.LogWarning("Unknown webhook event type: {EventType}", webhookEvent.EventType);
                break;
        }
    }
    
    private async Task HandleEmailBouncedAsync(EmailWebhookEvent webhookEvent)
    {
        _logger.LogWarning("Email bounced: {Email}, Reason: {Reason}", 
            webhookEvent.Email, webhookEvent.Reason);
        
        // Update user email status
        await UpdateEmailStatusAsync(webhookEvent.Email, EmailStatus.Bounced);
        
        // If hard bounce, disable email for this address
        if (webhookEvent.BounceType == "hard")
        {
            await DisableEmailDeliveryAsync(webhookEvent.Email);
            _logger.LogInformation("Email delivery disabled for {Email} due to hard bounce", 
                webhookEvent.Email);
        }
    }
    
    private async Task HandleEmailComplainedAsync(EmailWebhookEvent webhookEvent)
    {
        _logger.LogWarning("Email complaint received from {Email}", webhookEvent.Email);
        
        // Immediately unsubscribe user to prevent further complaints
        await UnsubscribeUserAsync(webhookEvent.Email);
        
        // Add to suppression list
        await AddToSuppressionListAsync(webhookEvent.Email, "spam complaint");
        
        _logger.LogInformation("User {Email} automatically unsubscribed due to spam complaint", 
            webhookEvent.Email);
    }
}
```

#### Email Analytics Dashboard
```csharp
public class EmailAnalyticsService
{
    public async Task<EmailCampaignAnalytics> GetCampaignAnalyticsAsync(
        Guid campaignId, DateTime startDate, DateTime endDate)
    {
        var analytics = new EmailCampaignAnalytics
        {
            CampaignId = campaignId,
            StartDate = startDate,
            EndDate = endDate
        };
        
        // Get campaign emails from database
        var campaignEmails = await GetCampaignEmailsAsync(campaignId, startDate, endDate);
        
        analytics.TotalSent = campaignEmails.Count;
        analytics.TotalDelivered = campaignEmails.Count(e => e.Status == EmailStatus.Delivered);
        analytics.TotalBounced = campaignEmails.Count(e => e.Status == EmailStatus.Bounced);
        analytics.TotalOpened = campaignEmails.Count(e => e.OpenedAt.HasValue);
        analytics.TotalClicked = campaignEmails.Count(e => e.ClickedAt.HasValue);
        analytics.TotalUnsubscribed = campaignEmails.Count(e => e.UnsubscribedAt.HasValue);
        analytics.TotalComplaints = campaignEmails.Count(e => e.ComplainedAt.HasValue);
        
        // Calculate rates
        analytics.DeliveryRate = analytics.TotalSent > 0 ? 
            (double)analytics.TotalDelivered / analytics.TotalSent : 0;
        analytics.OpenRate = analytics.TotalDelivered > 0 ? 
            (double)analytics.TotalOpened / analytics.TotalDelivered : 0;
        analytics.ClickRate = analytics.TotalDelivered > 0 ? 
            (double)analytics.TotalClicked / analytics.TotalDelivered : 0;
        analytics.UnsubscribeRate = analytics.TotalDelivered > 0 ? 
            (double)analytics.TotalUnsubscribed / analytics.TotalDelivered : 0;
        analytics.ComplaintRate = analytics.TotalDelivered > 0 ? 
            (double)analytics.TotalComplaints / analytics.TotalDelivered : 0;
        
        // Engagement over time
        analytics.DailyStats = campaignEmails
            .GroupBy(e => e.SentAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyEmailStats
            {
                Date = g.Key,
                Sent = g.Count(),
                Delivered = g.Count(e => e.Status == EmailStatus.Delivered),
                Opened = g.Count(e => e.OpenedAt.HasValue),
                Clicked = g.Count(e => e.ClickedAt.HasValue)
            })
            .ToList();
        
        return analytics;
    }
    
    public async Task<List<TopPerformingTemplate>> GetTopPerformingTemplatesAsync(
        DateTime startDate, DateTime endDate, int limit = 10)
    {
        var templateStats = await GetTemplateStatsAsync(startDate, endDate);
        
        return templateStats
            .OrderByDescending(t => t.OpenRate)
            .ThenByDescending(t => t.ClickRate)
            .Take(limit)
            .ToList();
    }
}
```

## 🏗 Template System

### 1. Scriban Templates

#### Advanced Template Features
```html
<!-- welcome-advanced.html -->
<!DOCTYPE html>
<html>
<head>
    <title>{{company.name}} - Hoş Geldiniz</title>
    <meta charset="utf-8">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
    
    <!-- Header -->
    <div style="background: #007bff; color: white; padding: 20px; text-align: center;">
        <h1>{{company.name}}</h1>
        <p>{{company.tagline}}</p>
    </div>
    
    <!-- Main Content -->
    <div style="padding: 30px;">
        <h2>Merhaba {{user.name | string.capitalize}}!</h2>
        
        <p>{{company.name}} ailesine katıldığınız için teşekkür ederiz. 
        {{user.registration_date | date.to_string '%d %B %Y'}} tarihinde oluşturduğunuz hesabınız aktif edilmiştir.</p>
        
        <!-- Conditional content based on user type -->
        {{if user.type == "premium"}}
        <div style="background: #f8f9fa; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0;">
            <h3>✨ Premium Üyelik Avantajlarınız</h3>
            <ul>
                {{for benefit in premium_benefits}}
                <li><strong>{{benefit.name}}</strong>: {{benefit.description}}</li>
                {{end}}
            </ul>
        </div>
        {{else if user.type == "business"}}
        <div style="background: #f8f9fa; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;">
            <h3>🏢 Business Üyelik Özellikleri</h3>
            <ul>
                {{for feature in business_features}}
                <li>{{feature.name}} - {{feature.limit}} {{feature.unit}}</li>
                {{end}}
            </ul>
        </div>
        {{else}}
        <div style="background: #f8f9fa; border-left: 4px solid #17a2b8; padding: 15px; margin: 20px 0;">
            <h3>🚀 Başlangıç Paketiniz</h3>
            <p>Ücretsiz hesabınızla aşağıdaki özellikleri kullanabilirsiniz:</p>
            <ul>
                {{for feature in basic_features}}
                <li>{{feature}}</li>
                {{end}}
            </ul>
        </div>
        {{end}}
        
        <!-- Dynamic recommendations -->
        {{if recommendations.size > 0}}
        <h3>Size Özel Öneriler</h3>
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 15px; margin: 20px 0;">
            {{for recommendation in recommendations}}
            <div style="border: 1px solid #dee2e6; border-radius: 5px; padding: 15px;">
                <h4>{{recommendation.title}}</h4>
                <p>{{recommendation.description}}</p>
                {{if recommendation.action_url}}
                <a href="{{recommendation.action_url}}" style="background: #007bff; color: white; padding: 8px 16px; text-decoration: none; border-radius: 3px; display: inline-block;">
                    {{recommendation.action_text || "Daha Fazla"}}
                </a>
                {{end}}
            </div>
            {{end}}
        </div>
        {{end}}
        
        <!-- Contact information -->
        <div style="background: #e9ecef; padding: 20px; border-radius: 5px; margin: 30px 0;">
            <h3>İletişim</h3>
            <p>Herhangi bir sorunuz olursa, bizimle iletişime geçmekten çekinmeyin:</p>
            <ul style="list-style: none; padding: 0;">
                <li>📧 Email: <a href="mailto:{{support.email}}">{{support.email}}</a></li>
                <li>📞 Telefon: {{support.phone}}</li>
                {{if support.chat_url}}
                <li>💬 Canlı Destek: <a href="{{support.chat_url}}">Hemen Başla</a></li>
                {{end}}
            </ul>
        </div>
        
        <!-- Call to action -->
        <div style="text-align: center; margin: 40px 0;">
            <a href="{{app.dashboard_url}}" 
               style="background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-size: 18px; display: inline-block;">
                🚀 Platformu Keşfet
            </a>
        </div>
        
        <!-- Social links -->
        {{if social_links.size > 0}}
        <div style="text-align: center; margin: 30px 0;">
            <p>Bizi takip edin:</p>
            {{for social in social_links}}
            <a href="{{social.url}}" style="margin: 0 10px; text-decoration: none;">
                {{social.name}}
            </a>
            {{if !for.last}} | {{end}}
            {{end}}
        </div>
        {{end}}
    </div>
    
    <!-- Footer -->
    <div style="background: #6c757d; color: white; padding: 20px; text-align: center; font-size: 12px;">
        <p>© {{date.now.year}} {{company.name}}. Tüm hakları saklıdır.</p>
        <p>{{company.address}}</p>
        
        <p>
            <a href="{{unsubscribe_url}}" style="color: #adb5bd;">E-posta aboneliğinizi iptal edin</a> |
            <a href="{{preferences_url}}" style="color: #adb5bd;">E-posta tercihlerinizi güncelleyin</a> |
            <a href="{{privacy_policy_url}}" style="color: #adb5bd;">Gizlilik Politikası</a>
        </p>
    </div>
    
</body>
</html>
```

### 2. Template Management

#### Template Import/Export
```csharp
public class TemplateManagementService
{
    private readonly IEmailTemplateService _templateService;
    
    public async Task ImportTemplatesFromDirectoryAsync(string directoryPath)
    {
        var templateFiles = Directory.GetFiles(directoryPath, "*.html", SearchOption.AllDirectories);
        
        foreach (var filePath in templateFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var content = await File.ReadAllTextAsync(filePath);
                
                // Parse template metadata from comments or separate .json file
                var metadataPath = Path.ChangeExtension(filePath, ".json");
                var metadata = File.Exists(metadataPath) 
                    ? JsonSerializer.Deserialize<TemplateMetadata>(await File.ReadAllTextAsync(metadataPath))
                    : new TemplateMetadata { Name = fileName };
                
                var template = new EmailTemplate
                {
                    Name = metadata.Name ?? fileName,
                    Subject = metadata.Subject ?? $"Subject for {fileName}",
                    Body = content,
                    Category = metadata.Category ?? "Imported",
                    Language = metadata.Language ?? "tr-TR",
                    IsHtml = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System Import"
                };
                
                var result = await _templateService.CreateTemplateAsync(template);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Template imported successfully: {TemplateName}", template.Name);
                }
                else
                {
                    _logger.LogError("Failed to import template {TemplateName}: {Errors}", 
                        template.Name, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing template from {FilePath}", filePath);
            }
        }
    }
    
    public async Task ExportTemplatesToDirectoryAsync(string directoryPath, string? category = null)
    {
        var templates = category != null 
            ? await _templateService.GetTemplatesByCategoryAsync(category)
            : await _templateService.GetAllTemplatesAsync();
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        foreach (var template in templates)
        {
            try
            {
                var fileName = SanitizeFileName(template.Name);
                var filePath = Path.Combine(directoryPath, $"{fileName}.html");
                var metadataPath = Path.Combine(directoryPath, $"{fileName}.json");
                
                // Export template content
                await File.WriteAllTextAsync(filePath, template.Body);
                
                // Export template metadata
                var metadata = new TemplateMetadata
                {
                    Name = template.Name,
                    Subject = template.Subject,
                    Category = template.Category,
                    Language = template.Language,
                    Variables = template.Variables?.ToDictionary(v => v.Name, v => v.Type.ToString()),
                    CreatedAt = template.CreatedAt,
                    CreatedBy = template.CreatedBy
                };
                
                var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(metadataPath, metadataJson);
                
                _logger.LogInformation("Template exported: {TemplateName} -> {FilePath}", 
                    template.Name, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting template {TemplateName}", template.Name);
            }
        }
    }
}
```

## 📊 Performance Monitoring

### 1. Email Service Health Check
```csharp
public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;
    
    public EmailServiceHealthCheck(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test SMTP connection
            var connectionResult = await _emailService.TestConnectionAsync();
            
            if (!connectionResult.IsSuccess)
            {
                return HealthCheckResult.Unhealthy(
                    "SMTP connection failed", 
                    data: new Dictionary<string, object>
                    {
                        { "errors", connectionResult.Errors },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            
            // Test template service
            var templateTestResult = await TestTemplateServiceAsync();
            if (!templateTestResult.healthy)
            {
                return HealthCheckResult.Degraded(
                    "Template service issues detected",
                    data: templateTestResult.data);
            }
            
            return HealthCheckResult.Healthy(
                "Email service is healthy",
                data: new Dictionary<string, object>
                {
                    { "smtp_connection", "OK" },
                    { "template_service", "OK" },
                    { "last_check", DateTime.UtcNow }
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Email service health check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "exception", ex.Message },
                    { "timestamp", DateTime.UtcNow }
                });
        }
    }
    
    private async Task<(bool healthy, Dictionary<string, object> data)> TestTemplateServiceAsync()
    {
        var data = new Dictionary<string, object>();
        
        try
        {
            // Test template rendering with sample data
            var testResult = await _templateService.RenderTemplateContentAsync(
                "Test template: {{test.value}}", 
                new { test = new { value = "OK" } });
            
            if (testResult.IsSuccess)
            {
                data["template_rendering"] = "OK";
                return (true, data);
            }
            else
            {
                data["template_rendering"] = "FAILED";
                data["template_errors"] = testResult.Errors;
                return (false, data);
            }
        }
        catch (Exception ex)
        {
            data["template_rendering"] = "EXCEPTION";
            data["template_exception"] = ex.Message;
            return (false, data);
        }
    }
}
```

### 2. Performance Metrics
```csharp
public class EmailMetricsService
{
    private readonly IMetrics _metrics;
    
    // Counters
    private readonly Counter<long> _emailsSentCounter;
    private readonly Counter<long> _emailsFailedCounter;
    private readonly Counter<long> _templatesRenderedCounter;
    
    // Histograms
    private readonly Histogram<double> _emailSendDuration;
    private readonly Histogram<double> _templateRenderDuration;
    private readonly Histogram<double> _bulkEmailDuration;
    
    public EmailMetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Enterprise.Shared.Email");
        
        _emailsSentCounter = meter.CreateCounter<long>(
            "emails_sent_total",
            "emails",
            "Total number of emails sent successfully");
            
        _emailsFailedCounter = meter.CreateCounter<long>(
            "emails_failed_total", 
            "emails",
            "Total number of emails that failed to send");
            
        _templatesRenderedCounter = meter.CreateCounter<long>(
            "templates_rendered_total",
            "templates", 
            "Total number of templates rendered");
            
        _emailSendDuration = meter.CreateHistogram<double>(
            "email_send_duration",
            "ms",
            "Duration of email send operations");
            
        _templateRenderDuration = meter.CreateHistogram<double>(
            "template_render_duration",
            "ms", 
            "Duration of template render operations");
            
        _bulkEmailDuration = meter.CreateHistogram<double>(
            "bulk_email_duration",
            "ms",
            "Duration of bulk email operations");
    }
    
    public void RecordEmailSent(string templateName, double durationMs)
    {
        _emailsSentCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("template", templateName),
            new("result", "success")
        });
        
        _emailSendDuration.Record(durationMs, new KeyValuePair<string, object?>[]
        {
            new("template", templateName),
            new("operation", "send")
        });
    }
    
    public void RecordEmailFailed(string templateName, string errorType, double durationMs)
    {
        _emailsFailedCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("template", templateName),
            new("error_type", errorType)
        });
        
        _emailSendDuration.Record(durationMs, new KeyValuePair<string, object?>[]
        {
            new("template", templateName),
            new("operation", "send_failed")
        });
    }
    
    public void RecordBulkEmailCompleted(int totalEmails, int successCount, double durationMs)
    {
        _emailsSentCounter.Add(successCount, new KeyValuePair<string, object?>[]
        {
            new("operation", "bulk"),
            new("result", "success")
        });
        
        _emailsFailedCounter.Add(totalEmails - successCount, new KeyValuePair<string, object?>[]
        {
            new("operation", "bulk"),
            new("result", "failed")
        });
        
        _bulkEmailDuration.Record(durationMs, new KeyValuePair<string, object?>[]
        {
            new("operation", "bulk"),
            new("batch_size", totalEmails.ToString())
        });
    }
}
```

## 🔧 Best Practices

### 1. Template Design

#### ✅ İyi Örnekler
```html
<!-- Responsive and accessible template -->
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{subject}}</title>
    <style>
        /* Responsive styles */
        @media screen and (max-width: 600px) {
            .container { width: 100% !important; }
            .content { padding: 10px !important; }
        }
        
        /* Dark mode support */
        @media (prefers-color-scheme: dark) {
            .container { background: #1a1a1a !important; color: #ffffff !important; }
        }
    </style>
</head>
<body>
    <div class="container" style="max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif;">
        <!-- Accessible content with proper alt text and semantic HTML -->
        <img src="{{company.logo}}" alt="{{company.name}} Logo" style="max-width: 200px;">
        
        <main class="content" style="padding: 20px;">
            <h1>{{title}}</h1>
            <p>{{content}}</p>
        </main>
        
        <!-- Proper unsubscribe link -->
        <footer style="margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;">
            <p><a href="{{unsubscribe_url}}" style="color: #666;">Abonelikten çık</a></p>
        </footer>
    </div>
</body>
</html>
```

#### ❌ Kötü Örnekler
```html
<!-- Avoid these practices -->
<html>
<body>
    <!-- No DOCTYPE, no lang attribute, no meta tags -->
    
    <!-- Inline styles everywhere, no mobile responsiveness -->
    <div style="width: 800px; background: red; font-size: 8px;">
        <!-- Poor accessibility -->
        <img src="logo.png"> <!-- No alt text -->
        
        <!-- No semantic HTML -->
        <div style="font-size: 24px;">Title</div>
        
        <!-- Hardcoded values -->
        <p>Hello John Doe!</p> <!-- Should use template variables -->
        
        <!-- No unsubscribe link -->
        
        <!-- Poor error handling in template -->
        {{user.name.toUpper()}} <!-- Will break if user.name is null -->
    </div>
</body>
</html>
```

### 2. Performance Optimization

```csharp
// Batch processing with proper resource management
public class OptimizedBulkEmailService
{
    private readonly IEmailService _emailService;
    private readonly SemaphoreSlim _semaphore;
    
    public OptimizedBulkEmailService(IEmailService emailService)
    {
        _emailService = emailService;
        _semaphore = new SemaphoreSlim(5, 5); // Limit concurrent operations
    }
    
    public async Task<BulkEmailResult> SendOptimizedBulkAsync(
        BulkEmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Pre-render template once if using same template for all
        string? preRenderedTemplate = null;
        if (!string.IsNullOrEmpty(request.TemplateName) && 
            request.Recipients.All(r => r.Data == null))
        {
            var renderResult = await _templateService.RenderTemplateAsync(
                request.TemplateName, request.CommonData);
            if (renderResult.IsSuccess)
            {
                preRenderedTemplate = renderResult.RenderedContent;
            }
        }
        
        // Process in optimized batches
        var results = new List<EmailResult>();
        var batches = request.Recipients.Chunk(request.BatchSize);
        
        foreach (var batch in batches)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var batchTasks = batch.Select(async recipient =>
                {
                    try
                    {
                        var message = preRenderedTemplate != null
                            ? CreateMessageFromRenderedTemplate(recipient, preRenderedTemplate)
                            : await CreateMessageFromTemplate(recipient, request);
                            
                        return await _emailService.SendAsync(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        return EmailResult.Failure($"Processing error: {ex.Message}");
                    }
                });
                
                var batchResults = await Task.WhenAll(batchTasks);
                results.AddRange(batchResults);
                
                // Delay between batches to respect rate limits
                if (request.DelayBetweenBatches > TimeSpan.Zero)
                {
                    await Task.Delay(request.DelayBetweenBatches, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        return new BulkEmailResult
        {
            TotalSent = results.Count(r => r.IsSuccess),
            TotalFailed = results.Count(r => !r.IsSuccess),
            Results = results,
            CompletedAt = DateTime.UtcNow
        };
    }
}
```

### 3. Error Handling and Resilience

```csharp
public class ResilientEmailService
{
    private readonly IEmailService _emailService;
    private readonly IRetryPolicy _retryPolicy;
    
    public async Task<EmailResult> SendWithRetryAsync(
        EmailMessage message, 
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var result = await _emailService.SendAsync(message, cancellationToken);
                
                // Don't retry on validation errors
                if (!result.IsSuccess && result.Errors.Any(e => e.Contains("validation")))
                {
                    throw new InvalidOperationException($"Validation error: {string.Join(", ", result.Errors)}");
                }
                
                return result;
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy)
            {
                // Temporary failure - retry
                throw new TransientException($"SMTP mailbox busy: {ex.Message}", ex);
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
            {
                // Permanent failure - don't retry
                throw new PermanentException($"SMTP mailbox unavailable: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                // Network timeout - retry
                throw new TransientException($"Network timeout: {ex.Message}", ex);
            }
        });
    }
}

public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
    public TransientException(string message, Exception innerException) : base(message, innerException) { }
}

public class PermanentException : Exception
{
    public PermanentException(string message) : base(message) { }
    public PermanentException(string message, Exception innerException) : base(message, innerException) { }
}
```

## 🐛 Troubleshooting

### Yaygın Problemler ve Çözümleri

#### 1. SMTP Connection Issues
```csharp
// Connection diagnostics
public async Task DiagnoseSmtpIssuesAsync()
{
    try
    {
        var result = await _emailService.TestConnectionAsync();
        
        if (!result.IsSuccess)
        {
            _logger.LogError("SMTP Connection Failed: {Errors}", string.Join(", ", result.Errors));
            
            // Common solutions
            _logger.LogInformation("Troubleshooting tips:");
            _logger.LogInformation("1. Check SMTP server address and port");
            _logger.LogInformation("2. Verify username and password");
            _logger.LogInformation("3. Ensure SSL/TLS settings are correct");
            _logger.LogInformation("4. Check firewall and network connectivity");
            _logger.LogInformation("5. Verify email provider allows SMTP access");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SMTP diagnostics failed");
    }
}
```

#### 2. Template Rendering Issues
```csharp
public async Task DiagnoseTemplateIssuesAsync(string templateName, object sampleData)
{
    try
    {
        // Validate template syntax
        var template = await _templateService.GetTemplateAsync(templateName);
        if (template == null)
        {
            _logger.LogError("Template not found: {TemplateName}", templateName);
            return;
        }
        
        var syntaxResult = await _templateService.ValidateTemplateSyntax(template.Body);
        if (!syntaxResult.IsSuccess)
        {
            _logger.LogError("Template syntax errors: {Errors}", string.Join(", ", syntaxResult.Errors));
            return;
        }
        
        // Test rendering
        var renderResult = await _templateService.RenderTemplateAsync(templateName, sampleData);
        if (!renderResult.IsSuccess)
        {
            _logger.LogError("Template rendering failed: {Errors}", string.Join(", ", renderResult.Errors));
            
            // Check for common issues
            var variables = await _templateService.ExtractTemplateVariables(template.Body);
            var dataProperties = GetObjectProperties(sampleData);
            
            var missingVariables = variables
                .Where(v => !dataProperties.Contains(v.Name, StringComparer.OrdinalIgnoreCase))
                .Select(v => v.Name)
                .ToList();
                
            if (missingVariables.Any())
            {
                _logger.LogWarning("Missing template variables in data: {MissingVariables}", 
                    string.Join(", ", missingVariables));
            }
        }
        else
        {
            _logger.LogInformation("Template rendered successfully: {TemplateName}", templateName);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Template diagnostics failed for {TemplateName}", templateName);
    }
}
```

#### 3. Performance Issues
```csharp
public async Task DiagnosePerformanceIssuesAsync()
{
    var diagnostics = new Dictionary<string, object>();
    
    // Test single email send
    var singleEmailStopwatch = Stopwatch.StartNew();
    var testMessage = new EmailMessage
    {
        To = "test@example.com",
        Subject = "Performance Test",
        Body = "Test email for performance diagnostics"
    };
    
    try
    {
        await _emailService.SendAsync(testMessage);
        singleEmailStopwatch.Stop();
        diagnostics["single_email_ms"] = singleEmailStopwatch.ElapsedMilliseconds;
    }
    catch (Exception ex)
    {
        diagnostics["single_email_error"] = ex.Message;
    }
    
    // Test template rendering
    var templateStopwatch = Stopwatch.StartNew();
    try
    {
        await _templateService.RenderTemplateContentAsync(
            "Test template: {{test}}", 
            new { test = "value" });
        templateStopwatch.Stop();
        diagnostics["template_render_ms"] = templateStopwatch.ElapsedMilliseconds;
    }
    catch (Exception ex)
    {
        diagnostics["template_render_error"] = ex.Message;
    }
    
    // Log recommendations
    _logger.LogInformation("Performance Diagnostics: {@Diagnostics}", diagnostics);
    
    if (diagnostics.TryGetValue("single_email_ms", out var emailMs) && (long)emailMs > 5000)
    {
        _logger.LogWarning("Single email send took {ElapsedMs}ms. Consider optimizing SMTP connection or using connection pooling", emailMs);
    }
    
    if (diagnostics.TryGetValue("template_render_ms", out var templateMs) && (long)templateMs > 1000)
    {
        _logger.LogWarning("Template rendering took {ElapsedMs}ms. Consider enabling template caching", templateMs);
    }
}
```

## 📝 Testing Strategy

### 1. Unit Tests
```csharp
[Test]
public async Task EmailService_SendAsync_ShouldReturnSuccess_WhenValidMessage()
{
    // Arrange
    var mockFluentEmail = new Mock<IFluentEmail>();
    var mockResponse = Mock.Of<ISendResponse>(r => r.Successful == true && r.MessageId == "test-123");
    
    mockFluentEmail.Setup(x => x.To(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(mockFluentEmail.Object);
    mockFluentEmail.Setup(x => x.Subject(It.IsAny<string>()))
              .Returns(mockFluentEmail.Object);
    mockFluentEmail.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>()))
              .Returns(mockFluentEmail.Object);
    mockFluentEmail.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(mockResponse);
    
    var emailService = new EmailService(mockFluentEmail.Object, Mock.Of<IEmailTemplateService>(), 
        Options.Create(new EmailConfiguration()), Mock.Of<ILogger<EmailService>>());
    
    var message = EmailMessage.Create("test@example.com", "Test Subject", "Test Body");
    
    // Act
    var result = await emailService.SendAsync(message);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("test-123", result.MessageId);
    mockFluentEmail.Verify(x => x.SendAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

### 2. Integration Tests
```csharp
[Test]
public async Task EmailService_IntegrationTest_ShouldSendActualEmail()
{
    // This test requires actual SMTP configuration
    // Use test SMTP server like MailHog or smtp4dev for integration testing
    
    var services = new ServiceCollection();
    services.AddEmailService(options =>
    {
        options.Smtp.Host = "localhost";
        options.Smtp.Port = 1025; // MailHog default port
        options.Smtp.EnableSsl = false;
    });
    
    var provider = services.BuildServiceProvider();
    var emailService = provider.GetRequiredService<IEmailService>();
    
    var message = EmailMessage.Create(
        "test@example.com", 
        "Integration Test", 
        "This is an integration test email");
    
    var result = await emailService.SendAsync(message);
    
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.MessageId);
}
```

## 📄 Lisans

Bu proje Enterprise Platform Team tarafından geliştirilmiştir.

## 📞 Destek

- **Dokümantasyon**: Bu README dosyası
- **Issue Tracking**: Internal issue tracking system
- **Email**: enterprise-platform@company.com

---

**🎉 Enterprise.Shared.Email ile profesyonel, ölçeklenebilir ve güvenilir email sistemlerinizi oluşturun!** 📧