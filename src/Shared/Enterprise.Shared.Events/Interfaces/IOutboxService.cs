using Enterprise.Shared.Events.Models;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Outbox pattern service interface'i
/// Transactional outbox pattern implementation
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Event'i outbox'a ekler
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Outbox'a eklenecek event</param>
    /// <param name="routingKey">Routing key (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddEventAsync<T>(T @event, string? routingKey = null, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent;

    /// <summary>
    /// Birden fazla event'i outbox'a ekler
    /// </summary>
    /// <param name="events">Outbox'a eklenecek event'ler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddEventsAsync(IEnumerable<IntegrationEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yayınlanmamış event'leri işler
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessUnpublishedEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Başarısız event'leri yeniden işler
    /// </summary>
    /// <param name="maxRetryCount">Maksimum retry sayısı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RetryFailedEventsAsync(int maxRetryCount = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Eski outbox event'leri temizler
    /// </summary>
    /// <param name="olderThanDays">Kaç günden eski event'ler temizlensin</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupOldEventsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Outbox istatistiklerini getirir
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbox istatistikleri
/// </summary>
public class OutboxStatistics
{
    /// <summary>
    /// Toplam event sayısı
    /// </summary>
    public int TotalEvents { get; init; }

    /// <summary>
    /// Yayınlanmış event sayısı
    /// </summary>
    public int PublishedEvents { get; init; }

    /// <summary>
    /// Yayınlanmamış event sayısı
    /// </summary>
    public int UnpublishedEvents { get; init; }

    /// <summary>
    /// Başarısız event sayısı
    /// </summary>
    public int FailedEvents { get; init; }

    /// <summary>
    /// Son işlenme zamanı
    /// </summary>
    public DateTime? LastProcessedAt { get; init; }

    /// <summary>
    /// En eski yayınlanmamış event zamanı
    /// </summary>
    public DateTime? OldestUnpublishedAt { get; init; }
}