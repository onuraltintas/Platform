using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Events.Handlers;

/// <summary>
/// Integration event handler base sınıfı
/// MassTransit IConsumer ve kendi IIntegrationEventHandler'ımızı implement eder
/// </summary>
/// <typeparam name="T">Integration event türü</typeparam>
public abstract class IntegrationEventHandlerBase<T> : IConsumer<T>, IIntegrationEventHandler<T>
    where T : IntegrationEvent
{
    /// <summary>
    /// Logger instance
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    protected IntegrationEventHandlerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// MassTransit consumer method
    /// Bu method MassTransit tarafından çağrılır
    /// </summary>
    /// <param name="context">Consume context</param>
    public async Task Consume(ConsumeContext<T> context)
    {
        var @event = context.Message;
        
        Logger.LogInformation("Received integration event {EventType} with ID {EventId} and CorrelationId {CorrelationId}",
            @event.EventType, @event.EventId, @event.CorrelationId);

        try
        {
            using var scope = Logger.BeginScope(new Dictionary<string, object>
            {
                { "EventId", @event.EventId },
                { "EventType", @event.EventType },
                { "CorrelationId", @event.CorrelationId },
                { "ConversationId", context.ConversationId?.ToString() ?? "" }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            await HandleAsync(@event, context.CancellationToken);
            
            stopwatch.Stop();
            
            Logger.LogInformation("Successfully processed integration event {EventType} with ID {EventId} in {ElapsedMs}ms",
                @event.EventType, @event.EventId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing integration event {EventType} with ID {EventId}",
                @event.EventType, @event.EventId);
            
            // Re-throw to let MassTransit handle retries and error policies
            throw;
        }
    }

    /// <summary>
    /// Integration event'i handle eden abstract method
    /// Derived class'lar bu method'u implement etmelidir
    /// </summary>
    /// <param name="event">Handle edilecek event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}