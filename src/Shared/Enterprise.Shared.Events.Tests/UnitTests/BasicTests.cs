using Enterprise.Shared.Events.Extensions;
using Enterprise.Shared.Events.Interfaces;
using Enterprise.Shared.Events.Models;
using Enterprise.Shared.Events.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Enterprise.Shared.Events.Tests.UnitTests;

public class BasicTests
{
    [Fact]
    public void AddSharedEventsWithInMemory_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedEventsWithInMemory();

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IEventSerializer>().Should().NotBeNull();
        serviceProvider.GetService<IDomainEventDispatcher>().Should().NotBeNull();
        serviceProvider.GetService<IEventStore>().Should().NotBeNull();
        serviceProvider.GetService<IOutboxService>().Should().NotBeNull();
        serviceProvider.GetService<IEventBus>().Should().NotBeNull();
    }

    [Fact]
    public void EventSerializer_SerializeAndDeserialize_WorksCorrectly()
    {
        var logger = new Mock<ILogger<EventSerializer>>();
        var serializer = new EventSerializer(logger.Object);

        var testEvent = new TestDomainEvent
        {
            TestProperty = "test value",
            TestNumber = 42
        };

        var json = serializer.Serialize(testEvent);
        var deserialized = serializer.Deserialize<TestDomainEvent>(json);

        deserialized.Should().NotBeNull();
        deserialized!.TestProperty.Should().Be("test value");
        deserialized.TestNumber.Should().Be(42);
    }

    [Fact]
    public async Task EventBus_PublishIntegrationEvent_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        var integrationEvent = new TestIntegrationEvent
        {
            TestProperty = "integration test"
        };

        var act = async () => await eventBus.PublishAsync(integrationEvent);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DomainEventDispatcher_DispatchEvent_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        var domainEvent = new TestDomainEvent
        {
            TestProperty = "domain test"
        };

        var act = async () => await dispatcher.DispatchAsync(domainEvent);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OutboxService_AddIntegrationEvent_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var outboxService = serviceProvider.GetRequiredService<IOutboxService>();

        var integrationEvent = new TestIntegrationEvent
        {
            TestProperty = "outbox test"
        };

        var act = async () => await outboxService.AddEventAsync(integrationEvent);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventStore_GetEvents_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var eventStore = serviceProvider.GetRequiredService<IEventStore>();

        var act = async () => await eventStore.GetEventsAsync<TestDomainEvent>("test-stream");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventStore_AppendEvents_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var eventStore = serviceProvider.GetRequiredService<IEventStore>();

        var events = new List<TestDomainEvent>
        {
            new() { TestProperty = "test1" },
            new() { TestProperty = "test2" }
        };

        var act = async () => await eventStore.AppendEventsAsync("test-stream", events, 0);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void DomainEvent_CreatesWithCorrectProperties()
    {
        var domainEvent = new TestDomainEvent
        {
            TestProperty = "test"
        };

        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.EventType.Should().Be("TestDomainEvent");
    }

    [Fact]
    public void IntegrationEvent_CreatesWithCorrectProperties()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            TestProperty = "test"
        };

        integrationEvent.EventId.Should().NotBeEmpty();
        integrationEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        integrationEvent.EventType.Should().Be("TestIntegrationEvent");
        integrationEvent.Version.Should().Be(1);
    }

    [Fact]
    public async Task OutboxService_ProcessUnpublishedEvents_DoesNotThrow()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var outboxService = serviceProvider.GetRequiredService<IOutboxService>();

        var act = async () => await outboxService.ProcessUnpublishedEventsAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OutboxService_GetStatistics_ReturnsStatistics()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSharedEventsWithInMemory()
            .BuildServiceProvider();

        var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
        
        // Add a test event first to avoid empty collection issue
        var integrationEvent = new TestIntegrationEvent
        {
            TestProperty = "statistics test"
        };
        await outboxService.AddEventAsync(integrationEvent);

        var statistics = await outboxService.GetStatisticsAsync();

        statistics.Should().NotBeNull();
        statistics.TotalEvents.Should().BeGreaterOrEqualTo(1);
    }

    private record TestDomainEvent : DomainEvent
    {
        public string TestProperty { get; init; } = string.Empty;
        public int TestNumber { get; init; }
    }

    private record TestIntegrationEvent : IntegrationEvent
    {
        public string TestProperty { get; init; } = string.Empty;
    }
}