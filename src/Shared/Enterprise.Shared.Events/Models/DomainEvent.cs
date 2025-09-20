using MediatR;

namespace Enterprise.Shared.Events.Models;

/// <summary>
/// Domain event'lerin base sınıfı. 
/// Domain içi olayları temsil eder ve MediatR ile işlenir.
/// </summary>
public abstract record DomainEvent : INotification
{
    /// <summary>
    /// Event'in benzersiz kimliği
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Event'in oluştuğu zaman (UTC)
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event türü (sınıf adı)
    /// </summary>
    public string EventType { get; init; }

    /// <summary>
    /// Correlation ID - event zincirini takip etmek için
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Domain event constructor
    /// </summary>
    protected DomainEvent()
    {
        EventType = GetType().Name;
    }

    /// <summary>
    /// Event'in string temsilini döndürür
    /// </summary>
    public override string ToString()
    {
        return $"{EventType} - ID: {EventId}, Occurred: {OccurredAt:yyyy-MM-dd HH:mm:ss} UTC";
    }
}