# Enterprise.Shared.Privacy

## Proje Hakkında

Enterprise.Shared.Privacy, kurumsal uygulamalar için kapsamlı veri gizliliği ve güvenlik çözümleri sunan modern bir .NET 8.0 kütüphanesidir. Bu sistem, GDPR, KVKK ve diğer uluslararası veri koruma düzenlemelerine uyum sağlamak için gerekli tüm araçları ve servisleri bir araya getirir.

## Temel Özellikler

### 🔒 Veri Anonimleştirme (Data Anonymization)
- **Çoklu Anonimleştirme Yöntemleri**: Maskeleme, pseudonymization, şifreleme ve tam anonimleştirme
- **Akıllı Veri Maskeleme**: E-mail, telefon, kredi kartı gibi özel veri türleri için özelleştirilmiş maskeleme
- **Geri Dönüşümlü/Geri Dönüşümsüz**: Şifreleme ile geri dönüşümlü, hash ile geri dönüşümsüz anonimleştirme
- **Toplu İşlem Desteği**: Büyük veri setlerini verimli şekilde anonimleştirme
- **Performans Optimizasyonu**: Async/await pattern ile yüksek performans

### ✅ İzin Yönetimi (Consent Management)
- **Granüler İzin Kontrolü**: Farklı amaçlar için ayrı izin yönetimi
- **İzin Geçmişi**: Kullanıcı izin değişikliklerinin tam denetimi
- **Otomatik Süre Yönetimi**: İzinlerin otomatik süresi dolması ve yenileme
- **Çoklu Hukuki Dayanak**: GDPR madde 6 kapsamında farklı hukuki dayanaklar
- **Esnek İzin Kapsamı**: Çok seviyeli izin kapsamı tanımlama

### 🗃️ Veri Saklama Yönetimi (Data Retention)
- **Otomatik Veri Silme**: Belirlenen sürelere göre otomatik veri silme
- **Kategori Bazlı Saklama**: Veri türüne göre farklı saklama süreleri
- **Arşivleme Desteği**: Silmeden önce güvenli arşivleme
- **Silme Öncesi Bildirim**: Veri sahiplerine silme öncesi bilgilendirme
- **Yasal Saklama Gereksinimleri**: Yasal zorunluluklar için esnek saklama kuralları

### 📋 GDPR Uyumluluk (GDPR Compliance)
- **Veri Sahibi Hakları**: Erişim, düzeltme, silme, taşınabilirlik hakları
- **Otomatik Uyumluluk Kontrolleri**: GDPR maddelerine göre otomatik kontrol
- **Veri Koruma Etkisi Değerlendirmesi**: Yeni projeler için otomatik DPIA
- **Yasal Dayanak Yönetimi**: 6 farklı yasal dayanak desteği
- **Süre Takibi**: 30 günlük yanıtlama süresi otomatik takibi

### 📊 Denetim ve Loglama (Privacy Audit)
- **Kapsamlı Denetim İzi**: Tüm veri işleme aktivitelerinin detaylı logları
- **Yapısal Loglama**: JSON formatında strukturlu log kayıtları
- **Gerçek Zamanlı İzleme**: Anlık veri erişim ve değişiklik takibi
- **Güvenlik Olayları**: Veri ihlali ve şüpheli aktivite tespiti
- **Uyumluluk Raporları**: Periyodik uyumluluk durumu raporları

## Kullanılan Teknolojiler

### 🏗️ .NET Ekosistemi
- **.NET 8.0**: Modern C# özellikleri ve üstün performans
- **Microsoft.Extensions.DependencyInjection**: Bağımlılık enjeksiyonu
- **Microsoft.Extensions.Logging**: Yapılandırılabilir loglama sistemi
- **Microsoft.Extensions.Options**: Tip güvenli konfigürasyon yönetimi
- **Microsoft.Extensions.HealthChecks**: Sistem sağlığı kontrolü

### 🛡️ Güvenlik Teknolojileri
- **System.Security.Cryptography**: Modern şifreleme algoritmaları
  - AES-256 şifreleme
  - PBKDF2 ile güvenli hash oluşturma
  - SHA-256 hash algoritması
- **System.Text.Json**: Yüksek performanslı JSON işleme
- **EntityFrameworkCore**: Güvenli veri erişim katmanı

### 📄 Uyumluluk Framework'leri
- **GDPR Article Implementation**: GDPR maddelerinin doğrudan implementasyonu
- **KVKK Compliance**: Türk mevzuatına uyum desteği
- **ISO 27001**: Bilgi güvenliği standartları uyumluluğu

## Kurulum ve Kullanım

### 1. Proje Referansı
```xml
<ProjectReference Include="../Enterprise.Shared.Privacy/Enterprise.Shared.Privacy.csproj" />
```

### 2. Servis Kaydı
```csharp
// Program.cs veya Startup.cs'de

// Tüm privacy servisleri
services.AddPrivacy(configuration);

// Sadece belirli servisler
services.AddDataAnonymization()
        .AddConsentManagement()
        .AddPrivacyAudit();

// Lambda ile konfigürasyon
services.AddPrivacy(settings =>
{
    settings.Anonymization.EnableAnonymization = true;
    settings.ConsentManagement.ConsentExpirationDays = 365;
    settings.GdprCompliance.EnableGdprCompliance = true;
});
```

### 3. Konfigürasyon (appsettings.json)
```json
{
  "Privacy": {
    "Anonymization": {
      "EnableAnonymization": true,
      "HashingSalt": "your-secure-salt-here",
      "HashingIterations": 10000,
      "EncryptionKey": "your-base64-encryption-key",
      "EnableDataMasking": true,
      "EnablePseudonymization": true
    },
    "ConsentManagement": {
      "EnableConsentTracking": true,
      "ConsentExpirationDays": 365,
      "RequireExplicitConsent": true,
      "EnableConsentWithdrawal": true,
      "EnableConsentHistory": true,
      "SupportedPurposes": [
        "Marketing",
        "Analytics",
        "Essential",
        "Functional"
      ]
    },
    "DataRetention": {
      "EnableAutomaticDeletion": true,
      "DefaultRetentionDays": 2555,
      "CategoryRetentionPeriods": {
        "Personal": 1095,
        "Financial": 2555,
        "Health": 3650,
        "Marketing": 730
      },
      "EnableDataArchiving": true,
      "NotifyBeforeDeletion": true
    },
    "GdprCompliance": {
      "EnableGdprCompliance": true,
      "DataControllerName": "Your Company Name",
      "DataProtectionOfficerEmail": "dpo@yourcompany.com",
      "LegalBasis": "Consent",
      "EnableRightOfAccess": true,
      "EnableRightOfRectification": true,
      "EnableRightOfErasure": true,
      "EnableDataPortability": true,
      "ResponseTimeLimit": 30
    },
    "AuditLogging": {
      "EnableAuditLogging": true,
      "LogDataAccess": true,
      "LogDataModification": true,
      "LogConsentChanges": true,
      "LogDataDeletion": true,
      "AuditLogRetentionDays": 2555,
      "EnableStructuredLogging": true
    }
  }
}
```

## Servis Kullanımı

### 1. Veri Anonimleştirme Servisi
```csharp
public class UserService
{
    private readonly IDataAnonymizationService _anonymizationService;
    
    public UserService(IDataAnonymizationService anonymizationService)
    {
        _anonymizationService = anonymizationService;
    }
    
    // Temel anonimleştirme
    public async Task<string> AnonymizeEmailAsync(string email)
    {
        return await _anonymizationService.AnonymizeAsync(
            email, 
            AnonymizationLevel.Masked
        );
        // Sonuç: "j***@example.com"
    }
    
    // Hassas veri maskeleme
    public async Task<string> MaskCreditCardAsync(string cardNumber)
    {
        return await _anonymizationService.MaskDataAsync(
            cardNumber, 
            DataCategory.Financial
        );
        // Sonuç: "****-****-****-1234"
    }
    
    // Geri dönüşümlü pseudonymization
    public async Task<string> PseudonymizeUserDataAsync(string personalData, string userId)
    {
        return await _anonymizationService.PseudonymizeAsync(personalData, userId);
        // Sonuç: "USER_A1B2C3D4"
    }
    
    // Kişisel veri kaydının tam anonimleştirmesi
    public async Task<PersonalDataRecord> AnonymizePersonalRecordAsync(
        PersonalDataRecord record)
    {
        return await _anonymizationService.AnonymizePersonalDataRecordAsync(
            record, 
            AnonymizationLevel.Anonymized
        );
    }
    
    // Toplu anonimleştirme
    public async Task<PersonalDataRecord[]> BulkAnonymizeAsync(
        PersonalDataRecord[] records, 
        AnonymizationLevel level)
    {
        return await _anonymizationService.BulkAnonymizeAsync(records, level);
    }
    
    // Anonimleştirme istatistikleri
    public async Task<Dictionary<string, object>> GetAnonymizationStatsAsync()
    {
        return await _anonymizationService.GetAnonymizationStatisticsAsync();
    }
}
```

### 2. İzin Yönetimi Servisi
```csharp
public class ConsentController : ControllerBase
{
    private readonly IConsentManagementService _consentService;
    
    // İzin talebi oluşturma
    [HttpPost("consent/request")]
    public async Task<ActionResult<ConsentRecord>> RequestConsentAsync(
        [FromBody] ConsentRequest request)
    {
        var consent = new ConsentRecord
        {
            UserId = request.UserId,
            Purpose = ConsentPurpose.Marketing,
            LegalBasis = LegalBasis.Consent,
            Status = ConsentStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(365),
            Scope = new[] { "email", "profile", "preferences" },
            Version = "1.0"
        };
        
        var result = await _consentService.RequestConsentAsync(consent);
        return Ok(result);
    }
    
    // İzin verme
    [HttpPost("consent/{consentId}/grant")]
    public async Task<ActionResult> GrantConsentAsync(Guid consentId)
    {
        await _consentService.GrantConsentAsync(consentId);
        return Ok(new { Message = "Consent granted successfully" });
    }
    
    // İzin geri çekme
    [HttpPost("consent/{consentId}/withdraw")]
    public async Task<ActionResult> WithdrawConsentAsync(Guid consentId)
    {
        await _consentService.WithdrawConsentAsync(consentId);
        return Ok(new { Message = "Consent withdrawn successfully" });
    }
    
    // İzin durumu kontrol
    [HttpGet("consent/{userId}/{purpose}")]
    public async Task<ActionResult<bool>> CheckConsentAsync(string userId, string purpose)
    {
        var hasConsent = await _consentService.HasValidConsentAsync(userId, purpose);
        return Ok(new { HasConsent = hasConsent });
    }
    
    // İzin geçmişi
    [HttpGet("consent/{userId}/history")]
    public async Task<ActionResult<ConsentRecord[]>> GetConsentHistoryAsync(string userId)
    {
        var history = await _consentService.GetConsentHistoryAsync(userId);
        return Ok(history);
    }
    
    // Toplu izin güncelleme
    [HttpPost("consent/bulk-update")]
    public async Task<ActionResult> BulkUpdateConsentsAsync(
        [FromBody] BulkConsentUpdateRequest request)
    {
        await _consentService.BulkUpdateConsentsAsync(
            request.UserIds, 
            request.Purpose, 
            request.NewStatus
        );
        return Ok();
    }
}
```

### 3. GDPR Uyumluluk Servisi
```csharp
public class DataSubjectRightsService
{
    private readonly IGdprComplianceService _gdprService;
    private readonly IDataAnonymizationService _anonymizationService;
    
    // Erişim hakkı (Right to Access - GDPR Article 15)
    public async Task<GdprDataExport> ProcessAccessRequestAsync(string userId)
    {
        var exportData = await _gdprService.ExportPersonalDataAsync(userId);
        
        return new GdprDataExport
        {
            UserId = userId,
            ExportedAt = DateTime.UtcNow,
            Format = ExportFormat.JSON,
            Data = exportData,
            Categories = new[]
            {
                DataCategory.Personal,
                DataCategory.Behavioral,
                DataCategory.Technical
            },
            IsEncrypted = true,
            RetentionPeriod = TimeSpan.FromDays(30)
        };
    }
    
    // Silme hakkı (Right to Erasure - GDPR Article 17)
    public async Task<GdprErasureResult> ProcessErasureRequestAsync(
        string userId, 
        ErasureRequest request)
    {
        var result = new GdprErasureResult
        {
            UserId = userId,
            RequestId = request.Id,
            ProcessedAt = DateTime.UtcNow
        };
        
        // Yasal saklama gereksinimlerini kontrol et
        var retentionCheck = await _gdprService.CheckRetentionRequirementsAsync(
            userId, 
            request.Categories
        );
        
        foreach (var category in request.Categories)
        {
            if (retentionCheck.RequiresRetention(category))
            {
                // Anonimleştir
                await _gdprService.AnonymizeDataAsync(userId, category);
                result.AnonymizedCategories.Add(category);
            }
            else
            {
                // Tamamen sil
                await _gdprService.DeleteDataAsync(userId, category);
                result.DeletedCategories.Add(category);
            }
        }
        
        return result;
    }
    
    // Taşınabilirlik hakkı (Right to Data Portability - GDPR Article 20)
    public async Task<GdprPortabilityExport> ProcessPortabilityRequestAsync(
        string userId, 
        PortabilityRequest request)
    {
        var portableData = await _gdprService.ExtractPortableDataAsync(
            userId, 
            request.Categories,
            request.Format
        );
        
        return new GdprPortabilityExport
        {
            UserId = userId,
            Format = request.Format,
            Data = portableData,
            ExportedAt = DateTime.UtcNow,
            StructuredData = request.Format == ExportFormat.JSON,
            MachineReadable = true
        };
    }
    
    // Düzeltme hakkı (Right to Rectification - GDPR Article 16)
    public async Task<GdprRectificationResult> ProcessRectificationRequestAsync(
        string userId,
        RectificationRequest request)
    {
        var result = new GdprRectificationResult
        {
            UserId = userId,
            RequestId = request.Id,
            ProcessedAt = DateTime.UtcNow
        };
        
        foreach (var correction in request.Corrections)
        {
            var updated = await _gdprService.UpdatePersonalDataAsync(
                userId,
                correction.Field,
                correction.OldValue,
                correction.NewValue
            );
            
            result.UpdatedFields.Add(new FieldUpdate
            {
                Field = correction.Field,
                OldValue = correction.OldValue,
                NewValue = correction.NewValue,
                UpdatedAt = DateTime.UtcNow,
                Success = updated
            });
        }
        
        return result;
    }
}
```

### 4. Privacy Audit Servisi
```csharp
public class DataAccessService
{
    private readonly IPrivacyAuditService _auditService;
    
    // Veri erişimi denetimi
    public async Task<UserData> GetUserDataAsync(string userId, string requesterId)
    {
        // Audit log oluştur
        await _auditService.LogDataAccessAsync(new DataAccessEvent
        {
            UserId = userId,
            AccessedBy = requesterId,
            EventType = AuditEventType.DataAccess,
            Timestamp = DateTime.UtcNow,
            DataCategory = DataCategory.Personal,
            Purpose = "User profile display",
            LegalBasis = LegalBasis.Consent,
            Details = new Dictionary<string, object>
            {
                ["RequestType"] = "Profile View",
                ["IPAddress"] = GetClientIP(),
                ["UserAgent"] = GetUserAgent()
            }
        });
        
        return await GetUserDataFromDatabase(userId);
    }
    
    // Veri değişikliği denetimi
    public async Task UpdateUserDataAsync(string userId, UserUpdateRequest request)
    {
        var oldData = await GetUserDataFromDatabase(userId);
        
        // Güncelleme yap
        await UpdateUserDataInDatabase(userId, request);
        
        var newData = await GetUserDataFromDatabase(userId);
        
        // Audit log oluştur
        await _auditService.LogDataModificationAsync(new DataModificationEvent
        {
            UserId = userId,
            ModifiedBy = GetCurrentUserId(),
            EventType = AuditEventType.DataModified,
            Timestamp = DateTime.UtcNow,
            OldValue = JsonSerializer.Serialize(oldData),
            NewValue = JsonSerializer.Serialize(newData),
            ModifiedFields = GetChangedFields(oldData, newData),
            Reason = request.Reason,
            LegalBasis = LegalBasis.LegitimateInterests
        });
    }
    
    // İzin değişikliği denetimi
    public async Task UpdateConsentAsync(string userId, ConsentPurpose purpose, bool granted)
    {
        await _auditService.LogConsentChangeAsync(new ConsentChangeEvent
        {
            UserId = userId,
            Purpose = purpose,
            PreviousStatus = await GetCurrentConsentStatus(userId, purpose),
            NewStatus = granted ? ConsentStatus.Granted : ConsentStatus.Withdrawn,
            Timestamp = DateTime.UtcNow,
            Source = "User Portal",
            IPAddress = GetClientIP()
        });
    }
    
    // Güvenlik olayı denetimi
    public async Task LogSecurityEventAsync(string userId, SecurityEvent securityEvent)
    {
        await _auditService.LogSecurityEventAsync(new SecurityAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.SecurityBreach,
            Severity = securityEvent.Severity,
            Description = securityEvent.Description,
            Timestamp = DateTime.UtcNow,
            Source = securityEvent.Source,
            Details = securityEvent.Details,
            RequiresNotification = securityEvent.Severity >= SecuritySeverity.High
        });
        
        // Yüksek seviye güvenlik olayları için otomatik bildirim
        if (securityEvent.Severity >= SecuritySeverity.High)
        {
            await NotifySecurityTeamAsync(securityEvent);
        }
    }
}
```

## Kombine Kullanım Senaryoları

### 1. Kapsamlı Kullanıcı Veri Yönetimi
```csharp
public class ComprehensiveUserDataService
{
    private readonly IDataAnonymizationService _anonymizationService;
    private readonly IConsentManagementService _consentService;
    private readonly IGdprComplianceService _gdprService;
    private readonly IPrivacyAuditService _auditService;
    
    public async Task<ProcessingResult> ProcessUserRegistrationAsync(
        UserRegistrationData userData)
    {
        var result = new ProcessingResult();
        
        try
        {
            // 1. Önce veri kategorilerini belirle
            var dataCategories = ClassifyUserData(userData);
            
            // 2. Gerekli izinleri kontrol et
            var requiredConsents = GetRequiredConsents(dataCategories);
            
            // 3. Her veri kategorisi için izin al
            foreach (var consent in requiredConsents)
            {
                await _consentService.RequestConsentAsync(consent);
            }
            
            // 4. Hassas verileri anonimleştir
            if (dataCategories.Contains(DataCategory.Sensitive))
            {
                userData.SensitiveData = await _anonymizationService.AnonymizeAsync(
                    userData.SensitiveData,
                    AnonymizationLevel.Pseudonymized
                );
            }
            
            // 5. Veri işleme aktivitesini denetle
            await _auditService.LogDataProcessingAsync(new DataProcessingEvent
            {
                UserId = userData.UserId,
                ProcessingType = "Registration",
                DataCategories = dataCategories,
                LegalBasis = LegalBasis.Consent,
                Purpose = "User account creation",
                Timestamp = DateTime.UtcNow
            });
            
            // 6. GDPR compliance check
            var complianceCheck = await _gdprService.ValidateProcessingAsync(userData);
            if (!complianceCheck.IsCompliant)
            {
                throw new ComplianceViolationException(complianceCheck.Violations);
            }
            
            result.Success = true;
            result.ProcessedData = userData;
            
        }
        catch (Exception ex)
        {
            await _auditService.LogErrorAsync(new ErrorEvent
            {
                UserId = userData.UserId,
                EventType = AuditEventType.PolicyViolation,
                Error = ex,
                Timestamp = DateTime.UtcNow
            });
            
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
}
```

### 2. Otomatik Veri Yaşam Döngüsü Yönetimi
```csharp
public class DataLifecycleService : BackgroundService
{
    private readonly IDataRetentionService _retentionService;
    private readonly IDataAnonymizationService _anonymizationService;
    private readonly IPrivacyAuditService _auditService;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Günlük veri yaşam döngüsü kontrolü
                await ProcessDataLifecycleAsync();
                
                // 24 saat bekle
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                // Log error and continue
                await _auditService.LogErrorAsync(new ErrorEvent
                {
                    EventType = AuditEventType.PolicyViolation,
                    Error = ex,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
    
    private async Task ProcessDataLifecycleAsync()
    {
        // 1. Süresi dolan verileri bul
        var expiredData = await _retentionService.FindExpiredDataAsync();
        
        foreach (var data in expiredData)
        {
            // 2. Yasal saklama gereksinimlerini kontrol et
            var retentionCheck = await _retentionService.CheckLegalRetentionAsync(data);
            
            if (retentionCheck.MustRetain)
            {
                // Anonimleştir ama sakla
                await _anonymizationService.AnonymizePersonalDataRecordAsync(
                    data, 
                    AnonymizationLevel.Anonymized
                );
                
                await _auditService.LogDataProcessingAsync(new DataProcessingEvent
                {
                    UserId = data.UserId,
                    ProcessingType = "Anonymization",
                    Reason = retentionCheck.Reason,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                // Tamamen sil
                await _retentionService.DeleteDataAsync(data);
                
                await _auditService.LogDataDeletionAsync(new DataDeletionEvent
                {
                    UserId = data.UserId,
                    DataId = data.Id,
                    DeletionType = "Automatic",
                    Reason = "Retention period expired",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        // 3. Süresi dolmak üzere olan veriler için kullanıcıları uyar
        var expiringSoon = await _retentionService.FindDataExpiringSoonAsync(
            TimeSpan.FromDays(30)
        );
        
        foreach (var data in expiringSoon)
        {
            await NotifyUserOfPendingDeletionAsync(data);
        }
    }
}
```

## API Controller Entegrasyonu

### Privacy Management Controller
```csharp
[ApiController]
[Route("api/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IDataAnonymizationService _anonymizationService;
    private readonly IConsentManagementService _consentService;
    private readonly IGdprComplianceService _gdprService;
    private readonly IPrivacyAuditService _auditService;
    
    // Veri anonimleştirme endpoint'i
    [HttpPost("anonymize")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnonymizationResult>> AnonymizeDataAsync(
        [FromBody] AnonymizationRequest request)
    {
        var result = await _anonymizationService.AnonymizeAsync(
            request.Data,
            request.Level
        );
        
        return Ok(new AnonymizationResult
        {
            OriginalData = request.Data,
            AnonymizedData = result,
            Level = request.Level,
            ProcessedAt = DateTime.UtcNow,
            IsReversible = _anonymizationService.CanReverseAnonymization(request.Level)
        });
    }
    
    // İzin yönetimi endpoint'leri
    [HttpPost("consent")]
    public async Task<ActionResult<ConsentRecord>> CreateConsentAsync(
        [FromBody] ConsentRequest request)
    {
        var consent = await _consentService.RequestConsentAsync(
            MapToConsentRecord(request)
        );
        
        return CreatedAtAction(nameof(GetConsent), new { id = consent.Id }, consent);
    }
    
    [HttpGet("consent/{id}")]
    public async Task<ActionResult<ConsentRecord>> GetConsent(Guid id)
    {
        var consent = await _consentService.GetConsentByIdAsync(id);
        if (consent == null)
            return NotFound();
        
        return Ok(consent);
    }
    
    [HttpPost("consent/{id}/grant")]
    public async Task<ActionResult> GrantConsent(Guid id)
    {
        await _consentService.GrantConsentAsync(id);
        return NoContent();
    }
    
    [HttpPost("consent/{id}/withdraw")]
    public async Task<ActionResult> WithdrawConsent(Guid id)
    {
        await _consentService.WithdrawConsentAsync(id);
        return NoContent();
    }
    
    // GDPR Data Subject Rights
    [HttpPost("gdpr/access-request")]
    public async Task<ActionResult<GdprDataExport>> RequestDataAccessAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User ID not found");
        
        var exportData = await _gdprService.ExportPersonalDataAsync(userId);
        
        // Audit log
        await _auditService.LogUserRightExercisedAsync(new UserRightEvent
        {
            UserId = userId,
            Right = DataSubjectRight.Access,
            Timestamp = DateTime.UtcNow,
            Source = "API"
        });
        
        return Ok(exportData);
    }
    
    [HttpPost("gdpr/erasure-request")]
    public async Task<ActionResult<GdprErasureResult>> RequestDataErasureAsync(
        [FromBody] ErasureRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User ID not found");
        
        var result = await _gdprService.ProcessErasureRequestAsync(userId, request);
        
        // Audit log
        await _auditService.LogUserRightExercisedAsync(new UserRightEvent
        {
            UserId = userId,
            Right = DataSubjectRight.Erasure,
            Timestamp = DateTime.UtcNow,
            Details = request.Categories
        });
        
        return Ok(result);
    }
    
    // Privacy dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<PrivacyDashboard>> GetPrivacyDashboardAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User ID not found");
        
        var dashboard = new PrivacyDashboard
        {
            UserId = userId,
            Consents = await _consentService.GetUserConsentsAsync(userId),
            DataCategories = await _gdprService.GetUserDataCategoriesAsync(userId),
            LastAccessed = await _auditService.GetLastDataAccessAsync(userId),
            RetentionStatus = await GetDataRetentionStatusAsync(userId),
            PrivacyScore = await CalculatePrivacyScoreAsync(userId)
        };
        
        return Ok(dashboard);
    }
}
```

## Middleware Entegrasyonu

### Privacy Enforcement Middleware
```csharp
public class PrivacyEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConsentManagementService _consentService;
    private readonly IPrivacyAuditService _auditService;
    private readonly ILogger<PrivacyEnforcementMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Privacy headers ekle
            context.Response.Headers.Add("X-Privacy-Policy", "/privacy-policy");
            context.Response.Headers.Add("X-Data-Controller", "Enterprise Platform");
            context.Response.Headers.Add("X-DPO-Contact", "dpo@enterprise.com");
            
            // Endpoint'in privacy gereksinimlerini kontrol et
            var endpoint = context.GetEndpoint();
            var privacyRequirement = endpoint?.Metadata.GetMetadata<RequireConsentAttribute>();
            
            if (privacyRequirement != null)
            {
                // İzin kontrolü
                var hasConsent = await _consentService.HasValidConsentAsync(
                    userId, 
                    privacyRequirement.Purpose
                );
                
                if (!hasConsent)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Consent required",
                        Purpose = privacyRequirement.Purpose,
                        ConsentUrl = "/api/privacy/consent",
                        Message = "This operation requires user consent for the specified purpose."
                    });
                    return;
                }
                
                // Veri erişimini denetle
                await _auditService.LogDataAccessAsync(new DataAccessEvent
                {
                    UserId = userId,
                    Purpose = privacyRequirement.Purpose,
                    Endpoint = context.Request.Path,
                    Method = context.Request.Method,
                    Timestamp = DateTime.UtcNow,
                    IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString()
                });
            }
        }

        await _next(context);
    }
}

// Custom Attributes
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireConsentAttribute : Attribute
{
    public string Purpose { get; }
    public DataCategory[] DataCategories { get; }
    
    public RequireConsentAttribute(string purpose, params DataCategory[] categories)
    {
        Purpose = purpose;
        DataCategories = categories ?? Array.Empty<DataCategory>();
    }
}

// Kullanım örnekleri
[HttpGet("profile")]
[RequireConsent("ProfileAccess", DataCategory.Personal)]
public async Task<ActionResult<UserProfile>> GetProfileAsync() { }

[HttpGet("analytics")]
[RequireConsent("Analytics", DataCategory.Behavioral, DataCategory.Technical)]
public async Task<ActionResult<AnalyticsData>> GetAnalyticsAsync() { }
```

## Test Desteği

### Test Helper'ları
```csharp
public static class PrivacyTestHelpers
{
    public static Mock<IDataAnonymizationService> CreateMockAnonymizationService()
    {
        var mock = new Mock<IDataAnonymizationService>();
        
        mock.Setup(x => x.AnonymizeAsync(It.IsAny<string>(), It.IsAny<AnonymizationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string data, AnonymizationLevel level, CancellationToken ct) => 
            {
                return level switch
                {
                    AnonymizationLevel.Masked => "***MASKED***",
                    AnonymizationLevel.Hashed => Convert.ToBase64String(Encoding.UTF8.GetBytes(data)),
                    AnonymizationLevel.Pseudonymized => $"USER_{data.GetHashCode():X8}",
                    _ => data
                };
            });
            
        return mock;
    }
    
    public static Mock<IConsentManagementService> CreateMockConsentService()
    {
        var mock = new Mock<IConsentManagementService>();
        
        mock.Setup(x => x.HasValidConsentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
            
        mock.Setup(x => x.RequestConsentAsync(It.IsAny<ConsentRecord>()))
            .ReturnsAsync((ConsentRecord consent) => 
            {
                consent.Id = Guid.NewGuid();
                consent.Status = ConsentStatus.Pending;
                return consent;
            });
            
        return mock;
    }
    
    // Test data factory'leri
    public static ConsentRecord CreateValidConsent(string userId = "test-user")
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = ConsentPurpose.Essential,
            Status = ConsentStatus.Granted,
            LegalBasis = LegalBasis.Consent,
            RequestedAt = DateTime.UtcNow,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(365),
            Version = "1.0",
            Scope = new[] { "profile", "email" }
        };
    }
    
    public static PersonalDataRecord CreateTestDataRecord()
    {
        return new PersonalDataRecord
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            Category = DataCategory.Personal,
            DataType = "Email",
            OriginalValue = "test@example.com",
            ProcessedValue = "test@example.com",
            AnonymizationLevel = AnonymizationLevel.None,
            CreatedAt = DateTime.UtcNow,
            Source = "Test",
            ProcessingStatus = ProcessingStatus.Active
        };
    }
}

// Integration Test örneği
[TestFixture]
public class PrivacyIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    
    [SetUp]
    public void Setup()
    {
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(services =>
            {
                services.AddPrivacy(settings =>
                {
                    settings.Anonymization.EnableAnonymization = true;
                    settings.Anonymization.HashingSalt = "test-salt";
                    settings.Anonymization.EncryptionKey = Convert.ToBase64String(new byte[32]);
                });
            });
            
        _server = new TestServer(hostBuilder);
        _client = _server.CreateClient();
    }
    
    [Test]
    public async Task ConsentManagement_FullWorkflow_ShouldWork()
    {
        // 1. İzin talebi oluştur
        var consentRequest = new ConsentRequest
        {
            UserId = "test-user",
            Purpose = ConsentPurpose.Marketing.ToString(),
            LegalBasis = LegalBasis.Consent.ToString()
        };
        
        var response = await _client.PostAsJsonAsync("/api/privacy/consent", consentRequest);
        response.EnsureSuccessStatusCode();
        
        var consent = await response.Content.ReadFromJsonAsync<ConsentRecord>();
        Assert.That(consent.Status, Is.EqualTo(ConsentStatus.Pending));
        
        // 2. İzni onayla
        var grantResponse = await _client.PostAsync($"/api/privacy/consent/{consent.Id}/grant", null);
        grantResponse.EnsureSuccessStatusCode();
        
        // 3. İzin durumunu kontrol et
        var checkResponse = await _client.GetAsync($"/api/privacy/consent/{consent.Id}");
        checkResponse.EnsureSuccessStatusCode();
        
        var updatedConsent = await checkResponse.Content.ReadFromJsonAsync<ConsentRecord>();
        Assert.That(updatedConsent.Status, Is.EqualTo(ConsentStatus.Granted));
    }
}
```

## Monitoring ve Observability

### Privacy Metrics ve KPIs
```csharp
public class PrivacyMetricsService
{
    private readonly IPrivacyAuditService _auditService;
    private readonly IConsentManagementService _consentService;
    private readonly IGdprComplianceService _gdprService;
    
    public async Task<PrivacyMetrics> GetPrivacyMetricsAsync(DateTime startDate, DateTime endDate)
    {
        return new PrivacyMetrics
        {
            // Consent Metrics
            TotalConsentRequests = await _consentService.GetConsentRequestsCountAsync(startDate, endDate),
            ConsentGrantRate = await _consentService.GetConsentGrantRateAsync(startDate, endDate),
            ConsentWithdrawalRate = await _consentService.GetWithdrawalRateAsync(startDate, endDate),
            ActiveConsents = await _consentService.GetActiveConsentsCountAsync(),
            
            // Data Subject Rights Metrics
            AccessRequests = await _gdprService.GetAccessRequestsCountAsync(startDate, endDate),
            ErasureRequests = await _gdprService.GetErasureRequestsCountAsync(startDate, endDate),
            RectificationRequests = await _gdprService.GetRectificationRequestsCountAsync(startDate, endDate),
            PortabilityRequests = await _gdprService.GetPortabilityRequestsCountAsync(startDate, endDate),
            AverageResponseTime = await _gdprService.GetAverageResponseTimeAsync(startDate, endDate),
            
            // Audit Metrics
            TotalAuditEvents = await _auditService.GetAuditEventsCountAsync(startDate, endDate),
            DataAccessEvents = await _auditService.GetDataAccessCountAsync(startDate, endDate),
            SecurityEvents = await _auditService.GetSecurityEventsCountAsync(startDate, endDate),
            PolicyViolations = await _auditService.GetPolicyViolationsCountAsync(startDate, endDate),
            
            // Anonymization Metrics
            AnonymizedRecords = await GetAnonymizedRecordsCountAsync(startDate, endDate),
            AnonymizationByLevel = await GetAnonymizationByLevelAsync(startDate, endDate),
            
            // Compliance Score
            OverallComplianceScore = await CalculateComplianceScoreAsync(),
            GdprComplianceScore = await _gdprService.GetComplianceScoreAsync(),
            
            // Risk Indicators
            HighRiskDataAccess = await GetHighRiskDataAccessAsync(startDate, endDate),
            SuspiciousActivities = await GetSuspiciousActivitiesAsync(startDate, endDate)
        };
    }
}

// Health Check
public class PrivacyHealthCheck : IHealthCheck
{
    private readonly IPrivacyAuditService _auditService;
    private readonly IConsentManagementService _consentService;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();
            
            // Audit service health
            var recentAuditEvents = await _auditService.GetRecentEventsCountAsync(TimeSpan.FromHours(1));
            healthData["RecentAuditEvents"] = recentAuditEvents;
            
            // Consent service health
            var pendingConsents = await _consentService.GetPendingConsentsCountAsync();
            healthData["PendingConsents"] = pendingConsents;
            
            // Check for critical issues
            var criticalIssues = new List<string>();
            
            if (recentAuditEvents == 0)
            {
                criticalIssues.Add("No recent audit events - audit service may be down");
            }
            
            if (pendingConsents > 1000)
            {
                criticalIssues.Add($"High number of pending consents: {pendingConsents}");
            }
            
            var status = criticalIssues.Any() ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = status == HealthStatus.Healthy 
                ? "Privacy services are healthy" 
                : string.Join("; ", criticalIssues);
            
            return new HealthCheckResult(status, description, data: healthData);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Privacy health check failed", ex);
        }
    }
}
```

## Güvenlik ve En İyi Uygulamalar

### 1. Şifreleme ve Veri Güvenliği
- **AES-256 Şifreleme**: Tüm hassas veriler için endüstri standardı şifreleme
- **PBKDF2 Hash**: 10.000+ iterasyon ile güvenli hash oluşturma
- **Salt Kullanımı**: Her hash işlemi için benzersiz salt
- **IV (Initialization Vector)**: Her şifreleme için rastgele IV
- **Anahtar Yönetimi**: Güvenli anahtar saklama ve rotasyon

### 2. Erişim Kontrolü ve Yetkilendirme
- **Rol Bazlı Erişim**: Admin, DPO, User rolleri
- **Purpose-Based Access**: Amaca dayalı veri erişim kontrolü
- **Minimum Privilege**: En az yetki prensibi
- **Audit Trail**: Tüm erişimlerin detaylı logları

### 3. Veri Minimizasyonu
- **Purpose Limitation**: Sadece belirtilen amaç için veri toplama
- **Data Retention**: Yasal gereksinimler doğrultusunda saklama
- **Automatic Deletion**: Süre dolduğunda otomatik silme
- **Anonymization**: Kimlik tespitine izin vermeyen anonimleştirme

## Yasal Uyumluluk

### GDPR (General Data Protection Regulation) Uyumluluğu

#### Article 6 - Lawfulness of Processing
```csharp
public enum LegalBasis
{
    Consent,           // (a) the data subject has given consent
    Contract,          // (b) processing is necessary for the performance of a contract
    LegalObligation,   // (c) processing is necessary for compliance with a legal obligation
    VitalInterests,    // (d) processing is necessary to protect vital interests
    PublicTask,        // (e) processing is necessary for a public task
    LegitimateInterests // (f) processing is necessary for legitimate interests
}
```

#### Article 7 - Conditions for Consent
- ✅ Explicit consent requirement
- ✅ Consent withdrawal mechanism
- ✅ Proof of consent storage
- ✅ Clear and plain language

#### Article 15 - Right of Access
- ✅ Data export functionality
- ✅ 30-day response period
- ✅ Free of charge (first request)
- ✅ Machine-readable format

#### Article 16 - Right to Rectification
- ✅ Data correction mechanism
- ✅ Notification to third parties
- ✅ Accuracy verification

#### Article 17 - Right to Erasure ("Right to be Forgotten")
- ✅ Complete data deletion
- ✅ Legal retention check
- ✅ Third-party notification
- ✅ Anonymization alternative

#### Article 20 - Right to Data Portability
- ✅ Structured data export
- ✅ Common machine-readable format
- ✅ Direct transmission capability

#### Article 25 - Data Protection by Design and by Default
- ✅ Privacy by design principles
- ✅ Default privacy settings
- ✅ Built-in protection measures

#### Article 32 - Security of Processing
- ✅ Encryption of personal data
- ✅ Confidentiality and integrity
- ✅ Availability and resilience
- ✅ Regular security testing

### KVKK (Kişisel Verilerin Korunması Kanunu) Uyumluluğu

#### Temel İlkeler
- **Hukuka ve Dürüstlük Kuralına Uygunluk**: Legal basis kontrolü
- **Doğruluk**: Veri doğrulama ve düzeltme
- **Amaçla Bağlılık**: Purpose limitation
- **Orantılılık**: Data minimization
- **İlgili Kişinin Haklarına Saygı**: Data subject rights

#### Veri Sahibinin Hakları
- ✅ Bilgilendirilme hakkı
- ✅ Verilere erişim hakkı  
- ✅ Düzeltme hakkı
- ✅ Silme hakkı
- ✅ İtiraz hakkı
- ✅ Zarara uğrama halinde giderim talep etme hakkı

## Mimari ve Tasarım

### Sistem Mimarisi
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│                Privacy Orchestration Layer                  │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐ │
│  │   Consent   │ │Anonymization│ │     GDPR Compliance     │ │
│  │ Management  │ │   Service   │ │       Service           │ │
│  └─────────────┘ └─────────────┘ └─────────────────────────┘ │
│  ┌─────────────┐ ┌─────────────────────────────────────────┐ │
│  │Privacy Audit│ │      Data Retention Service             │ │
│  │   Service   │ │                                         │ │
│  └─────────────┘ └─────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                      │
│   Encryption   │  Audit Store  │  Configuration │  Health  │
└─────────────────────────────────────────────────────────────┘
```

### Veri Akış Diyagramı
```
User Request → Privacy Middleware → Consent Check → Service Layer
                      ↓                  ↓              ↓
               Audit Logging ←── Anonymization ←─ Data Access
                      ↓                  ↓              ↓
              Compliance Check ← Retention Check ←─ Processing
                      ↓                                ↓
               Final Response ←─────────────────── Result
```

## Sonuç

Enterprise.Shared.Privacy projesi, modern kurumsal uygulamalar için kapsamlı veri gizliliği ve uyumluluk çözümü sunar. .NET 8.0 tabanlı bu sistem, GDPR ve KVKK gibi kritik veri koruma düzenlemelerine tam uyum sağlarken, geliştiricilere kolay kullanım ve güçlü özellikler sunar.

### Temel Avantajlar:
- **Yasal Uyumluluk**: GDPR ve KVKK maddelerinin tam implementasyonu
- **Güvenli Anonimleştirme**: Çoklu seviye veri anonimleştirme teknikleri  
- **Kapsamlı Audit**: Detaylı veri işleme denetim sistemi
- **Kolay Entegrasyon**: Minimal kurulum ile hızlı başlangıç
- **Production Ready**: Kurumsal seviyede test edilmiş ve optimize edilmiş
- **Type-Safe**: Strong typing ile compile-time güvenlik
- **Extensible**: Özelleştirilebilir ve genişletilebilir mimari

### Teknik Özellikler:
- **Modern .NET**: .NET 8.0 ile en son teknoloji
- **Async/Await**: Yüksek performans için asenkron programlama
- **Dependency Injection**: IoC container desteği
- **Configuration**: Esnek yapılandırma sistemi
- **Health Checks**: Sistem sağlığı monitörü
- **Structured Logging**: JSON formatında detaylı loglar

Bu sistem sayesinde uygulamalarınız veri gizliliği konularında tam uyumlu, güvenli ve yönetilebilir hale gelir.