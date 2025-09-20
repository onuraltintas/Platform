namespace Enterprise.Shared.Auditing.Tests.HealthChecks;

public class AuditHealthCheckSimpleTests
{
    private readonly IAuditStore _mockStore;
    private readonly IAuditService _mockService;
    private readonly ILogger<AuditHealthCheck> _logger;
    private readonly AuditConfiguration _configuration;
    private readonly AuditHealthCheck _healthCheck;

    public AuditHealthCheckSimpleTests()
    {
        _mockStore = Substitute.For<IAuditStore>();
        _mockService = Substitute.For<IAuditService>();
        _logger = new FakeLogger<AuditHealthCheck>();
        _configuration = new AuditConfiguration
        {
            Enabled = true,
            DefaultServiceName = "TestService",
            DefaultEnvironment = "Test",
            EnrichWithHttpContext = true,
            EnrichWithUserContext = true,
            MaxBatchSize = 100,
            BatchFlushIntervalSeconds = 30,
            Storage = { StorageType = AuditStorageType.Memory }
        };

        var options = Options.Create(_configuration);
        _healthCheck = new AuditHealthCheck(_mockStore, _mockService, options, _logger);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAuditingDisabled_ReturnsHealthy()
    {
        _configuration.Enabled = false;

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("disabled");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreHealthCheckFails_ReturnsDegraded()
    {
        _mockStore.HealthCheckAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Store connection failed"));

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("store health check failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceTestFails_ReturnsUnhealthy()
    {
        _mockStore.HealthCheckAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _mockService.LogEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Service error"));

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("service test failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllHealthy_ReturnsHealthy()
    {
        _mockStore.HealthCheckAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _mockService.LogEventAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _mockStore.GetEventCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42));

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
        result.Data.Should().ContainKey("events_last_hour");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnexpectedException_ReturnsUnhealthy()
    {
        _mockStore.HealthCheckAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Result>(new InvalidOperationException("Unexpected error")));

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
        result.Exception.Should().NotBeNull();
    }
}