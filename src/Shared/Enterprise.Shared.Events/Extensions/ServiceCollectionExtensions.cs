using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Enterprise.Shared.Events.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Enterprise.Shared.Events.Extensions;

/// <summary>
/// Service collection extensions for Enterprise.Shared.Events
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enterprise Events servislerini DI container'a ekler
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="assemblies">Event handler'ları içeren assembly'ler (opsiyonel)</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSharedEvents(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        // Configuration bind et
        services.Configure<EventSettings>(configuration.GetSection(EventSettings.SectionName));
        
        // Core services
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventStore, InMemoryEventStore>();
        services.AddScoped<IOutboxService, InMemoryOutboxService>();

        // MediatR'yi ekle
        var allAssemblies = GetAssemblies(assemblies);
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(allAssemblies);
        });

        // MassTransit'i konfigüre et
        services.AddMassTransit(x =>
        {
            // Consumer'ları register et
            foreach (var assembly in allAssemblies)
            {
                x.AddConsumers(assembly);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                // Environment değişkenlerinden RabbitMQ ayarlarını al
                var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                var username = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER") ?? "guest";
                var password = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS") ?? "guest";
                var connectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING");

                // Connection string varsa onu kullan, yoksa individual ayarları kullan
                if (!string.IsNullOrEmpty(connectionString))
                {
                    cfg.Host(connectionString);
                }
                else
                {
                    cfg.Host(host, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                }

                // Prefetch count ayarla (environment'tan al yoksa default)
                var prefetchCount = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PREFETCH_COUNT"), out var prefetch) ? prefetch : 10;
                cfg.PrefetchCount = (ushort)prefetchCount;

                // Endpoint'leri konfigüre et
                cfg.ConfigureEndpoints(context);

                // Retry policy
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                // JSON serializer
                cfg.UseRawJsonSerializer();
            });
        });


        // Event bus'ı register et
        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }

    /// <summary>
    /// Enterprise Events servislerini minimal configuration ile ekler
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureMassTransit">MassTransit configuration action (opsiyonel)</param>
    /// <param name="assemblies">Event handler'ları içeren assembly'ler (opsiyonel)</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSharedEventsWithInMemory(
        this IServiceCollection services,
        Action<IBusRegistrationConfigurator>? configureMassTransit = null,
        params Assembly[] assemblies)
    {
        // Default configuration
        services.Configure<EventSettings>(settings =>
        {
            settings.RabbitMQ = new RabbitMqSettings
            {
                Host = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest",
                VirtualHost = "/",
                ConnectionRetryCount = 3,
                PrefetchCount = 10
            };
            settings.DomainEvents = new DomainEventSettings
            {
                EnableOutbox = true,
                PublishAfterCommit = true,
                MaxRetryCount = 3,
                RetryIntervalSeconds = 30
            };
            settings.Outbox = new OutboxSettings
            {
                Enabled = true,
                ProcessorIntervalSeconds = 30,
                BatchSize = 100,
                MaxRetryCount = 3
            };
        });

        // Core services
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventStore, InMemoryEventStore>();
        services.AddScoped<IOutboxService, InMemoryOutboxService>();

        // MediatR
        var allAssemblies = GetAssemblies(assemblies);
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(allAssemblies);
        });

        // MassTransit with in-memory transport (for testing)
        services.AddMassTransit(x =>
        {
            // Custom configuration
            configureMassTransit?.Invoke(x);

            // Consumer'ları register et
            foreach (var assembly in allAssemblies)
            {
                x.AddConsumers(assembly);
            }

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // Event bus
        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }

    /// <summary>
    /// Sadece domain event servislerini ekler (integration event'ler olmadan)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Domain event handler'ları içeren assembly'ler</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddDomainEvents(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Core services
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventStore, InMemoryEventStore>();

        // MediatR
        var allAssemblies = GetAssemblies(assemblies);
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(allAssemblies);
        });

        return services;
    }

    /// <summary>
    /// Background service'ler için outbox processor'ı ekler
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessorService>();
        return services;
    }

    /// <summary>
    /// Assembly listesini hazırlar
    /// </summary>
    /// <param name="additionalAssemblies">Ek assembly'ler</param>
    /// <returns>Tüm assembly'ler</returns>
    private static Assembly[] GetAssemblies(params Assembly[] additionalAssemblies)
    {
        var assemblies = new List<Assembly>
        {
            Assembly.GetExecutingAssembly(), // Enterprise.Shared.Events
            Assembly.GetCallingAssembly()    // Calling assembly
        };

        if (additionalAssemblies?.Length > 0)
        {
            assemblies.AddRange(additionalAssemblies);
        }

        return assemblies.Distinct().ToArray();
    }
}