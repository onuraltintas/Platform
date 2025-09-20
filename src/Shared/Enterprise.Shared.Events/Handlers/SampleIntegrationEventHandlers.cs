using Enterprise.Shared.Events.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Events.Handlers;

/// <summary>
/// UserRegisteredIntegrationEvent handler'ı
/// Email servis tarafından consume edilir
/// </summary>
public class UserRegisteredEventHandler : IntegrationEventHandlerBase<UserRegisteredIntegrationEvent>
{
    public UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger) : base(logger)
    {
    }

    public override async Task HandleAsync(UserRegisteredIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Processing user registration for {Email}. Sending welcome email.", @event.Email);

        // Welcome email gönderme simülasyonu
        await SendWelcomeEmailAsync(@event.UserId, @event.Email, @event.FirstName, cancellationToken);

        // Analytics servise bildirim
        await NotifyAnalyticsServiceAsync(@event, cancellationToken);

        Logger.LogInformation("Welcome email sent and analytics notified for user {UserId}", @event.UserId);
    }

    private async Task SendWelcomeEmailAsync(Guid userId, string email, string firstName, CancellationToken cancellationToken)
    {
        // Email servisi entegrasyonu burada olacak
        Logger.LogDebug("Sending welcome email to {Email} for user {UserId}", email, userId);
        
        // Simulate email sending
        await Task.Delay(100, cancellationToken);
        
        Logger.LogDebug("Welcome email sent successfully to {Email}", email);
    }

    private async Task NotifyAnalyticsServiceAsync(UserRegisteredIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // Analytics servis entegrasyonu burada olacak
        Logger.LogDebug("Notifying analytics service about user registration {UserId}", @event.UserId);
        
        // Simulate analytics notification
        await Task.Delay(50, cancellationToken);
        
        Logger.LogDebug("Analytics service notified successfully for user {UserId}", @event.UserId);
    }
}

/// <summary>
/// EmailVerificationRequestedIntegrationEvent handler'ı  
/// Email servis tarafından consume edilir
/// </summary>
public class EmailVerificationRequestedEventHandler : IntegrationEventHandlerBase<EmailVerificationRequestedIntegrationEvent>
{
    public EmailVerificationRequestedEventHandler(ILogger<EmailVerificationRequestedEventHandler> logger) : base(logger)
    {
    }

    public override async Task HandleAsync(EmailVerificationRequestedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Processing email verification request for {Email} with token {Token}", 
            @event.Email, @event.VerificationToken[..8] + "...");

        if (@event.ExpiresAt < DateTime.UtcNow)
        {
            Logger.LogWarning("Email verification token for {Email} has expired", @event.Email);
            return;
        }

        // Email doğrulama mail'i gönder
        await SendVerificationEmailAsync(@event, cancellationToken);

        Logger.LogInformation("Email verification email sent to {Email}", @event.Email);
    }

    private async Task SendVerificationEmailAsync(EmailVerificationRequestedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        var verificationLink = $"https://app.enterprise.com/verify-email?token={@event.VerificationToken}";
        
        Logger.LogDebug("Sending verification email to {Email} with link {Link}", @event.Email, verificationLink);
        
        // Email gönderme simülasyonu
        await Task.Delay(150, cancellationToken);
        
        Logger.LogDebug("Verification email sent successfully to {Email}", @event.Email);
    }
}

/// <summary>
/// OrderCreatedIntegrationEvent handler'ı
/// Inventory ve Payment servisleri tarafından consume edilir
/// </summary>
public class OrderCreatedEventHandler : IntegrationEventHandlerBase<OrderCreatedIntegrationEvent>
{
    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : base(logger)
    {
    }

    public override async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Processing order creation for Order {OrderId} with total amount {Amount}",
            @event.OrderId, @event.TotalAmount);

        // Inventory reservation
        await ReserveInventoryAsync(@event, cancellationToken);

        // Payment processing initialization
        await InitializePaymentAsync(@event, cancellationToken);

        Logger.LogInformation("Order processing initiated for Order {OrderId}", @event.OrderId);
    }

    private async Task ReserveInventoryAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Reserving inventory for Order {OrderId} with {ItemCount} items",
            @event.OrderId, @event.Items.Count);

        foreach (var item in @event.Items)
        {
            Logger.LogDebug("Reserving {Quantity} units of product {ProductId}", 
                item.Quantity, item.ProductId);
            
            // Inventory reservation simülasyonu
            await Task.Delay(20, cancellationToken);
        }

        Logger.LogDebug("Inventory reserved successfully for Order {OrderId}", @event.OrderId);
    }

    private async Task InitializePaymentAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Initializing payment for Order {OrderId} with amount {Amount}",
            @event.OrderId, @event.TotalAmount);

        // Payment initialization simülasyonu
        await Task.Delay(100, cancellationToken);

        Logger.LogDebug("Payment initialized successfully for Order {OrderId}", @event.OrderId);
    }
}