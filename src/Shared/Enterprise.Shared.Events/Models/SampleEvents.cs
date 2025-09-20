namespace Enterprise.Shared.Events.Models;

// Domain Events
/// <summary>
/// Kullanıcı oluşturulduğunda tetiklenen domain event
/// </summary>
public record UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    
    public UserCreatedEvent()
    {
    }
    
    public UserCreatedEvent(Guid userId, string email, string firstName, string lastName, string correlationId)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Kullanıcı email'i değiştirildiğinde tetiklenen domain event
/// </summary>
public record UserEmailChangedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string OldEmail { get; init; } = string.Empty;
    public string NewEmail { get; init; } = string.Empty;
    
    public UserEmailChangedEvent()
    {
    }
    
    public UserEmailChangedEvent(Guid userId, string oldEmail, string newEmail, string correlationId)
    {
        UserId = userId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Kullanıcı silindiğinde tetiklenen domain event
/// </summary>
public record UserDeletedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
    
    public UserDeletedEvent()
    {
    }
    
    public UserDeletedEvent(Guid userId, string email, DateTime deletedAt, string correlationId)
    {
        UserId = userId;
        Email = email;
        DeletedAt = deletedAt;
        CorrelationId = correlationId;
    }
}

// Integration Events
/// <summary>
/// Kullanıcı kayıt edildiğinde tetiklenen integration event
/// </summary>
public record UserRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}

/// <summary>
/// Email doğrulama isteği integration event'i
/// </summary>
public record EmailVerificationRequestedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Kullanıcı profili güncellendiğinde tetiklenen integration event
/// </summary>
public record UserProfileUpdatedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public Dictionary<string, object> UpdatedFields { get; init; } = new();
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Sipariş oluşturulduğunda tetiklenen integration event
/// </summary>
public record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemData> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Sipariş item verisi
/// </summary>
public record OrderItemData
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}