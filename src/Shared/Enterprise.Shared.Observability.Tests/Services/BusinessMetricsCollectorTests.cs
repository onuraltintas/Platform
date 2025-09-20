using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Observability.Tests.Services;

public class BusinessMetricsCollectorTests
{
    private readonly Mock<IMetricsService> _metricsServiceMock;
    private readonly Mock<IBusinessMetricsRepository> _repositoryMock;
    private readonly Mock<ILogger<BusinessMetricsCollector>> _loggerMock;
    private readonly Mock<IOptions<ObservabilitySettings>> _optionsMock;
    private readonly ObservabilitySettings _settings;
    private readonly BusinessMetricsCollector _collector;

    public BusinessMetricsCollectorTests()
    {
        _metricsServiceMock = new Mock<IMetricsService>();
        _repositoryMock = new Mock<IBusinessMetricsRepository>();
        _loggerMock = new Mock<ILogger<BusinessMetricsCollector>>();
        _optionsMock = new Mock<IOptions<ObservabilitySettings>>();
        
        _settings = new ObservabilitySettings
        {
            BusinessMetrics = new BusinessMetricsSettings
            {
                EnableUserMetrics = true,
                EnableOrderMetrics = true,
                EnablePaymentMetrics = true,
                EnableFeatureUsageMetrics = true
            }
        };
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        
        _collector = new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _repositoryMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var collector = new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _repositoryMock.Object);
        collector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Initialize_Without_Repository()
    {
        // Act & Assert - Constructor should not throw without repository
        var collector = new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
        collector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_MetricsService_Is_Null()
    {
        // Act & Assert
        Action act = () => new BusinessMetricsCollector(
            null!,
            _loggerMock.Object,
            _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("metricsService");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            null!,
            _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            _loggerMock.Object,
            null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("settings");
    }

    [Fact]
    public async Task RecordUserRegistrationAsync_Should_Call_Metrics_Service()
    {
        // Act
        await _collector.RecordUserRegistrationAsync("user123", "website");

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("user_registrations_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "source" && 
                    kvp[0].Value.ToString() == "website")), 
            Times.Once);
    }

    [Fact]
    public async Task RecordUserRegistrationAsync_Should_Store_In_Repository()
    {
        // Act
        await _collector.RecordUserRegistrationAsync("user123", "mobile", 
            new Dictionary<string, object> { ["campaign"] = "summer2023" });

        // Assert
        _repositoryMock.Verify(
            x => x.StoreMetricAsync(It.Is<BusinessMetricData>(m => 
                m.MetricName == "UserRegistration" &&
                m.UserId == "user123" &&
                m.Value == 1 &&
                m.Dimensions.ContainsKey("source") &&
                m.Dimensions.ContainsKey("campaign"))), 
            Times.Once);
    }

    [Fact]
    public async Task RecordUserRegistrationAsync_Should_Not_Process_When_Disabled()
    {
        // Arrange
        _settings.BusinessMetrics.EnableUserMetrics = false;

        // Act
        await _collector.RecordUserRegistrationAsync("user123", "website");

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<KeyValuePair<string, object>[]>()), 
            Times.Never);
    }

    [Fact]
    public async Task RecordUserLoginAsync_Should_Record_Successful_Login()
    {
        // Act
        await _collector.RecordUserLoginAsync("user456", true);

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("user_logins_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "success" && 
                    kvp[0].Value.ToString() == "True")), 
            Times.Once);
    }

    [Fact]
    public async Task RecordUserLoginAsync_Should_Record_Failed_Login_With_Reason()
    {
        // Act
        await _collector.RecordUserLoginAsync("user456", false, "invalid_password");

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("user_logins_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 2 && 
                    kvp.Any(k => k.Key == "success" && k.Value.ToString() == "False") &&
                    kvp.Any(k => k.Key == "failure_reason" && k.Value.ToString() == "invalid_password"))), 
            Times.Once);
    }

    [Fact]
    public async Task RecordOrderCreatedAsync_Should_Record_Order_Metrics()
    {
        // Act
        await _collector.RecordOrderCreatedAsync("order123", 99.99m, "USD", "user789");

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("orders_created_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "currency" && 
                    kvp[0].Value.ToString() == "USD")), 
            Times.Once);

        _metricsServiceMock.Verify(
            x => x.RecordHistogram("order_amount", 99.99, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "currency" && 
                    kvp[0].Value.ToString() == "USD")), 
            Times.Once);
    }

    [Fact]
    public async Task RecordPaymentProcessedAsync_Should_Record_Successful_Payment()
    {
        // Act
        await _collector.RecordPaymentProcessedAsync("payment456", 149.99m, "credit_card", true);

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("payments_processed_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 2 && 
                    kvp.Any(k => k.Key == "method" && k.Value.ToString() == "credit_card") &&
                    kvp.Any(k => k.Key == "success" && k.Value.ToString() == "True"))), 
            Times.Once);

        _metricsServiceMock.Verify(
            x => x.RecordHistogram("payment_amount", 149.99, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "method" && 
                    kvp[0].Value.ToString() == "credit_card")), 
            Times.Once);
    }

    [Fact]
    public async Task RecordPaymentProcessedAsync_Should_Not_Record_Amount_For_Failed_Payment()
    {
        // Act
        await _collector.RecordPaymentProcessedAsync("payment789", 199.99m, "paypal", false);

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("payments_processed_total", 1, It.IsAny<KeyValuePair<string, object>[]>()), 
            Times.Once);

        _metricsServiceMock.Verify(
            x => x.RecordHistogram("payment_amount", It.IsAny<double>(), It.IsAny<KeyValuePair<string, object>[]>()), 
            Times.Never);
    }

    [Fact]
    public async Task RecordFeatureUsageAsync_Should_Record_Feature_Usage()
    {
        // Act
        await _collector.RecordFeatureUsageAsync("advanced_search", "user321", 
            new Dictionary<string, object> { ["filters_count"] = 5 });

        // Assert
        _metricsServiceMock.Verify(
            x => x.IncrementCounter("feature_usage_total", 1, 
                It.Is<KeyValuePair<string, object>[]>(kvp => 
                    kvp.Length == 1 && 
                    kvp[0].Key == "feature" && 
                    kvp[0].Value.ToString() == "advanced_search")), 
            Times.Once);
    }

    [Fact]
    public async Task RecordCustomEventAsync_Should_Record_Custom_Event()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["event_category"] = "user_interaction",
            ["action"] = "button_click",
            ["label"] = "signup_cta"
        };

        // Act
        await _collector.RecordCustomEventAsync("custom_event", properties);

        // Assert
        _metricsServiceMock.Verify(
            x => x.RecordBusinessMetric("custom_event", 1, properties), 
            Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_Should_Return_Empty_Report_Without_Repository()
    {
        // Arrange
        var collectorWithoutRepo = new BusinessMetricsCollector(
            _metricsServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
        
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var report = await collectorWithoutRepo.GenerateReportAsync(from, to);

        // Assert
        report.Should().NotBeNull();
        report.FromDate.Should().Be(from);
        report.ToDate.Should().Be(to);
        report.TotalMetrics.Should().BeEmpty();
        report.TimeSeries.Should().BeEmpty();
        report.DimensionBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateReportAsync_Should_Generate_Report_With_Repository()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        
        var testMetrics = new List<BusinessMetricData>
        {
            new() { MetricName = "UserRegistration", Value = 1, Timestamp = from.AddDays(1) },
            new() { MetricName = "UserRegistration", Value = 1, Timestamp = from.AddDays(2) }
        };

        _repositoryMock.Setup(x => x.GetMetricsAsync("UserRegistration", from, to))
                     .ReturnsAsync(testMetrics);
        _repositoryMock.Setup(x => x.GetMetricsAsync("UserLogin", from, to))
                     .ReturnsAsync(new List<BusinessMetricData>());
        _repositoryMock.Setup(x => x.GetMetricsAsync("OrderCreated", from, to))
                     .ReturnsAsync(new List<BusinessMetricData>());
        _repositoryMock.Setup(x => x.GetMetricsAsync("PaymentProcessed", from, to))
                     .ReturnsAsync(new List<BusinessMetricData>());
        _repositoryMock.Setup(x => x.GetMetricsAsync("FeatureUsage", from, to))
                     .ReturnsAsync(new List<BusinessMetricData>());

        // Act
        var report = await _collector.GenerateReportAsync(from, to);

        // Assert
        report.Should().NotBeNull();
        report.FromDate.Should().Be(from);
        report.ToDate.Should().Be(to);
        report.TotalMetrics.Should().ContainKey("UserRegistration");
        report.TotalMetrics["UserRegistration"].Should().Be(2);
    }
}