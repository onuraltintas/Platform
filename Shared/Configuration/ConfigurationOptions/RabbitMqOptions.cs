namespace EgitimPlatform.Shared.Configuration.ConfigurationOptions;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";
    
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int Heartbeat { get; set; } = 60;
    public int RequestedConnectionTimeout { get; set; } = 30000;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
}