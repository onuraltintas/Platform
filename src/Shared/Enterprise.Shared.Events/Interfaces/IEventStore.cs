using Enterprise.Shared.Events.Models;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Event store interface'i
/// Event sourcing için event'lerin saklanması ve okunması
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Event'leri stream'e append eder
    /// </summary>
    /// <typeparam name="T">Domain event türü</typeparam>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="events">Append edilecek event'ler</param>
    /// <param name="expectedVersion">Beklenen stream versiyonu (concurrency kontrolü)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AppendEventsAsync<T>(string streamId, IEnumerable<T> events, int expectedVersion, 
        CancellationToken cancellationToken = default) where T : DomainEvent;

    /// <summary>
    /// Stream'den event'leri okur
    /// </summary>
    /// <typeparam name="T">Domain event türü</typeparam>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="fromVersion">Başlangıç versiyonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<T>> GetEventsAsync<T>(string streamId, int fromVersion = 0, 
        CancellationToken cancellationToken = default) where T : DomainEvent;

    /// <summary>
    /// Stream'i okur (tüm event'leri StoredEvent olarak)
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="fromVersion">Başlangıç versiyonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<EventStream> GetStreamAsync(string streamId, int fromVersion = 0, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream'in mevcut versiyonunu getirir
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> GetStreamVersionAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream var mı kontrol eder
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> StreamExistsAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream'i siler
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteStreamAsync(string streamId, CancellationToken cancellationToken = default);
}