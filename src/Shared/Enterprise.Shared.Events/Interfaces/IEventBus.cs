using Enterprise.Shared.Events.Models;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Event bus interface'i - Integration event'lerin yayınlanması için
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Integration event'i yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    /// <summary>
    /// Integration event'i belirli routing key ile yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="routingKey">Routing key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    /// <summary>
    /// Integration event'i delayed yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="delay">Gecikme süresi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishDelayedAsync<T>(T @event, TimeSpan delay, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    /// <summary>
    /// Integration event'ler için subscription oluşturur
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <typeparam name="TH">Handler türü</typeparam>
    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    /// <summary>
    /// Integration event subscription'ını kaldırır
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <typeparam name="TH">Handler türü</typeparam>
    void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;
}