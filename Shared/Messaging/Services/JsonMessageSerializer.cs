using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using EgitimPlatform.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace EgitimPlatform.Shared.Messaging.Services;

public class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerSettings _settings;
    private readonly ILogger<JsonMessageSerializer> _logger;

    public JsonMessageSerializer(ILogger<JsonMessageSerializer> logger)
    {
        _logger = logger;
        _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None
        };
    }

    public string Serialize<T>(T obj) where T : class
    {
        try
        {
            if (obj == null)
            {
                _logger.LogWarning("Attempting to serialize null object of type {Type}", typeof(T).Name);
                return string.Empty;
            }

            var json = JsonConvert.SerializeObject(obj, _settings);
            _logger.LogDebug("Serialized object of type {Type} to JSON: {Json}", typeof(T).Name, json);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize object of type {Type}", typeof(T).Name);
            throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name}", ex);
        }
    }

    public T? Deserialize<T>(string json) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Attempting to deserialize empty or null JSON string to type {Type}", typeof(T).Name);
                return null;
            }

            var obj = JsonConvert.DeserializeObject<T>(json, _settings);
            _logger.LogDebug("Deserialized JSON to object of type {Type}: {Json}", typeof(T).Name, json);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to object of type {Type}. JSON: {Json}", typeof(T).Name, json);
            throw new InvalidOperationException($"Failed to deserialize JSON to object of type {typeof(T).Name}", ex);
        }
    }

    public object? Deserialize(string json, Type type)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Attempting to deserialize empty or null JSON string to type {Type}", type.Name);
                return null;
            }

            var obj = JsonConvert.DeserializeObject(json, type, _settings);
            _logger.LogDebug("Deserialized JSON to object of type {Type}: {Json}", type.Name, json);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to object of type {Type}. JSON: {Json}", type.Name, json);
            throw new InvalidOperationException($"Failed to deserialize JSON to object of type {type.Name}", ex);
        }
    }

    public byte[] SerializeToBytes<T>(T obj) where T : class
    {
        try
        {
            var json = Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            _logger.LogDebug("Serialized object of type {Type} to {ByteCount} bytes", typeof(T).Name, bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize object of type {Type} to bytes", typeof(T).Name);
            throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name} to bytes", ex);
        }
    }

    public T? DeserializeFromBytes<T>(byte[] bytes) where T : class
    {
        try
        {
            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogWarning("Attempting to deserialize empty or null byte array to type {Type}", typeof(T).Name);
                return null;
            }

            var json = Encoding.UTF8.GetString(bytes);
            var obj = Deserialize<T>(json);
            _logger.LogDebug("Deserialized {ByteCount} bytes to object of type {Type}", bytes.Length, typeof(T).Name);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize byte array to object of type {Type}", typeof(T).Name);
            throw new InvalidOperationException($"Failed to deserialize byte array to object of type {typeof(T).Name}", ex);
        }
    }
}