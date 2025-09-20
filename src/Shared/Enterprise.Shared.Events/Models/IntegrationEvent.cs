namespace Enterprise.Shared.Events.Models;

/// <summary>
/// Integration event'lerin base sınıfı.
/// Mikroservisler arası iletişim için kullanılır.
/// </summary>
public abstract record IntegrationEvent
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
    /// Event versiyonu - backward compatibility için
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Event'i gönderen servis
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Event'in metadata'sı
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Integration event constructor
    /// </summary>
    protected IntegrationEvent()
    {
        EventType = GetType().Name;
    }

    /// <summary>
    /// Event'in string temsilini döndürür
    /// </summary>
    public override string ToString()
    {
        return $"{EventType} v{Version} - ID: {EventId}, Source: {Source}, Occurred: {OccurredAt:yyyy-MM-dd HH:mm:ss} UTC";
    }
}