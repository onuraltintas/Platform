using MassTransit;
using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Messaging.Events;

namespace EgitimPlatform.Shared.Messaging.Consumers;

public abstract class BaseConsumer<T> : IConsumer<T> where T : class, IIntegrationEvent
{
    protected readonly ILogger Logger;

    protected BaseConsumer(ILogger logger)
    {
        Logger = logger;
    }

    public async Task Consume(ConsumeContext<T> context)
    {
        var integrationEvent = context.Message;
        
        Logger.LogInformation("Processing integration event: {EventType} with ID: {EventId} from {Source}",
            integrationEvent.EventType, integrationEvent.Id, integrationEvent.Source);

        try
        {
            await ProcessAsync(integrationEvent, context.CancellationToken);
            
            Logger.LogInformation("Successfully processed integration event: {EventType} with ID: {EventId}",
                integrationEvent.EventType, integrationEvent.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process integration event: {EventType} with ID: {EventId}",
                integrationEvent.EventType, integrationEvent.Id);
            
            // Re-throw to let MassTransit handle retry/dead letter logic
            throw;
        }
    }

    protected abstract Task ProcessAsync(T integrationEvent, CancellationToken cancellationToken);
}

public abstract class BaseConsumerWithRetry<T> : BaseConsumer<T> where T : class, IIntegrationEvent
{
    private const int MaxRetryAttempts = 3;
    private readonly TimeSpan[] _retryDelays = {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15)
    };

    protected BaseConsumerWithRetry(ILogger logger) : base(logger)
    {
    }

    protected override async Task ProcessAsync(T integrationEvent, CancellationToken cancellationToken)
    {
        var attemptCount = 0;
        Exception? lastException = null;

        while (attemptCount < MaxRetryAttempts)
        {
            try
            {
                await HandleAsync(integrationEvent, cancellationToken);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (attemptCount < MaxRetryAttempts - 1)
            {
                lastException = ex;
                attemptCount++;
                
                Logger.LogWarning(ex, "Attempt {Attempt} failed for integration event: {EventType} with ID: {EventId}. Retrying in {Delay}ms",
                    attemptCount, integrationEvent.EventType, integrationEvent.Id, _retryDelays[attemptCount - 1].TotalMilliseconds);
                
                await Task.Delay(_retryDelays[attemptCount - 1], cancellationToken);
            }
            catch (Exception ex) when (attemptCount >= MaxRetryAttempts - 1)
            {
                lastException = ex;
                break;
            }
        }

        Logger.LogError(lastException, "All retry attempts failed for integration event: {EventType} with ID: {EventId}",
            integrationEvent.EventType, integrationEvent.Id);
        
        throw lastException ?? new InvalidOperationException("Unknown error occurred during message processing");
    }

    protected abstract Task HandleAsync(T integrationEvent, CancellationToken cancellationToken);
}