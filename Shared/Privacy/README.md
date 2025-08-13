# EgitimPlatform.Shared.Privacy

A comprehensive GDPR and privacy compliance library for the EgitimPlatform microservices architecture.

## 🛡️ Features

### Core Privacy Compliance
- **GDPR Article 6** - Lawful basis for processing
- **GDPR Article 7** - Consent management
- **GDPR Chapter III** - Data subject rights (Access, Rectification, Erasure, Portability, etc.)
- **GDPR Article 30** - Records of processing activities
- **GDPR Article 35** - Data protection impact assessments

### Data Management
- **Personal Data Inventory** - Track all personal data across systems
- **Consent Management** - Granular consent tracking and validation
- **Data Retention** - Automatic data lifecycle management
- **Data Subject Rights** - Automated request processing
- **Cookie Consent** - EU ePrivacy directive compliance

### Security & Protection
- **Data Encryption** - Attribute-based encryption requirements
- **Pseudonymization** - Privacy-preserving data processing  
- **Anonymization** - Irreversible data de-identification
- **Cross-border Transfers** - Third country transfer validation

## 🚀 Quick Start

### 1. Installation

Add the privacy library to your service:

```xml
<PackageReference Include="EgitimPlatform.Shared.Privacy" Version="1.0.0" />
```

### 2. Configuration

Add privacy configuration to `appsettings.json`:

```json
{
  "Privacy": {
    "ConsentManagement": {
      "EnableConsentManagement": true,
      "ConsentExpiryDays": 365,
      "RequireExplicitConsent": true
    },
    "DataSubjectRights": {
      "EnableDataSubjectRights": true,
      "RequestProcessingTimeoutDays": 30,
      "MaxRequestsPerUserPerMonth": 5
    },
    "Compliance": {
      "DataControllerName": "EgitimPlatform Ltd",
      "DataControllerEmail": "privacy@egitimplatform.com",
      "DataProtectionOfficerEmail": "dpo@egitimplatform.com",
      "PrivacyPolicyUrl": "https://egitimplatform.com/privacy"
    }
  }
}
```

### 3. Service Registration

Register privacy services in `Program.cs`:

```csharp
using EgitimPlatform.Shared.Privacy.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add privacy compliance services
builder.Services.AddPrivacyCompliance(builder.Configuration);

var app = builder.Build();

// Add privacy compliance middleware
app.UsePrivacyCompliance();

app.Run();
```

## 📊 Data Annotations

### Personal Data Attributes

Mark properties containing personal data:

```csharp
public class UserProfile
{
    [PersonalData(PersonalDataCategory.BasicIdentity, 
        Purpose = "user-identification",
        RequiresConsent = true,
        RetentionPeriodDays = 2555)]
    public string FirstName { get; set; }

    [PersonalData(PersonalDataCategory.ContactInformation,
        Purpose = "communication",
        IsEncrypted = true)]
    public string Email { get; set; }

    [SensitivePersonalData(RequiresExplicitConsent = true)]
    public string HealthInformation { get; set; }
}
```

### Data Retention

Configure automatic data retention:

```csharp
public class UserSession
{
    [DataRetention(30, Reason = DataRetentionReason.ConsentGiven, AutoDelete = true)]
    public string SessionData { get; set; }

    [DataRetention(90, Reason = DataRetentionReason.LegalObligation)]
    public string SecurityLogs { get; set; }
}
```

### Encryption & Pseudonymization

```csharp
public class PaymentInfo
{
    [EncryptedData(EncryptionMethod = "AES-256")]
    [PersonalData(PersonalDataCategory.FinancialInformation)]
    public string CreditCardNumber { get; set; }

    [PseudonymizedData(IsReversible = false)]
    public string CustomerId { get; set; }
}
```

## 🔧 Usage Examples

### Consent Management

```csharp
public class UserController : ControllerBase
{
    private readonly IConsentService _consentService;

    public async Task<IActionResult> CreateProfile(CreateProfileRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.GetClientIpAddress();
        var userAgent = HttpContext.GetUserAgent();

        // Create consent record
        await _consentService.CreateConsentAsync(
            userId,
            PersonalDataCategory.BasicIdentity,
            "profile-management",
            "I consent to processing my profile data",
            ipAddress,
            userAgent
        );

        // Validate consent before processing
        var hasConsent = await _consentService.HasValidConsentAsync(
            userId, 
            PersonalDataCategory.BasicIdentity, 
            "profile-management"
        );

        if (!hasConsent)
        {
            return BadRequest("Valid consent required");
        }

        // Process profile creation...
        return Ok();
    }
}
```

### Data Subject Rights

```csharp
public class PrivacyController : ControllerBase
{
    private readonly IDataSubjectRightsService _rightsService;

    [HttpPost("data-subject-request")]
    public async Task<IActionResult> CreateDataSubjectRequest(DataSubjectRightRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.GetClientIpAddress();
        var userAgent = HttpContext.GetUserAgent();

        var dsrRequest = await _rightsService.CreateRequestAsync(
            userId,
            request.Email,
            request.RequestType,
            request.Description,
            request.AffectedCategories,
            ipAddress,
            userAgent
        );

        return Ok(new { RequestId = dsrRequest.Id });
    }

    [HttpGet("export-data")]
    public async Task<IActionResult> ExportUserData()
    {
        var userId = User.GetUserId();
        var userData = await _rightsService.ProcessAccessRequestAsync(userId);
        
        return File(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData),
            "application/json",
            $"user-data-{userId}-{DateTime.UtcNow:yyyyMMdd}.json"
        );
    }
}
```

### Cookie Consent Management

```csharp
public class CookieController : ControllerBase
{
    [HttpPost("cookie-consent")]
    public IActionResult SetCookieConsent(CookieConsentRequest request)
    {
        HttpContext.SetCookieConsent(
            essential: true,
            analytical: request.AcceptAnalytical,
            marketing: request.AcceptMarketing
        );

        // Handle cookie consent changes
        CookieConsentHelper.SetAnalyticalCookies(HttpContext, request.AcceptAnalytical);
        CookieConsentHelper.SetMarketingCookies(HttpContext, request.AcceptMarketing);

        return Ok();
    }
}
```

### Privacy Validation

```csharp
public class ProfileService
{
    private readonly IPrivacyValidator _validator;

    public async Task<bool> UpdateProfile(UserProfile profile, string userId)
    {
        // Validate privacy compliance
        var validation = await _validator.ValidateComplianceAsync(profile, userId);
        
        if (!validation.IsValid)
        {
            throw new PrivacyViolationException(string.Join("; ", validation.Errors));
        }

        // Process profile update...
        return true;
    }
}
```

## 📋 Data Processing Activities

Register processing activities for GDPR Article 30 compliance:

```csharp
public async Task RegisterProcessingActivities()
{
    await _activityService.CreateActivityAsync(
        name: "User Registration",
        description: "Processing user registration data",
        controller: "EgitimPlatform Ltd",
        lawfulBasis: DataProcessingLawfulBasis.Consent,
        dataCategories: new[] { 
            PersonalDataCategory.BasicIdentity,
            PersonalDataCategory.ContactInformation 
        }.ToList(),
        processingPurposes: new[] { "account-creation", "service-delivery" }.ToList(),
        dataSubjectCategories: new[] { "website-users", "customers" }.ToList()
    );
}
```

## 🔍 Monitoring & Auditing

The library automatically logs privacy-relevant activities:

```json
{
  "timestamp": "2024-08-04T10:30:00Z",
  "level": "Information",
  "message": "Privacy-relevant request: POST /api/users by user 12345 from 192.168.1.1",
  "properties": {
    "UserId": "12345",
    "IpAddress": "192.168.1.1",
    "UserAgent": "Mozilla/5.0...",
    "RequestPath": "/api/users",
    "Method": "POST"
  }
}
```

## 🌍 Multi-Regulation Support

The library supports multiple privacy regulations:

- **GDPR** (General Data Protection Regulation) - EU
- **CCPA** (California Consumer Privacy Act) - US
- **PIPEDA** (Personal Information Protection and Electronic Documents Act) - Canada
- **LGPD** (Lei Geral de Proteção de Dados) - Brazil
- **PDPA** (Personal Data Protection Act) - Singapore

## 🚨 Error Handling

Privacy violations are handled through structured exceptions:

```csharp
try
{
    await ProcessPersonalData(userData, userId);
}
catch (ConsentRequiredException ex)
{
    return BadRequest(new { 
        Error = "Consent required",
        RequiredConsents = ex.RequiredConsents 
    });
}
catch (DataRetentionViolationException ex)
{
    return BadRequest(new { 
        Error = "Data retention violation",
        RetentionDetails = ex.RetentionDetails 
    });
}
```

## 📈 Performance Considerations

- **Caching**: Consent records are cached for performance
- **Async Processing**: All operations are asynchronous
- **Batch Operations**: Support for bulk consent and data operations
- **Lazy Loading**: Data is loaded only when needed

## 🧪 Testing Support

Mock services are provided for testing:

```csharp
public class PrivacyServiceTests
{
    private readonly Mock<IConsentService> _mockConsentService;
    
    [Test]
    public async Task Should_Require_Consent_For_Personal_Data()
    {
        _mockConsentService
            .Setup(x => x.HasValidConsentAsync(It.IsAny<string>(), It.IsAny<PersonalDataCategory>(), It.IsAny<string>()))
            .ReturnsAsync(false);
            
        var validator = new PrivacyValidator(_mockConsentService.Object);
        var result = await validator.ValidateConsentRequiredAsync(entity, "userId");
        
        Assert.False(result.IsValid);
    }
}
```

## 📚 Additional Resources

- [GDPR Full Text](https://gdpr-info.eu/)
- [ICO GDPR Guidelines](https://ico.org.uk/for-organisations/guide-to-data-protection/guide-to-the-general-data-protection-regulation-gdpr/)
- [EDPB Guidelines](https://edpb.europa.eu/our-work-tools/general-guidance/gdpr-guidelines-recommendations-best-practices_en)

## 🤝 Contributing

When contributing to privacy features:

1. Ensure GDPR compliance
2. Add comprehensive tests
3. Update documentation
4. Consider performance impact
5. Validate against multiple regulations

## ⚖️ Legal Disclaimer

This library provides technical tools for privacy compliance but does not constitute legal advice. Organizations should consult with qualified legal professionals to ensure full regulatory compliance.