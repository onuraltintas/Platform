using EgitimPlatform.Shared.Messaging.Events;

namespace EgitimPlatform.Shared.Messaging.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
    
    Task PublishAsync<T>(T integrationEvent, string routingKey, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
    
    Task PublishBatchAsync<T>(IEnumerable<T> integrationEvents, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
    
    Task SchedulePublishAsync<T>(T integrationEvent, DateTime scheduleTime, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
}

public interface IIntegrationEventHandler<in T> where T : class, IIntegrationEvent
{
    Task HandleAsync(T integrationEvent, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventHandlerFactory
{
    IEnumerable<IIntegrationEventHandler<T>> GetHandlers<T>() where T : class, IIntegrationEvent;
    IIntegrationEventHandler<T>? GetHandler<T>() where T : class, IIntegrationEvent;
}