namespace Enterprise.Shared.Events.Models;

/// <summary>
/// Outbox pattern için event entity'si
/// Transactional outbox pattern implementation
/// </summary>
public class OutboxEvent
{
    /// <summary>
    /// Outbox event'in kimliği
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Event türü
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialize edilmiş event verisi
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Event'in oluştuğu zaman
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Event yayınlandı mı?
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Event'in yayınlandığı zaman
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Retry sayısı
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Son hata mesajı
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Son retry zamanı
    /// </summary>
    public DateTime? LastRetryAt { get; set; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Event'in gönderileceği routing key
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Event metadata'sı (JSON)
    /// </summary>
    public string? Metadata { get; set; }
}