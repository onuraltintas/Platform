using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace User.Infrastructure.Services;

/// <summary>
/// Service for event serialization and deserialization
/// </summary>
public class EventSerializationService : IEventSerializationService
{
    private readonly ILogger<EventSerializationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    public EventSerializationService(ILogger<EventSerializationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Serialize event to JSON
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="eventData">Event data</param>
    /// <returns>JSON string</returns>
    public string Serialize<T>(T eventData) where T : class
    {
        try
        {
            if (eventData == null)
                return string.Empty;

            return JsonSerializer.Serialize(eventData, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing event of type {EventType}", typeof(T).Name);
            throw new InvalidOperationException($"Failed to serialize event of type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Deserialize JSON to event
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="json">JSON string</param>
    /// <returns>Deserialized event</returns>
    public T? Deserialize<T>(string json) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing JSON to event of type {EventType}: {Json}", typeof(T).Name, json);
            throw new InvalidOperationException($"Failed to deserialize JSON to event of type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Try to serialize event safely
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="eventData">Event data</param>
    /// <param name="json">Output JSON</param>
    /// <returns>True if successful</returns>
    public bool TrySerialize<T>(T eventData, out string json) where T : class
    {
        json = string.Empty;
        
        try
        {
            if (eventData == null)
                return false;

            json = Serialize(eventData);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize event of type {EventType}", typeof(T).Name);
            return false;
        }
    }

    /// <summary>
    /// Try to deserialize JSON safely
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="json">JSON string</param>
    /// <param name="eventData">Output event data</param>
    /// <returns>True if successful</returns>
    public bool TryDeserialize<T>(string json, out T? eventData) where T : class
    {
        eventData = null;
        
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            eventData = Deserialize<T>(json);
            return eventData != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON to event of type {EventType}", typeof(T).Name);
            return false;
        }
    }
}

/// <summary>
/// Interface for event serialization service
/// </summary>
public interface IEventSerializationService
{
    /// <summary>
    /// Serialize event data to JSON
    /// </summary>
    string Serialize<T>(T eventData) where T : class;
    /// <summary>
    /// Deserialize JSON to event data
    /// </summary>
    T? Deserialize<T>(string json) where T : class;
    /// <summary>
    /// Try to serialize event data to JSON
    /// </summary>
    bool TrySerialize<T>(T eventData, out string json) where T : class;
    /// <summary>
    /// Try to deserialize JSON to event data
    /// </summary>
    bool TryDeserialize<T>(string json, out T? eventData) where T : class;
}