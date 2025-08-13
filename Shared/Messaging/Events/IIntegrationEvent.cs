namespace EgitimPlatform.Shared.Messaging.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
    string Source { get; }
    string Version { get; }
    Dictionary<string, object> Metadata { get; }
}

public abstract class IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        EventType = GetType().Name;
        Source = GetType().Assembly.GetName().Name ?? "Unknown";
        Version = "1.0";
        Metadata = new Dictionary<string, object>();
    }

    public Guid Id { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType { get; private set; }
    public string Source { get; private set; }
    public string Version { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}