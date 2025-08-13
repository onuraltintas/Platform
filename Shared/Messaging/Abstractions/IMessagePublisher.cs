namespace EgitimPlatform.Shared.Messaging.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(T message, string exchange, string routingKey, CancellationToken cancellationToken = default) where T : class;
    Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class;
    Task SchedulePublishAsync<T>(T message, DateTime scheduleTime, CancellationToken cancellationToken = default) where T : class;
}

public interface IMessageConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class;
    void Subscribe<T>(string queueName, Func<T, CancellationToken, Task> handler) where T : class;
}

public interface IMessageHandler<in T> where T : class
{
    Task HandleAsync(T message, CancellationToken cancellationToken = default);
}

public interface IMessageSerializer
{
    string Serialize<T>(T obj) where T : class;
    T? Deserialize<T>(string json) where T : class;
    object? Deserialize(string json, Type type);
    byte[] SerializeToBytes<T>(T obj) where T : class;
    T? DeserializeFromBytes<T>(byte[] bytes) where T : class;
}