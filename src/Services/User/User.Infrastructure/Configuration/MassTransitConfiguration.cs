using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User.Application.Handlers.IntegrationEventHandlers;
using User.Core.Events;

namespace User.Infrastructure.Configuration;

/// <summary>
/// MassTransit configuration for User Service
/// </summary>
public static class MassTransitConfiguration
{
    /// <summary>
    /// Configure MassTransit for User Service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="rabbitMqConnectionString">RabbitMQ connection string</param>
    /// <param name="queueName">Service queue name</param>
    /// <param name="exchangeName">Exchange name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddUserServiceMassTransit(
        this IServiceCollection services,
        string rabbitMqConnectionString,
        string queueName = "userservice",
        string exchangeName = "platform_events")
    {
        services.AddMassTransit(x =>
        {
            // Add consumers
            x.AddConsumer<UserRegisteredFromIdentityEventHandler>(typeof(UserRegisteredFromIdentityEventConsumerDefinition));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConnectionString);

                // Configure message retry policy
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5)
                ));

                // Configure circuit breaker
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });

                // Configure rate limiting
                cfg.UseRateLimit(1000, TimeSpan.FromMinutes(1));

                // Configure exchanges and queues
                cfg.ReceiveEndpoint(queueName, e =>
                {
                    e.DefaultContentType = new System.Net.Mime.ContentType("application/json");
                    e.UseMessageRetry(r => r.Immediate(3));
                    
                    // Bind to exchange
                    e.Bind(exchangeName, s =>
                    {
                        s.RoutingKey = "user.registered";
                        s.ExchangeType = "topic";
                    });

                    // Configure consumer
                    e.ConfigureConsumer<UserRegisteredFromIdentityEventHandler>(context);

                    // Configure dead letter handling
                    e.DiscardSkippedMessages();
                });

                // Configure publishing
                cfg.Publish<UserRegisteredFromIdentityEvent>(p =>
                {
                    p.ExchangeType = "topic";
                });

                // Use default JSON serialization - removed custom configuration to fix compatibility issues

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

/// <summary>
/// Consumer definition for UserRegisteredFromIdentityEventHandler
/// </summary>
public class UserRegisteredFromIdentityEventConsumerDefinition : ConsumerDefinition<UserRegisteredFromIdentityEventHandler>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public UserRegisteredFromIdentityEventConsumerDefinition()
    {
        // Set consumer options
        EndpointName = "user-registered-consumer";
        ConcurrentMessageLimit = 10;
    }

    /// <summary>
    /// Configure consumer
    /// </summary>
    /// <param name="endpointConfigurator">Endpoint configurator</param>
    /// <param name="consumerConfigurator">Consumer configurator</param>
    [Obsolete]
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UserRegisteredFromIdentityEventHandler> consumerConfigurator)
    {
        // Configure retry and error handling
        consumerConfigurator.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            r.Handle<InvalidOperationException>();
            r.Handle<ArgumentException>();
        });

        // Configure filtering if needed
        // Configure filtering if needed - removed UseConsumeFilter due to API changes
    }
}

/// <summary>
/// Logging filter for consumer messages
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public class LoggingConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ILogger<LoggingConsumeFilter<T>> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public LoggingConsumeFilter(ILogger<LoggingConsumeFilter<T>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send message through filter
    /// </summary>
    /// <param name="context">Consume context</param>
    /// <param name="next">Next filter</param>
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageType = typeof(T).Name;
        var correlationId = context.CorrelationId?.ToString() ?? "Unknown";
        
        _logger.LogInformation("Processing message {MessageType} with CorrelationId: {CorrelationId}", 
            messageType, correlationId);

        var startTime = DateTime.UtcNow;
        
        try
        {
            await next.Send(context);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Successfully processed message {MessageType} in {Duration}ms", 
                messageType, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error processing message {MessageType} after {Duration}ms", 
                messageType, duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Probe filter
    /// </summary>
    /// <param name="context">Probe context</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("logging");
    }
}