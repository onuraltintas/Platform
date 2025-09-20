# Enterprise.Shared.Observability

## Proje Hakkında

Enterprise.Shared.Observability, kurumsal platformlar için geliştirilmiş kapsamlı bir gözlemlenebilirlik (observability) sistemidir. Bu sistem, uygulamanızın performansını, sağlığını ve davranışını izlemek için gerekli olan üç temel bileşeni (tracing, metrics, logging) bir araya getirir.

## Temel Özellikler

### 🔍 Dağıtık İzleme (Distributed Tracing)
- **OpenTelemetry Integration**: Modern tracing standartları
- **Activity Source**: Özelleştirilmiş aktivite izleme
- **Context Propagation**: İstekler arası bağlam aktarımı
- **Correlation ID**: Tüm loglar ve metrikler arasında tutarlı izleme
- **User Context Enrichment**: Kullanıcı bazlı izleme bilgileri

### 📊 Metrikler (Metrics)
- **Prometheus Integration**: Endüstri standardı metrik toplama
- **Custom Metrics**: İş mantığına özel metrikler
- **Performance Metrics**: API, veritabanı ve cache performansı
- **Business Metrics**: Kullanıcı eylemleri ve iş süreçleri
- **System Metrics**: CPU, bellek, disk kullanımı

### 🏥 Sağlık Kontrolleri (Health Checks)
- **Advanced Health Checks**: Kapsamlı sistem sağlığı kontrolü
- **Dependency Checks**: Bağımlı servislerin durumu
- **Custom Health Endpoints**: /health, /ready, /live endpoints
- **Detailed Reporting**: JSON formatında detaylı sağlık raporları

### 🔗 İlişkilendirme (Correlation)
- **Correlation Context**: İstekler arası bağlam yönetimi
- **Thread-Safe Operations**: Async/await desteği
- **Request Tracking**: Tüm mikroservisler arası izleme

## Kullanılan Teknolojiler

### 🏗️ .NET Ekosistemi
- **.NET 8.0**: Modern C# özellikleri ve performans
- **Microsoft.Extensions.DependencyInjection**: Bağımlılık enjeksiyonu
- **Microsoft.Extensions.Logging**: Yapılandırılabilir loglama
- **Microsoft.Extensions.Diagnostics.HealthChecks**: Sağlık kontrolü framework

### 📈 Observability Stack
- **OpenTelemetry**: Açık kaynak observability framework
  - OpenTelemetry.Extensions.Hosting (v1.7.0)
  - OpenTelemetry.Instrumentation.AspNetCore (v1.8.1)
  - OpenTelemetry.Instrumentation.Http (v1.9.0)
  - OpenTelemetry.Instrumentation.SqlClient (v1.7.0-beta.1)
- **Prometheus**: Metrik toplama ve monitoring
  - prometheus-net (v8.2.1)
  - prometheus-net.AspNetCore (v8.2.1)
- **System.Diagnostics.DiagnosticSource**: .NET diagnostics API

### 🩺 Health Check Providers
- **AspNetCore.HealthChecks.SqlServer**: SQL Server bağlantı kontrolü
- **AspNetCore.HealthChecks.Redis**: Redis bağlantı kontrolü
- **Custom Health Checks**: Sistem kaynak kontrolü

## Kurulum ve Kullanım

### 1. Proje Referansı
```xml
<ProjectReference Include="../Enterprise.Shared.Observability/Enterprise.Shared.Observability.csproj" />
```

### 2. Servis Kaydı
```csharp
// Startup.cs veya Program.cs
services.AddSharedObservability(configuration);
```

### 3. Middleware Yapılandırması
```csharp
// Program.cs
app.UseSharedObservability();
```

### 4. Yapılandırma (appsettings.json)
```json
{
  "ObservabilitySettings": {
    "ServiceName": "MyMicroservice",
    "ServiceVersion": "1.0.0",
    "Environment": "Production",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableHealthChecks": true,
    "EnableBusinessMetrics": true,
    "SamplingRate": 0.1,
    "Tracing": {
      "JaegerEndpoint": "http://jaeger:14268/api/traces",
      "EnableSqlInstrumentation": true,
      "EnableHttpInstrumentation": true,
      "ConsoleExporter": false
    },
    "Metrics": {
      "PrometheusEndpoint": "/metrics",
      "EnableRuntimeMetrics": true,
      "EnableHttpMetrics": true,
      "EnableProcessMetrics": true,
      "CustomMetricsPrefix": "myservice_"
    },
    "HealthChecks": {
      "Endpoint": "/health",
      "DetailedEndpoint": "/health/detailed",
      "ReadyEndpoint": "/health/ready",
      "LiveEndpoint": "/health/live",
      "CheckIntervalSeconds": 30
    },
    "CorrelationId": {
      "HeaderName": "X-Correlation-ID",
      "EnableLogging": true,
      "GenerateIfMissing": true
    }
  }
}
```

## Servis Kullanımı

### 1. Dağıtık İzleme (Tracing)
```csharp
public class OrderService
{
    private readonly ITracingService _tracingService;
    
    public OrderService(ITracingService tracingService)
    {
        _tracingService = tracingService;
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        using var activity = _tracingService.StartActivity("CreateOrder");
        
        try
        {
            // İş mantığı kodu
            _tracingService.AddTag("order.id", order.Id);
            _tracingService.AddTag("order.total", order.Total);
            
            _tracingService.EnrichWithUserContext(request.UserId, request.UserEmail);
            _tracingService.EnrichWithBusinessContext(new Dictionary<string, object>
            {
                ["order.type"] = order.OrderType,
                ["payment.method"] = order.PaymentMethod
            });
            
            _tracingService.AddEvent("Order validation completed");
            
            return order;
        }
        catch (Exception ex)
        {
            _tracingService.RecordException(ex);
            _tracingService.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### 2. Metrik Toplama
```csharp
public class PaymentController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly IBusinessMetricsCollector _businessMetrics;
    
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment(PaymentRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await ProcessPaymentInternal(request);
            
            // API metriği
            _metricsService.RecordApiCall("POST", "/payment/process", 200, stopwatch.ElapsedMilliseconds);
            
            // İş metriği
            await _businessMetrics.RecordPaymentProcessedAsync(
                request.Amount, 
                request.Currency, 
                success: true);
            
            // Kullanıcı eylem metriği
            _metricsService.IncrementUserAction("payment.processed", request.UserId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _metricsService.RecordApiCall("POST", "/payment/process", 500, stopwatch.ElapsedMilliseconds);
            await _businessMetrics.RecordPaymentProcessedAsync(
                request.Amount, 
                request.Currency, 
                success: false);
                
            throw;
        }
    }
}
```

### 3. Özel Metrikler
```csharp
public class CustomMetricsService
{
    private readonly IMetricsService _metricsService;
    
    public void TrackFeatureUsage(string featureName, string userId)
    {
        // Counter metrik
        _metricsService.IncrementCounter("feature_usage_total", 1, 
            new KeyValuePair<string, object>("feature", featureName));
        
        // Histogram metrik
        _metricsService.RecordHistogram("feature_response_time", responseTime.TotalMilliseconds,
            new KeyValuePair<string, object>("feature", featureName));
        
        // Gauge metrik
        _metricsService.SetGauge("active_feature_users", activeUserCount,
            new KeyValuePair<string, object>("feature", featureName));
        
        // İş metriği
        _metricsService.RecordBusinessMetric("feature_adoption", 1, new Dictionary<string, object>
        {
            ["feature"] = featureName,
            ["user_segment"] = GetUserSegment(userId),
            ["platform"] = GetUserPlatform(userId)
        });
    }
}
```

### 4. Sağlık Kontrolleri
```csharp
public class CustomHealthCheckService
{
    private readonly IAdvancedHealthChecks _healthChecks;
    
    public async Task<HealthReport> GetSystemStatusAsync()
    {
        // Detaylı sistem raporu
        var report = await _healthChecks.GetDetailedHealthReportAsync();
        
        // Özel kontroller
        var dbHealth = await _healthChecks.CheckDatabaseConnectionAsync("DefaultConnection");
        var redisHealth = await _healthChecks.CheckRedisConnectionAsync("Cache");
        var externalApiHealth = await _healthChecks.CheckExternalServiceAsync("https://api.partner.com/status");
        
        // Sistem kaynakları
        var memoryHealth = await _healthChecks.CheckMemoryUsageAsync();
        var cpuHealth = await _healthChecks.CheckCpuUsageAsync();
        var diskHealth = await _healthChecks.CheckDiskSpaceAsync();
        
        return report;
    }
}
```

## İş Metriklerini Takip Etme

```csharp
public class BusinessMetricsExample
{
    private readonly IBusinessMetricsCollector _businessMetrics;
    
    public async Task TrackBusinessEvents()
    {
        // Kullanıcı kayıt
        await _businessMetrics.RecordUserRegistrationAsync("user123", "Premium", "Facebook");
        
        // Kullanıcı girişi
        await _businessMetrics.RecordUserLoginAsync("user123", success: true);
        
        // Sipariş oluşturma
        await _businessMetrics.RecordOrderCreatedAsync(299.99m, "USD", "Electronics");
        
        // Ödeme işlemi
        await _businessMetrics.RecordPaymentProcessedAsync(299.99m, "USD", success: true);
        
        // Özellik kullanımı
        await _businessMetrics.RecordFeatureUsageAsync("advanced_search", "user123");
        
        // Özel iş olayı
        await _businessMetrics.RecordCustomEventAsync("newsletter_signup", new Dictionary<string, object>
        {
            ["source"] = "homepage",
            ["user_type"] = "premium",
            ["campaign"] = "summer2024"
        });
        
        // İş metrik raporu
        var report = await _businessMetrics.GenerateReportAsync(
            DateTime.UtcNow.AddDays(-30), 
            DateTime.UtcNow);
    }
}
```

## Correlation Context Kullanımı

```csharp
public class CorrelationExample
{
    private readonly ICorrelationContextAccessor _correlationContext;
    
    public async Task ProcessRequestAsync()
    {
        // Correlation context oluşturma
        var context = new CorrelationContext
        {
            CorrelationId = Guid.NewGuid().ToString(),
            UserId = "user123",
            SessionId = "session456",
            RequestId = "req789",
            AdditionalProperties = new Dictionary<string, object>
            {
                ["Source"] = "mobile-app",
                ["Version"] = "2.1.0"
            }
        };
        
        _correlationContext.CorrelationContext = context;
        
        // Bu context tüm child işlemlerde otomatik olarak propagate edilir
        await CallAnotherServiceAsync();
    }
}
```

## Middleware Özellikleri

### 1. Correlation ID Middleware
- Her request için benzersiz correlation ID
- Header'dan okuma veya otomatik oluşturma
- Response header'a correlation ID ekleme
- Logging context'e correlation ID ekleme

### 2. Metrics Middleware
- Otomatik HTTP request metrikleri
- Response time ölçümü
- Status code dağılımı
- Request/response boyutları

## Monitoring Dashboard Entegrasyonu

### Prometheus Metrikleri
```
# CPU kullanımı
myservice_cpu_usage_percentage

# Bellek kullanımı
myservice_memory_usage_mb

# API çağrıları
myservice_api_calls_total{method="POST",endpoint="/orders",status_code="200"}

# Veritabanı sorguları
myservice_database_queries_total{operation="SELECT",table="orders",success="true"}

# İş metrikleri
myservice_business_user_registrations_total{source="organic"}
myservice_business_orders_total{category="electronics"}
myservice_business_payment_amount_total{currency="USD"}
```

### Jaeger Tracing
- Distributed trace görselleştirme
- Service dependency haritası
- Performance bottleneck analizi
- Error tracking ve debugging

## Test Edilmiş Durumu

Proje **74/74 test** ile %100 başarı oranında test edilmiştir. Test kapsamı:

- ✅ Tracing service tüm operasyonları
- ✅ Metrics collection ve Prometheus integration
- ✅ Health checks tüm senaryoları
- ✅ Correlation context yönetimi
- ✅ Business metrics tracking
- ✅ Middleware functionality
- ✅ Error handling senaryoları

## Production Hazırlığı

Proje production ortamında kullanıma hazırdır:

- ✅ Exception handling ve resilience
- ✅ Performance optimizasyonu
- ✅ Memory leak koruması
- ✅ Thread-safe operasyonlar
- ✅ Configurable sampling rates
- ✅ Security best practices
- ✅ Privacy-aware user tracking (hashed IDs)

## Mimari Yapı

### Katmanlar
1. **Interfaces**: Service contracts ve abstraction
2. **Models**: Configuration ve data models
3. **Services**: Core observability services
4. **Middleware**: HTTP request/response processing
5. **Extensions**: DI container ve application setup

### Temel Servisler
- **OpenTelemetryTracingService**: Distributed tracing yönetimi
- **PrometheusMetricsService**: Metric collection ve export
- **AdvancedHealthChecksService**: Kapsamlı sağlık kontrolleri
- **BusinessMetricsCollector**: İş metriklerini toplama
- **CorrelationContextAccessor**: Request context yönetimi

### Veri Akışı
1. Request gelir, CorrelationIdMiddleware correlation ID atar
2. MetricsMiddleware request metriklerini toplar
3. Application code tracing ve custom metrics ekler
4. Response dönülür, metrics ve traces export edilir
5. Health checks periyodik olarak sistem durumunu kontrol eder

## En İyi Uygulamalar

### 1. Tracing
- Kritik iş operasyonları için custom activity oluşturun
- Exception'ları trace'lerde kaydedin
- Business context ile trace'leri zenginleştirin
- Sampling rate'i production load'a göre ayarlayın

### 2. Metrics
- Business KPI'larını metric olarak takip edin
- High cardinality metric'lerden kaçının
- Metric label'larını optimize edin
- Custom metric'ler için naming convention kullanın

### 3. Health Checks
- Dependency'leri health check'lerde kontrol edin
- Timeout değerlerini uygun ayarlayın
- Degraded state'ler için threshold tanımlayın
- Health check endpoint'lerini monitoring'e entegre edin

### 4. Correlation
- Tüm log mesajlarında correlation ID kullanın
- Downstream service'lere correlation ID propagate edin
- User privacy için hash'lenmiş ID'ler kullanın

## Lisans

Bu proje Enterprise Platform bünyesinde geliştirilmiş olup, kurumsal kullanım için tasarlanmıştır.

## Katkıda Bulunma

Proje geliştirme sürecine katkıda bulunmak için:
1. Feature branch oluşturun
2. Değişikliklerinizi yapın
3. Unit testler ekleyin
4. Pull request açın

## İletişim

Enterprise Platform Geliştirme Ekibi
- Versiyon: 1.0.0
- .NET: 8.0
- Son Güncelleme: 2025