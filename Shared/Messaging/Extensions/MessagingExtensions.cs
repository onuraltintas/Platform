using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Messaging.Abstractions;
using EgitimPlatform.Shared.Messaging.Configuration;
using EgitimPlatform.Shared.Messaging.Events;
using EgitimPlatform.Shared.Messaging.Services;

namespace EgitimPlatform.Shared.Messaging.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        var messagingOptions = new MessagingOptions();
        configuration.GetSection(MessagingOptions.SectionName).Bind(messagingOptions);
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        // Register message serializer
        services.AddSingleton<EgitimPlatform.Shared.Messaging.Abstractions.IMessageSerializer, JsonMessageSerializer>();

        // Register MassTransit
        services.AddMassTransit(x =>
        {
            // Configure bus
            configureBus?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqOptions = messagingOptions.RabbitMq;
                
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                    h.Heartbeat(TimeSpan.FromSeconds(rabbitMqOptions.Heartbeat));
                    h.RequestedConnectionTimeout(TimeSpan.FromMilliseconds(rabbitMqOptions.RequestedConnectionTimeout));
                    
                    if (rabbitMqOptions.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                        });
                    }
                });

                // Configure retry policy
                if (messagingOptions.EnableRetry)
                {
                    cfg.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromMilliseconds(messagingOptions.RetryInterval),
                        TimeSpan.FromMilliseconds(messagingOptions.RetryInterval * 2),
                        TimeSpan.FromMilliseconds(messagingOptions.RetryInterval * 4)
                    ));
                }

                // Configure prefetch count
                cfg.PrefetchCount = messagingOptions.PrefetchCount;
                cfg.ConcurrentMessageLimit = messagingOptions.ConcurrentMessageLimit;

                // Configure message persistence
                if (messagingOptions.EnableMessagePersistence)
                {
                    cfg.Durable = true;
                }

                // Configure dead letter queue
                if (messagingOptions.EnableDeadLetter)
                {
                    cfg.ReceiveEndpoint($"{serviceName.ToLower()}-dead-letter", e =>
                    {
                        e.Bind(messagingOptions.DeadLetterExchange, x =>
                        {
                            x.ExchangeType = "fanout";
                            x.Durable = true;
                        });
                    });
                }

                // Configure service-specific exchanges and queues
                ConfigureServiceEndpoints(cfg, serviceName, messagingOptions);

                cfg.ConfigureEndpoints(context);
            });
        });

        // Register event bus
        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }

    public static IServiceCollection AddEventHandlers(
        this IServiceCollection services,
        params Type[] handlerTypes)
    {
        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, handlerType);
            }
        }

        return services;
    }

    public static IBusRegistrationConfigurator AddConsumer<TConsumer, TEvent>(
        this IBusRegistrationConfigurator configurator,
        string? queueName = null)
        where TConsumer : class, IConsumer<TEvent>
        where TEvent : class, IIntegrationEvent
    {
        configurator.AddConsumer<TConsumer>();
        
        // Note: Queue naming is now handled in the UsingRabbitMq configuration
        // Individual consumer endpoint configuration has been simplified in MassTransit 8.x

        return configurator;
    }

    private static void ConfigureServiceEndpoints(
        IRabbitMqBusFactoryConfigurator cfg,
        string serviceName,
        MessagingOptions options)
    {
        var exchangeName = $"{serviceName.ToLower()}-exchange";
        
        cfg.Message<IIntegrationEvent>(x => x.SetEntityName(exchangeName));
        
        // Configure service-specific settings
        cfg.Publish<IIntegrationEvent>(x =>
        {
            x.ExchangeType = "topic";
            x.Durable = options.EnableMessagePersistence;
        });

        if (options.MessageTimeToLive > 0)
        {
            cfg.Send<IIntegrationEvent>(x =>
            {
                x.UseRoutingKeyFormatter(context => context.Message.EventType.ToLower());
            });
        }
    }

    public static void UseMessagingHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var messagingOptions = new MessagingOptions();
        configuration.GetSection(MessagingOptions.SectionName).Bind(messagingOptions);

        // Add RabbitMQ health checks
        services.AddHealthChecks()
            .AddRabbitMQ(messagingOptions.RabbitMq.GetConnectionString(), 
                name: "rabbitmq",
                tags: new[] { "messaging", "rabbitmq" });
    }
}