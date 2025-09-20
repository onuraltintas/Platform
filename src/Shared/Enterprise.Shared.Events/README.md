# Enterprise.Shared.Events

**Versiyon:** 1.0.0  
**Hedef Framework:** .NET 8.0  
**Geliştirici:** Enterprise Platform Team

## 📋 Proje Amacı

Enterprise.Shared.Events, Enterprise mikroservis platformu için geliştirilmiş kapsamlı bir event-driven architecture (olay güdümlü mimari) kütüphanesidir. Domain events, integration events, event sourcing, RabbitMQ tabanlı asenkron mesajlaşma ve outbox pattern desteği ile enterprise-grade event yönetimi çözümleri sunar.

## 🌟 Ana Özellikler

### Event-Driven Architecture
- **Domain Events**: Domain içi olayların yönetimi ve MediatR ile işlenmesi
- **Integration Events**: Mikroservisler arası asenkron iletişim
- **Event Sourcing**: Event tabanlı veri persistance desteği
- **Outbox Pattern**: Transactional event publishing garantisi

### Message Broker Entegrasyonu
- **RabbitMQ**: MassTransit ile gelişmiş mesaj kuyruğu entegrasyonu
- **Routing**: Flexible routing key desteği
- **Retry Logic**: Otomatik hata yönetimi ve retry mekanizması
- **Dead Letter Queue**: Başarısız mesaj yönetimi

### Enterprise-Grade Özellikler
- **Event Versioning**: Backward compatibility desteği
- **Correlation Tracking**: Event zinciri takibi
- **Performance Monitoring**: Event processing metrikleri
- **Transactional Safety**: ACID garantileri

## 🛠 Kullanılan Teknolojiler

### Ana Bağımlılıklar
- **MassTransit 8.1.3**: Message broker abstraction katmanı
- **MassTransit.RabbitMQ 8.1.3**: RabbitMQ transport provider
- **MediatR 12.2.0**: In-process messaging patterns
- **Microsoft.EntityFrameworkCore 9.0.0**: ORM desteği
- **System.Text.Json 9.0.0**: JSON serialization

### Microsoft Extensions
- **Microsoft.Extensions.DependencyInjection 9.0.0**: Dependency injection
- **Microsoft.Extensions.Hosting.Abstractions 9.0.0**: Background services
- **Microsoft.Extensions.Configuration.Abstractions 9.0.0**: Configuration management
- **Microsoft.Extensions.Logging.Abstractions 9.0.0**: Structured logging
- **Microsoft.Extensions.Options 9.0.0**: Options pattern

## ⚙️ Kurulum ve Konfigürasyon

### 1. NuGet Paketi Yükleme
```bash
dotnet add package Enterprise.Shared.Events
```

### 2. Dependency Injection Konfigürasyonu

#### Tam Konfigürasyon (Production)
```csharp
// Program.cs
using Enterprise.Shared.Events.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Enterprise Events servislerini ekle
builder.Services.AddSharedEvents(
    builder.Configuration,
    typeof(Program).Assembly  // Event handler'ları içeren assembly'ler
);

// Outbox processor'ı background service olarak ekle
builder.Services.AddOutboxProcessor();

var app = builder.Build();
```

#### Test/Development için In-Memory Konfigürasyon
```csharp
// Test/Development için
builder.Services.AddSharedEventsWithInMemory(
    configureMassTransit: x =>
    {
        // Ek MassTransit konfigürasyonu
        x.AddConsumer<CustomEventHandler>();
    },
    typeof(Program).Assembly
);
```

#### Sadece Domain Events
```csharp
// Integration events olmadan sadece domain events
builder.Services.AddDomainEvents(typeof(Program).Assembly);
```

### 3. appsettings.json Konfigürasyonu

```json
{
  "EventSettings": {
    "RabbitMQ": {
      "Host": "localhost",
      "Port": 5672,
      "Username": "enterprise_user",
      "Password": "enterprise_pass123",
      "VirtualHost": "/enterprise",
      "ConnectionRetryCount": 5,
      "PrefetchCount": 20,
      "ConnectionTimeout": 30,
      "UseSsl": false
    },
    "EventStore": {
      "ConnectionString": "Server=localhost;Database=EnterpriseEventStore;Trusted_Connection=true;",
      "StreamPrefix": "enterprise-stream-",
      "SnapshotInterval": 100,
      "RetentionDays": 365
    },
    "DomainEvents": {
      "EnableOutbox": true,
      "PublishAfterCommit": true,
      "MaxRetryCount": 3,
      "RetryIntervalSeconds": 30
    },
    "Outbox": {
      "Enabled": true,
      "ProcessorIntervalSeconds": 30,
      "BatchSize": 100,
      "MaxRetryCount": 3,
      "FailedEventRetentionDays": 30
    }
  },
  "Logging": {
    "LogLevel": {
      "Enterprise.Shared.Events": "Information",
      "MassTransit": "Warning"
    }
  }
}
```

## 📖 Kullanım Kılavuzu

### 1. Domain Events

#### Domain Event Oluşturma
```csharp
using Enterprise.Shared.Events.Models;

// Domain event tanımı
public record UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string UserName { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
    
    public UserCreatedEvent(Guid userId, string userName, string email)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }
}
```

#### Domain Event Handler Oluşturma
```csharp
using Enterprise.Shared.Events.Interfaces;
using MediatR;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;
    
    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing UserCreated event for user {UserId}", 
            notification.UserId);
        
        // Welcome email gönder
        await _emailService.SendWelcomeEmailAsync(
            notification.Email, 
            notification.UserName, 
            cancellationToken);
            
        _logger.LogInformation("Welcome email sent to user {UserId}", 
            notification.UserId);
    }
}
```

#### Domain Event Yayınlama
```csharp
public class UserService
{
    private readonly IDomainEventDispatcher _eventDispatcher;
    
    public UserService(IDomainEventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
    }
    
    public async Task<User> CreateUserAsync(string userName, string email)
    {
        var user = new User(userName, email);
        
        // Domain event yayınla
        var domainEvent = new UserCreatedEvent(user.Id, user.UserName, user.Email);
        await _eventDispatcher.DispatchAsync(domainEvent);
        
        return user;
    }
}
```

### 2. Integration Events

#### Integration Event Oluşturma
```csharp
using Enterprise.Shared.Events.Models;

public record OrderCompletedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItem> Items { get; init; } = new();
    public string PaymentMethod { get; init; } = string.Empty;
    
    public OrderCompletedIntegrationEvent(
        Guid orderId, 
        Guid customerId, 
        decimal totalAmount,
        List<OrderItem> items,
        string paymentMethod,
        string correlationId = "")
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Items = items;
        PaymentMethod = paymentMethod;
        CorrelationId = correlationId;
        Source = "OrderService";
        Version = 1;
        
        // Metadata ekle
        Metadata["Currency"] = "TRY";
        Metadata["Region"] = "Turkey";
    }
}

public record OrderItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
```

#### Integration Event Handler Oluşturma
```csharp
using Enterprise.Shared.Events.Interfaces;
using MassTransit;

public class OrderCompletedIntegrationEventHandler : 
    IConsumer<OrderCompletedIntegrationEvent>,
    IIntegrationEventHandler<OrderCompletedIntegrationEvent>
{
    private readonly ILogger<OrderCompletedIntegrationEventHandler> _logger;
    private readonly IInventoryService _inventoryService;
    private readonly IShippingService _shippingService;
    
    public OrderCompletedIntegrationEventHandler(
        ILogger<OrderCompletedIntegrationEventHandler> logger,
        IInventoryService inventoryService,
        IShippingService shippingService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
        _shippingService = shippingService;
    }
    
    public async Task Consume(ConsumeContext<OrderCompletedIntegrationEvent> context)
    {
        await HandleAsync(context.Message, context.CancellationToken);
    }
    
    public async Task HandleAsync(
        OrderCompletedIntegrationEvent integrationEvent, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order completed event for order {OrderId}", 
            integrationEvent.OrderId);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "OrderId", integrationEvent.OrderId },
            { "CustomerId", integrationEvent.CustomerId },
            { "CorrelationId", integrationEvent.CorrelationId }
        });
        
        try
        {
            // Envanter güncellemesi
            foreach (var item in integrationEvent.Items)
            {
                await _inventoryService.UpdateStockAsync(
                    item.ProductId, 
                    -item.Quantity, 
                    cancellationToken);
            }
            
            // Kargo başlatma
            await _shippingService.InitiateShippingAsync(
                integrationEvent.OrderId,
                integrationEvent.CustomerId,
                cancellationToken);
            
            _logger.LogInformation("Successfully processed order completed event for order {OrderId}", 
                integrationEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order completed event for order {OrderId}", 
                integrationEvent.OrderId);
            throw; // MassTransit retry mekanizması devreye girer
        }
    }
}
```

#### Integration Event Yayınlama
```csharp
public class OrderService
{
    private readonly IEventBus _eventBus;
    private readonly IOutboxService _outboxService;
    
    public OrderService(IEventBus eventBus, IOutboxService outboxService)
    {
        _eventBus = eventBus;
        _outboxService = outboxService;
    }
    
    // Direkt yayınlama
    public async Task CompleteOrderAsync(Guid orderId)
    {
        var order = await GetOrderAsync(orderId);
        order.MarkAsCompleted();
        
        var integrationEvent = new OrderCompletedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            order.Items,
            order.PaymentMethod,
            Guid.NewGuid().ToString()
        );
        
        // Direkt event bus ile yayınla
        await _eventBus.PublishAsync(integrationEvent);
    }
    
    // Outbox pattern ile güvenli yayınlama
    public async Task CompleteOrderWithOutboxAsync(Guid orderId)
    {
        using var transaction = await BeginTransactionAsync();
        
        try
        {
            var order = await GetOrderAsync(orderId);
            order.MarkAsCompleted();
            await SaveOrderAsync(order);
            
            var integrationEvent = new OrderCompletedIntegrationEvent(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.Items,
                order.PaymentMethod,
                Guid.NewGuid().ToString()
            );
            
            // Outbox'a ekle (transaction içinde)
            await _outboxService.AddEventAsync(integrationEvent, "order.completed");
            
            await transaction.CommitAsync();
            
            // Outbox processor arkaplanda event'i yayınlayacak
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### 3. Event Store Kullanımı

#### Event Store ile Event Saklama
```csharp
public class UserEventStore
{
    private readonly IEventStore _eventStore;
    private readonly IEventSerializer _serializer;
    
    public UserEventStore(IEventStore eventStore, IEventSerializer serializer)
    {
        _eventStore = eventStore;
        _serializer = serializer;
    }
    
    public async Task SaveUserEventsAsync(Guid userId, IEnumerable<DomainEvent> events)
    {
        var streamId = $"user-{userId}";
        var eventData = events.Select(e => new EventStream
        {
            StreamId = streamId,
            EventType = e.EventType,
            EventData = _serializer.Serialize(e),
            Version = 1,
            CreatedAt = DateTime.UtcNow
        });
        
        await _eventStore.AppendEventsAsync(streamId, eventData);
    }
    
    public async Task<IEnumerable<DomainEvent>> LoadUserEventsAsync(Guid userId)
    {
        var streamId = $"user-{userId}";
        var eventStreams = await _eventStore.GetEventsAsync(streamId);
        
        var events = new List<DomainEvent>();
        foreach (var eventStream in eventStreams)
        {
            var domainEvent = _serializer.Deserialize(eventStream.EventData, eventStream.EventType);
            if (domainEvent is DomainEvent de)
            {
                events.Add(de);
            }
        }
        
        return events.OrderBy(e => e.OccurredAt);
    }
}
```

## 🏗 Mimari ve Design Patterns

### 1. Event-Driven Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Domain        │    │  Integration    │    │  External       │
│   Events        │    │  Events         │    │  Systems        │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ MediatR     │ │────│ │ MassTransit │ │────│ │ RabbitMQ    │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 2. Outbox Pattern

```
┌──────────────────────────────────────────────────────────┐
│                    Transaction                           │
│                                                          │
│  ┌─────────────┐       ┌─────────────────────────────┐   │
│  │  Business   │       │        Outbox Table         │   │
│  │  Operation  │  ────►│  - Event Data               │   │
│  │             │       │  - Created At               │   │
│  └─────────────┘       │  - Published                │   │
│                        │  - Retry Count              │   │
│                        └─────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                        ┌─────────────────────────────┐
                        │   Background Processor       │
                        │   - Reads unpublished       │
                        │   - Publishes to bus        │
                        │   - Handles retries         │
                        └─────────────────────────────┘
```

### 3. CQRS Integration

```csharp
// Command tarafı - write operations
public class CreateUserCommand : IRequest<User>
{
    public string UserName { get; set; }
    public string Email { get; set; }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.UserName, request.Email);
        await _repository.AddAsync(user);
        
        // Domain event yayınla
        await _eventDispatcher.DispatchAsync(new UserCreatedEvent(user.Id, user.UserName, user.Email));
        
        return user;
    }
}

// Query tarafı - read operations (event handler'lar ile güncellenir)
public class UserViewModelUpdater : INotificationHandler<UserCreatedEvent>
{
    private readonly IUserReadModelRepository _readRepository;
    
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        var userViewModel = new UserViewModel
        {
            Id = notification.UserId,
            UserName = notification.UserName,
            Email = notification.Email,
            CreatedAt = notification.OccurredAt
        };
        
        await _readRepository.AddAsync(userViewModel);
    }
}
```

## 📊 Monitoring ve Metrics

### 1. Event Processing Metrikleri

```csharp
public class EventMetricsService
{
    private readonly ILogger<EventMetricsService> _logger;
    
    public async Task TrackEventProcessing<T>(T @event, Func<Task> operation) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var eventType = typeof(T).Name;
        
        try
        {
            await operation();
            stopwatch.Stop();
            
            _logger.LogInformation("Event {EventType} processed successfully in {ElapsedMs}ms",
                eventType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Event {EventType} processing failed after {ElapsedMs}ms",
                eventType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 2. Health Checks

```csharp
// Startup.cs
builder.Services.AddHealthChecks()
    .AddCheck<EventBusHealthCheck>("event_bus")
    .AddCheck<OutboxHealthCheck>("outbox")
    .AddRabbitMQ(connectionString: "amqp://localhost:5672", name: "rabbitmq");

// EventBusHealthCheck implementation
public class EventBusHealthCheck : IHealthCheck
{
    private readonly IBus _bus;
    
    public EventBusHealthCheck(IBus bus)
    {
        _bus = bus;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test event yayınlayarak bağlantıyı kontrol et
            var testEvent = new HealthCheckEvent();
            await _bus.Publish(testEvent, cancellationToken);
            
            return HealthCheckResult.Healthy("Event bus is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Event bus is not healthy", ex);
        }
    }
}

public record HealthCheckEvent : IntegrationEvent;
```

## 🔧 Best Practices

### 1. Event Design

#### ✅ İyi Örnekler
```csharp
// Event'ler immutable olmalı (record kullanın)
public record ProductPriceChangedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; }
    public string ChangedBy { get; init; }
}

// Integration event'ler versiyonlanmalı
public record OrderShippedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; }
    public DateTime ShippedAt { get; init; }
    
    // Version 2'de eklenen özellik (backward compatibility)
    public string CourierCompany { get; init; } = "Unknown";
}
```

#### ❌ Kötü Örnekler
```csharp
// Mutable event'ler (class kullanımı)
public class BadEvent : DomainEvent
{
    public string Data { get; set; } // Mutable!
}

// Çok fazla bilgi içeren event'ler
public record MonolithicEvent : DomainEvent
{
    public User User { get; init; }        // Büyük nesne
    public Order Order { get; init; }      // Büyük nesne
    public List<Product> Products { get; init; } // List
    // Bu event çok büyük ve network trafiğini artırır
}
```

### 2. Error Handling

```csharp
public class RobustEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<RobustEventHandler> _logger;
    private readonly IRetryPolicy _retryPolicy;
    
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                // İş logic'i buraya
                await ProcessEventAsync(notification, cancellationToken);
            }
            catch (TransientException ex)
            {
                _logger.LogWarning(ex, "Transient error processing event {EventId}, will retry", 
                    notification.EventId);
                throw; // Retry için exception'ı yeniden fırlat
            }
            catch (PermanentException ex)
            {
                _logger.LogError(ex, "Permanent error processing event {EventId}, moving to DLQ", 
                    notification.EventId);
                await SendToDeadLetterQueueAsync(notification, ex);
                // Permanent error'lar için exception fırlatma (retry yapmasın)
            }
        });
    }
}
```

### 3. Event Versioning

```csharp
// Event version handling
public class EventVersionHandler : IConsumer<OrderCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var @event = context.Message;
        
        // Version-based handling
        switch (@event.Version)
        {
            case 1:
                await HandleV1Event(@event, context.CancellationToken);
                break;
            case 2:
                await HandleV2Event(@event, context.CancellationToken);
                break;
            default:
                throw new NotSupportedException($"Event version {@event.Version} is not supported");
        }
    }
    
    private async Task HandleV1Event(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // V1 logic (backward compatibility)
    }
    
    private async Task HandleV2Event(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // V2 logic (yeni özellikler)
    }
}
```

## 🚀 Performans İpuçları

### 1. Event Batching
```csharp
public async Task ProcessEventsInBatch(IEnumerable<IntegrationEvent> events)
{
    var batches = events.Chunk(100); // 100'lük gruplar halinde işle
    
    foreach (var batch in batches)
    {
        var tasks = batch.Select(async @event =>
        {
            await _eventBus.PublishAsync(@event);
        });
        
        await Task.WhenAll(tasks);
    }
}
```

### 2. Event Compression
```csharp
public class CompressedEventSerializer : IEventSerializer
{
    public string Serialize<T>(T @event)
    {
        var json = JsonSerializer.Serialize(@event);
        return Compress(json);
    }
    
    private string Compress(string data)
    {
        // GZip compression implementation
        // Büyük event'ler için network trafiğini azaltır
    }
}
```

## 🐛 Troubleshooting

### Yaygın Problemler ve Çözümleri

#### 1. RabbitMQ Connection Issues
```bash
# RabbitMQ durumunu kontrol et
rabbitmqctl status

# Queue'ları listele
rabbitmqctl list_queues

# Connection'ları kontrol et
rabbitmqctl list_connections
```

**Çözüm:**
```csharp
// Connection retry configuration
services.Configure<EventSettings>(settings =>
{
    settings.RabbitMQ.ConnectionRetryCount = 10;
    settings.RabbitMQ.ConnectionTimeout = 60;
});
```

#### 2. Event Handler Exceptions
```csharp
// Global exception handler
public class GlobalEventExceptionHandler : IConsumer<Fault<IntegrationEvent>>
{
    private readonly ILogger<GlobalEventExceptionHandler> _logger;
    
    public async Task Consume(ConsumeContext<Fault<IntegrationEvent>> context)
    {
        var exception = context.Message.Exceptions.FirstOrDefault();
        var originalEvent = context.Message.Message;
        
        _logger.LogError("Global event processing error for {EventType}: {Error}",
            originalEvent.EventType, exception?.Message);
        
        // Dead letter queue'ya gönder
        await context.Publish(new DeadLetterEvent
        {
            OriginalEvent = originalEvent,
            Error = exception?.Message ?? "Unknown error",
            FailedAt = DateTime.UtcNow
        });
    }
}
```

#### 3. Memory Leaks
```csharp
// Event handler'larda resource cleanup
public class ResourceAwareEventHandler : INotificationHandler<LargeDataEvent>, IDisposable
{
    private readonly IDisposableResource _resource;
    
    public async Task Handle(LargeDataEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Processing {EventId}", notification.EventId);
        
        try
        {
            await ProcessEvent(notification);
        }
        finally
        {
            // Cleanup logic
            await _resource.DisposeAsync();
        }
    }
    
    public void Dispose()
    {
        _resource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

## 📝 Test Stratejisi

### 1. Unit Tests
```csharp
[Test]
public async Task DomainEventDispatcher_Should_Dispatch_Event()
{
    // Arrange
    var mockHandler = new Mock<INotificationHandler<UserCreatedEvent>>();
    var mockMediator = new Mock<IMediator>();
    var dispatcher = new DomainEventDispatcher(mockMediator.Object, Mock.Of<ILogger<DomainEventDispatcher>>());
    
    var domainEvent = new UserCreatedEvent(Guid.NewGuid(), "testuser", "test@example.com");
    
    // Act
    await dispatcher.DispatchAsync(domainEvent);
    
    // Assert
    mockMediator.Verify(m => m.Publish(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
}
```

### 2. Integration Tests
```csharp
[Test]
public async Task EventBus_Should_Publish_And_Consume_Event()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddSharedEventsWithInMemory(
        configureMassTransit: x => x.AddConsumer<TestEventHandler>()
    );
    
    var provider = services.BuildServiceProvider();
    var eventBus = provider.GetRequiredService<IEventBus>();
    
    // Act
    var testEvent = new TestIntegrationEvent { Data = "test" };
    await eventBus.PublishAsync(testEvent);
    
    // Assert
    await Task.Delay(1000); // Event processing için bekle
    Assert.True(TestEventHandler.EventReceived);
}
```

## 🔄 Migration ve Upgrade

### Version 1.0.0'dan Upgrade
```csharp
// Eski kullanım (deprecated)
services.AddMediatR(typeof(Program));

// Yeni kullanım
services.AddSharedEvents(configuration, typeof(Program).Assembly);
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add some amazing feature'`)
4. Branch'i push yapın (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje Enterprise Platform Team tarafından geliştirilmiştir.

## 📞 Destek

- **Dokümantasyon**: Bu README dosyası
- **Issue Tracking**: Internal issue tracking system
- **Email**: enterprise-platform@company.com

---

**🎉 Enterprise.Shared.Events ile güçlü, ölçeklenebilir ve güvenilir event-driven microservice mimarinizi oluşturun!** 🚀