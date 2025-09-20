using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// In-memory outbox service implementation (test/development için)
/// Production'da database-backed implementation kullanılmalı
/// </summary>
public class InMemoryOutboxService : IOutboxService
{
    private readonly IEventBus _eventBus;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<InMemoryOutboxService> _logger;
    private readonly ConcurrentDictionary<Guid, OutboxEvent> _outboxEvents;
    private readonly object _lockObject = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="eventBus">Event bus instance</param>
    /// <param name="serializer">Event serializer</param>
    /// <param name="logger">Logger instance</param>
    public InMemoryOutboxService(IEventBus eventBus, IEventSerializer serializer, ILogger<InMemoryOutboxService> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outboxEvents = new ConcurrentDictionary<Guid, OutboxEvent>();
    }

    /// <summary>
    /// Event'i outbox'a ekler
    /// </summary>
    /// <typeparam name="T">Integration event türü</typeparam>
    /// <param name="event">Outbox'a eklenecek event</param>
    /// <param name="routingKey">Routing key (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddEventAsync<T>(T @event, string? routingKey = null, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = @event.EventType,
            EventData = _serializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            Published = false,
            RetryCount = 0,
            CorrelationId = @event.CorrelationId,
            RoutingKey = routingKey,
            Metadata = CreateMetadata(@event)
        };

        _outboxEvents.TryAdd(outboxEvent.Id, outboxEvent);

        _logger.LogDebug("Added integration event {EventType} with ID {EventId} to outbox",
            @event.EventType, @event.EventId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Birden fazla event'i outbox'a ekler
    /// </summary>
    /// <param name="events">Outbox'a eklenecek event'ler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddEventsAsync(IEnumerable<IntegrationEvent> events, CancellationToken cancellationToken = default)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return Task.CompletedTask;
        }

        foreach (var integrationEvent in eventList)
        {
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = integrationEvent.EventType,
                EventData = _serializer.Serialize(integrationEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false,
                RetryCount = 0,
                CorrelationId = integrationEvent.CorrelationId,
                Metadata = CreateMetadata(integrationEvent)
            };

            _outboxEvents.TryAdd(outboxEvent.Id, outboxEvent);
        }

        _logger.LogDebug("Added {EventCount} integration events to outbox", eventList.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Yayınlanmamış event'leri işler
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ProcessUnpublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        var unpublishedEvents = _outboxEvents.Values
            .Where(e => !e.Published && e.RetryCount < 3)
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToList();

        if (!unpublishedEvents.Any())
        {
            _logger.LogDebug("No unpublished events found in outbox");
            return;
        }

        _logger.LogDebug("Processing {EventCount} unpublished events from outbox", unpublishedEvents.Count);

        var processedCount = 0;
        var failedCount = 0;

        foreach (var outboxEvent in unpublishedEvents)
        {
            try
            {
                var integrationEvent = _serializer.Deserialize(outboxEvent.EventData, outboxEvent.EventType);
                if (integrationEvent is IntegrationEvent evt)
                {
                    if (!string.IsNullOrWhiteSpace(outboxEvent.RoutingKey))
                    {
                        await _eventBus.PublishAsync(evt, outboxEvent.RoutingKey, cancellationToken);
                    }
                    else
                    {
                        await _eventBus.PublishAsync(evt, cancellationToken);
                    }

                    outboxEvent.Published = true;
                    outboxEvent.PublishedAt = DateTime.UtcNow;
                    processedCount++;

                    _logger.LogDebug("Published outbox event {EventType} with ID {OutboxId}",
                        outboxEvent.EventType, outboxEvent.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize outbox event {EventType} with ID {OutboxId}",
                        outboxEvent.EventType, outboxEvent.Id);
                    outboxEvent.RetryCount++;
                    outboxEvent.LastError = "Failed to deserialize event";
                    outboxEvent.LastRetryAt = DateTime.UtcNow;
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing outbox event {EventType} with ID {OutboxId}",
                    outboxEvent.EventType, outboxEvent.Id);

                outboxEvent.RetryCount++;
                outboxEvent.LastError = ex.Message;
                outboxEvent.LastRetryAt = DateTime.UtcNow;
                failedCount++;
            }
        }

        _logger.LogInformation("Processed {ProcessedCount} outbox events successfully, {FailedCount} failed",
            processedCount, failedCount);
    }

    /// <summary>
    /// Başarısız event'leri yeniden işler
    /// </summary>
    /// <param name="maxRetryCount">Maksimum retry sayısı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RetryFailedEventsAsync(int maxRetryCount = 3, CancellationToken cancellationToken = default)
    {
        var failedEvents = _outboxEvents.Values
            .Where(e => !e.Published && e.RetryCount > 0 && e.RetryCount < maxRetryCount)
            .Where(e => !e.LastRetryAt.HasValue || e.LastRetryAt.Value.AddMinutes(5) < DateTime.UtcNow) // 5 dakika bekleme
            .OrderBy(e => e.LastRetryAt ?? e.CreatedAt)
            .Take(50)
            .ToList();

        if (!failedEvents.Any())
        {
            _logger.LogDebug("No failed events found for retry in outbox");
            return;
        }

        _logger.LogDebug("Retrying {EventCount} failed events from outbox", failedEvents.Count);

        foreach (var outboxEvent in failedEvents)
        {
            try
            {
                var integrationEvent = _serializer.Deserialize(outboxEvent.EventData, outboxEvent.EventType);
                if (integrationEvent is IntegrationEvent evt)
                {
                    if (!string.IsNullOrWhiteSpace(outboxEvent.RoutingKey))
                    {
                        await _eventBus.PublishAsync(evt, outboxEvent.RoutingKey, cancellationToken);
                    }
                    else
                    {
                        await _eventBus.PublishAsync(evt, cancellationToken);
                    }

                    outboxEvent.Published = true;
                    outboxEvent.PublishedAt = DateTime.UtcNow;

                    _logger.LogInformation("Successfully retried outbox event {EventType} with ID {OutboxId} after {RetryCount} retries",
                        outboxEvent.EventType, outboxEvent.Id, outboxEvent.RetryCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry outbox event {EventType} with ID {OutboxId}. Retry count: {RetryCount}",
                    outboxEvent.EventType, outboxEvent.Id, outboxEvent.RetryCount);

                outboxEvent.RetryCount++;
                outboxEvent.LastError = ex.Message;
                outboxEvent.LastRetryAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Eski outbox event'leri temizler
    /// </summary>
    /// <param name="olderThanDays">Kaç günden eski event'ler temizlensin</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task CleanupOldEventsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var eventsToRemove = _outboxEvents.Values
            .Where(e => e.Published && e.PublishedAt.HasValue && e.PublishedAt.Value < cutoffDate)
            .ToList();

        var removedCount = 0;
        foreach (var outboxEvent in eventsToRemove)
        {
            if (_outboxEvents.TryRemove(outboxEvent.Id, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {RemovedCount} old published outbox events older than {Days} days",
                removedCount, olderThanDays);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Outbox istatistiklerini getirir
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allEvents = _outboxEvents.Values.ToList();
        
        var statistics = new OutboxStatistics
        {
            TotalEvents = allEvents.Count,
            PublishedEvents = allEvents.Count(e => e.Published),
            UnpublishedEvents = allEvents.Count(e => !e.Published),
            FailedEvents = allEvents.Count(e => !e.Published && e.RetryCount > 0),
            LastProcessedAt = allEvents.Where(e => e.Published).Max(e => e.PublishedAt),
            OldestUnpublishedAt = allEvents.Where(e => !e.Published).Min(e => e.CreatedAt)
        };

        return Task.FromResult(statistics);
    }

    /// <summary>
    /// Event metadata'sını oluşturur
    /// </summary>
    /// <param name="integrationEvent">Integration event</param>
    /// <returns>Metadata JSON string'i</returns>
    private string CreateMetadata(IntegrationEvent integrationEvent)
    {
        var metadata = new Dictionary<string, object>
        {
            { "EventId", integrationEvent.EventId },
            { "CorrelationId", integrationEvent.CorrelationId },
            { "Source", integrationEvent.Source },
            { "Version", integrationEvent.Version },
            { "OccurredAt", integrationEvent.OccurredAt },
            { "AssemblyName", integrationEvent.GetType().Assembly.GetName().Name ?? "" },
            { "TypeName", integrationEvent.GetType().FullName ?? "" }
        };

        return _serializer.Serialize(metadata);
    }
}