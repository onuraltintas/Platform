namespace Enterprise.Shared.Auditing.Tests.Services;

public class AuditServiceSimpleTests
{
    private readonly IAuditStore _mockStore;
    private readonly IAuditContextProvider _mockContextProvider;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditConfiguration _configuration;
    private readonly AuditService _auditService;

    public AuditServiceSimpleTests()
    {
        _mockStore = Substitute.For<IAuditStore>();
        _mockContextProvider = Substitute.For<IAuditContextProvider>();
        _logger = new FakeLogger<AuditService>();
        _configuration = new AuditConfiguration
        {
            Enabled = true,
            DefaultServiceName = "TestService",
            DefaultEnvironment = "Test",
            EnrichWithHttpContext = true,
            EnrichWithUserContext = true
        };

        // Set up context provider defaults
        _mockContextProvider.GetCurrentServiceName().Returns("TestService");
        _mockContextProvider.GetCurrentEnvironment().Returns("Test");
        _mockContextProvider.GetCurrentUserId().Returns("test-user");
        _mockContextProvider.GetCurrentUsername().Returns("testuser");
        _mockContextProvider.GetContextProperties().Returns(new Dictionary<string, object>());

        var options = Options.Create(_configuration);
        _auditService = new AuditService(_mockStore, _mockContextProvider, options, _logger);
    }

    [Fact]
    public async Task LogEventAsync_WithValidEvent_StoresEventSuccessfully()
    {
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");
        _mockStore.StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _auditService.LogEventAsync(auditEvent);

        result.IsSuccess.Should().BeTrue();
        await _mockStore.Received(1).StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogEventAsync_WithNullEvent_ReturnsFailure()
    {
        var result = await _auditService.LogEventAsync(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Audit event cannot be null");
        await _mockStore.DidNotReceive().StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogEventAsync_WhenDisabled_ReturnsSuccessWithoutStoring()
    {
        _configuration.Enabled = false;
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");

        var result = await _auditService.LogEventAsync(auditEvent);

        result.IsSuccess.Should().BeTrue();
        await _mockStore.DidNotReceive().StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchEventsAsync_WithValidCriteria_ReturnsResults()
    {
        var criteria = new AuditSearchCriteria { PageSize = 10 };
        var events = new List<AuditEvent> { AuditEvent.Create("Test", "Resource") };
        _mockStore.QueryEventsAsync(criteria, Arg.Any<CancellationToken>())
            .Returns((events, 1));

        var result = await _auditService.SearchEventsAsync(criteria);

        result.Should().NotBeNull();
        result.Events.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEventAsync_WithValidId_ReturnsEvent()
    {
        var eventId = "event123";
        var auditEvent = AuditEvent.Create("Test", "Resource");
        _mockStore.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(auditEvent);

        var result = await _auditService.GetEventAsync(eventId);

        result.Should().NotBeNull();
        result.Should().Be(auditEvent);
    }

    [Fact]
    public async Task GetEventAsync_WithEmptyId_ReturnsNull()
    {
        var result = await _auditService.GetEventAsync(string.Empty);

        result.Should().BeNull();
        await _mockStore.DidNotReceive().GetEventAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogSecurityEventAsync_WithValidEvent_StoresEventSuccessfully()
    {
        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.Authentication, "Login", "Application");
        _mockStore.StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _auditService.LogSecurityEventAsync(securityEvent);

        result.IsSuccess.Should().BeTrue();
        await _mockStore.Received(1).StoreEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }
}