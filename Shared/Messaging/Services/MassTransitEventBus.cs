using MassTransit;
using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Messaging.Abstractions;
using EgitimPlatform.Shared.Messaging.Events;

namespace EgitimPlatform.Shared.Messaging.Services;

public class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMessageScheduler _messageScheduler;
    private readonly ILogger<MassTransitEventBus> _logger;

    public MassTransitEventBus(
        IPublishEndpoint publishEndpoint,
        IMessageScheduler messageScheduler,
        ILogger<MassTransitEventBus> logger)
    {
        _publishEndpoint = publishEndpoint;
        _messageScheduler = messageScheduler;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent
    {
        try
        {
            _logger.LogInformation("Publishing integration event: {EventType} with ID: {EventId}", 
                integrationEvent.EventType, integrationEvent.Id);

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully published integration event: {EventType} with ID: {EventId}", 
                integrationEvent.EventType, integrationEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish integration event: {EventType} with ID: {EventId}", 
                integrationEvent.EventType, integrationEvent.Id);
            throw;
        }
    }

    public async Task PublishAsync<T>(T integrationEvent, string routingKey, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent
    {
        try
        {
            _logger.LogInformation("Publishing integration event: {EventType} with ID: {EventId} and routing key: {RoutingKey}", 
                integrationEvent.EventType, integrationEvent.Id, routingKey);

            await _publishEndpoint.Publish(integrationEvent, context =>
            {
                context.SetRoutingKey(routingKey);
            }, cancellationToken);

            _logger.LogInformation("Successfully published integration event: {EventType} with ID: {EventId} and routing key: {RoutingKey}", 
                integrationEvent.EventType, integrationEvent.Id, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish integration event: {EventType} with ID: {EventId} and routing key: {RoutingKey}", 
                integrationEvent.EventType, integrationEvent.Id, routingKey);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> integrationEvents, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent
    {
        try
        {
            var eventsList = integrationEvents.ToList();
            _logger.LogInformation("Publishing batch of {Count} integration events of type: {EventType}", 
                eventsList.Count, typeof(T).Name);

            var tasks = eventsList.Select(evt => _publishEndpoint.Publish(evt, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published batch of {Count} integration events of type: {EventType}", 
                eventsList.Count, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of integration events of type: {EventType}", typeof(T).Name);
            throw;
        }
    }

    public async Task SchedulePublishAsync<T>(T integrationEvent, DateTime scheduleTime, CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent
    {
        try
        {
            _logger.LogInformation("Scheduling integration event: {EventType} with ID: {EventId} for {ScheduleTime}", 
                integrationEvent.EventType, integrationEvent.Id, scheduleTime);

            await _messageScheduler.SchedulePublish(scheduleTime, integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully scheduled integration event: {EventType} with ID: {EventId} for {ScheduleTime}", 
                integrationEvent.EventType, integrationEvent.Id, scheduleTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule integration event: {EventType} with ID: {EventId} for {ScheduleTime}", 
                integrationEvent.EventType, integrationEvent.Id, scheduleTime);
            throw;
        }
    }
}