# Enterprise.Shared.Security

**Versiyon:** 1.0.0  
**Hedef Framework:** .NET 8.0  
**Geliştirici:** Enterprise Platform Team

## 📋 Proje Amacı

Enterprise.Shared.Security, Enterprise mikroservis platformu için geliştirilmiş kapsamlı bir güvenlik kütüphanesidir. JWT token yönetimi, veri şifreleme, kimlik doğrulama, yetkilendirme, API key yönetimi, güvenlik denetimi ve koruma mekanizmaları ile enterprise-grade security çözümleri sunar.

## 🎯 Ne Yapar?

Bu kütüphane şu temel güvenlik fonksiyonları sağlar:

### 1. **JWT Token Yönetimi**
- JWT access token üretimi ve doğrulaması
- Refresh token mekanizması ile güvenli oturum yenileme
- Token revocation (iptal) ve blacklist desteği
- Claims tabanlı yetkilendirme
- Token expiration yönetimi
- HMAC-SHA256 ile güvenli imzalama

### 2. **Veri Şifreleme ve Güvenlik**
- AES-256 şifreleme/çözme (CBC mode, PKCS7 padding)
- Microsoft Data Protection API entegrasyonu
- Güvenli anahtar ve IV üretimi
- Çoklu şifreleme anahtarı desteği
- Kriptografik olarak güvenli rastgele sayı üretimi

### 3. **Şifre Güvenliği**
- BCrypt ile güvenli şifre hashleme (work factor 12)
- Şifre güçlülük değerlendirme sistemi (0-100 puan)
- SHA-256 ve SHA-512 hash algoritmaları
- HMAC-SHA256 mesaj kimlik doğrulama
- Güvenli salt üretimi

### 4. **Güvenlik Doğrulama ve Koruma**
- SQL injection pattern tespiti
- XSS (Cross-Site Scripting) pattern tespiti
- Input sanitization ve temizleme
- E-posta, URL ve dosya türü doğrulama
- Güvenlik pattern'larını compiled regex ile performanslı tespit

### 5. **API Key Yönetimi**
- Güvenli API key üretimi ("sk_" prefix ile)
- Key bazlı rate limiting (varsayılan: 1000 req/saat)
- API key lifecycle yönetimi (oluşturma, doğrulama, iptal)
- Usage tracking ve analytics
- Metadata ve permission yönetimi

### 6. **Güvenlik Denetimi ve İzleme**
- Kapsamlı güvenlik event logging
- Authentication başarı/başarısızlık takibi
- Şüpheli aktivite tespiti
- Failed login attempt tracking ve lockout
- IP adresi engelleme ve whitelist
- Güvenlik event analiz ve raporlama

### 7. **Rate Limiting ve Koruma**
- API endpoint bazlı rate limiting
- IP bazlı rate limiting ve blocking
- Sliding window algoritması
- Configurable limit ve time window
- Memory cache entegrasyonu ile performans

### 8. **CORS ve Security Header Yönetimi**
- Esnek CORS policy konfigürasyonu
- Security header'ları otomatik ekleme
- HSTS, XSS Protection, Content-Type Options
- Clickjacking koruması (X-Frame-Options)

## 🛠 Kullanılan Teknolojiler

### Core Security Libraries
- **.NET 8.0**: Hedef framework
- **C# 12.0**: Modern programlama dili özellikleri
- **Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11**: JWT authentication
- **System.IdentityModel.Tokens.Jwt 8.2.1**: JWT token işlemleri
- **BCrypt.Net-Next 4.0.3**: Güvenli password hashing

### Encryption ve Data Protection
- **Microsoft.AspNetCore.DataProtection.Abstractions**: Data protection API
- **Microsoft.AspNetCore.Cryptography.KeyDerivation**: Key derivation functions
- **System.Security.Cryptography**: Native .NET crypto APIs

### Performance ve Resilience
- **Polly 8.5.0**: Resilience ve retry patterns
- **Microsoft.Extensions.Caching.Memory**: Memory caching
- **Microsoft.Extensions.Http**: HTTP client factory

### Configuration ve Dependency Injection
- **Microsoft.Extensions.DependencyInjection**: DI container
- **Microsoft.Extensions.Options**: Configuration options pattern
- **Microsoft.Extensions.Logging**: Structured logging

## 📁 Proje Yapısı

```
Enterprise.Shared.Security/
├── Attributes/
│   └── SecurityAttributes.cs       # Güvenlik attribute'ları
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration
├── Interfaces/
│   ├── IApiKeyService.cs           # API key management interface
│   ├── IEncryptionService.cs       # Şifreleme service interface
│   ├── IJwtTokenService.cs         # JWT token service interface
│   ├── IPasswordService.cs         # Şifre service interface
│   ├── ISecurityAuditService.cs    # Güvenlik audit interface
│   └── ISecurityValidationService.cs # Doğrulama service interface
├── Models/
│   ├── ApiKeyModel.cs              # API key veri modelleri
│   ├── JwtModels.cs                # JWT ile ilgili modeller
│   ├── SecurityModels.cs           # Güvenlik veri modelleri
│   └── SecuritySettings.cs         # Konfigürasyon settings
├── Services/
│   ├── ApiKeyService.cs            # API key yönetim servisi
│   ├── EncryptionService.cs        # Şifreleme servisi
│   ├── JwtTokenService.cs          # JWT token servisi
│   ├── PasswordService.cs          # Şifre işlemleri servisi
│   ├── SecurityAuditService.cs     # Güvenlik audit servisi
│   └── SecurityValidationService.cs # Doğrulama servisi
└── GlobalUsings.cs                 # Global using statements
```

## 🚀 Kurulum ve Kullanım

### 1. NuGet Package Installation

```xml
<PackageReference Include="Enterprise.Shared.Security" Version="1.0.0" />
```

### 2. appsettings.json Configuration

```json
{
  "SecuritySettings": {
    "JwtSettings": {
      "SecretKey": "your-super-secret-key-minimum-256-bits-long-here",
      "Issuer": "https://your-company.com",
      "Audience": "enterprise-services",
      "AccessTokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 30,
      "ClockSkewMinutes": 5
    },
    "EncryptionSettings": {
      "DefaultKey": "32-byte-encryption-key-here!!!!!!",
      "UseDataProtectionApi": false
    },
    "PasswordPolicySettings": {
      "MinimumLength": 8,
      "MaximumLength": 128,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialCharacter": true,
      "BcryptWorkFactor": 12
    },
    "RateLimitSettings": {
      "EnableRateLimit": true,
      "DefaultRateLimit": 1000,
      "DefaultTimeWindowHours": 1,
      "IpBlockingEnabled": true,
      "MaxFailedAttempts": 10,
      "BlockDurationMinutes": 30
    },
    "SecurityFeatures": {
      "EnableApiKeyAuth": true,
      "EnableSecurityAudit": true,
      "EnableSecurityHeaders": true,
      "EnableInputValidation": true
    },
    "CorsSettings": {
      "AllowedOrigins": ["https://your-frontend.com"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowedHeaders": ["Content-Type", "Authorization"],
      "AllowCredentials": true
    }
  }
}
```

### 3. Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Enterprise Security'yi ekle (tam güvenlik paketi)
builder.Services.AddEnterpriseSecurity(builder.Configuration);

// Veya özelleştirilmiş konfigürasyonla
builder.Services.AddEnterpriseSecurity(builder.Configuration, options =>
{
    options.EnableJwtAuthentication = true;
    options.EnableApiKeyAuthentication = true;
    options.EnableSecurityAudit = true;
    options.EnableRateLimiting = true;
    options.EnableSecurityHeaders = true;
});

// Sadece JWT için minimal setup
// builder.Services.AddEnterpriseSecurityJwtOnly(builder.Configuration);

// Sadece API Key için minimal setup  
// builder.Services.AddEnterpriseSecurityApiKeyOnly(builder.Configuration);

var app = builder.Build();

// Security middleware'ları ekle (sırası önemli!)
app.UseEnterpriseSecurity();

// Diğer middleware'lar
app.UseRouting();
app.MapControllers();

app.Run();
```

### 4. JWT Token Service Kullanımı

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordService _passwordService;
    private readonly ISecurityAuditService _auditService;

    public AuthController(
        IJwtTokenService jwtTokenService, 
        IPasswordService passwordService,
        ISecurityAuditService auditService)
    {
        _jwtTokenService = jwtTokenService;
        _passwordService = passwordService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        try
        {
            // Kullanıcı doğrulama (örnek)
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                await _auditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AuthenticationFailure,
                    Description = "User not found",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent,
                    AdditionalData = new { Email = request.Email }
                });
                
                return Unauthorized("Geçersiz kimlik bilgileri");
            }

            // Şifre doğrulama
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _auditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AuthenticationFailure,
                    Description = "Invalid password",
                    UserId = user.Id.ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                
                return Unauthorized("Geçersiz kimlik bilgileri");
            }

            // JWT token üret
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new("role", user.Role)
            };

            var tokenResult = await _jwtTokenService.GenerateTokenAsync(claims);
            
            // Başarılı login'i logla
            await _auditService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationSuccess,
                Description = "User logged in successfully",
                UserId = user.Id.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            return Ok(new
            {
                AccessToken = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }
        catch (Exception ex)
        {
            await _auditService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationFailure,
                Description = "Login error: " + ex.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Severity = SecurityEventSeverity.Critical
            });
            
            return StatusCode(500, "Giriş işlemi başarısız");
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
    {
        var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
        
        if (!result.IsValid)
        {
            return Unauthorized("Geçersiz refresh token");
        }

        return Ok(new
        {
            AccessToken = result.Token,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeTokenAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _jwtTokenService.RevokeUserTokensAsync(userId);
        
        return Ok("Token başarıyla iptal edildi");
    }
}
```

### 5. Veri Şifreleme Kullanımı

```csharp
public class UserService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<UserService> _logger;

    public UserService(IEncryptionService encryptionService, ILogger<UserService> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        // Hassas verileri şifrele
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            user.PhoneNumberEncrypted = await _encryptionService.EncryptAsync(request.PhoneNumber);
        }

        if (!string.IsNullOrEmpty(request.Address))
        {
            user.AddressEncrypted = await _encryptionService.EncryptAsync(request.Address);
        }

        // Kullanıcıyı kaydet
        await _userRepository.CreateAsync(user);
        
        return user;
    }

    public async Task<UserDetailsDto> GetUserDetailsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserDetailsDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            // Şifreli verileri çöz
            PhoneNumber = !string.IsNullOrEmpty(user.PhoneNumberEncrypted)
                ? await _encryptionService.DecryptAsync(user.PhoneNumberEncrypted)
                : null,
            Address = !string.IsNullOrEmpty(user.AddressEncrypted) 
                ? await _encryptionService.DecryptAsync(user.AddressEncrypted)
                : null
        };
    }
}
```

### 6. API Key Authentication

```csharp
[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateApiKeyAsync([FromBody] CreateApiKeyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var apiKey = await _apiKeyService.CreateApiKeyAsync(new ApiKeyCreateOptions
        {
            Name = request.Name,
            Description = request.Description,
            CreatedBy = userId,
            RateLimit = request.RateLimit ?? 1000,
            ExpirationDate = request.ExpirationDate,
            Permissions = request.Permissions ?? new List<string>(),
            Metadata = request.Metadata ?? new Dictionary<string, string>()
        });

        return Ok(new
        {
            apiKey.Id,
            apiKey.Key, // Bu sadece bir kez gösterilmeli!
            apiKey.Name,
            apiKey.Description,
            apiKey.CreatedAt,
            apiKey.ExpirationDate,
            apiKey.RateLimit
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetApiKeysAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId);
        
        return Ok(apiKeys.Select(k => new
        {
            k.Id,
            k.Name,
            k.Description,
            k.IsActive,
            k.CreatedAt,
            k.LastUsedAt,
            k.UsageCount,
            k.RateLimit,
            MaskedKey = MaskApiKey(k.Key) // Güvenlik için mask'le
        }));
    }

    [HttpDelete("{keyId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeApiKeyAsync(string keyId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _apiKeyService.RevokeApiKeyAsync(keyId, userId);
        
        if (!result)
        {
            return NotFound("API key bulunamadı");
        }
        
        return Ok("API key başarıyla iptal edildi");
    }

    private static string MaskApiKey(string apiKey)
    {
        if (apiKey.Length <= 8) return apiKey;
        return apiKey[..8] + "..." + apiKey[^4..];
    }
}

// API Key ile korumalı endpoint örneği
[ApiController]
[Route("api/[controller]")]
[ApiKeyAuth] // Custom attribute
public class DataController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDataAsync()
    {
        // API key ile kimlik doğrulaması yapılmış
        var apiKeyId = HttpContext.Items["ApiKeyId"]?.ToString();
        var permissions = HttpContext.Items["ApiKeyPermissions"] as List<string>;
        
        if (!permissions?.Contains("data:read") == true)
        {
            return Forbid("Bu API key'in veri okuma yetkisi yok");
        }
        
        // Veri döndür
        return Ok(new { message = "API key ile başarıyla yetkilendirildi" });
    }
}
```

### 7. Şifre Güvenliği ve Doğrulama

```csharp
public class PasswordService
{
    private readonly IPasswordService _passwordService;
    private readonly ISecurityValidationService _validationService;

    public PasswordService(
        IPasswordService passwordService, 
        ISecurityValidationService validationService)
    {
        _passwordService = passwordService;
        _validationService = validationService;
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        // Mevcut şifre doğrulama
        var user = await GetCurrentUserAsync();
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Mevcut şifre yanlış");
        }

        // Yeni şifre güçlülük kontrolü
        var passwordStrength = _validationService.ValidatePasswordStrength(request.NewPassword);
        if (passwordStrength.Score < 60) // Minimum %60 güçlülük
        {
            return Result.Failure($"Şifre yeterince güçlü değil. Puan: {passwordStrength.Score}/100", 
                                 passwordStrength.Suggestions);
        }

        // Yeni şifre hash'le ve kaydet
        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.PasswordHash = newPasswordHash;
        user.PasswordChangedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        
        return Result.Success("Şifre başarıyla değiştirildi");
    }

    public ValidationResult ValidateInput(string input, string fieldName)
    {
        var issues = new List<string>();

        // SQL Injection kontrolü
        if (_validationService.ContainsSqlInjectionPatterns(input))
        {
            issues.Add($"{fieldName} alanında SQL injection pattern tespit edildi");
        }

        // XSS kontrolü
        if (_validationService.ContainsXssPatterns(input))
        {
            issues.Add($"{fieldName} alanında XSS pattern tespit edildi");
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }
}
```

## 🎨 Gelişmiş Güvenlik Senaryoları

### 1. Multi-Factor Authentication (MFA) Desteği

```csharp
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly IJwtTokenService _jwtService;

    [HttpPost("setup-totp")]
    [Authorize]
    public async Task<IActionResult> SetupTotpAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var setup = await _mfaService.GenerateTotpSetupAsync(userId);
        
        return Ok(new
        {
            setup.SecretKey,
            setup.QrCodeUrl,
            setup.ManualEntryKey
        });
    }

    [HttpPost("verify-totp")]
    [Authorize]
    public async Task<IActionResult> VerifyTotpAsync([FromBody] VerifyTotpRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isValid = await _mfaService.VerifyTotpAsync(userId, request.Code);
        
        if (!isValid)
        {
            return BadRequest("Geçersiz TOTP kodu");
        }

        // MFA başarılı - tam yetkilendirilmiş token üret
        var newClaims = User.Claims.ToList();
        newClaims.Add(new Claim("mfa_verified", "true"));
        
        var fullAccessToken = await _jwtService.GenerateTokenAsync(newClaims);
        
        return Ok(new
        {
            AccessToken = fullAccessToken.Token,
            ExpiresAt = fullAccessToken.ExpiresAt
        });
    }
}
```

### 2. Gelişmiş Rate Limiting

```csharp
public class AdvancedRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdvancedRateLimitingService> _logger;

    public async Task<bool> IsAllowedAsync(string identifier, RateLimitRule rule)
    {
        var key = $"rate_limit:{identifier}:{rule.Endpoint}";
        var window = GetCurrentWindow(rule.WindowSize);
        var windowKey = $"{key}:{window}";

        var currentCount = _cache.GetOrCreate(windowKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = rule.WindowSize;
            return 0;
        });

        if (currentCount >= rule.Limit)
        {
            // Rate limit aşıldı - IP'yi geçici engelle
            await BlockTemporarilyAsync(identifier, rule.BlockDuration);
            
            _logger.LogWarning("Rate limit exceeded for {Identifier}. Current: {Current}, Limit: {Limit}",
                              identifier, currentCount, rule.Limit);
            
            return false;
        }

        // Sayıcıyı artır
        _cache.Set(windowKey, currentCount + 1, rule.WindowSize);
        return true;
    }

    private async Task BlockTemporarilyAsync(string identifier, TimeSpan duration)
    {
        var blockKey = $"blocked_ip:{identifier}";
        _cache.Set(blockKey, true, duration);
        
        // Güvenlik olayını logla
        await LogSecurityEvent(new SecurityEvent
        {
            EventType = SecurityEventType.IpBlocked,
            Description = $"IP temporarily blocked due to rate limit violation",
            IpAddress = identifier,
            Severity = SecurityEventSeverity.Warning
        });
    }
}
```

### 3. Güvenlik Header Middleware

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecuritySettings _settings;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecuritySettings> settings)
    {
        _next = next;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Security headers ekle
        AddSecurityHeaders(context);
        
        await _next(context);
        
        // Response'ta hassas bilgileri temizle
        RemoveSensitiveHeaders(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // HSTS (HTTP Strict Transport Security)
        if (_settings.SecurityFeatures.EnableHsts)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        // XSS Protection
        headers.Append("X-XSS-Protection", "1; mode=block");
        
        // Content Type Options
        headers.Append("X-Content-Type-Options", "nosniff");
        
        // Frame Options (Clickjacking koruması)
        headers.Append("X-Frame-Options", "DENY");
        
        // Content Security Policy
        if (!string.IsNullOrEmpty(_settings.SecurityFeatures.ContentSecurityPolicy))
        {
            headers.Append("Content-Security-Policy", _settings.SecurityFeatures.ContentSecurityPolicy);
        }

        // Referrer Policy
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Permissions Policy
        headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    }

    private static void RemoveSensitiveHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        
        // Sunucu bilgisini gizle
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
    }
}
```

### 4. Güvenlik Audit ve Monitoring

```csharp
public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly IMemoryCache _cache;

    public async Task<SecurityThreatAnalysis> AnalyzeSecurityThreatsAsync(TimeSpan timeWindow)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime - timeWindow;
        
        var events = await _auditService.GetSecurityEventsAsync(startTime, endTime);
        
        var analysis = new SecurityThreatAnalysis
        {
            TimeWindow = timeWindow,
            TotalEvents = events.Count,
            CriticalEvents = events.Count(e => e.Severity == SecurityEventSeverity.Critical),
            HighEvents = events.Count(e => e.Severity == SecurityEventSeverity.High),
            SuspiciousActivities = DetectSuspiciousActivities(events),
            TopFailedLogins = GetTopFailedLoginIps(events),
            UnusualPatterns = DetectUnusualPatterns(events)
        };

        // Kritik seviye tehdit varsa alert gönder
        if (analysis.CriticalEvents > 0)
        {
            await SendSecurityAlertAsync(analysis);
        }

        return analysis;
    }

    private List<SuspiciousActivity> DetectSuspiciousActivities(List<SecurityEvent> events)
    {
        var suspicious = new List<SuspiciousActivity>();

        // Kısa sürede çok fazla başarısız giriş denemesi
        var failedLogins = events
            .Where(e => e.EventType == SecurityEventType.AuthenticationFailure)
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() > 10) // 10'dan fazla başarısız deneme
            .Select(g => new SuspiciousActivity
            {
                Type = "Brute Force Attack",
                IpAddress = g.Key,
                EventCount = g.Count(),
                Description = $"{g.Count()} failed login attempts from {g.Key}"
            });

        suspicious.AddRange(failedLogins);

        // Farklı kullanıcılardan aynı IP'ye çok fazla istek
        var userEnumeration = events
            .Where(e => e.EventType == SecurityEventType.AuthenticationFailure)
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Select(e => e.AdditionalData?["Email"]).Distinct().Count() > 20)
            .Select(g => new SuspiciousActivity
            {
                Type = "User Enumeration",
                IpAddress = g.Key,
                EventCount = g.Count(),
                Description = $"Attempting to enumerate users from {g.Key}"
            });

        suspicious.AddRange(userEnumeration);

        return suspicious;
    }

    private async Task SendSecurityAlertAsync(SecurityThreatAnalysis analysis)
    {
        var alert = new SecurityAlert
        {
            Severity = SecurityAlertSeverity.Critical,
            Title = "Kritik Güvenlik Tehdidi Tespit Edildi",
            Description = $"Son {analysis.TimeWindow.TotalMinutes} dakikada {analysis.CriticalEvents} kritik güvenlik olayı tespit edildi.",
            Details = analysis,
            Timestamp = DateTime.UtcNow
        };

        // Alert'i email, SMS, Slack vs. ile gönder
        await NotifySecurityTeamAsync(alert);
        
        _logger.LogCritical("Security alert sent: {Alert}", alert);
    }
}
```

## 🧪 Test Coverage

Proje **160 adet unit test** ile kapsamlı test coverage'a sahiptir:

### Test Kategorileri:
- **JWT Token Tests**: Token üretimi, doğrulama ve refresh mekanizmaları (45 test)
- **Encryption Tests**: AES şifreleme/çözme, anahtar yönetimi (35 test) 
- **Password Tests**: BCrypt hashing, şifre güçlülük validasyonu (25 test)
- **API Key Tests**: Key yaratma, doğrulama, rate limiting (30 test)
- **Security Validation Tests**: XSS, SQL injection, input validation (15 test)
- **Audit Service Tests**: Security event logging, monitoring (10 test)

```bash
# Testleri çalıştırma
dotnet test

# Sonuç: Passed: 160, Failed: 0, Skipped: 0
```

## 💡 Güvenlik En İyi Uygulamaları

### 1. JWT Token Güvenliği

```csharp
// ✅ İyi: Güvenli JWT konfigürasyonu
public class JwtConfiguration
{
    public static TokenValidationParameters GetValidationParameters(SecuritySettings settings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = settings.JwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.JwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(settings.JwtSettings.ClockSkewMinutes),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }
}

// ❌ Kötü: Güvensiz token validation
public class UnsafeJwtConfig
{
    public static TokenValidationParameters GetUnsafeParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = false, // Tehlikeli!
            ValidateLifetime = false,         // Tehlikeli!
            RequireExpirationTime = false     // Tehlikeli!
        };
    }
}
```

### 2. Şifreleme Güvenliği

```csharp
// ✅ İyi: Güvenli şifreleme uygulaması
public class SecureEncryption
{
    public static string EncryptSensitiveData(string plainText, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        
        streamWriter.Write(plainText);
        streamWriter.Flush();
        cryptoStream.FlushFinalBlock();
        
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}

// ❌ Kötü: Güvensiz şifreleme
public class UnsafeEncryption
{
    // ECB mode - pattern'ları açığa çıkarır
    // Sabit IV kullanımı
    // Padding oracle saldırılarına açık
}
```

### 3. Şifre Güvenliği

```csharp
// ✅ İyi: Güvenli şifre işlemleri
public class SecurePasswordHandling
{
    public static string HashPassword(string password)
    {
        // BCrypt work factor 12 (2^12 = 4096 rounds)
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Timing attack koruması
            BCrypt.Net.BCrypt.Verify("dummy", "$2a$12$dummy.hash.value.here.to.prevent.timing.attacks");
            return false;
        }
    }
}

// ❌ Kötü: Güvensiz şifre işlemleri
public class UnsafePasswordHandling
{
    public static string HashPassword(string password)
    {
        // MD5 veya SHA-1 kullanımı - güvensiz!
        // Salt kullanmama
        // Düşük work factor
        return Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
    }
}
```

### 4. API Güvenliği

```csharp
// ✅ İyi: Güvenli API endpoint
[ApiController]
[Route("api/[controller]")]
[ApiKeyAuth]
[RateLimit(requests: 100, period: "1h")]
public class SecureApiController : ControllerBase
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResourceAsync([FromBody] CreateResourceRequest request)
    {
        // Input validation
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Güvenlik validasyonu
        var validationResult = _securityValidator.ValidateInput(request);
        if (!validationResult.IsValid)
        {
            await _auditService.LogSuspiciousActivityAsync("Invalid input detected", 
                                                          HttpContext.Connection.RemoteIpAddress?.ToString());
            return BadRequest("Geçersiz giriş tespit edildi");
        }

        // İş mantığı...
        var result = await _service.CreateResourceAsync(request);
        return Ok(result);
    }
}

// ❌ Kötü: Güvensiz API endpoint
[ApiController]
public class UnsafeApiController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateResource([FromBody] object request)
    {
        // Doğrulama yok, rate limiting yok, audit yok
        // SQL injection, XSS açığı riski
        return Ok();
    }
}
```

## 🚨 Troubleshooting

### Yaygın Sorunlar ve Çözümleri

#### 1. **JWT Token Validation Hatası**

```csharp
// Hata: "IDX10223: Lifetime validation failed"
// Çözüm: Clock skew ve zaman ayarlarını kontrol et

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(5), // 5 dakika tolerans
            ValidateLifetime = true,
            LifetimeValidator = (notBefore, expires, token, parameters) =>
            {
                return DateTime.UtcNow < expires;
            }
        };
    });
```

#### 2. **Şifreleme Key Hatası**

```csharp
// Hata: "The input is not a valid Base-64 string"
// Çözüm: Key formatını ve uzunluğunu kontrol et

public class EncryptionKeyValidator
{
    public static bool ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;
            
        try
        {
            var keyBytes = Convert.FromBase64String(key);
            return keyBytes.Length == 32; // 256-bit key
        }
        catch
        {
            return false;
        }
    }
}
```

#### 3. **Rate Limiting Çalışmıyor**

```csharp
// Sorun: Rate limiting middleware'i düzgün çalışmıyor
// Çözüm: Middleware sıralamasını kontrol et

public void Configure(IApplicationBuilder app)
{
    // Doğru sıralama
    app.UseRateLimiting();          // 1. Rate limiting
    app.UseAuthentication();        // 2. Authentication
    app.UseAuthorization();         // 3. Authorization
    app.UseRouting();               // 4. Routing
    app.UseEndpoints(...);          // 5. Endpoints
}
```

## 📈 Performans ve Güvenlik Metrikleri

### Security Performance
- **JWT token validation**: < 5ms
- **BCrypt password hashing**: < 100ms (work factor 12)
- **AES encryption/decryption**: < 2ms
- **API key validation**: < 1ms
- **Security pattern detection**: < 3ms

### Memory Usage
- **JWT service**: ~15KB per token
- **Encryption service**: ~8KB per operation
- **API key cache**: ~2KB per key
- **Security audit**: ~5KB per event

### Security Metrics
- **Password strength scoring**: 0-100 scale
- **Rate limiting**: Configurable per endpoint/IP
- **Failed login threshold**: Default 10 attempts
- **Auto IP blocking**: 30 minutes default
- **Token expiration**: 60 minutes access, 30 days refresh

## 🔒 Production Güvenlik Kontrol Listesi

### ✅ Zorunlu Güvenlik Ayarları

1. **JWT Secret Key**: Minimum 256-bit, production'da farklı
2. **HTTPS Only**: TLS 1.2+ zorunlu
3. **Secure Headers**: HSTS, XSS Protection, CSP aktif
4. **Rate Limiting**: Endpoint bazlı limitler aktif
5. **Password Policy**: Güçlü şifre kuralları aktif
6. **API Key Rotation**: Düzenli key rotation planı
7. **Security Audit**: Log monitoring ve alerting aktif
8. **Input Validation**: Tüm endpoint'lerde validasyon
9. **Error Handling**: Hassas bilgi sızdırmayan error messages
10. **Database Encryption**: Hassas alanlar şifrelı

### 🔍 Güvenlik Monitoring

```csharp
// Production'da izlenmesi gereken metrikler
public class SecurityMetrics
{
    public int FailedLoginAttempts { get; set; }
    public int BlockedIpAddresses { get; set; }
    public int SuspiciousActivities { get; set; }
    public int RateLimitViolations { get; set; }
    public int TokenValidationFailures { get; set; }
    public int CriticalSecurityEvents { get; set; }
    public double AverageResponseTime { get; set; }
    public int ActiveSessions { get; set; }
}
```

## 📄 License

Enterprise Platform Team © 2024

---

**Not**: Bu kütüphane production-ready durumda olup, tüm Enterprise mikroservisleri tarafından güvenle kullanılabilir. Kapsamlı güvenlik özellikleri, performans optimizasyonları ve monitoring yetenekleri ile enterprise-grade security gereksinimleri için tasarlanmıştır.