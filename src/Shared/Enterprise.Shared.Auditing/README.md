# Enterprise.Shared.Auditing

**Versiyon:** 1.0.0  
**Hedef Framework:** .NET 8.0  
**Geliştirici:** Enterprise Platform Team

## 📋 Proje Amacı

Enterprise.Shared.Auditing, Enterprise mikroservis platformu için geliştirilmiş kapsamlı bir audit trail ve activity tracking kütüphanesidir. Kullanıcı aktivitelerinin, veri değişikliklerinin, güvenlik olaylarının ve sistem operasyonlarının izlenmesi için enterprise-grade auditing çözümleri sunar. Compliance gereksinimleri (GDPR, SOX, PCI-DSS) ve güvenlik standartlarına uygun audit logging sağlar.

## 🌟 Ana Özellikler

### Comprehensive Audit Logging
- **Automatic Activity Tracking**: HTTP request/response otomatik takibi
- **Method-Level Auditing**: AOP ile method seviyesinde audit
- **Security Event Auditing**: Authentication/authorization olayları
- **Data Change Tracking**: Entity değişikliklerinin detaylı kaydı

### Enterprise-Grade Features
- **Flexible Storage**: In-memory, database ve custom store desteği  
- **Risk Assessment**: Güvenlik olayları için risk skorlama
- **Correlation Tracking**: Distributed tracing ile event korelasyonu
- **Batch Processing**: Yüksek performans için batch işleme

### Compliance & Security
- **GDPR Compliance**: Kişisel veri işleme audit'i
- **Security Alerts**: Yüksek riskli olaylar için otomatik uyarı
- **Data Retention**: Yapılandırılabilir veri saklama politikaları
- **Access Control**: Role-based audit access kontrolü

### Monitoring & Analytics
- **Health Checks**: Audit sistemi sağlık kontrolü
- **Performance Metrics**: Audit operasyon metrikleri
- **Search & Filter**: Güçlü arama ve filtreleme özellikleri
- **Reporting**: Compliance ve security raporlama

## 🛠 Kullanılan Teknolojiler

### Ana Bağımlılıklar
- **Castle.Core 5.1.1**: AOP method interceptors için
- **System.Text.Json 8.0.5**: JSON serialization
- **Microsoft.AspNetCore.App**: HTTP middleware desteği
- **Microsoft.EntityFrameworkCore.Abstractions 8.0.8**: Database abstraction

### Microsoft Extensions
- **Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2**: DI support
- **Microsoft.Extensions.Logging.Abstractions 8.0.2**: Structured logging
- **Microsoft.Extensions.Options 8.0.2**: Configuration options pattern
- **Microsoft.Extensions.Configuration.Abstractions 8.0.0**: Configuration
- **Microsoft.Extensions.Hosting.Abstractions 8.0.1**: Background services
- **Microsoft.Extensions.Diagnostics.HealthChecks 8.0.10**: Health monitoring

## ⚙️ Kurulum ve Konfigürasyon

### 1. NuGet Paketi Yükleme
```bash
dotnet add package Enterprise.Shared.Auditing
```

### 2. Dependency Injection Konfigürasyonu

#### Basic Setup (Program.cs)
```csharp
using Enterprise.Shared.Auditing.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Basic audit services
builder.Services.AddAuditing(builder.Configuration);

// In-memory store (development)
builder.Services.AddInMemoryAuditStore();

// Health checks
builder.Services.AddAuditHealthChecks();

var app = builder.Build();

// Audit middleware
app.UseCorrelationId(); // Correlation ID tracking
app.UseAuditing();      // HTTP request auditing

app.Run();
```

#### Development Setup
```csharp
// Development-specific configuration
builder.AddDevelopmentAuditing();

// Development middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCorrelationId();
    app.UseAuditing();
}
```

#### Production Setup
```csharp
// Production-ready configuration
builder.AddProductionAuditing();

// Add custom audit store
builder.Services.AddAuditStore<DatabaseAuditStore>();

// Production middleware pipeline
app.UseCorrelationId();
app.UseAuditing();
```

#### Advanced Configuration
```csharp
// Custom audit configuration
builder.Services.AddAuditing(options =>
{
    options.Enabled = true;
    options.DefaultEnvironment = "Production";
    options.EnableBatchProcessing = true;
    
    // Performance settings
    options.Performance.UseAsyncProcessing = true;
    options.Performance.BatchSize = 500;
    options.Performance.FlushIntervalMs = 30000;
    
    // Retention settings
    options.Retention.EnableAutoPurge = true;
    options.Retention.RetentionDays = 2555; // 7 years
    options.Retention.PurgeIntervalDays = 30;
    
    // Security settings
    options.Security.EncryptSensitiveData = true;
    options.Security.EnableAlerting = true;
    options.Security.RiskThreshold = 75;
    
    // Filtering
    options.Filter.ExcludedPaths = new[] { "/health", "/metrics" };
    options.Filter.ExcludedProperties = new[] { "Password", "Token" };
});

// AOP interceptors
builder.Services.AddAuditInterceptors();
```

### 3. appsettings.json Konfigürasyonu

```json
{
  "AuditConfiguration": {
    "Enabled": true,
    "DefaultEnvironment": "Production",
    "ServiceName": "MyMicroservice",
    "EnableBatchProcessing": true,
    
    "Performance": {
      "UseAsyncProcessing": true,
      "BatchSize": 500,
      "FlushIntervalMs": 30000,
      "MaxConcurrentOperations": 10,
      "EnableBackgroundProcessing": true
    },
    
    "Retention": {
      "EnableAutoPurge": true,
      "RetentionDays": 2555,
      "PurgeIntervalDays": 30,
      "ArchiveOldRecords": true
    },
    
    "Security": {
      "EncryptSensitiveData": true,
      "EnableAlerting": true,
      "RiskThreshold": 75,
      "EnableIntegrityChecks": true,
      "SensitiveDataFields": ["Password", "CreditCard", "SSN"]
    },
    
    "Filter": {
      "ExcludedPaths": ["/health", "/metrics", "/swagger"],
      "ExcludedProperties": ["Password", "PasswordHash", "Token"],
      "ExcludedUserAgents": ["HealthCheck", "Prometheus"],
      "MinLogLevel": "Information"
    },
    
    "Storage": {
      "StoreType": "Database",
      "ConnectionString": "Server=localhost;Database=EnterpriseAudit;Trusted_Connection=true;",
      "TableName": "AuditEvents",
      "EnableCompression": true
    }
  },
  
  "Logging": {
    "LogLevel": {
      "Enterprise.Shared.Auditing": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## 📖 Kullanım Kılavuzu

### 1. Basic Audit Logging

#### Manual Audit Logging
```csharp
public class UserController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<UserController> _logger;
    
    public UserController(IAuditService auditService, ILogger<UserController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        try
        {
            // Business logic
            var user = await _userService.CreateUserAsync(request);
            
            // Audit logging
            var auditEvent = AuditEvent.Create("CREATE_USER", "User", "Success")
                .WithUser(User.Identity.Name, User.FindFirst("sub")?.Value)
                .WithHttpContext(HttpContext.Connection.RemoteIpAddress?.ToString(), 
                               Request.Headers["User-Agent"])
                .WithCorrelation(HttpContext.TraceIdentifier)
                .WithProperty("UserId", user.Id)
                .WithProperty("Email", user.Email)
                .WithTags("user-management", "creation")
                .WithSeverity(AuditSeverity.Information);
            
            await _auditService.LogEventAsync(auditEvent);
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            // Audit failed operation
            var auditEvent = AuditEvent.Create("CREATE_USER", "User", "Failed")
                .WithUser(User.Identity.Name)
                .WithProperty("Error", ex.Message)
                .WithSeverity(AuditSeverity.Error);
                
            await _auditService.LogEventAsync(auditEvent);
            throw;
        }
    }
}
```

#### Fluent API Usage
```csharp
// Complex audit event with fluent API
var auditEvent = AuditEvent.Create("UPDATE_PROFILE", "UserProfile")
    .WithUser(userId: "12345", username: "john.doe")
    .WithHttpContext(ipAddress: "192.168.1.100", userAgent: "Mozilla/5.0...")
    .WithCorrelation(correlationId: Guid.NewGuid().ToString(), traceId: Activity.Current?.Id)
    .WithDuration(durationMs: stopwatch.ElapsedMilliseconds)
    .WithCategory(AuditEventCategory.DataAccess)
    .WithSeverity(AuditSeverity.Information)
    .WithTags("profile", "personal-data", "gdpr")
    .WithProperty("Fields", new[] { "FirstName", "LastName", "Email" })
    .WithProperty("PreviousValues", previousData)
    .WithProperty("NewValues", newData)
    .WithMetadata(new { 
        Department = "IT", 
        Manager = "jane.smith",
        DataClassification = "Personal"
    });

await _auditService.LogEventAsync(auditEvent);
```

### 2. Security Event Auditing

#### Authentication Events
```csharp
public class AuthenticationService
{
    private readonly IAuditService _auditService;
    
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var securityEvent = SecurityAuditEvent.Create(
            SecurityEventType.Authentication,
            "USER_LOGIN", 
            "AuthenticationSystem"
        );
        
        try
        {
            // Authentication logic
            var result = await PerformAuthenticationAsync(request);
            
            if (result.IsSuccess)
            {
                securityEvent.Outcome = SecurityOutcome.Success;
                securityEvent
                    .WithUser(result.UserId, result.Username)
                    .WithAuthentication("OAuth2", result.Role)
                    .WithRisk(CalculateRiskScore(request), isAlert: false)
                    .WithDevice(geoLocation: await GetGeoLocationAsync(request.IpAddress));
            }
            else
            {
                securityEvent.Outcome = SecurityOutcome.Failed;
                securityEvent
                    .WithRisk(85, isAlert: true) // High risk for failed login
                    .WithProperty("FailureReason", result.ErrorMessage);
            }
            
            await _auditService.LogSecurityEventAsync(securityEvent);
            return result;
        }
        catch (Exception ex)
        {
            securityEvent.Outcome = SecurityOutcome.Error;
            securityEvent.WithProperty("Exception", ex.Message);
            await _auditService.LogSecurityEventAsync(securityEvent);
            throw;
        }
    }
}
```

#### Authorization Events
```csharp
public class AuthorizationService
{
    public async Task<bool> AuthorizeAsync(string userId, string resource, string action)
    {
        var securityEvent = SecurityAuditEvent.Create(
            SecurityEventType.Authorization,
            $"AUTHORIZE_{action.ToUpper()}",
            resource
        )
        .WithUser(userId)
        .WithAuthorization(permission: $"{resource}:{action}");
        
        var isAuthorized = await CheckPermissionAsync(userId, resource, action);
        
        securityEvent.Outcome = isAuthorized ? SecurityOutcome.Success : SecurityOutcome.Denied;
        securityEvent.WithRisk(isAuthorized ? 0 : 50);
        
        await _auditService.LogSecurityEventAsync(securityEvent);
        
        return isAuthorized;
    }
}
```

#### Administrative Actions
```csharp
public class AdminService
{
    public async Task ResetUserPasswordAsync(string adminId, string targetUserId)
    {
        var securityEvent = SecurityAuditEvent.Create(
            SecurityEventType.AccountManagement,
            "ADMIN_PASSWORD_RESET",
            "UserAccount"
        )
        .WithUser(adminId)
        .WithTarget(targetUserId)
        .WithRisk(60, isAlert: true) // Administrative actions are monitored
        .WithProperty("AdminAction", "PasswordReset")
        .WithTags("privileged-access", "user-management");
        
        try
        {
            await PerformPasswordResetAsync(targetUserId);
            securityEvent.Outcome = SecurityOutcome.Success;
        }
        catch (Exception ex)
        {
            securityEvent.Outcome = SecurityOutcome.Failed;
            securityEvent.WithProperty("Error", ex.Message);
            throw;
        }
        finally
        {
            await _auditService.LogSecurityEventAsync(securityEvent);
        }
    }
}
```

### 3. AOP-Based Automatic Auditing

#### Attribute-Based Auditing
```csharp
// Audit attribute definition
[AttributeUsage(AttributeTargets.Method)]
public class AuditAttribute : Attribute
{
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public bool IncludeParameters { get; set; } = true;
    public bool IncludeReturnValue { get; set; } = false;
    public string Properties { get; set; } = string.Empty;
    public AuditEventCategory Category { get; set; } = AuditEventCategory.Application;
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
}

// Service with audit attributes
public class ProductService
{
    [Audit(Action = "CREATE_PRODUCT", Resource = "Product", IncludeParameters = true)]
    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        // Method implementation
        return new Product { Id = Guid.NewGuid(), Name = request.Name };
    }
    
    [Audit(Action = "DELETE_PRODUCT", Resource = "Product", 
           Severity = AuditSeverity.Warning, Category = AuditEventCategory.DataAccess)]
    public async Task DeleteProductAsync(Guid productId)
    {
        // Deletion logic
    }
    
    [Audit(Action = "GET_SENSITIVE_DATA", Resource = "CustomerData", 
           IncludeParameters = false, IncludeReturnValue = false)]
    public async Task<SensitiveData> GetSensitiveDataAsync(Guid customerId)
    {
        // Don't log sensitive parameters or return values
        return await _repository.GetSensitiveDataAsync(customerId);
    }
}
```

#### Proxy-Based Service Registration
```csharp
// Register service with audit proxy
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditedService<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TImplementation>();
        
        services.AddScoped<TInterface>(provider =>
        {
            var implementation = provider.GetRequiredService<TImplementation>();
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var interceptor = provider.GetRequiredService<AuditInterceptor>();
            
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(
                implementation, interceptor);
        });
        
        return services;
    }
}

// Usage in Program.cs
builder.Services.AddAuditInterceptors();
builder.Services.AddAuditedService<IProductService, ProductService>();
```

### 4. HTTP Request Auditing

#### Automatic Middleware Auditing
```csharp
// Automatically audits all HTTP requests
public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // Correlation ID tracking
        app.UseCorrelationId("X-Correlation-ID");
        
        // Automatic HTTP auditing
        app.UseAuditing();
        
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

#### Custom Audit Middleware
```csharp
public class CustomAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditService _auditService;
    
    public CustomAuditMiddleware(RequestDelegate next, IAuditService auditService)
    {
        _next = next;
        _auditService = auditService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip certain paths
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        var auditEvent = AuditEvent.Create(
            $"{context.Request.Method} {context.Request.Path}",
            "HTTP_API",
            "Success"
        );
        
        try
        {
            await _next(context);
            
            // Audit successful requests
            auditEvent
                .WithHttpContext(
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers["User-Agent"]
                )
                .WithDuration(stopwatch.ElapsedMilliseconds)
                .WithProperty("StatusCode", context.Response.StatusCode)
                .WithProperty("ContentLength", context.Response.ContentLength)
                .WithCorrelation(
                    context.Request.Headers["X-Correlation-ID"],
                    Activity.Current?.Id
                );
        }
        catch (Exception ex)
        {
            auditEvent.Result = "Failed";
            auditEvent.Severity = AuditSeverity.Error;
            auditEvent.WithProperty("Exception", ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            await _auditService.LogEventAsync(auditEvent);
        }
    }
}
```

### 5. Search and Querying

#### Basic Search
```csharp
public class AuditQueryService
{
    private readonly IAuditService _auditService;
    
    public async Task<(List<AuditEvent> Events, int TotalCount)> SearchEventsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        string? action = null,
        AuditEventCategory? category = null,
        int page = 1,
        int pageSize = 50)
    {
        var criteria = new AuditSearchCriteria
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
            EndDate = endDate ?? DateTime.UtcNow,
            UserId = userId,
            Action = action,
            Category = category,
            Page = page,
            PageSize = pageSize,
            OrderBy = "Timestamp",
            OrderDirection = "DESC"
        };
        
        return await _auditService.SearchEventsAsync(criteria);
    }
}
```

#### Advanced Search with Filters
```csharp
public async Task<IActionResult> GetAuditReport(AuditReportRequest request)
{
    var criteria = new AuditSearchCriteria
    {
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        UserIds = request.UserIds,
        Actions = request.Actions,
        Resources = request.Resources,
        Categories = request.Categories,
        Severities = request.Severities,
        Results = new[] { "Success", "Failed" },
        IpAddresses = request.IpAddresses,
        Tags = request.Tags,
        
        // Text search
        SearchText = request.SearchText,
        
        // Property filters
        PropertyFilters = new Dictionary<string, object>
        {
            { "Department", request.Department },
            { "MinRiskScore", request.MinRiskScore }
        },
        
        // Pagination and sorting
        Page = request.Page,
        PageSize = request.PageSize,
        OrderBy = request.OrderBy ?? "Timestamp",
        OrderDirection = request.OrderDirection ?? "DESC"
    };
    
    var (events, totalCount) = await _auditService.SearchEventsAsync(criteria);
    
    return Ok(new AuditReportResponse
    {
        Events = events.Select(e => new AuditEventDto(e)).ToList(),
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
    });
}
```

#### Security Event Analysis
```csharp
public class SecurityAnalyticsService
{
    public async Task<SecurityAnalyticsReport> GenerateSecurityReportAsync(
        DateTime startDate, DateTime endDate)
    {
        var criteria = new AuditSearchCriteria
        {
            StartDate = startDate,
            EndDate = endDate,
            Category = AuditEventCategory.Security,
            PageSize = int.MaxValue
        };
        
        var (events, _) = await _auditService.SearchEventsAsync(criteria);
        var securityEvents = events.OfType<SecurityAuditEvent>().ToList();
        
        return new SecurityAnalyticsReport
        {
            TotalEvents = securityEvents.Count,
            FailedLogins = securityEvents.Count(e => 
                e.EventType == SecurityEventType.Authentication && 
                e.Outcome == SecurityOutcome.Failed),
            HighRiskEvents = securityEvents.Count(e => e.RiskScore >= 75),
            AlertEvents = securityEvents.Count(e => e.IsAlert),
            TopUsers = securityEvents
                .GroupBy(e => e.UserId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { UserId = g.Key, EventCount = g.Count() })
                .ToList(),
            EventsByType = securityEvents
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }
}
```

## 🏗 Custom Audit Store Implementation

### Database Audit Store
```csharp
public class DatabaseAuditStore : IAuditStore
{
    private readonly IDbContextFactory<AuditDbContext> _contextFactory;
    private readonly ILogger<DatabaseAuditStore> _logger;
    
    public DatabaseAuditStore(
        IDbContextFactory<AuditDbContext> contextFactory,
        ILogger<DatabaseAuditStore> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }
    
    public async Task<Result> StoreEventAsync(
        AuditEvent auditEvent, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var entity = new AuditEventEntity
            {
                Id = auditEvent.Id,
                Timestamp = auditEvent.Timestamp,
                Action = auditEvent.Action,
                Resource = auditEvent.Resource,
                ResourceId = auditEvent.ResourceId,
                Result = auditEvent.Result,
                UserId = auditEvent.UserId,
                Username = auditEvent.Username,
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                ServiceName = auditEvent.ServiceName,
                CorrelationId = auditEvent.CorrelationId,
                TraceId = auditEvent.TraceId,
                Category = auditEvent.Category,
                Severity = auditEvent.Severity,
                DurationMs = auditEvent.DurationMs,
                Environment = auditEvent.Environment,
                Metadata = auditEvent.Metadata,
                Details = auditEvent.Details,
                PropertiesJson = auditEvent.PropertiesJson,
                TagsJson = JsonSerializer.Serialize(auditEvent.Tags)
            };
            
            context.AuditEvents.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing audit event to database");
            return Result.Failure($"Database error: {ex.Message}");
        }
    }
    
    public async Task<(List<AuditEvent> Events, int TotalCount)> QueryEventsAsync(
        AuditSearchCriteria criteria, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var query = context.AuditEvents.AsQueryable();
            
            // Apply filters
            if (criteria.StartDate.HasValue)
                query = query.Where(e => e.Timestamp >= criteria.StartDate.Value);
                
            if (criteria.EndDate.HasValue)
                query = query.Where(e => e.Timestamp <= criteria.EndDate.Value);
                
            if (!string.IsNullOrEmpty(criteria.UserId))
                query = query.Where(e => e.UserId == criteria.UserId);
                
            if (!string.IsNullOrEmpty(criteria.Action))
                query = query.Where(e => e.Action == criteria.Action);
                
            if (criteria.Category.HasValue)
                query = query.Where(e => e.Category == criteria.Category.Value);
                
            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);
            
            // Apply sorting
            query = criteria.OrderDirection?.ToLower() == "asc"
                ? query.OrderBy(e => EF.Property<object>(e, criteria.OrderBy ?? "Timestamp"))
                : query.OrderByDescending(e => EF.Property<object>(e, criteria.OrderBy ?? "Timestamp"));
                
            // Apply pagination
            query = query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize);
            
            var entities = await query.ToListAsync(cancellationToken);
            var events = entities.Select(MapToAuditEvent).ToList();
            
            return (events, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying audit events from database");
            throw;
        }
    }
    
    private static AuditEvent MapToAuditEvent(AuditEventEntity entity)
    {
        var auditEvent = new AuditEvent
        {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            Action = entity.Action,
            Resource = entity.Resource,
            ResourceId = entity.ResourceId,
            Result = entity.Result,
            UserId = entity.UserId,
            Username = entity.Username,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            ServiceName = entity.ServiceName,
            CorrelationId = entity.CorrelationId,
            TraceId = entity.TraceId,
            Category = entity.Category,
            Severity = entity.Severity,
            DurationMs = entity.DurationMs,
            Environment = entity.Environment,
            Metadata = entity.Metadata,
            Details = entity.Details
        };
        
        // Deserialize properties and tags
        if (!string.IsNullOrEmpty(entity.PropertiesJson))
        {
            try
            {
                auditEvent.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.PropertiesJson) ?? new();
            }
            catch { }
        }
        
        if (!string.IsNullOrEmpty(entity.TagsJson))
        {
            try
            {
                auditEvent.Tags = JsonSerializer.Deserialize<List<string>>(
                    entity.TagsJson) ?? new();
            }
            catch { }
        }
        
        return auditEvent;
    }
}
```

### Service Registration
```csharp
// Register custom audit store
builder.Services.AddDbContextFactory<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuditDatabase")));
    
builder.Services.AddAuditStore<DatabaseAuditStore>();
```

## 📊 Monitoring ve Health Checks

### Health Check Implementation
```csharp
public class CustomAuditHealthCheck : IHealthCheck
{
    private readonly IAuditService _auditService;
    private readonly IAuditStore _auditStore;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test audit store connectivity
            var healthResult = await _auditStore.HealthCheckAsync(cancellationToken);
            if (!healthResult.IsSuccess)
            {
                return HealthCheckResult.Unhealthy(
                    "Audit store health check failed",
                    data: new Dictionary<string, object> 
                    { 
                        { "error", healthResult.Error ?? "Unknown error" } 
                    });
            }
            
            // Test audit service functionality
            var testEvent = AuditEvent.Create("HEALTH_CHECK", "System", "Success");
            var logResult = await _auditService.LogEventAsync(testEvent, cancellationToken);
            
            if (!logResult.IsSuccess)
            {
                return HealthCheckResult.Degraded(
                    "Audit service test failed",
                    data: new Dictionary<string, object> 
                    { 
                        { "error", logResult.Error ?? "Service test failed" } 
                    });
            }
            
            return HealthCheckResult.Healthy("Audit system is functioning properly");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Audit system health check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "exception", ex.Message }
                });
        }
    }
}

// Registration
builder.Services.AddHealthChecks()
    .AddCheck<CustomAuditHealthCheck>("custom_audit")
    .AddCheck<AuditHealthCheck>("audit_system");
```

### Performance Metrics
```csharp
public class AuditMetricsService
{
    private readonly IMetrics _metrics;
    private readonly Counter<long> _auditEventCounter;
    private readonly Histogram<double> _auditDuration;
    
    public AuditMetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Enterprise.Shared.Auditing");
        
        _auditEventCounter = meter.CreateCounter<long>(
            "audit_events_total",
            "events",
            "Total number of audit events logged");
            
        _auditDuration = meter.CreateHistogram<double>(
            "audit_operation_duration",
            "ms",
            "Duration of audit operations");
    }
    
    public void RecordAuditEvent(AuditEvent auditEvent, double durationMs)
    {
        _auditEventCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("action", auditEvent.Action),
            new("resource", auditEvent.Resource),
            new("result", auditEvent.Result),
            new("category", auditEvent.Category.ToString()),
            new("severity", auditEvent.Severity.ToString())
        });
        
        _auditDuration.Record(durationMs, new KeyValuePair<string, object?>[]
        {
            new("operation", "log_event"),
            new("category", auditEvent.Category.ToString())
        });
    }
}
```

## 🔧 Best Practices

### 1. Event Design

#### ✅ İyi Örnekler
```csharp
// Specific, meaningful action names
var auditEvent = AuditEvent.Create("UPDATE_USER_PROFILE", "UserProfile", "Success");

// Proper categorization
auditEvent.Category = AuditEventCategory.DataAccess;
auditEvent.Severity = AuditSeverity.Information;

// Meaningful tags for filtering
auditEvent.WithTags("profile", "personal-data", "gdpr-relevant");

// Structured properties
auditEvent.WithProperty("ChangedFields", new[] { "Email", "Phone" });
auditEvent.WithProperty("DataClassification", "Personal");
```

#### ❌ Kötü Örnekler
```csharp
// Generic, meaningless action names
var auditEvent = AuditEvent.Create("Update", "Data", "OK"); // Too generic

// Missing important context
auditEvent.UserId = null; // Who performed the action?
auditEvent.CorrelationId = null; // Cannot trace related events

// Sensitive data in audit
auditEvent.WithProperty("Password", "plaintext123"); // Security risk!
auditEvent.WithProperty("CreditCard", "4111111111111111"); // PCI violation!
```

### 2. Performance Optimization

```csharp
// Batch processing for high volume
public class BatchAuditService
{
    private readonly Channel<AuditEvent> _auditChannel;
    private readonly IAuditStore _auditStore;
    
    public async Task LogEventAsync(AuditEvent auditEvent)
    {
        // Non-blocking audit logging
        await _auditChannel.Writer.WriteAsync(auditEvent);
    }
    
    private async Task ProcessAuditBatch()
    {
        var batch = new List<AuditEvent>();
        
        await foreach (var auditEvent in _auditChannel.Reader.ReadAllAsync())
        {
            batch.Add(auditEvent);
            
            if (batch.Count >= 100) // Batch size
            {
                await _auditStore.StoreEventsAsync(batch);
                batch.Clear();
            }
        }
    }
}
```

### 3. Security Considerations

```csharp
// Secure audit event handling
public class SecureAuditService
{
    public async Task LogEventAsync(AuditEvent auditEvent)
    {
        // Remove sensitive data
        CleanSensitiveData(auditEvent);
        
        // Add integrity hash
        auditEvent.WithProperty("Integrity", CalculateHash(auditEvent));
        
        // Encrypt if required
        if (_configuration.Security.EncryptSensitiveData)
        {
            EncryptSensitiveFields(auditEvent);
        }
        
        await _auditStore.StoreEventAsync(auditEvent);
    }
    
    private void CleanSensitiveData(AuditEvent auditEvent)
    {
        var sensitiveFields = _configuration.Security.SensitiveDataFields;
        
        foreach (var field in sensitiveFields)
        {
            if (auditEvent.Properties.ContainsKey(field))
            {
                auditEvent.Properties[field] = "[REDACTED]";
            }
        }
    }
}
```

### 4. Compliance Support

```csharp
// GDPR compliance implementation
public class GdprAuditService
{
    public async Task LogPersonalDataAccessAsync(
        string userId, 
        string dataType, 
        string purpose)
    {
        var auditEvent = AuditEvent.Create("ACCESS_PERSONAL_DATA", dataType, "Success")
            .WithUser(userId)
            .WithTags("gdpr", "personal-data", "data-access")
            .WithProperty("DataType", dataType)
            .WithProperty("ProcessingPurpose", purpose)
            .WithProperty("LegalBasis", "Legitimate Interest")
            .WithProperty("RetentionPeriod", "7 years")
            .WithCategory(AuditEventCategory.DataAccess);
            
        await _auditService.LogEventAsync(auditEvent);
    }
    
    public async Task LogDataDeletionAsync(string userId, string reason)
    {
        var auditEvent = AuditEvent.Create("DELETE_PERSONAL_DATA", "UserData", "Success")
            .WithUser(userId)
            .WithTags("gdpr", "right-to-be-forgotten", "data-deletion")
            .WithProperty("DeletionReason", reason)
            .WithProperty("CompletionDate", DateTime.UtcNow)
            .WithSeverity(AuditSeverity.Information);
            
        await _auditService.LogEventAsync(auditEvent);
    }
}
```

## 🚀 Advanced Features

### 1. Real-time Audit Streaming
```csharp
public class AuditStreamingService
{
    private readonly IHubContext<AuditHub> _hubContext;
    
    public async Task StreamAuditEventAsync(AuditEvent auditEvent)
    {
        // Stream to connected clients
        await _hubContext.Clients.Group("AuditMonitors")
            .SendAsync("AuditEvent", auditEvent);
            
        // High-risk events to security team
        if (auditEvent is SecurityAuditEvent secEvent && secEvent.RiskScore >= 75)
        {
            await _hubContext.Clients.Group("SecurityTeam")
                .SendAsync("SecurityAlert", secEvent);
        }
    }
}
```

### 2. Event Correlation and Analysis
```csharp
public class AuditCorrelationService
{
    public async Task<List<AuditEvent>> GetRelatedEventsAsync(string correlationId)
    {
        var criteria = new AuditSearchCriteria
        {
            CorrelationId = correlationId,
            OrderBy = "Timestamp",
            OrderDirection = "ASC"
        };
        
        var (events, _) = await _auditService.SearchEventsAsync(criteria);
        return events;
    }
    
    public async Task<AuditEventChain> BuildEventChainAsync(string traceId)
    {
        var criteria = new AuditSearchCriteria { TraceId = traceId };
        var (events, _) = await _auditService.SearchEventsAsync(criteria);
        
        return new AuditEventChain
        {
            TraceId = traceId,
            Events = events.OrderBy(e => e.Timestamp).ToList(),
            Duration = events.Max(e => e.Timestamp) - events.Min(e => e.Timestamp),
            Success = events.All(e => e.Result == "Success")
        };
    }
}
```

### 3. Automated Compliance Reporting
```csharp
public class ComplianceReportService
{
    public async Task<ComplianceReport> GenerateGdprReportAsync(
        DateTime startDate, DateTime endDate)
    {
        var personalDataEvents = await GetPersonalDataEventsAsync(startDate, endDate);
        
        return new ComplianceReport
        {
            ReportType = ComplianceReportType.GDPR,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            
            DataAccessEvents = personalDataEvents
                .Where(e => e.Tags.Contains("data-access"))
                .Count(),
                
            DataModificationEvents = personalDataEvents
                .Where(e => e.Tags.Contains("data-modification"))
                .Count(),
                
            DataDeletionEvents = personalDataEvents
                .Where(e => e.Tags.Contains("data-deletion"))
                .Count(),
                
            DataExportEvents = personalDataEvents
                .Where(e => e.Tags.Contains("data-export"))
                .Count(),
                
            // Data subject requests
            SubjectAccessRequests = personalDataEvents
                .Where(e => e.Action.Contains("SUBJECT_ACCESS_REQUEST"))
                .Count(),
                
            RightToBeForgottenRequests = personalDataEvents
                .Where(e => e.Action.Contains("RIGHT_TO_BE_FORGOTTEN"))
                .Count(),
                
            // Compliance violations
            ViolationEvents = personalDataEvents
                .Where(e => e.Severity >= AuditSeverity.Warning && 
                           e.Tags.Contains("compliance-violation"))
                .ToList()
        };
    }
}
```

## 🐛 Troubleshooting

### Yaygın Problemler ve Çözümleri

#### 1. Performance Issues
```csharp
// Problem: Audit logging causing performance bottleneck
// Solution: Asynchronous batch processing
public class PerformantAuditService : IAuditService
{
    private readonly BackgroundTaskQueue _taskQueue;
    
    public async Task<Result> LogEventAsync(AuditEvent auditEvent)
    {
        // Queue for background processing
        _taskQueue.QueueBackgroundWorkItem(async token =>
        {
            await _auditStore.StoreEventAsync(auditEvent, token);
        });
        
        return Result.Success();
    }
}
```

#### 2. Storage Issues
```bash
# Database connection problems
# Check connection string and database availability
sqlcmd -S localhost -d EnterpriseAudit -E -Q "SELECT COUNT(*) FROM AuditEvents"

# Disk space issues
# Monitor audit table size
SELECT 
    t.NAME AS TableName,
    p.rows AS RowCounts,
    SUM(a.total_pages) * 8 AS TotalSpaceKB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.NAME = 'AuditEvents'
GROUP BY t.Name, p.Rows
```

#### 3. Configuration Issues
```csharp
// Validate configuration on startup
public class AuditConfigurationValidator
{
    public static void ValidateConfiguration(AuditConfiguration config)
    {
        var errors = new List<string>();
        
        if (config.Performance.BatchSize <= 0)
            errors.Add("BatchSize must be greater than 0");
            
        if (config.Retention.RetentionDays <= 0)
            errors.Add("RetentionDays must be greater than 0");
            
        if (string.IsNullOrEmpty(config.DefaultEnvironment))
            errors.Add("DefaultEnvironment cannot be empty");
            
        if (errors.Any())
            throw new InvalidOperationException(
                $"Invalid audit configuration: {string.Join(", ", errors)}");
    }
}
```

## 📝 Testing Strategy

### 1. Unit Tests
```csharp
[Test]
public async Task AuditService_LogEvent_StoresEventSuccessfully()
{
    // Arrange
    var mockStore = new Mock<IAuditStore>();
    var mockContextProvider = new Mock<IAuditContextProvider>();
    var auditService = new AuditService(mockStore.Object, mockContextProvider.Object, 
        Options.Create(new AuditConfiguration { Enabled = true }), Mock.Of<ILogger<AuditService>>());
    
    var auditEvent = AuditEvent.Create("TEST_ACTION", "TestResource", "Success");
    
    mockStore.Setup(s => s.StoreEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(Result.Success());
    
    // Act
    var result = await auditService.LogEventAsync(auditEvent);
    
    // Assert
    Assert.True(result.IsSuccess);
    mockStore.Verify(s => s.StoreEventAsync(
        It.Is<AuditEvent>(e => e.Action == "TEST_ACTION"), 
        It.IsAny<CancellationToken>()), Times.Once);
}
```

### 2. Integration Tests
```csharp
[Test]
public async Task AuditMiddleware_ProcessRequest_CreatesAuditEvent()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddAuditing();
    services.AddInMemoryAuditStore();
    
    var provider = services.BuildServiceProvider();
    var auditService = provider.GetRequiredService<IAuditService>();
    var auditStore = provider.GetRequiredService<IAuditStore>() as InMemoryAuditStore;
    
    var context = new DefaultHttpContext();
    context.Request.Method = "GET";
    context.Request.Path = "/api/users";
    
    var middleware = new AuditMiddleware(
        next: async ctx => { ctx.Response.StatusCode = 200; },
        auditService, 
        Options.Create(new AuditConfiguration { Enabled = true }),
        Mock.Of<ILogger<AuditMiddleware>>());
    
    // Act
    await middleware.InvokeAsync(context);
    
    // Assert
    var events = await auditStore.GetAllEventsAsync();
    Assert.Single(events);
    Assert.Equal("GET /api/users", events.First().Action);
}
```

## 📄 Lisans

Bu proje Enterprise Platform Team tarafından geliştirilmiştir.

## 📞 Destek

- **Dokümantasyon**: Bu README dosyası
- **Issue Tracking**: Internal issue tracking system
- **Email**: enterprise-platform@company.com

---

**🎉 Enterprise.Shared.Auditing ile kapsamlı, güvenilir ve compliance-ready audit sisteminizi oluşturun!** 🔍