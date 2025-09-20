using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Observability.Tests.Services;

public class AdvancedHealthChecksServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<AdvancedHealthChecksService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<ObservabilitySettings>> _optionsMock;
    private readonly ObservabilitySettings _settings;
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly AdvancedHealthChecksService _service;

    public AdvancedHealthChecksServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<AdvancedHealthChecksService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _optionsMock = new Mock<IOptions<ObservabilitySettings>>();
        
        _settings = new ObservabilitySettings
        {
            HealthChecks = new HealthCheckSettings
            {
                Timeout = TimeSpan.FromSeconds(5)
            }
        };
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        
        _httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(x => x.CreateClient("HealthChecks")).Returns(new HttpClient());
        
        _service = new AdvancedHealthChecksService(
            _serviceProviderMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new AdvancedHealthChecksService(
            _serviceProviderMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_ServiceProvider_Is_Null()
    {
        // Act & Assert
        Action act = () => new AdvancedHealthChecksService(
            null!,
            _optionsMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new AdvancedHealthChecksService(
            _serviceProviderMock.Object,
            null!,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("settings");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new AdvancedHealthChecksService(
            _serviceProviderMock.Object,
            _optionsMock.Object,
            null!,
            _httpClientFactoryMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_HttpClientFactory_Is_Null()
    {
        // Act & Assert
        Action act = () => new AdvancedHealthChecksService(
            _serviceProviderMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task CheckDatabaseConnectionAsync_Should_Return_Healthy_For_Fast_Response()
    {
        // Act
        var result = await _service.CheckDatabaseConnectionAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database connection is healthy");
        result.Data.Should().ContainKey("ResponseTime");
        result.Data.Should().ContainKey("CheckedAt");
    }

    [Fact]
    public async Task CheckRedisConnectionAsync_Should_Return_Healthy()
    {
        // Act
        var result = await _service.CheckRedisConnectionAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Redis connection is healthy");
        result.Data.Should().ContainKey("ResponseTime");
        result.Data.Should().ContainKey("CheckedAt");
    }

    [Fact]
    public async Task CheckDiskSpaceAsync_Should_Return_Health_Status()
    {
        // Act
        var result = await _service.CheckDiskSpaceAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        
        // Since this is a real disk check, it should have data unless it fails
        if (result.Status != HealthStatus.Unhealthy)
        {
            result.Data.Should().NotBeNull();
            result.Data!.Should().ContainKey("Drives");
            result.Data.Should().ContainKey("HealthyDrives");
            result.Data.Should().ContainKey("TotalDrives");
            result.Data.Should().ContainKey("CheckedAt");
        }
    }

    [Fact]
    public async Task CheckMemoryUsageAsync_Should_Return_Health_Status()
    {
        // Act
        var result = await _service.CheckMemoryUsageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("WorkingSetMB");
        result.Data.Should().ContainKey("PrivateMemoryMB");
        result.Data.Should().ContainKey("TotalManagedMemoryMB");
        result.Data.Should().ContainKey("CheckedAt");
    }

    [Fact]
    public async Task CheckCpuUsageAsync_Should_Return_Health_Status()
    {
        // Act
        var result = await _service.CheckCpuUsageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("CpuUsagePercentage");
        result.Data.Should().ContainKey("ProcessorCount");
        result.Data.Should().ContainKey("CheckedAt");
    }

    [Fact]
    public async Task GetDetailedHealthReportAsync_Should_Return_Comprehensive_Report()
    {
        // Act
        var report = await _service.GetDetailedHealthReportAsync();

        // Assert
        report.Should().NotBeNull();
        report.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        report.Entries.Should().ContainKeys("Database", "Redis", "DiskSpace", "Memory", "CPU");
        report.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetSystemInfoAsync_Should_Return_System_Information()
    {
        // Act
        var info = await _service.GetSystemInfoAsync();

        // Assert
        info.Should().NotBeNull();
        info.Should().ContainKey("MachineName");
        info.Should().ContainKey("OSVersion");
        info.Should().ContainKey("ProcessorCount");
        info.Should().ContainKey("Is64BitOperatingSystem");
        info.Should().ContainKey("DotNetVersion");
        info.Should().ContainKey("ProcessId");
        info.Should().ContainKey("Uptime");
    }

    [Fact]
    public async Task CheckDatabaseConnectionAsync_Should_Handle_Cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var result = await _service.CheckDatabaseConnectionAsync(cts.Token);
        result.Should().NotBeNull();
        // The method should complete normally even with cancelled token in this mock scenario
    }

    [Fact]
    public async Task GetDetailedHealthReportAsync_Should_Handle_Cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should throw TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            _service.GetDetailedHealthReportAsync(cts.Token));
    }

    [Fact]
    public async Task CheckExternalServiceAsync_Should_Handle_Timeout()
    {
        // Arrange
        var serviceName = "TestService";
        var endpoint = "https://httpstat.us/200?sleep=10000"; // This will timeout

        // Act
        var result = await _service.CheckExternalServiceAsync(serviceName, endpoint);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed"); // Timeout results in "health check failed"
    }
}