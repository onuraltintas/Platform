using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Events.Handlers;

/// <summary>
/// UserCreatedEvent handler'ı
/// </summary>
public class UserCreatedEventHandler : IDomainEventHandler<UserCreatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(
        IEventBus eventBus, 
        IOutboxService outboxService,
        ILogger<UserCreatedEventHandler> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling domain event: {EventType} for User: {UserId}",
            notification.EventType, notification.UserId);

        try
        {
            // Domain içi işlemler (örnek)
            await ProcessUserCreatedAsync(notification, cancellationToken);

            // Integration event oluştur ve outbox'a ekle
            var integrationEvent = new UserRegisteredIntegrationEvent
            {
                UserId = notification.UserId,
                Email = notification.Email,
                FirstName = notification.FirstName,
                LastName = notification.LastName,
                RegisteredAt = notification.OccurredAt,
                CorrelationId = notification.CorrelationId,
                Source = "UserService"
            };

            // Outbox pattern ile güvenli integration event yayınlama
            await _outboxService.AddEventAsync(integrationEvent, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully handled UserCreatedEvent for user {UserId}. Integration event added to outbox.",
                notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserCreatedEvent for user {UserId}", notification.UserId);
            throw;
        }
    }

    private async Task ProcessUserCreatedAsync(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Domain içi işlemler burada yapılır
        // Örnek: Welcome email hazırlama, profil initialization, vb.
        
        _logger.LogDebug("Processing user created business logic for user {UserId}", notification.UserId);
        
        // Simulate async work
        await Task.Delay(10, cancellationToken);
        
        _logger.LogDebug("User created business logic processed for user {UserId}", notification.UserId);
    }
}

/// <summary>
/// UserEmailChangedEvent handler'ı
/// </summary>
public class UserEmailChangedEventHandler : IDomainEventHandler<UserEmailChangedEvent>
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<UserEmailChangedEventHandler> _logger;

    public UserEmailChangedEventHandler(
        IOutboxService outboxService,
        ILogger<UserEmailChangedEventHandler> logger)
    {
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UserEmailChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling domain event: {EventType} for User: {UserId}. Email changed from {OldEmail} to {NewEmail}",
            notification.EventType, notification.UserId, notification.OldEmail, notification.NewEmail);

        try
        {
            // Email değişikliği için email verification isteği oluştur
            var verificationEvent = new EmailVerificationRequestedIntegrationEvent
            {
                UserId = notification.UserId,
                Email = notification.NewEmail,
                VerificationToken = GenerateVerificationToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CorrelationId = notification.CorrelationId,
                Source = "UserService"
            };

            await _outboxService.AddEventAsync(verificationEvent, cancellationToken: cancellationToken);

            _logger.LogInformation("Email verification event added to outbox for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserEmailChangedEvent for user {UserId}", notification.UserId);
            throw;
        }
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}