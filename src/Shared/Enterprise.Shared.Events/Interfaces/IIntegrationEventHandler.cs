using Enterprise.Shared.Events.Models;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Integration event handler interface'i
/// </summary>
/// <typeparam name="T">Integration event türü</typeparam>
public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    /// <summary>
    /// Integration event'i handle eder
    /// </summary>
    /// <param name="event">Handle edilecek event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}