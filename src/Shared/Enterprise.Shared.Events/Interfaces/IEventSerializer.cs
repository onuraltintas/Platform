using Enterprise.Shared.Events.Models;

namespace Enterprise.Shared.Events.Interfaces;

/// <summary>
/// Event serialization interface'i
/// Event'lerin JSON'a serialize/deserialize edilmesi için
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Event'i JSON string'e serialize eder
    /// </summary>
    /// <param name="event">Serialize edilecek event</param>
    /// <returns>JSON string</returns>
    string Serialize<T>(T @event) where T : class;

    /// <summary>
    /// JSON string'i event'e deserialize eder
    /// </summary>
    /// <typeparam name="T">Event türü</typeparam>
    /// <param name="data">JSON string</param>
    /// <returns>Deserialize edilmiş event</returns>
    T? Deserialize<T>(string data) where T : class;

    /// <summary>
    /// JSON string'i belirli türe deserialize eder
    /// </summary>
    /// <param name="data">JSON string</param>
    /// <param name="type">Event türü</param>
    /// <returns>Deserialize edilmiş event</returns>
    object? Deserialize(string data, Type type);

    /// <summary>
    /// JSON string'i event type'ına göre deserialize eder
    /// </summary>
    /// <param name="data">JSON string</param>
    /// <param name="eventType">Event type string</param>
    /// <returns>Deserialize edilmiş event</returns>
    object? Deserialize(string data, string eventType);
}