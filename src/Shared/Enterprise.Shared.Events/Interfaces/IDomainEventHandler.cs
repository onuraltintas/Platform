using Enterprise.Shared.Events.Models;
using MediatR;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Domain event handler interface'i
/// MediatR INotificationHandler'ından türer
/// </summary>
/// <typeparam name="TEvent">Domain event türü</typeparam>
public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : DomainEvent
{
}

/// <summary>
/// Domain event dispatcher interface'i
/// Domain event'lerin MediatR ile dispatch edilmesi için
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Domain event'i dispatch eder
    /// </summary>
    /// <param name="domainEvent">Dispatch edilecek domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla domain event'i dispatch eder
    /// </summary>
    /// <param name="domainEvents">Dispatch edilecek domain event'ler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}