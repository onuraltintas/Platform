# EgitimPlatform.Shared.Email

Bu kütüphane, eğitim platformu için kapsamlı email gönderim, şablon yönetimi, kuyruk sistemi ve doğrulama hizmetlerini sağlar.

## Özellikler

- **SMTP Email Gönderimi**: MailKit kullanarak güvenli email gönderimi
- **Template Engine**: Handlebars.NET ile dinamik email şablonları
- **Email Kuyruğu**: Asenkron email işleme ve yeniden deneme mekanizması
- **Email Doğrulama**: Email adresi format ve domain doğrulama
- **Bulk Email**: Toplu email gönderimi ve throttling
- **Attachment Desteği**: Dosya ekleri ve inline resimler
- **Tracking**: Email açılma ve tıklama takibi
- **Resilience**: Polly ile hata yönetimi ve retry politikaları

## Kurulum

```xml
<PackageReference Include="EgitimPlatform.Shared.Email" Version="1.0.0" />
```

## Konfigürasyon

### appsettings.json

```json
{
  "Email": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSsl": false,
      "EnableStartTls": true,
      "Domain": "gmail.com",
      "Timeout": 30000
    },
    "Settings": {
      "DefaultFromEmail": "noreply@egitimplatform.com",
      "DefaultFromName": "Eğitim Platform",
      "DefaultReplyToEmail": "support@egitimplatform.com",
      "DefaultReplyToName": "Destek Ekibi",
      "DefaultSubjectPrefix": "[Eğitim Platform] "
    },
    "Templates": {
      "TemplatesPath": "EmailTemplates",
      "DefaultLayout": "layout",
      "CacheTemplates": true,
      "CacheExpirationMinutes": 60,
      "AllowedTemplates": [],
      "GlobalVariables": {
        "companyName": "Eğitim Platform",
        "supportEmail": "support@egitimplatform.com",
        "baseUrl": "https://egitimplatform.com"
      }
    },
    "Throttling": {
      "MaxEmailsPerHour": 1000,
      "MaxEmailsPerDay": 10000,
      "DelayBetweenEmailsMs": 100,
      "BulkEmailBatchSize": 50
    },
    "Queue": {
      "MaxRetryAttempts": 3,
      "RetryDelayMinutes": 5,
      "ProcessingIntervalSeconds": 30,
      "MaxBatchSize": 100,
      "DeliveryResultRetentionDays": 7
    },
    "Validation": {
      "ValidateDomains": true,
      "BlockDisposableEmails": true,
      "ValidationLevel": "Full",
      "BlockedDomains": [
        "tempmail.org",
        "10minutemail.com"
      ],
      "DisposableEmailApiUrl": "https://api.example.com/check-disposable/{domain}"
    },
    "Security": {
      "RequireSSL": true,
      "AllowSelfSignedCertificates": false,
      "MaxAttachmentSizeMB": 25,
      "AllowedAttachmentTypes": [
        ".pdf", ".doc", ".docx", ".txt", ".jpg", ".png"
      ]
    }
  }
}
```

### Dependency Injection

```csharp
// Program.cs
builder.Services.AddEmailServices(builder.Configuration);

// Veya kısmi servisler
builder.Services.AddSmtpEmailService(builder.Configuration);
builder.Services.AddEmailTemplateService(builder.Configuration);
builder.Services.AddEmailValidationService(builder.Configuration);
builder.Services.AddEmailQueueService(builder.Configuration);
```

## Kullanım

### 1. Basit Email Gönderimi

```csharp
public class UserController : ControllerBase
{
    private readonly IEmailService _emailService;

    public UserController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-welcome")]
    public async Task<IActionResult> SendWelcomeEmail(string email, string userName)
    {
        var result = await _emailService.SendEmailAsync(
            email,
            "Hoş Geldiniz!",
            $"<h1>Merhaba {userName}!</h1><p>Platformumuza hoş geldiniz.</p>",
            isHtml: true
        );

        return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
    }
}
```

### 2. Template ile Email Gönderimi

```html
<!-- EmailTemplates/welcome.hbs -->
<!DOCTYPE html>
<html>
<head>
    <title>{{subject}}</title>
</head>
<body>
    <h1>Merhaba {{userName}}!</h1>
    <p>{{companyName}} platformuna hoş geldiniz.</p>
    <p>Kayıt tarihiniz: {{formatDate registrationDate "dd/MM/yyyy"}}</p>
    <a href="{{url 'dashboard'}}">Dashboard'a Git</a>
</body>
</html>
```

```csharp
var templateData = new
{
    userName = "Ahmet Yılmaz",
    registrationDate = DateTime.Now,
    subject = "Hoş Geldiniz"
};

var result = await _emailService.SendTemplateEmailAsync(
    "ahmet@example.com",
    "welcome",
    templateData,
    "Hoş Geldiniz!"
);
```

### 3. Fluent API ile Email Oluşturma

```csharp
var message = new EmailMessage()
    .AddTo("user@example.com", "Kullanıcı Adı")
    .AddCc("manager@example.com")
    .SetFrom("noreply@egitimplatform.com", "Eğitim Platform")
    .SetTemplate("course-notification", new { courseName = "C# Programlama" })
    .SetPriority(EmailPriority.High)
    .SetCategory("course-notifications")
    .AddTag("automated")
    .EnableTracking(trackOpens: true, trackClicks: true)
    .AddAttachment("course-guide.pdf", pdfBytes, "application/pdf");

var result = await _emailService.SendEmailAsync(message);
```

### 4. Bulk Email Gönderimi

```csharp
var messages = new List<EmailMessage>();

foreach (var user in users)
{
    var message = new EmailMessage()
        .AddTo(user.Email, user.FullName)
        .SetTemplate("newsletter", new { userName = user.FirstName })
        .SetCategory("newsletter");
    
    messages.Add(message);
}

var bulkResult = await _emailService.SendBulkEmailAsync(messages);
Console.WriteLine($"Başarılı: {bulkResult.SuccessfulDeliveries}, Başarısız: {bulkResult.FailedDeliveries}");
```

### 5. Email Kuyruğu Kullanımı

```csharp
public class NotificationService
{
    private readonly IEmailQueueService _emailQueue;

    public NotificationService(IEmailQueueService emailQueue)
    {
        _emailQueue = emailQueue;
    }

    public async Task SendDelayedNotification(string email, DateTime sendAt)
    {
        var message = new EmailMessage()
            .AddTo(email)
            .SetTemplate("reminder", new { })
            .ScheduleFor(sendAt);

        var queueId = await _emailQueue.QueueEmailAsync(message);
        
        // Daha sonra durumunu kontrol et
        var status = await _emailQueue.GetDeliveryStatusAsync(message.Id);
    }
}
```

### 6. Email Doğrulama

```csharp
public class ValidationController : ControllerBase
{
    private readonly IEmailValidationService _validation;

    public ValidationController(IEmailValidationService validation)
    {
        _validation = validation;
    }

    [HttpPost("validate-email")]
    public async Task<IActionResult> ValidateEmail(string email)
    {
        var result = await _validation.ValidateEmailAsync(email);
        
        return Ok(new
        {
            isValid = result.IsValid,
            isDeliverable = result.IsDeliverable,
            isDisposable = result.IsDisposable,
            domain = result.Domain,
            errors = result.ValidationErrors
        });
    }

    [HttpPost("validate-bulk")]
    public async Task<IActionResult> ValidateBulkEmails(List<string> emails)
    {
        var results = await _validation.ValidateEmailsAsync(emails);
        return Ok(results);
    }
}
```

### 7. Template Helpers

Handlebars template'lerinde kullanabileceğiniz helper'lar:

```html
<!-- Tarih formatlama -->
{{formatDate dateValue "dd/MM/yyyy HH:mm"}}

<!-- Para birimi formatlama -->
{{formatCurrency priceValue "tr-TR"}}

<!-- Koşullu içerik -->
{{#if_equals userType "premium"}}
    <p>Premium kullanıcı avantajları...</p>
{{else}}
    <p>Ücretsiz kullanıcı bilgileri...</p>
{{/if_equals}}

<!-- URL oluşturma -->
<a href="{{url 'courses/123'}}">Kursa Git</a>

<!-- Metin kısaltma -->
{{truncate longDescription 100}}

<!-- Matematik işlemleri -->
Sayfa {{add @index 1}} / {{totalPages}}
```

## Monitoring ve Logging

Email servisler otomatik olarak OpenTelemetry metrikleri ve Serilog ile detaylı loglama sağlar:

```csharp
// Metrics
- email_sent_total
- email_failed_total
- email_processing_duration
- email_queue_size

// Logs
- Email gönderim durumları
- Template render işlemleri
- Kuyruk işleme bilgileri
- Hata detayları
```

## Resilience

Polly ile otomatik retry, circuit breaker ve timeout politikaları:

```csharp
// Otomatik retry (3 deneme)
// Circuit breaker (5 başarısız işlem sonrası devre açılır)
// Timeout (30 saniye)
// Bulkhead isolation (eş zamanlı işlem sınırları)
```

## Test

```csharp
// Connection testi
var isConnected = await _emailService.TestConnectionAsync();

// Template varlık kontrolü
var templateExists = await _templateService.TemplateExistsAsync("welcome");

// Email format doğrulama
var isValidFormat = await _validation.IsValidEmailAsync("test@example.com");
```

## Güvenlik

- SSL/TLS şifreleme zorunlu
- Attachment boyut ve tip kısıtlamaları
- Template path traversal koruması
- Disposable email domain engelleme
- Rate limiting ve throttling