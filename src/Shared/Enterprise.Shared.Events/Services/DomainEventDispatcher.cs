using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// Domain event dispatcher implementation
/// MediatR kullanarak domain event'leri dispatch eder
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">MediatR mediator instance</param>
    /// <param name="logger">Logger instance</param>
    public DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Domain event'i dispatch eder
    /// </summary>
    /// <param name="domainEvent">Dispatch edilecek domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        _logger.LogDebug("Dispatching domain event {EventType} with ID {EventId}", 
            domainEvent.EventType, domainEvent.EventId);

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "EventId", domainEvent.EventId },
                { "EventType", domainEvent.EventType },
                { "CorrelationId", domainEvent.CorrelationId }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _mediator.Publish(domainEvent, cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Successfully dispatched domain event {EventType} with ID {EventId} in {ElapsedMs}ms",
                domainEvent.EventType, domainEvent.EventId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event {EventType} with ID {EventId}",
                domainEvent.EventType, domainEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Birden fazla domain event'i dispatch eder
    /// </summary>
    /// <param name="domainEvents">Dispatch edilecek domain event'ler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventList = domainEvents.ToList();
        if (!eventList.Any())
        {
            _logger.LogDebug("No domain events to dispatch");
            return;
        }

        _logger.LogDebug("Dispatching {EventCount} domain events", eventList.Count);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Tüm event'leri parallel olarak dispatch et
            var dispatchTasks = eventList.Select(domainEvent => 
                DispatchAsync(domainEvent, cancellationToken));

            await Task.WhenAll(dispatchTasks);

            stopwatch.Stop();

            _logger.LogInformation("Successfully dispatched {EventCount} domain events in {ElapsedMs}ms",
                eventList.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching {EventCount} domain events", eventList.Count);
            throw;
        }
    }
}