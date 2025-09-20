using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace User.Infrastructure.Services;

/// <summary>
/// Service for handling event processing errors
/// </summary>
public class EventErrorHandlingService : IEventErrorHandlingService
{
    private readonly ILogger<EventErrorHandlingService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public EventErrorHandlingService(ILogger<EventErrorHandlingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle event processing error
    /// </summary>
    /// <param name="context">Consume context</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="eventType">Event type name</param>
    /// <returns>Error handling result</returns>
    public async Task<EventErrorHandlingResult> HandleEventErrorAsync<T>(
        ConsumeContext<T> context, 
        Exception exception, 
        string eventType) where T : class
    {
        try
        {
            var correlationId = context.CorrelationId?.ToString() ?? "Unknown";
            var messageId = context.MessageId?.ToString() ?? "Unknown";

            _logger.LogError(exception, 
                "Event processing error: EventType={EventType}, MessageId={MessageId}, CorrelationId={CorrelationId}, Error={Error}",
                eventType, messageId, correlationId, exception.Message);

            // Determine if error is retryable
            var isRetryable = IsRetryableError(exception);
            var shouldDeadLetter = ShouldSendToDeadLetter(exception, context.GetRetryAttempt());

            // Log event details for debugging
            await LogEventDetailsAsync(context, exception, eventType);

            if (shouldDeadLetter)
            {
                await SendToDeadLetterAsync(context, exception, eventType);
                return EventErrorHandlingResult.DeadLetter;
            }

            if (isRetryable)
            {
                await ScheduleRetryAsync(context, exception, eventType);
                return EventErrorHandlingResult.Retry;
            }

            return EventErrorHandlingResult.Ignore;
        }
        catch (Exception handlingException)
        {
            _logger.LogCritical(handlingException, "Critical error in event error handling for EventType: {EventType}", eventType);
            return EventErrorHandlingResult.Ignore; // Fallback to prevent infinite loops
        }
    }

    /// <summary>
    /// Determine if error is retryable
    /// </summary>
    /// <param name="exception">Exception</param>
    /// <returns>True if retryable</returns>
    private static bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            // Transient network errors
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            
            // Database connection issues (provider-agnostic)
            System.Data.Common.DbException dbEx when IsTransientDbError(dbEx) => true,
            
            // Temporary service unavailable
            InvalidOperationException opEx when opEx.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) => true,
            
            // Business rule violations - not retryable
            ArgumentNullException => false,
            InvalidDataException => false,
            
            // Unknown errors - be conservative and retry
            _ => true
        };
    }

    /// <summary>
    /// Check if SQL error is transient
    /// </summary>
    /// <param name="sqlException">SQL exception</param>
    /// <returns>True if transient</returns>
    private static bool IsTransientDbError(System.Data.Common.DbException dbException)
    {
        var message = dbException.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("timeout") || message.Contains("temporarily unavailable") || message.Contains("deadlock");
    }

    /// <summary>
    /// Determine if message should be sent to dead letter queue
    /// </summary>
    /// <param name="exception">Exception</param>
    /// <param name="retryAttempt">Current retry attempt</param>
    /// <returns>True if should dead letter</returns>
    private static bool ShouldSendToDeadLetter(Exception exception, int retryAttempt)
    {
        // After max retries, send to dead letter
        if (retryAttempt >= 5)
            return true;

        // Certain errors should go straight to dead letter
        return exception switch
        {
            ArgumentNullException => true,
            JsonException => true,
            FormatException => true,
            _ => false
        };
    }

    /// <summary>
    /// Log detailed event information for debugging
    /// </summary>
    /// <param name="context">Consume context</param>
    /// <param name="exception">Exception</param>
    /// <param name="eventType">Event type</param>
    private async Task LogEventDetailsAsync<T>(ConsumeContext<T> context, Exception exception, string eventType) where T : class
    {
        try
        {
            var eventDetails = new
            {
                EventType = eventType,
                MessageId = context.MessageId?.ToString(),
                CorrelationId = context.CorrelationId?.ToString(),
                RetryAttempt = context.GetRetryAttempt(),
                Host = context.Host?.ToString(),
                SentTime = context.SentTime,
                Headers = context.Headers?.ToDictionary(h => h.Key, h => h.Value?.ToString()),
                Exception = new
                {
                    Type = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                }
            };

            _logger.LogDebug("Event processing error details: {EventDetails}", 
                JsonSerializer.Serialize(eventDetails, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception logException)
        {
            _logger.LogWarning(logException, "Failed to log event details for {EventType}", eventType);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Send message to dead letter queue
    /// </summary>
    /// <param name="context">Consume context</param>
    /// <param name="exception">Exception</param>
    /// <param name="eventType">Event type</param>
    private async Task SendToDeadLetterAsync<T>(ConsumeContext<T> context, Exception exception, string eventType) where T : class
    {
        try
        {
            _logger.LogWarning("Sending message to dead letter queue: EventType={EventType}, MessageId={MessageId}, Error={Error}",
                eventType, context.MessageId, exception.Message);

            // MassTransit will automatically handle dead lettering based on configuration
            // We just need to not acknowledge the message
        }
        catch (Exception deadLetterException)
        {
            _logger.LogError(deadLetterException, "Failed to send message to dead letter queue for EventType: {EventType}", eventType);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Schedule message retry
    /// </summary>
    /// <param name="context">Consume context</param>
    /// <param name="exception">Exception</param>
    /// <param name="eventType">Event type</param>
    private async Task ScheduleRetryAsync<T>(ConsumeContext<T> context, Exception exception, string eventType) where T : class
    {
        try
        {
            var retryAttempt = context.GetRetryAttempt();
            var delay = CalculateRetryDelay(retryAttempt);

            _logger.LogInformation("Scheduling retry for message: EventType={EventType}, MessageId={MessageId}, RetryAttempt={RetryAttempt}, Delay={Delay}ms",
                eventType, context.MessageId, retryAttempt, delay.TotalMilliseconds);
        }
        catch (Exception retryException)
        {
            _logger.LogError(retryException, "Failed to schedule retry for EventType: {EventType}", eventType);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Calculate retry delay with exponential backoff
    /// </summary>
    /// <param name="retryAttempt">Retry attempt number</param>
    /// <returns>Delay timespan</returns>
    private static TimeSpan CalculateRetryDelay(int retryAttempt)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        var maxDelay = TimeSpan.FromMinutes(5);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

        return baseDelay > maxDelay ? maxDelay + jitter : baseDelay + jitter;
    }
}

/// <summary>
/// Interface for event error handling service
/// </summary>
public interface IEventErrorHandlingService
{
    /// <summary>
    /// Handle event processing error
    /// </summary>
    Task<EventErrorHandlingResult> HandleEventErrorAsync<T>(ConsumeContext<T> context, Exception exception, string eventType) where T : class;
}

/// <summary>
/// Event error handling result
/// </summary>
public enum EventErrorHandlingResult
{
    /// <summary>
    /// Retry the message
    /// </summary>
    Retry,

    /// <summary>
    /// Send to dead letter queue
    /// </summary>
    DeadLetter,

    /// <summary>
    /// Ignore the error and continue
    /// </summary>
    Ignore
}