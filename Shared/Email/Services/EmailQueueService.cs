using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using EgitimPlatform.Shared.Email.Configuration;
using EgitimPlatform.Shared.Email.Models;
using EgitimPlatform.Shared.Email.Services;

namespace EgitimPlatform.Shared.Email.Services;

public class EmailQueueService : IEmailQueueService, IDisposable
{
    private readonly EmailOptions _options;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailQueueService> _logger;
    private readonly ConcurrentQueue<QueuedEmailMessage> _emailQueue;
    private readonly ConcurrentDictionary<string, EmailDeliveryResult> _deliveryResults;
    private readonly Timer _processTimer;
    private readonly SemaphoreSlim _processingSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public EmailQueueService(
        IOptions<EmailOptions> options,
        IEmailService emailService,
        ILogger<EmailQueueService> logger)
    {
        _options = options.Value;
        _emailService = emailService;
        _logger = logger;
        _emailQueue = new ConcurrentQueue<QueuedEmailMessage>();
        _deliveryResults = new ConcurrentDictionary<string, EmailDeliveryResult>();
        _processingSemaphore = new SemaphoreSlim(1, 1);
        _cancellationTokenSource = new CancellationTokenSource();

        // Start processing timer
        var processingInterval = _options.Queue.ProcessingIntervalSeconds * 1000;
        _processTimer = new Timer(ProcessQueueCallback, null, processingInterval, processingInterval);

        _logger.LogInformation("Email queue service started with processing interval: {Interval}s", 
            _options.Queue.ProcessingIntervalSeconds);
    }

    public async Task<string> QueueEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var queuedMessage = new QueuedEmailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Message = message,
                QueuedAt = DateTime.UtcNow,
                Priority = message.Priority,
                MaxRetryAttempts = message.DeliveryOptions?.MaxRetryAttempts ?? _options.Queue.MaxRetryAttempts,
                RetryDelay = message.DeliveryOptions?.RetryDelay ?? TimeSpan.FromMinutes(_options.Queue.RetryDelayMinutes),
                ScheduledFor = message.ScheduledAt ?? DateTime.UtcNow
            };

            _emailQueue.Enqueue(queuedMessage);
            
            // Store initial delivery result
            var deliveryResult = EmailDeliveryResult.Queued(message.Id);
            _deliveryResults.TryAdd(message.Id, deliveryResult);

            _logger.LogDebug("Email queued with ID: {MessageId}, Queue ID: {QueueId}", 
                message.Id, queuedMessage.Id);

            return queuedMessage.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email with ID: {MessageId}", message.Id);
            throw;
        }
    }

    public async Task<BulkEmailResult> QueueBulkEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var result = new BulkEmailResult
        {
            TotalEmails = messageList.Count,
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Queuing {Count} emails for bulk processing", messageList.Count);

        try
        {
            var successCount = 0;
            var failCount = 0;
            
            var tasks = messageList.Select(async message =>
            {
                try
                {
                    var queueId = await QueueEmailAsync(message, cancellationToken);
                    Interlocked.Increment(ref successCount);
                    return EmailDeliveryResult.Queued(message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue email with ID: {MessageId}", message.Id);
                    Interlocked.Increment(ref failCount);
                    return EmailDeliveryResult.Failure(message.Id, ex.Message, ex);
                }
            });

            var queueResults = await Task.WhenAll(tasks);
            result.Results.AddRange(queueResults);
            
            result.SuccessfulDeliveries = successCount;
            result.FailedDeliveries = failCount;
            result.CompletedAt = DateTime.UtcNow;
            result.TotalProcessingTime = result.CompletedAt.Value - result.StartedAt;

            _logger.LogInformation("Completed bulk email queuing. Queued: {Success}, Failed: {Failed}", 
                result.SuccessfulDeliveries, result.FailedDeliveries);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk email queuing");
            throw;
        }
    }

    public async Task<EmailDeliveryResult> GetDeliveryStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (_deliveryResults.TryGetValue(messageId, out var result))
        {
            return result;
        }

        return EmailDeliveryResult.Failure(messageId, "Message not found in delivery results");
    }

    public async Task<IEnumerable<EmailDeliveryResult>> GetPendingEmailsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var pendingEmails = new List<EmailDeliveryResult>();
        var processedCount = 0;

        foreach (var kvp in _deliveryResults)
        {
            if (processedCount >= count)
                break;

            var result = kvp.Value;
            if (result.Status == EmailDeliveryStatus.Pending || 
                result.Status == EmailDeliveryStatus.Queued ||
                result.Status == EmailDeliveryStatus.Deferred)
            {
                pendingEmails.Add(result);
                processedCount++;
            }
        }

        return pendingEmails;
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!await _processingSemaphore.WaitAsync(100, cancellationToken))
        {
            _logger.LogDebug("Queue processing already in progress, skipping");
            return;
        }

        try
        {
            var processedCount = 0;
            var maxBatchSize = _options.Queue.MaxBatchSize;
            var now = DateTime.UtcNow;

            _logger.LogDebug("Starting queue processing, current queue size: {QueueSize}", _emailQueue.Count);

            var messagesToProcess = new List<QueuedEmailMessage>();

            // Collect messages to process
            while (messagesToProcess.Count < maxBatchSize && 
                   _emailQueue.TryDequeue(out var queuedMessage))
            {
                // Check if message is scheduled for future delivery
                if (queuedMessage.ScheduledFor > now)
                {
                    // Re-queue for later processing
                    _emailQueue.Enqueue(queuedMessage);
                    continue;
                }

                // Check if message has exceeded max retry attempts
                if (queuedMessage.AttemptCount >= queuedMessage.MaxRetryAttempts)
                {
                    var failureResult = EmailDeliveryResult.Failure(
                        queuedMessage.Message.Id, 
                        "Maximum retry attempts exceeded");
                    _deliveryResults.TryUpdate(queuedMessage.Message.Id, failureResult, 
                        _deliveryResults.GetValueOrDefault(queuedMessage.Message.Id));
                    continue;
                }

                // Check retry delay
                if (queuedMessage.LastAttemptAt.HasValue && 
                    now - queuedMessage.LastAttemptAt.Value < queuedMessage.RetryDelay)
                {
                    // Re-queue for later processing
                    _emailQueue.Enqueue(queuedMessage);
                    continue;
                }

                messagesToProcess.Add(queuedMessage);
            }

            // Process messages in parallel
            if (messagesToProcess.Any())
            {
                var processingTasks = messagesToProcess.Select(async queuedMessage =>
                {
                    await ProcessSingleEmailAsync(queuedMessage, cancellationToken);
                });

                await Task.WhenAll(processingTasks);
                processedCount = messagesToProcess.Count;
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} emails from queue", processedCount);
            }

            // Clean up old delivery results
            await CleanupOldDeliveryResultsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during queue processing");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async Task ProcessSingleEmailAsync(QueuedEmailMessage queuedMessage, CancellationToken cancellationToken)
    {
        try
        {
            queuedMessage.AttemptCount++;
            queuedMessage.LastAttemptAt = DateTime.UtcNow;

            // Update status to sending
            var sendingResult = new EmailDeliveryResult
            {
                MessageId = queuedMessage.Message.Id,
                Status = EmailDeliveryStatus.Sending,
                AttemptNumber = queuedMessage.AttemptCount,
                AttemptedAt = DateTime.UtcNow
            };
            _deliveryResults.TryUpdate(queuedMessage.Message.Id, sendingResult, 
                _deliveryResults.GetValueOrDefault(queuedMessage.Message.Id));

            // Send email
            var deliveryResult = await _emailService.SendEmailAsync(queuedMessage.Message, cancellationToken);
            deliveryResult.AttemptNumber = queuedMessage.AttemptCount;

            // Update delivery result
            _deliveryResults.TryUpdate(queuedMessage.Message.Id, deliveryResult, sendingResult);

            if (!deliveryResult.IsSuccess && queuedMessage.AttemptCount < queuedMessage.MaxRetryAttempts)
            {
                // Re-queue for retry
                queuedMessage.NextRetryAt = DateTime.UtcNow.Add(queuedMessage.RetryDelay);
                _emailQueue.Enqueue(queuedMessage);
                
                _logger.LogWarning("Email delivery failed, queued for retry. MessageId: {MessageId}, Attempt: {Attempt}, Error: {Error}",
                    queuedMessage.Message.Id, queuedMessage.AttemptCount, deliveryResult.ErrorMessage);
            }
            else if (deliveryResult.IsSuccess)
            {
                _logger.LogDebug("Email delivered successfully. MessageId: {MessageId}, Attempts: {Attempts}",
                    queuedMessage.Message.Id, queuedMessage.AttemptCount);
            }
            else
            {
                _logger.LogError("Email delivery failed permanently. MessageId: {MessageId}, Attempts: {Attempts}, Error: {Error}",
                    queuedMessage.Message.Id, queuedMessage.AttemptCount, deliveryResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queued email. MessageId: {MessageId}", queuedMessage.Message.Id);
            
            var errorResult = EmailDeliveryResult.Failure(queuedMessage.Message.Id, ex.Message, ex);
            errorResult.AttemptNumber = queuedMessage.AttemptCount;
            _deliveryResults.TryUpdate(queuedMessage.Message.Id, errorResult, 
                _deliveryResults.GetValueOrDefault(queuedMessage.Message.Id));
        }
    }

    private async Task CleanupOldDeliveryResultsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_options.Queue.DeliveryResultRetentionDays);
            var keysToRemove = new List<string>();

            foreach (var kvp in _deliveryResults)
            {
                var result = kvp.Value;
                if (result.AttemptedAt < cutoffDate && 
                    (result.Status == EmailDeliveryStatus.Delivered || 
                     result.Status == EmailDeliveryStatus.Failed))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _deliveryResults.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old delivery results", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during delivery results cleanup");
        }
    }

    private void ProcessQueueCallback(object? state)
    {
        if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessQueueAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue processing timer callback");
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _processTimer?.Dispose();
        _processingSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();

        _logger.LogInformation("Email queue service disposed");
    }

    private class QueuedEmailMessage
    {
        public string Id { get; set; } = string.Empty;
        public EmailMessage Message { get; set; } = new();
        public DateTime QueuedAt { get; set; }
        public DateTime ScheduledFor { get; set; }
        public EmailPriority Priority { get; set; }
        public int AttemptCount { get; set; }
        public int MaxRetryAttempts { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
    }
}