# Enterprise.Shared.Common

**Versiyon:** 1.0.0  
**Hedef Framework:** .NET 8.0  
**Geliştirici:** Enterprise Platform Team

## 📋 Proje Amacı

Enterprise.Shared.Common, Enterprise mikroservis platformundaki tüm servislerin ortak kullanabileceği temel sınıfları, yardımcı metodları, sabitleri ve arayüzleri sağlayan temel kütüphanedir. Bu kütüphane, kod tekrarını önlemek, tutarlılığı sağlamak ve geliştirme sürecini hızlandırmak amacıyla oluşturulmuştur.

## 🎯 Ne Yapar?

Bu kütüphane şu temel fonksiyonları sağlar:

### 1. **Temel Entity Sınıfları**
- `BaseEntity`: Integer ID'li temel entity sınıfı
- `BaseGuidEntity`: GUID ID'li temel entity sınıfı  
- `SoftDeleteEntity`: Soft delete destekli entity sınıfı
- `IAuditable` ve `ISoftDelete` arayüzleri

### 2. **Result Pattern Implementation**
- `Result` ve `Result<T>` sınıfları
- Hata yönetimi ve operasyon sonuçlarının standart şekilde dönülmesi
- Fluent API desteği

### 3. **API Response Wrappers**
- `ApiResponse` ve `ApiResponse<T>` sınıfları
- `PagedApiResponse<T>` sınıfı
- Standart HTTP response formatı

### 4. **String Extensions**
- E-posta, telefon, URL validasyonları
- Case dönüşümleri (camelCase, PascalCase, kebab-case, snake_case)
- Slug oluşturma
- Masking işlemleri (e-posta, telefon, kredi kartı)
- Hash işlemleri (SHA256, MD5)
- Base64 encoding/decoding

### 5. **DateTime Extensions**
- Tarih formatlamaları
- Zaman dilimi dönüşümleri
- Business günleri hesaplama

### 6. **Collection Extensions**
- Güvenli enumeration işlemleri
- Batch processing
- Chunk işlemleri

### 7. **Repository Pattern**
- Generic repository arayüzleri
- Unit of Work pattern desteği
- Async CRUD operasyonları
- Pagination desteği

### 8. **Paginated Models**
- `PagedRequest` ve `PagedResult<T>` sınıfları
- Sorting ve filtering desteği

### 9. **Common Enumerations**
- Status, priority, file type enumerations
- HTTP method, data format enumerations
- Language, timezone enumerations

### 10. **Exception Types**
- `ValidationException`
- `BusinessException`
- `NotFoundException`
- `SecurityException`

### 11. **Constants ve Helpers**
- HTTP header names
- Validation constants
- Regex patterns
- Configuration keys
- TimeZone helper utilities

## 🚀 Nasıl Kullanılır?

### Kurulum

```xml
<PackageReference Include="Enterprise.Shared.Common" Version="1.0.0" />
```

### Temel Kullanım Örnekleri

#### 1. Entity Sınıfları

```csharp
public class User : BaseEntity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class Product : SoftDeleteEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

#### 2. Result Pattern

```csharp
public async Task<Result<User>> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        return Result<User>.Failure("User not found");
    
    return Result<User>.Success(user);
}

// Kullanım
var result = await GetUserAsync(1);
if (result.IsSuccess)
{
    var user = result.Value;
    // İşlemler...
}
else
{
    var error = result.Error;
    // Hata yönetimi...
}
```

#### 3. API Response

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
{
    var result = await _userService.GetUserAsync(id);
    var response = ApiResponse<UserDto>.FromResult(result);
    
    return StatusCode(response.StatusCode, response);
}
```

#### 4. String Extensions

```csharp
// Validation
var isValidEmail = "user@example.com".IsValidEmail();
var isValidPhone = "+905551234567".IsValidPhone();

// Case Conversions
var kebabCase = "HelloWorld".ToKebabCase(); // "hello-world"
var camelCase = "hello-world".ToCamelCase(); // "helloWorld"
var slug = "Hello World!".ToSlug(); // "hello-world"

// Masking
var maskedEmail = "user@example.com".MaskEmail(); // "u***@example.com"
var maskedPhone = "05551234567".MaskPhone(); // "*******4567"

// Hashing
var sha256Hash = "password".ToSha256();
var base64 = "text".ToBase64();
```

#### 5. DateTime Extensions

```csharp
// Timezone dönüşümü
var turkeyTime = DateTime.UtcNow.ToTurkeyTime();
var utcTime = DateTime.Now.ToUtc();

// Business günleri
var businessDays = DateTime.Today.GetBusinessDaysBetween(DateTime.Today.AddDays(10));
var nextBusinessDay = DateTime.Today.GetNextBusinessDay();

// Formatlar
var isoFormat = DateTime.Now.ToIso8601String();
var shortDate = DateTime.Now.ToShortDateString();
```

#### 6. Repository Pattern

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}

public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<User>.Success(user);
    }
}
```

#### 7. Pagination

```csharp
public async Task<PagedResult<UserDto>> GetUsersAsync(PagedRequest request)
{
    var pagedResult = await _repository.GetPagedAsync(request);
    
    return new PagedResult<UserDto>
    {
        Data = pagedResult.Data.Select(MapToDto).ToList(),
        Page = pagedResult.Page,
        PageSize = pagedResult.PageSize,
        TotalCount = pagedResult.TotalCount
    };
}
```

#### 8. Constants Kullanımı

```csharp
// HTTP headers
request.Headers.Add(CommonConstants.CorrelationIdHeader, correlationId);

// Validation
if (password.Length < CommonConstants.MinPasswordLength)
    return Result.Failure("Password too short");

// Cache keys
var cacheKey = $"user{CommonConstants.CacheKeySeparator}{userId}";

// Error messages
return Result.Failure(CommonConstants.NotFoundErrorMessage);
```

## 🛠 Kullanılan Teknolojiler

### Core Technologies
- **.NET 8.0**: Hedef framework
- **C# 12.0**: Programlama dili
- **System.Text.Json**: JSON serialization
- **System.ComponentModel.Annotations**: Data annotations

### Development Tools
- **Source Generators**: Regex pattern generation için
- **Nullable Reference Types**: Type safety için
- **Global Usings**: Import management için
- **File-scoped namespaces**: Modern C# syntax

### Code Quality
- **TreatWarningsAsErrors**: Warning'ları error olarak treat etme
- **GenerateDocumentationFile**: XML documentation generation
- **Implicit Usings**: Otomatik using statements

### Testing
- **190 Unit Test**: %100 code coverage
- **xUnit**: Test framework
- **FluentAssertions**: Test assertions

## 📁 Proje Yapısı

```
Enterprise.Shared.Common/
├── Constants/
│   └── CommonConstants.cs          # Uygulama sabitleri
├── Entities/
│   └── BaseEntity.cs               # Temel entity sınıfları
├── Enums/
│   └── CommonEnums.cs              # Ortak enum'lar
├── Exceptions/
│   └── CommonExceptions.cs         # Özel exception sınıfları
├── Extensions/
│   ├── CollectionExtensions.cs     # Collection extension metodları
│   ├── DateTimeExtensions.cs       # DateTime extension metodları
│   └── StringExtensions.cs         # String extension metodları
├── Interfaces/
│   ├── IRepository.cs              # Repository pattern arayüzleri
│   └── IUnitOfWork.cs              # Unit of Work pattern arayüzü
├── Models/
│   ├── ApiResponse.cs              # API response wrapper'ları
│   ├── PagedModels.cs              # Pagination modelleri
│   └── Result.cs                   # Result pattern implementation
├── Utilities/
│   └── TimeZoneHelper.cs           # Timezone yardımcı sınıfı
└── GlobalUsings.cs                 # Global using statements
```

## 🔧 Configuration

### appsettings.json

```json
{
  "Common": {
    "DefaultPageSize": 10,
    "MaxPageSize": 100,
    "DefaultTimeZone": "Europe/Istanbul",
    "EnableValidation": true
  }
}
```

### Dependency Injection

```csharp
services.AddScoped<IRepository<User>, UserRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<TimeZoneHelper>();
```

## 🧪 Test Coverage

Proje **190 adet unit test** ile **%100 kod coverage**'a sahiptir:

- **Constants Tests**: Sabit değerlerin doğruluğu
- **Extensions Tests**: Tüm extension metodlarının test edilmesi
- **Models Tests**: Result ve ApiResponse sınıflarının tüm senaryoları
- **Entities Tests**: Base entity sınıflarının davranışları
- **Utilities Tests**: Helper sınıflarının fonksiyonalitesi

```bash
# Testleri çalıştırma
dotnet test

# Sonuç: Passed: 190, Failed: 0, Skipped: 0
```

## 📊 Performance Considerations

### String Operations
- Compiled regex patterns kullanımı
- StringBuilder kullanımı büyük string işlemlerinde
- Memory-efficient string operations

### Collection Operations
- LINQ optimizasyonları
- Lazy evaluation
- Batch processing desteği

### Async Operations
- ConfigureAwait(false) kullanımı
- CancellationToken desteği
- Memory-efficient async operations

## 🔐 Security Features

### Data Protection
- Email, phone, credit card masking
- SHA256 ve MD5 hashing
- Base64 encoding güvenliği

### Input Validation
- SQL injection korunması
- XSS korunması
- Input sanitization

### Error Handling
- Güvenli error messages
- Exception details masking
- Audit trail support

## 🌐 Globalization

### Multi-language Support
- Language code enumerations
- Culture-specific formatting
- Timezone conversions

### Localization Ready
- Resource key patterns
- Culture-aware string operations
- Date/time formatting

## 💡 Best Practices

### Coding Standards
- SOLID principles uygulanması
- Clean Code principles
- Consistent naming conventions
- Comprehensive documentation

### Error Handling
- Result pattern kullanımı
- Proper exception types
- Correlation ID tracking

### Performance
- Async/await best practices
- Memory management
- Resource disposal patterns

## 🔄 Migration Guide

### v1.0.0'dan sonraki versiyonlara geçiş için:

1. NuGet paketini güncelleyin
2. Breaking change'leri kontrol edin
3. Deprecated metodları güncelleyin
4. Test coverage'ı çalıştırın

## 🤝 Contributing

1. Feature branch oluşturun
2. Kod değişikliklerini yapın
3. Unit testler yazın
4. Code review için PR açın

## 📄 License

Enterprise Platform Team © 2024

---

**Not**: Bu kütüphane production-ready durumda olup, tüm Enterprise mikroservisleri tarafından güvenle kullanılabilir.