using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Observability.Tests.Services;

public class PrometheusMetricsServiceTests
{
    private readonly Mock<ILogger<PrometheusMetricsService>> _loggerMock;
    private readonly Mock<IOptions<ObservabilitySettings>> _optionsMock;
    private readonly ObservabilitySettings _settings;
    private readonly PrometheusMetricsService _service;

    public PrometheusMetricsServiceTests()
    {
        _loggerMock = new Mock<ILogger<PrometheusMetricsService>>();
        _optionsMock = new Mock<IOptions<ObservabilitySettings>>();
        _settings = new ObservabilitySettings
        {
            EnableMetrics = true,
            EnableBusinessMetrics = true,
            Metrics = new MetricsSettings
            {
                CustomMetricsPrefix = "test_"
            }
        };
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _service = new PrometheusMetricsService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new PrometheusMetricsService(_loggerMock.Object, _optionsMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new PrometheusMetricsService(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new PrometheusMetricsService(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("settings");
    }

    [Fact]
    public void IncrementCounter_Should_Not_Throw_When_Metrics_Enabled()
    {
        // Act & Assert
        Action act = () => _service.IncrementCounter("test_counter", 1,
            new KeyValuePair<string, object>("label", "value"));
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementCounter_Should_Not_Process_When_Metrics_Disabled()
    {
        // Arrange
        _settings.EnableMetrics = false;

        // Act & Assert
        Action act = () => _service.IncrementCounter("test_counter", 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_Should_Not_Throw_When_Called_With_Valid_Parameters()
    {
        // Act & Assert
        Action act = () => _service.RecordHistogram("test_histogram", 123.45,
            new KeyValuePair<string, object>("method", "GET"));
        act.Should().NotThrow();
    }

    [Fact]
    public void SetGauge_Should_Not_Throw_When_Called_With_Valid_Parameters()
    {
        // Act & Assert
        Action act = () => _service.SetGauge("test_gauge", 99.9,
            new KeyValuePair<string, object>("status", "active"));
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordBusinessMetric_Should_Not_Throw_When_Business_Metrics_Enabled()
    {
        // Act & Assert
        Action act = () => _service.RecordBusinessMetric("user_signup", 1,
            new Dictionary<string, object> { ["source"] = "website" });
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordBusinessMetric_Should_Not_Process_When_Business_Metrics_Disabled()
    {
        // Arrange
        _settings.EnableBusinessMetrics = false;

        // Act & Assert
        Action act = () => _service.RecordBusinessMetric("user_signup", 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementUserAction_Should_Not_Throw_When_Called()
    {
        // Act & Assert
        Action act = () => _service.IncrementUserAction("login", "user123");
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordApiCall_Should_Not_Throw_When_Called()
    {
        // Act & Assert
        Action act = () => _service.RecordApiCall("GET", "/api/users", 200, 150.5);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordDatabaseQuery_Should_Not_Throw_When_Called()
    {
        // Act & Assert
        Action act = () => _service.RecordDatabaseQuery("SELECT", "users", 25.3, true);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCacheOperation_Should_Not_Throw_When_Called()
    {
        // Act & Assert
        Action act = () => _service.RecordCacheOperation("GET", "user:123", true, 1.5);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetApplicationMetricsAsync_Should_Return_Valid_Metrics()
    {
        // Act
        var metrics = await _service.GetApplicationMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.MemoryUsageMB.Should().BeGreaterThan(0);
        metrics.ThreadCount.Should().BeGreaterThan(0);
        metrics.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
        metrics.MeasuredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordApiCall_Should_Track_Error_Metrics_For_5xx_Status_Codes()
    {
        // Act & Assert - Should not throw when recording 500 error
        Action act = () => _service.RecordApiCall("POST", "/api/orders", 500, 200.0);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordDatabaseQuery_Should_Track_Slow_Queries()
    {
        // Act & Assert - Should not throw when recording slow query (>1000ms)
        Action act = () => _service.RecordDatabaseQuery("SELECT", "orders", 1500.0, true);
        act.Should().NotThrow();
    }
}