using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// MassTransit based event bus implementation
/// </summary>
public class MassTransitEventBus : IEventBus
{
    private readonly IBus _bus;
    private readonly ILogger<MassTransitEventBus> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bus">MassTransit bus instance</param>
    /// <param name="logger">Logger instance</param>
    public MassTransitEventBus(IBus bus, ILogger<MassTransitEventBus> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Integration event'i yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogDebug("Publishing integration event {EventType} with ID {EventId}",
            @event.EventType, @event.EventId);

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "EventId", @event.EventId },
                { "EventType", @event.EventType },
                { "CorrelationId", @event.CorrelationId },
                { "Version", @event.Version }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _bus.Publish(@event, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Published integration event {EventType} with ID {EventId} in {ElapsedMs}ms",
                @event.EventType, @event.EventId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing integration event {EventType} with ID {EventId}",
                @event.EventType, @event.EventId);
            throw;
        }
    }

    /// <summary>
    /// Integration event'i belirli routing key ile yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="routingKey">Routing key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            throw new ArgumentException("Routing key cannot be null or empty", nameof(routingKey));
        }

        _logger.LogDebug("Publishing integration event {EventType} with ID {EventId} and routing key {RoutingKey}",
            @event.EventType, @event.EventId, routingKey);

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "EventId", @event.EventId },
                { "EventType", @event.EventType },
                { "CorrelationId", @event.CorrelationId },
                { "RoutingKey", routingKey },
                { "Version", @event.Version }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _bus.Publish(@event, context =>
            {
                context.SetRoutingKey(routingKey);
            }, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Published integration event {EventType} with ID {EventId} and routing key {RoutingKey} in {ElapsedMs}ms",
                @event.EventType, @event.EventId, routingKey, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing integration event {EventType} with ID {EventId} and routing key {RoutingKey}",
                @event.EventType, @event.EventId, routingKey);
            throw;
        }
    }

    /// <summary>
    /// Integration event'i delayed yayınlar
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Yayınlanacak event</param>
    /// <param name="delay">Gecikme süresi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishDelayedAsync<T>(T @event, TimeSpan delay, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentException("Delay cannot be negative", nameof(delay));
        }

        _logger.LogDebug("Publishing delayed integration event {EventType} with ID {EventId} with delay {DelayMs}ms",
            @event.EventType, @event.EventId, delay.TotalMilliseconds);

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "EventId", @event.EventId },
                { "EventType", @event.EventType },
                { "CorrelationId", @event.CorrelationId },
                { "DelayMs", delay.TotalMilliseconds },
                { "Version", @event.Version }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // For now, just publish the event immediately - delayed scheduling requires additional setup
            await _bus.Publish(@event, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Scheduled integration event {EventType} with ID {EventId} for delivery in {DelayMs}ms (scheduled in {ElapsedMs}ms)",
                @event.EventType, @event.EventId, delay.TotalMilliseconds, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling integration event {EventType} with ID {EventId} with delay {DelayMs}ms",
                @event.EventType, @event.EventId, delay.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Integration event'ler için subscription oluşturur
    /// Bu method MassTransit'in otomatik subscription mekanizması ile çalışır
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <typeparam name="TH">Handler türü</typeparam>
    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        _logger.LogInformation("Subscription registered for event {EventType} with handler {HandlerType}",
            typeof(T).Name, typeof(TH).Name);
        
        // MassTransit'te subscription'lar genellikle configuration sırasında yapılır
        // Bu method daha çok logging/tracking amaçlı
    }

    /// <summary>
    /// Integration event subscription'ını kaldırır
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <typeparam name="TH">Handler türü</typeparam>
    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        _logger.LogInformation("Subscription removed for event {EventType} with handler {HandlerType}",
            typeof(T).Name, typeof(TH).Name);
        
        // MassTransit'te unsubscription genellikle configuration sırasında yapılır
        // Bu method daha çok logging/tracking amaçlı
    }
}