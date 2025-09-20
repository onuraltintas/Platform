namespace Enterprise.Shared.Auditing.Tests.Services;

public class InMemoryAuditStoreSimpleTests : IDisposable
{
    private readonly InMemoryAuditStore _store;
    private readonly ILogger<InMemoryAuditStore> _logger;
    private readonly AuditConfiguration _configuration;

    public InMemoryAuditStoreSimpleTests()
    {
        _configuration = new AuditConfiguration
        {
            Enabled = true,
            DefaultServiceName = "TestService"
        };
        _logger = new FakeLogger<InMemoryAuditStore>();
        var options = Options.Create(_configuration);
        _store = new InMemoryAuditStore(options, _logger);
    }

    [Fact]
    public async Task StoreEventAsync_WithValidEvent_StoresSuccessfully()
    {
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");

        var result = await _store.StoreEventAsync(auditEvent);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StoreEventAsync_WithNullEvent_ReturnsFailure()
    {
        var result = await _store.StoreEventAsync(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Audit event cannot be null");
    }

    [Fact]
    public async Task GetEventAsync_WithExistingId_ReturnsEvent()
    {
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");
        await _store.StoreEventAsync(auditEvent);

        var result = await _store.GetEventAsync(auditEvent.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(auditEvent.Id);
        result.Action.Should().Be(auditEvent.Action);
    }

    [Fact]
    public async Task GetEventAsync_WithNonExistingId_ReturnsNull()
    {
        var result = await _store.GetEventAsync("non-existing-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryEventsAsync_WithNoFilters_ReturnsAllEvents()
    {
        var events = new List<AuditEvent>
        {
            AuditEvent.Create("Action1", "Resource1"),
            AuditEvent.Create("Action2", "Resource2")
        };
        await _store.StoreEventsAsync(events);

        var criteria = new AuditSearchCriteria();
        var (resultEvents, totalCount) = await _store.QueryEventsAsync(criteria);

        resultEvents.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryEventsAsync_WithSorting_ReturnsSortedResults()
    {
        var events = new List<AuditEvent>
        {
            AuditEvent.Create("ZAction", "Resource"),
            AuditEvent.Create("AAction", "Resource"),
            AuditEvent.Create("MAction", "Resource")
        };
        await _store.StoreEventsAsync(events);

        var criteria = new AuditSearchCriteria
        {
            SortBy = "action",
            SortDirection = SortDirection.Ascending
        };

        var (resultEvents, totalCount) = await _store.QueryEventsAsync(criteria);

        resultEvents.Should().HaveCount(3);
        resultEvents.First().Action.Should().Be("AAction");
        resultEvents.Last().Action.Should().Be("ZAction");
    }

    [Fact]
    public async Task HealthCheckAsync_WithHealthyStore_ReturnsSuccess()
    {
        var result = await _store.HealthCheckAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventCountAsync_WithDateRange_ReturnsCorrectCount()
    {
        var events = new List<AuditEvent>
        {
            AuditEvent.Create("Action1", "Resource1"),
            AuditEvent.Create("Action2", "Resource2")
        };
        events[0].Timestamp = DateTime.UtcNow.AddDays(-1);
        events[1].Timestamp = DateTime.UtcNow.AddDays(-10);

        await _store.StoreEventsAsync(events);

        var startDate = DateTime.UtcNow.AddDays(-2);
        var endDate = DateTime.UtcNow;
        var count = await _store.GetEventCountAsync(startDate, endDate);

        count.Should().Be(1);
    }

    public void Dispose()
    {
        _store?.Dispose();
    }
}