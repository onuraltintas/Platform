namespace EgitimPlatform.Shared.Messaging.Configuration;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    public RabbitMqOptions RabbitMq { get; set; } = new();
    public bool EnableRetry { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public int RetryInterval { get; set; } = 5000; // milliseconds
    public bool EnableDeadLetter { get; set; } = true;
    public string DeadLetterExchange { get; set; } = "dead-letter";
    public int MessageTimeToLive { get; set; } = 3600000; // 1 hour in milliseconds
    public bool EnableMessagePersistence { get; set; } = true;
    public int PrefetchCount { get; set; } = 10;
    public int ConcurrentMessageLimit { get; set; } = 32;
}

public class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public bool UseSsl { get; set; } = false;
    public int Heartbeat { get; set; } = 60;
    public int RequestedConnectionTimeout { get; set; } = 30000;
    public string ClusterMembers { get; set; } = string.Empty; // Comma-separated list
    
    public string GetConnectionString()
    {
        var protocol = UseSsl ? "amqps" : "amqp";
        return $"{protocol}://{Username}:{Password}@{Host}:{Port}{VirtualHost}";
    }
}