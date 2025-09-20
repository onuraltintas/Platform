namespace Enterprise.Shared.Events.Models;

/// <summary>
/// Event sourcing için event stream'i temsil eder
/// </summary>
public class EventStream
{
    /// <summary>
    /// Stream'in kimliği
    /// </summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>
    /// Stream'in mevcut versiyonu
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Stream içindeki event'ler
    /// </summary>
    public IEnumerable<StoredEvent> Events { get; init; } = Enumerable.Empty<StoredEvent>();

    /// <summary>
    /// Stream'in oluştuğu zaman
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Stream'in son güncellenme zamanı
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event store'da saklanan event'i temsil eder
/// </summary>
public class StoredEvent
{
    /// <summary>
    /// Event'in kimliği
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Stream ID'si
    /// </summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>
    /// Event versiyonu
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Event türü
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Serialize edilmiş event verisi
    /// </summary>
    public string EventData { get; init; } = string.Empty;

    /// <summary>
    /// Event metadata'sı (JSON)
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Event'in oluştuğu zaman
    /// </summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Event'in store'a kayıt edildiği zaman
    /// </summary>
    public DateTime StoredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event store concurrency exception
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
    
    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}