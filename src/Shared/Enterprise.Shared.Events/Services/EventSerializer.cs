using Enterprise.Shared.Events.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// JSON based event serializer implementation
/// </summary>
public class EventSerializer : IEventSerializer
{
    private readonly ILogger<EventSerializer> _logger;
    private readonly JsonSerializerOptions _options;
    private readonly ConcurrentDictionary<string, Type> _typeCache;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public EventSerializer(ILogger<EventSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _typeCache = new ConcurrentDictionary<string, Type>();
        
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = false
        };
    }

    /// <summary>
    /// Event'i JSON string'e serialize eder
    /// </summary>
    /// <param name="event">Serialize edilecek event</param>
    /// <returns>JSON string</returns>
    public string Serialize<T>(T @event) where T : class
    {
        try
        {
            return JsonSerializer.Serialize(@event, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize event of type {EventType}", typeof(T).Name);
            throw new InvalidOperationException($"Failed to serialize event of type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// JSON string'i event'e deserialize eder
    /// </summary>
    /// <typeparam name="T">Event türü</typeparam>
    /// <param name="data">JSON string</param>
    /// <returns>Deserialize edilmiş event</returns>
    public T? Deserialize<T>(string data) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(data, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize data to type {EventType}. Data: {Data}", 
                typeof(T).Name, data);
            throw new InvalidOperationException($"Failed to deserialize data to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// JSON string'i belirli türe deserialize eder
    /// </summary>
    /// <param name="data">JSON string</param>
    /// <param name="type">Event türü</param>
    /// <returns>Deserialize edilmiş event</returns>
    public object? Deserialize(string data, Type type)
    {
        try
        {
            return JsonSerializer.Deserialize(data, type, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize data to type {EventType}. Data: {Data}", 
                type.Name, data);
            throw new InvalidOperationException($"Failed to deserialize data to type {type.Name}", ex);
        }
    }

    /// <summary>
    /// JSON string'i event type'ına göre deserialize eder
    /// </summary>
    /// <param name="data">JSON string</param>
    /// <param name="eventType">Event type string</param>
    /// <returns>Deserialize edilmiş event</returns>
    public object? Deserialize(string data, string eventType)
    {
        var type = GetEventType(eventType);
        if (type == null)
        {
            throw new InvalidOperationException($"Cannot find event type: {eventType}");
        }

        return Deserialize(data, type);
    }

    /// <summary>
    /// Event type string'ine göre Type'ı bulur
    /// </summary>
    /// <param name="eventType">Event type string</param>
    /// <returns>Event Type'ı</returns>
    private Type? GetEventType(string eventType)
    {
        return _typeCache.GetOrAdd(eventType, typeName =>
        {
            try
            {
                // Önce mevcut assembly'de ara
                var currentAssembly = Assembly.GetExecutingAssembly();
                var type = currentAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

                if (type != null)
                {
                    return type;
                }

                // Tüm loaded assembly'lerde ara
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        type = assembly.GetTypes()
                            .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

                        if (type != null)
                        {
                            return type;
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Assembly yüklenirken hata olursa devam et
                        _logger.LogWarning(ex, "Error loading types from assembly {AssemblyName}", 
                            assembly.FullName);
                    }
                }

                _logger.LogWarning("Event type {EventType} not found in any loaded assemblies", typeName);
                return null!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving event type {EventType}", typeName);
                return null!;
            }
        });
    }
}