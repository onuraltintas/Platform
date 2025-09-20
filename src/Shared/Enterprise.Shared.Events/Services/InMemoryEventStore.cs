using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// In-memory event store implementation (test/development için)
/// Production'da SQL/NoSQL based implementation kullanılmalı
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly IEventSerializer _serializer;
    private readonly ILogger<InMemoryEventStore> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, StoredEvent>> _streams;
    private readonly object _lockObject = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serializer">Event serializer</param>
    /// <param name="logger">Logger instance</param>
    public InMemoryEventStore(IEventSerializer serializer, ILogger<InMemoryEventStore> logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _streams = new ConcurrentDictionary<string, ConcurrentDictionary<int, StoredEvent>>();
    }

    /// <summary>
    /// Event'leri stream'e append eder
    /// </summary>
    /// <typeparam name="T">Domain event türü</typeparam>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="events">Append edilecek event'ler</param>
    /// <param name="expectedVersion">Beklenen stream versiyonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AppendEventsAsync<T>(string streamId, IEnumerable<T> events, int expectedVersion, 
        CancellationToken cancellationToken = default) where T : DomainEvent
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return Task.CompletedTask;
        }

        lock (_lockObject)
        {
            var stream = _streams.GetOrAdd(streamId, _ => new ConcurrentDictionary<int, StoredEvent>());
            var currentVersion = GetCurrentVersion(stream);

            if (currentVersion != expectedVersion)
            {
                var message = $"Concurrency conflict for stream {streamId}. Expected version {expectedVersion} but current version is {currentVersion}";
                _logger.LogError(message);
                throw new ConcurrencyException(message);
            }

            var version = expectedVersion;
            foreach (var domainEvent in eventList)
            {
                version++;
                
                var storedEvent = new StoredEvent
                {
                    EventId = domainEvent.EventId,
                    StreamId = streamId,
                    Version = version,
                    EventType = domainEvent.EventType,
                    EventData = _serializer.Serialize(domainEvent),
                    OccurredAt = domainEvent.OccurredAt,
                    StoredAt = DateTime.UtcNow,
                    Metadata = CreateMetadata(domainEvent)
                };

                stream.TryAdd(version, storedEvent);

                _logger.LogDebug("Appended event {EventType} with ID {EventId} to stream {StreamId} at version {Version}",
                    domainEvent.EventType, domainEvent.EventId, streamId, version);
            }

            _logger.LogInformation("Appended {EventCount} events to stream {StreamId}. New version: {Version}",
                eventList.Count, streamId, version);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stream'den event'leri okur
    /// </summary>
    /// <typeparam name="T">Domain event türü</typeparam>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="fromVersion">Başlangıç versiyonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<IEnumerable<T>> GetEventsAsync<T>(string streamId, int fromVersion = 0, 
        CancellationToken cancellationToken = default) where T : DomainEvent
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            _logger.LogDebug("Stream {StreamId} not found", streamId);
            return Task.FromResult(Enumerable.Empty<T>());
        }

        var events = stream.Values
            .Where(e => e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .Select(e => _serializer.Deserialize<T>(e.EventData))
            .Where(e => e != null)
            .Cast<T>()
            .ToList();

        _logger.LogDebug("Retrieved {EventCount} events from stream {StreamId} starting from version {FromVersion}",
            events.Count, streamId, fromVersion);

        return Task.FromResult<IEnumerable<T>>(events);
    }

    /// <summary>
    /// Stream'i okur (tüm event'leri StoredEvent olarak)
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="fromVersion">Başlangıç versiyonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<EventStream> GetStreamAsync(string streamId, int fromVersion = 0, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            _logger.LogDebug("Stream {StreamId} not found", streamId);
            
            var emptyStream = new EventStream
            {
                StreamId = streamId,
                Version = 0,
                Events = Enumerable.Empty<StoredEvent>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            return Task.FromResult(emptyStream);
        }

        var events = stream.Values
            .Where(e => e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToList();

        var currentVersion = GetCurrentVersion(stream);
        var createdAt = stream.Values.Count > 0 ? stream.Values.Min(e => e.StoredAt) : DateTime.UtcNow;
        var updatedAt = stream.Values.Count > 0 ? stream.Values.Max(e => e.StoredAt) : DateTime.UtcNow;

        var eventStream = new EventStream
        {
            StreamId = streamId,
            Version = currentVersion,
            Events = events,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        _logger.LogDebug("Retrieved stream {StreamId} with {EventCount} events from version {FromVersion}. Current version: {CurrentVersion}",
            streamId, events.Count, fromVersion, currentVersion);

        return Task.FromResult(eventStream);
    }

    /// <summary>
    /// Stream'in mevcut versiyonunu getirir
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<int> GetStreamVersionAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult(0);
        }

        var version = GetCurrentVersion(stream);
        return Task.FromResult(version);
    }

    /// <summary>
    /// Stream var mı kontrol eder
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<bool> StreamExistsAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        var exists = _streams.ContainsKey(streamId);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Stream'i siler
    /// </summary>
    /// <param name="streamId">Stream ID'si</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task DeleteStreamAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        var removed = _streams.TryRemove(streamId, out _);
        
        if (removed)
        {
            _logger.LogInformation("Deleted stream {StreamId}", streamId);
        }
        else
        {
            _logger.LogDebug("Stream {StreamId} not found for deletion", streamId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stream'in mevcut versiyonunu bulur
    /// </summary>
    /// <param name="stream">Stream dictionary'si</param>
    /// <returns>Current version</returns>
    private static int GetCurrentVersion(ConcurrentDictionary<int, StoredEvent> stream)
    {
        return stream.Keys.Count > 0 ? stream.Keys.Max() : 0;
    }

    /// <summary>
    /// Event metadata'sını oluşturur
    /// </summary>
    /// <param name="domainEvent">Domain event</param>
    /// <returns>Metadata JSON string'i</returns>
    private string CreateMetadata(DomainEvent domainEvent)
    {
        var metadata = new Dictionary<string, object>
        {
            { "CorrelationId", domainEvent.CorrelationId },
            { "EventType", domainEvent.EventType },
            { "AssemblyName", domainEvent.GetType().Assembly.GetName().Name ?? "" },
            { "TypeName", domainEvent.GetType().FullName ?? "" }
        };

        return _serializer.Serialize(metadata);
    }
}