namespace Enterprise.Shared.Events.Models;

/// <summary>
/// Event ayarları configuration modeli
/// </summary>
public class EventSettings
{
    /// <summary>
    /// Configuration section adı
    /// </summary>
    public const string SectionName = "EventSettings";

    /// <summary>
    /// RabbitMQ ayarları
    /// </summary>
    public RabbitMqSettings RabbitMQ { get; set; } = new();

    /// <summary>
    /// Event store ayarları
    /// </summary>
    public EventStoreSettings EventStore { get; set; } = new();

    /// <summary>
    /// Domain event ayarları
    /// </summary>
    public DomainEventSettings DomainEvents { get; set; } = new();

    /// <summary>
    /// Outbox pattern ayarları
    /// </summary>
    public OutboxSettings Outbox { get; set; } = new();
}

/// <summary>
/// RabbitMQ connection ayarları
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// RabbitMQ host adresi
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Şifre
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Connection retry sayısı
    /// </summary>
    public int ConnectionRetryCount { get; set; } = 5;

    /// <summary>
    /// Prefetch count
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Connection timeout (seconds)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// SSL/TLS kullanılsın mı?
    /// </summary>
    public bool UseSsl { get; set; } = false;
}

/// <summary>
/// Event store ayarları
/// </summary>
public class EventStoreSettings
{
    /// <summary>
    /// Event store connection string'i
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Stream prefix'i
    /// </summary>
    public string StreamPrefix { get; set; } = "enterprise-";

    /// <summary>
    /// Snapshot interval'ı (kaç event'te bir snapshot)
    /// </summary>
    public int SnapshotInterval { get; set; } = 100;

    /// <summary>
    /// Event retention süresi (gün)
    /// </summary>
    public int RetentionDays { get; set; } = 365;
}

/// <summary>
/// Domain event ayarları
/// </summary>
public class DomainEventSettings
{
    /// <summary>
    /// Outbox pattern aktif mi?
    /// </summary>
    public bool EnableOutbox { get; set; } = true;

    /// <summary>
    /// Transaction commit'ten sonra event'ler yayınlansın mı?
    /// </summary>
    public bool PublishAfterCommit { get; set; } = true;

    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Retry interval'ı (saniye)
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Outbox pattern ayarları
/// </summary>
public class OutboxSettings
{
    /// <summary>
    /// Outbox processor aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Processor interval'ı (saniye)
    /// </summary>
    public int ProcessorIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Batch size (kaç event birden işlensin)
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Failed event'leri ne kadar süre tutulsun (gün)
    /// </summary>
    public int FailedEventRetentionDays { get; set; } = 30;
}