using Microsoft.Extensions.Logging;
using Polly;
using EgitimPlatform.Shared.Resilience.Policies;

namespace EgitimPlatform.Shared.Resilience.Services;

public interface IResilientMessagingService
{
    Task PublishAsync<T>(T message, string operationName, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(T message, string exchange, string routingKey, string operationName, CancellationToken cancellationToken = default) where T : class;
    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, string operationName, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class;
    Task ConsumeAsync<T>(T message, Func<T, Task> handler, string operationName, CancellationToken cancellationToken = default) where T : class;
}

public class ResilientMessagingService : IResilientMessagingService
{
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<ResilientMessagingService> _logger;

    public ResilientMessagingService(
        IResiliencePolicyFactory policyFactory,
        ILogger<ResilientMessagingService> logger,
        string? serviceName = null)
    {
        _resiliencePipeline = policyFactory.CreateMessagingPipeline(serviceName);
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string operationName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Publishing message with resilience: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);

            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                // This would be replaced with actual message bus publish logic
                await SimulateMessagePublish(message, ct);
            }, cancellationToken);

            _logger.LogDebug("Successfully published message: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, string exchange, string routingKey, string operationName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Publishing message to exchange with resilience: {OperationName}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, MessageType: {MessageType}", 
                operationName, exchange, routingKey, typeof(T).Name);

            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                // This would be replaced with actual message bus publish logic
                await SimulateMessagePublish(message, exchange, routingKey, ct);
            }, cancellationToken);

            _logger.LogDebug("Successfully published message to exchange: {OperationName}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, MessageType: {MessageType}", 
                operationName, exchange, routingKey, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange: {OperationName}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, MessageType: {MessageType}", 
                operationName, exchange, routingKey, typeof(T).Name);
            throw;
        }
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, string operationName, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class
    {
        try
        {
            _logger.LogDebug("Sending request with resilience: {OperationName}, RequestType: {RequestType}, ResponseType: {ResponseType}", 
                operationName, typeof(TRequest).Name, typeof(TResponse).Name);

            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                // This would be replaced with actual message bus request logic
                return await SimulateMessageRequest<TRequest, TResponse>(request, ct);
            }, cancellationToken);

            _logger.LogDebug("Successfully received response: {OperationName}, RequestType: {RequestType}, ResponseType: {ResponseType}", 
                operationName, typeof(TRequest).Name, typeof(TResponse).Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request: {OperationName}, RequestType: {RequestType}, ResponseType: {ResponseType}", 
                operationName, typeof(TRequest).Name, typeof(TResponse).Name);
            throw;
        }
    }

    public async Task ConsumeAsync<T>(T message, Func<T, Task> handler, string operationName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Processing message with resilience: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);

            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                await handler(message);
            }, cancellationToken);

            _logger.LogDebug("Successfully processed message: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: {OperationName}, MessageType: {MessageType}", 
                operationName, typeof(T).Name);
            throw;
        }
    }

    // Simulation methods - replace with actual message bus implementation
    private async Task SimulateMessagePublish<T>(T message, CancellationToken cancellationToken) where T : class
    {
        // Simulate network delay and potential failure
        await Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
        
        // Simulate occasional failures for testing resilience
        if (Random.Shared.NextDouble() < 0.1) // 10% failure rate
        {
            throw new InvalidOperationException("Simulated message publish failure");
        }
    }

    private async Task SimulateMessagePublish<T>(T message, string exchange, string routingKey, CancellationToken cancellationToken) where T : class
    {
        // Simulate network delay and potential failure
        await Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
        
        // Simulate occasional failures for testing resilience
        if (Random.Shared.NextDouble() < 0.1) // 10% failure rate
        {
            throw new InvalidOperationException("Simulated message publish failure");
        }
    }

    private async Task<TResponse> SimulateMessageRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) 
        where TRequest : class where TResponse : class
    {
        // Simulate network delay and potential failure
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);
        
        // Simulate occasional failures for testing resilience
        if (Random.Shared.NextDouble() < 0.15) // 15% failure rate
        {
            throw new TimeoutException("Simulated request timeout");
        }

        // Return a mock response - replace with actual logic
        return Activator.CreateInstance<TResponse>();
    }
}