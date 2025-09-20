using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Events.Services;

/// <summary>
/// Outbox pattern için background service
/// Yayınlanmamış event'leri periyodik olarak işler
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly OutboxSettings _settings;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Outbox settings</param>
    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger,
        IOptions<EventSettings> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value?.Outbox ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Background service execution
    /// </summary>
    /// <param name="stoppingToken">Stopping token</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Outbox processor is disabled. Exiting.");
            return;
        }

        _logger.LogInformation("Outbox processor started. Processing interval: {IntervalSeconds} seconds, Batch size: {BatchSize}",
            _settings.ProcessorIntervalSeconds, _settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
                
                var delay = TimeSpan.FromSeconds(_settings.ProcessorIntervalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Outbox processor is stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processor. Will retry in {IntervalSeconds} seconds.",
                    _settings.ProcessorIntervalSeconds);

                try
                {
                    var delay = TimeSpan.FromSeconds(_settings.ProcessorIntervalSeconds);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Outbox processor is stopping.");
                    break;
                }
            }
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    /// <summary>
    /// Outbox event'leri işler
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

            _logger.LogDebug("Processing unpublished outbox events...");
            
            // Yayınlanmamış event'leri işle
            await outboxService.ProcessUnpublishedEventsAsync(cancellationToken);

            // Başarısız event'leri retry et
            await outboxService.RetryFailedEventsAsync(_settings.MaxRetryCount, cancellationToken);

            _logger.LogDebug("Outbox events processing completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox events");
            throw;
        }
    }

    /// <summary>
    /// Service stopped
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping outbox processor...");

        // Son kez outbox'ı işlemeyi dene
        try
        {
            await ProcessOutboxEventsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during final outbox processing on shutdown");
        }

        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("Outbox processor stopped.");
    }
}