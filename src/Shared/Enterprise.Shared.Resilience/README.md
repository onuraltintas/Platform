# Enterprise.Shared.Resilience

## Proje Hakkında

Enterprise.Shared.Resilience, kurumsal platformlar için geliştirilmiş kapsamlı bir dayanıklılık (resilience) sistemidir. Bu sistem, mikroservislerin ve dağıtık sistemlerin güvenilirliğini artırmak için gerekli olan tüm dayanıklılık kalıplarını (resilience patterns) bir araya getirir.

## Temel Özellikler

### 🔄 Yeniden Deneme (Retry)
- **Polly Integration**: Endüstri standardı retry kütüphanesi
- **Configurable Strategies**: Üstel, doğrusal ve sabit gecikme stratejileri
- **Jitter Support**: Thundering herd problemini önlemek için rastgelelik
- **Smart Exception Handling**: Geçici ve kalıcı hataları ayırt etme
- **Custom Retry Policies**: İş mantığına özel retry politikaları

### ⚡ Devre Kesici (Circuit Breaker)
- **Advanced Circuit Breaker**: Hızlı başarısızlık ve sistem koruması
- **State Management**: Kapalı, açık, yarı-açık durumları
- **Health Monitoring**: Gerçek zamanlı durum ve metrik takibi
- **Configurable Thresholds**: Hata oranı ve minimum throughput ayarları
- **Manual Control**: Circuit breaker'ları manuel kontrol etme

### 🚧 Bölme Duvarı (Bulkhead)
- **Resource Isolation**: Kaynak izolasyonu ve koruma
- **Parallelization Limits**: Eş zamanlı operasyon sınırlama
- **Queue Management**: Bekleme kuyruğu yönetimi
- **Performance Metrics**: Throughput ve kaynak kullanım metrikleri

### ⏱️ Zaman Aşımı (Timeout)
- **Operation Timeouts**: Operasyon bazlı zaman aşımı
- **Context-Aware**: HTTP, veritabanı, genel operasyon timeoutları
- **Cancellation Support**: Graceful cancellation desteği

### 🚦 Oran Sınırlama (Rate Limiting)
- **Token Bucket Algorithm**: Modern rate limiting algoritması
- **Sliding Window**: Kayan pencere tabanlı sınırlama
- **Queue Support**: İstek kuyruklama desteği
- **Auto-replenishment**: Otomatik token yenileme

### 📊 Monitoring ve Health Checks
- **Real-time Metrics**: Gerçek zamanlı performans metrikleri
- **Health Endpoints**: Sistem sağlığı kontrolü
- **Detailed Reporting**: Kapsamlı durum raporları

## Kullanılan Teknolojiler

### 🏗️ .NET Ekosistemi
- **.NET 8.0**: Modern C# özellikleri ve performans
- **Microsoft.Extensions.DependencyInjection**: Bağımlılık enjeksiyonu
- **Microsoft.Extensions.Logging**: Yapılandırılabilir loglama
- **Microsoft.Extensions.Options**: Yapılandırma yönetimi
- **Microsoft.Extensions.Diagnostics.HealthChecks**: Sağlık kontrolü

### 🛡️ Resilience Stack
- **Polly v8.2.0**: Modern resilience framework
  - Circuit Breaker patterns
  - Retry strategies with jitter
  - Timeout policies
  - Result caching
- **System.Threading.RateLimiting**: .NET native rate limiting
- **Microsoft.Data.SqlClient**: SQL Server transient error handling

## Kurulum ve Kullanım

### 1. Proje Referansı
```xml
<ProjectReference Include="../Enterprise.Shared.Resilience/Enterprise.Shared.Resilience.csproj" />
```

### 2. Servis Kaydı
```csharp
// Tüm resilience servisleri
services.AddResilience(configuration);

// Sadece belirli servisler
services.AddCircuitBreaker()
        .AddRetryPolicy()
        .AddBulkhead()
        .AddTimeout()
        .AddRateLimit();

// Health checks ile birlikte
services.AddResilience(configuration)
        .AddResilienceHealthChecks();
```

### 3. Yapılandırma (appsettings.json)
```json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 50,
      "MinimumThroughput": 10,
      "SamplingDuration": "00:01:00",
      "BreakDuration": "00:00:30",
      "EnableLogging": true,
      "EnableHealthCheck": true
    },
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 60000,
      "BackoffType": "Exponential",
      "UseJitter": true,
      "EnableRetryLogging": true
    },
    "Bulkhead": {
      "MaxParallelization": 10,
      "MaxQueuedActions": 25,
      "EnableBulkheadLogging": true
    },
    "Timeout": {
      "DefaultTimeoutMs": 30000,
      "HttpTimeoutMs": 30000,
      "DatabaseTimeoutMs": 15000,
      "EnableTimeoutLogging": true
    },
    "RateLimit": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 50,
      "AutoReplenishment": true,
      "EnableRateLimitLogging": true
    }
  }
}
```

## Servis Kullanımı

### 1. Circuit Breaker Kullanımı
```csharp
public class PaymentService
{
    private readonly ICircuitBreakerService _circuitBreaker;
    
    public PaymentService(ICircuitBreakerService circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }
    
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var result = await _circuitBreaker.ExecuteAsync(async () =>
            {
                // Dış ödeme servisine çağrı
                return await CallExternalPaymentService(request);
            }, "payment-service");
            
            return result;
        }
        catch (ServiceUnavailableException ex)
        {
            // Circuit breaker açık - fallback işlemi
            return new PaymentResult 
            { 
                Success = false, 
                Message = "Payment service temporarily unavailable" 
            };
        }
    }
    
    public async Task<CircuitBreakerState> GetPaymentServiceStatusAsync()
    {
        return _circuitBreaker.GetCircuitBreakerState("payment-service");
    }
}
```

### 2. Retry Pattern Kullanımı
```csharp
public class UserService
{
    private readonly IRetryService _retryService;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<User> GetUserFromApiAsync(int userId)
    {
        return await _retryService.ExecuteAsync(async () =>
        {
            using var client = _httpClientFactory.CreateClient("UserAPI");
            var response = await client.GetAsync($"users/{userId}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API returned {response.StatusCode}");
            }
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(json);
        }, "get-user-api");
    }
    
    public async Task<User> GetUserWithCustomPolicyAsync(int userId)
    {
        var customPolicy = new RetryPolicy
        {
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromMinutes(2),
            BackoffType = "Exponential",
            UseJitter = true,
            ShouldRetry = ex => ex is HttpRequestException || ex is TimeoutException
        };
        
        return await _retryService.ExecuteWithCustomPolicyAsync(async () =>
        {
            // API çağrısı
            return await CallUserApiAsync(userId);
        }, customPolicy);
    }
}
```

### 3. Bulkhead Pattern Kullanımı
```csharp
public class DataProcessingService
{
    private readonly IBulkheadService _bulkheadService;
    
    public async Task ProcessLargeDatasetAsync(IEnumerable<DataItem> items)
    {
        var tasks = items.Select(async item =>
        {
            try
            {
                await _bulkheadService.ExecuteAsync(async () =>
                {
                    await ProcessSingleItemAsync(item);
                }, "data-processing");
            }
            catch (BulkheadRejectedException)
            {
                // Sistem kapasitesi dolu - kuyruğa al veya daha sonra dene
                await QueueForLaterProcessing(item);
            }
        });
        
        await Task.WhenAll(tasks);
    }
    
    public async Task<BulkheadHealthInfo> GetProcessingHealthAsync()
    {
        return await _bulkheadService.GetHealthInfoAsync("data-processing");
    }
}
```

### 4. Timeout Kullanımı
```csharp
public class DatabaseService
{
    private readonly ITimeoutService _timeoutService;
    
    public async Task<List<Order>> GetOrdersAsync(int customerId)
    {
        return await _timeoutService.ExecuteAsync(async (ct) =>
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Orders WHERE CustomerId = @customerId", connection);
            command.Parameters.AddWithValue("@customerId", customerId);
            
            await connection.OpenAsync(ct);
            
            var orders = new List<Order>();
            using var reader = await command.ExecuteReaderAsync(ct);
            
            while (await reader.ReadAsync(ct))
            {
                orders.Add(MapToOrder(reader));
            }
            
            return orders;
        }, TimeoutContext.Database); // 15 saniye timeout
    }
    
    public async Task<ApiResponse> CallExternalApiAsync(string endpoint)
    {
        return await _timeoutService.ExecuteAsync(async (ct) =>
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(endpoint, ct);
            return await response.Content.ReadAsAsync<ApiResponse>(ct);
        }, TimeoutContext.Http); // 30 saniye timeout
    }
}
```

### 5. Rate Limiting Kullanımı
```csharp
public class ApiController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync([FromQuery] string query)
    {
        var userId = User.Identity.Name ?? "anonymous";
        
        try
        {
            await _rateLimitService.ExecuteAsync(async () =>
            {
                var results = await SearchServiceAsync(query);
                return Ok(results);
            }, $"search-{userId}");
            
            var results = await SearchServiceAsync(query);
            return Ok(results);
        }
        catch (RateLimitExceededException)
        {
            return StatusCode(429, "Rate limit exceeded. Please try again later.");
        }
    }
    
    [HttpGet("rate-limit-status/{key}")]
    public async Task<IActionResult> GetRateLimitStatusAsync(string key)
    {
        var status = await _rateLimitService.GetRateLimitStatusAsync(key);
        return Ok(status);
    }
}
```

## Kombine Kullanım

### Tüm Pattern'leri Birlikte Kullanma
```csharp
public class ResilientOrderService
{
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly IRetryService _retry;
    private readonly IBulkheadService _bulkhead;
    private readonly ITimeoutService _timeout;
    private readonly IRateLimitService _rateLimit;
    
    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        var userId = order.CustomerId.ToString();
        
        // Rate limiting - kullanıcı başına sınırlama
        await _rateLimit.ExecuteAsync(async () => { }, $"order-{userId}");
        
        // Bulkhead - sistem kaynaklarını koruma
        return await _bulkhead.ExecuteAsync(async () =>
        {
            // Circuit breaker - dış servis koruması
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                // Retry - geçici hatalar için
                return await _retry.ExecuteAsync(async () =>
                {
                    // Timeout - uzun süren operasyonları engelleme
                    return await _timeout.ExecuteAsync(async (ct) =>
                    {
                        return await CallOrderProcessingServiceAsync(order, ct);
                    }, TimeoutContext.Default);
                }, "order-processing");
            }, "order-service");
        }, "order-bulkhead");
    }
}
```

## Monitoring ve Metrics

### Health Check Entegrasyonu
```csharp
// Startup.cs veya Program.cs içinde
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Resilience pattern'larının sağlık kontrolü
services.AddHealthChecks()
    .AddCheck<ResilienceHealthCheck>("resilience");
```

### Monitoring Dashboard
```csharp
[ApiController]
[Route("api/[controller]")]
public class ResilienceController : ControllerBase
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly IBulkheadService _bulkheadService;
    
    [HttpGet("circuit-breakers")]
    public async Task<IActionResult> GetCircuitBreakerStates()
    {
        var states = _circuitBreakerService.GetAllCircuitBreakerStates();
        return Ok(states);
    }
    
    [HttpGet("bulkhead/{key}/stats")]
    public async Task<IActionResult> GetBulkheadStats(string key)
    {
        var stats = _bulkheadService.GetBulkheadStats(key);
        return Ok(stats);
    }
    
    [HttpPost("circuit-breakers/{key}/reset")]
    public async Task<IActionResult> ResetCircuitBreaker(string key)
    {
        _circuitBreakerService.ResetCircuitBreaker(key);
        return Ok(new { Message = $"Circuit breaker {key} has been reset" });
    }
}
```

## Kullanım Örnekleri

### Service'de Resilience Kullanımı
```csharp
public class PaymentService
{
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly IRetryService _retryService;
    private readonly IBulkheadService _bulkheadService;
    private readonly ITimeoutService _timeoutService;
    private readonly HttpClient _httpClient;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Combine multiple resilience patterns
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _bulkheadService.ExecuteAsync(async () =>
            {
                return await _retryService.ExecuteAsync(async () =>
                {
                    return await _timeoutService.ExecuteAsync(async () =>
                    {
                        var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
                        response.EnsureSuccessStatusCode();
                        
                        var result = await response.Content.ReadFromJsonAsync<PaymentResult>();
                        return result ?? throw new InvalidOperationException("Invalid payment response");
                    }, "external-api");
                }, "external-api");
            }, "payment-processing");
        }, "payment-gateway");
    }

    // Alternative: Use a combined resilience pipeline
    public async Task<PaymentResult> ProcessPaymentWithPipelineAsync(PaymentRequest request)
    {
        return await _resiliencePipeline.ExecuteAsync(async (context) =>
        {
            var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PaymentResult>()
                ?? throw new InvalidOperationException("Invalid payment response");
        });
    }
}
```

### Database Operations ile Resilience
```csharp
public class UserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IRetryService _retryService;
    private readonly ITimeoutService _timeoutService;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _retryService.ExecuteAsync(async () =>
        {
            return await _timeoutService.ExecuteAsync(async () =>
            {
                return await _context.Users.FindAsync(id);
            }, "database");
        }, "database");
    }

    public async Task<User> CreateAsync(User user)
    {
        return await _retryService.ExecuteAsync(async () =>
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }, "database");
    }

    public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize)
    {
        return await _bulkheadService.ExecuteAsync(async () =>
        {
            return await _retryService.ExecuteAsync(async () =>
            {
                var skip = (page - 1) * pageSize;
                var users = await _context.Users
                    .OrderBy(u => u.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();
                
                var totalCount = await _context.Users.CountAsync();
                
                return new PagedResult<User>
                {
                    Data = users,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }, "database");
        }, "database-queries");
    }
}
```

### Fallback Mechanisms
```csharp
public class WeatherService
{
    private readonly HttpClient _primaryHttpClient;
    private readonly HttpClient _fallbackHttpClient;
    private readonly IMemoryCache _cache;
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly ILogger<WeatherService> _logger;

    public async Task<WeatherInfo> GetWeatherAsync(string city)
    {
        try
        {
            // Primary service with circuit breaker
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response = await _primaryHttpClient.GetAsync($"/weather/{city}");
                response.EnsureSuccessStatusCode();
                
                var weather = await response.Content.ReadFromJsonAsync<WeatherInfo>();
                
                // Cache successful response
                _cache.Set($"weather_{city}", weather, TimeSpan.FromMinutes(15));
                
                return weather!;
            }, "weather-primary");
        }
        catch (ServiceUnavailableException) when (_circuitBreaker.GetCircuitBreakerState("weather-primary") == CircuitBreakerState.Open)
        {
            _logger.LogWarning("Primary weather service is down, trying fallback");
            
            try
            {
                // Fallback to secondary service
                var response = await _fallbackHttpClient.GetAsync($"/api/weather/{city}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WeatherInfo>()
                    ?? throw new InvalidOperationException("Invalid weather response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback weather service also failed");
                
                // Final fallback to cache
                if (_cache.TryGetValue($"weather_{city}", out WeatherInfo? cachedWeather))
                {
                    _logger.LogInformation("Returning cached weather data for {City}", city);
                    return cachedWeather;
                }
                
                // Ultimate fallback - return default weather
                return new WeatherInfo
                {
                    City = city,
                    Temperature = 20,
                    Description = "Weather information unavailable",
                    IsFromCache = true,
                    LastUpdated = DateTime.UtcNow.AddHours(-1)
                };
            }
        }
    }
}
```

### Resilience Health Check
```csharp
public class ResilienceHealthCheck : IHealthCheck
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly IBulkheadService _bulkheadService;
    private readonly ITimeoutService _timeoutService;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();
            
            // Check circuit breakers
            var circuitBreakerStates = new[]
            {
                ("payment-gateway", _circuitBreakerService.GetCircuitBreakerState("payment-gateway")),
                ("weather-primary", _circuitBreakerService.GetCircuitBreakerState("weather-primary")),
                ("user-service", _circuitBreakerService.GetCircuitBreakerState("user-service"))
            };
            
            var openCircuitBreakers = circuitBreakerStates
                .Where(cb => cb.Item2 == CircuitBreakerState.Open)
                .Select(cb => cb.Item1)
                .ToArray();
            
            healthData["CircuitBreakers"] = circuitBreakerStates.ToDictionary(cb => cb.Item1, cb => cb.Item2.ToString());
            healthData["OpenCircuitBreakers"] = openCircuitBreakers;
            
            // Check bulkheads
            var bulkheadStats = new[]
            {
                _bulkheadService.GetBulkheadStats("payment-processing"),
                _bulkheadService.GetBulkheadStats("database-queries"),
                _bulkheadService.GetBulkheadStats("file-operations")
            };
            
            var overloadedBulkheads = bulkheadStats
                .Where(bs => bs.RejectionRate > 0.1) // More than 10% rejection rate
                .Select(bs => bs.BulkheadKey)
                .ToArray();
            
            healthData["Bulkheads"] = bulkheadStats.ToDictionary(bs => bs.BulkheadKey, bs => new
            {
                bs.CurrentExecutions,
                bs.AvailableSlots,
                bs.RejectionRate,
                bs.SuccessRate
            });
            healthData["OverloadedBulkheads"] = overloadedBulkheads;
            
            // Check timeout rates
            var timeoutStats = new[]
            {
                _timeoutService.GetTimeoutStats("external-api"),
                _timeoutService.GetTimeoutStats("database"),
                _timeoutService.GetTimeoutStats("http")
            };
            
            var highTimeoutRates = timeoutStats
                .Where(ts => ts.TimeoutRate > 0.05) // More than 5% timeout rate
                .Select(ts => ts.TimeoutKey)
                .ToArray();
            
            healthData["TimeoutStats"] = timeoutStats.ToDictionary(ts => ts.TimeoutKey, ts => new
            {
                ts.TimeoutRate,
                ts.AverageDuration,
                ts.TotalTimeouts
            });
            healthData["HighTimeoutRates"] = highTimeoutRates;
            
            // Determine overall health
            var status = HealthStatus.Healthy;
            var issues = new List<string>();
            
            if (openCircuitBreakers.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"Open circuit breakers: {string.Join(", ", openCircuitBreakers)}");
            }
            
            if (overloadedBulkheads.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"Overloaded bulkheads: {string.Join(", ", overloadedBulkheads)}");
            }
            
            if (highTimeoutRates.Any())
            {
                status = HealthStatus.Degraded;
                issues.Add($"High timeout rates: {string.Join(", ", highTimeoutRates)}");
            }
            
            var description = status == HealthStatus.Healthy 
                ? "All resilience patterns are healthy"
                : string.Join("; ", issues);
            
            return new HealthCheckResult(status, description, data: healthData);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check resilience patterns health", ex);
        }
    }
}
```

## Test Örnekleri

### Unit Tests
```csharp
[Test]
public async Task CircuitBreakerService_ShouldOpenCircuit_AfterFailureThreshold()
{
    // Arrange
    var settings = Options.Create(new ResilienceSettings
    {
        CircuitBreaker = new CircuitBreakerSettings
        {
            FailureThreshold = 3,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromMinutes(1),
            BreakDuration = TimeSpan.FromSeconds(30)
        }
    });
    
    var circuitBreakerService = new PollyCircuitBreakerService(settings, Mock.Of<ILogger<PollyCircuitBreakerService>>());
    
    // Act - Cause failures to trip the circuit breaker
    for (int i = 0; i < 6; i++)
    {
        try
        {
            await circuitBreakerService.ExecuteAsync(async () =>
            {
                throw new HttpRequestException("Service unavailable");
            }, "test-circuit");
        }
        catch (Exception) { /* Expected */ }
    }
    
    // Assert - Circuit should be open now
    await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
        circuitBreakerService.ExecuteAsync(async () => "success", "test-circuit"));
    
    var state = circuitBreakerService.GetCircuitBreakerState("test-circuit");
    Assert.That(state, Is.EqualTo(CircuitBreakerState.Open));
}

[Test]
public async Task RetryService_ShouldRetryOnTransientFailure()
{
    // Arrange
    var settings = Options.Create(new ResilienceSettings
    {
        Retry = new RetrySettings
        {
            MaxRetryAttempts = 3,
            BaseDelayMs = 100
        }
    });
    
    var retryService = new PollyRetryService(settings, Mock.Of<ILogger<PollyRetryService>>());
    var attemptCount = 0;
    
    // Act
    var result = await retryService.ExecuteAsync(async () =>
    {
        attemptCount++;
        if (attemptCount < 3)
            throw new HttpRequestException("Transient error");
        return "success";
    });
    
    // Assert
    Assert.That(result, Is.EqualTo("success"));
    Assert.That(attemptCount, Is.EqualTo(3));
}

[Test]
public async Task BulkheadService_ShouldRejectWhenAtCapacity()
{
    // Arrange
    var settings = Options.Create(new ResilienceSettings
    {
        Bulkhead = new BulkheadSettings
        {
            MaxParallelization = 2
        }
    });
    
    var bulkheadService = new BulkheadService(settings, Mock.Of<ILogger<BulkheadService>>());
    
    // Act - Start two long-running operations to fill the bulkhead
    var task1 = bulkheadService.ExecuteAsync(async () =>
    {
        await Task.Delay(1000);
        return "result1";
    }, "test-bulkhead");
    
    var task2 = bulkheadService.ExecuteAsync(async () =>
    {
        await Task.Delay(1000);
        return "result2";
    }, "test-bulkhead");
    
    // Third operation should be rejected immediately
    await Assert.ThrowsAsync<BulkheadRejectedException>(() =>
        bulkheadService.ExecuteAsync(async () => "result3", "test-bulkhead"));
    
    // Cleanup
    await Task.WhenAll(task1, task2);
}
```

## En İyi Uygulamalar

1. **Layered Defense**: Birden fazla resilience pattern kombinasyonu
2. **Monitoring**: Resilience pattern'larının health monitoring'i
3. **Configuration**: Environment-specific resilience settings
4. **Testing**: Chaos engineering ile resilience testing
5. **Fallback Strategy**: Graceful degradation mechanisms
6. **Circuit Breaker**: External dependencies için circuit breaker
7. **Bulkhead**: Resource isolation için bulkhead patterns
8. **Observability**: Resilience pattern'larının observability'si

## Troubleshooting

### Yaygın Sorunlar
1. **Circuit Breaker Oscillation**: Too sensitive thresholds
2. **Retry Storms**: Aggressive retry without backoff
3. **Bulkhead Starvation**: Too restrictive parallelization limits
4. **Timeout Issues**: Too short timeout values
5. **Resource Leaks**: Improper semaphore/resource management

### Performance Monitoring
```csharp
// Application Insights ile entegrasyon
services.AddApplicationInsightsTelemetry();

// Custom telemetry tracking
public class ResilienceTelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackCircuitBreakerStateChange(string key, CircuitBreakerState oldState, CircuitBreakerState newState)
    {
        _telemetryClient.TrackEvent("CircuitBreakerStateChanged", new Dictionary<string, string>
        {
            ["CircuitBreakerKey"] = key,
            ["OldState"] = oldState.ToString(),
            ["NewState"] = newState.ToString()
        });
    }
    
    public void TrackRetryAttempt(string operation, int attemptNumber, Exception exception)
    {
        _telemetryClient.TrackMetric("RetryAttempts", attemptNumber, new Dictionary<string, string>
        {
            ["Operation"] = operation,
            ["ExceptionType"] = exception.GetType().Name
        });
    }
}
```

## Mimari ve Tasarım Prensipleri

### Dayanıklılık Mimarisi
```
┌─────────────────────────────────────────────────────┐
│                   Application Layer                 │
├─────────────────────────────────────────────────────┤
│              Resilience Orchestration               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐   │
│  │Rate Limiting│ │  Bulkhead   │ │Circuit Break│   │
│  └─────────────┘ └─────────────┘ └─────────────┘   │
│  ┌─────────────┐ ┌─────────────────────────────┐   │
│  │   Timeout   │ │        Retry Policy         │   │
│  └─────────────┘ └─────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│                 External Services                   │
│    Database    │    APIs     │   File Systems     │
└─────────────────────────────────────────────────────┘
```

### Pattern Kombinasyonları
1. **Defensive Combination**: Rate Limit → Bulkhead → Circuit Breaker → Retry → Timeout
2. **Performance Combination**: Bulkhead → Timeout → Circuit Breaker
3. **Reliability Combination**: Retry → Circuit Breaker → Timeout

## Konfigürasyon Yönetimi

### Environment-Specific Settings
```json
// appsettings.Development.json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 3,
      "BreakDuration": "00:00:10"
    },
    "Retry": {
      "MaxRetryAttempts": 2,
      "BaseDelayMs": 500
    }
  }
}

// appsettings.Production.json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "BreakDuration": "00:01:00"
    },
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelayMs": 1000
    }
  }
}
```

### Dynamic Configuration
```csharp
// IOptionsMonitor kullanarak runtime'da değişiklik
public class DynamicResilienceService
{
    private readonly IOptionsMonitor<ResilienceSettings> _settings;
    
    public DynamicResilienceService(IOptionsMonitor<ResilienceSettings> settings)
    {
        _settings = settings;
        _settings.OnChange(OnSettingsChanged);
    }
    
    private void OnSettingsChanged(ResilienceSettings settings)
    {
        // Yeni ayarlarla servisleri yeniden yapılandır
        ReconfigureServices(settings);
    }
}
```

## Sonuç

Enterprise.Shared.Resilience projesi, modern kurumsal uygulamalar için kapsamlı bir dayanıklılık çözümü sunar. Polly v8.2.0 tabanlı bu sistem, mikroservis mimarilerinde kritik olan güvenilirlik, performans ve izleme özelliklerini tek bir pakette birleştirir.

### Temel Avantajlar:
- **Kolay Entegrasyon**: Minimal kurulum ile hızlı başlangıç
- **Kapsamlı Pattern Desteği**: Tüm ana resilience pattern'ları
- **Production Ready**: Kurumsal seviyede test edilmiş
- **Monitoring & Observability**: Detaylı metrik ve health check desteği
- **Configurable**: Environment-specific yapılandırma desteği
- **Type-Safe**: Strong typing ile compile-time güvenlik

Bu sistem sayesinde uygulamalarınız geçici hatalar karşısında daha dayanıklı, performanslı ve güvenilir hale gelir.