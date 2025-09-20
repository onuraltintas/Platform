namespace Enterprise.Shared.Logging.Tests.Models;

public class LoggingModelsTests
{
    [Fact]
    public void LoggingSettings_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var settings = new LoggingSettings();

        // Assert
        settings.EnableSensitiveDataLogging.Should().BeFalse();
        settings.EnablePerformanceLogging.Should().BeTrue();
        settings.SlowQueryThresholdMs.Should().Be(1000);
        settings.EnableDistributedTracing.Should().BeTrue();
        settings.SamplingRate.Should().Be(0.1);
        settings.MaskingSensitiveFields.Should().NotBeEmpty();
        settings.MaxPropertiesPerEvent.Should().Be(50);
        settings.MaxPropertyLength.Should().Be(2000);
        settings.EnableStructuredLogging.Should().BeTrue();
        settings.EnableCorrelationId.Should().BeTrue();
        settings.EnableUserEnrichment.Should().BeTrue();
        settings.EnableEnvironmentEnrichment.Should().BeTrue();
        settings.ServiceName.Should().Be("Unknown Service");
        settings.ServiceVersion.Should().Be("1.0.0");
        settings.Environment.Should().Be("Development");
    }

    [Fact]
    public void LoggingSettings_SectionName_ShouldBeCorrect()
    {
        // Act & Assert
        LoggingSettings.SectionName.Should().Be("LoggingSettings");
    }

    [Fact]
    public void PerformanceMetrics_FromDuration_ShouldCreateCorrectMetrics()
    {
        // Arrange
        var operationName = "TestOperation";
        var duration = TimeSpan.FromMilliseconds(500);

        // Act
        var metrics = PerformanceMetrics.FromDuration(operationName, duration);

        // Assert
        metrics.OperationName.Should().Be(operationName);
        metrics.Duration.Should().Be(duration);
        metrics.IsSuccessful.Should().BeTrue();
        metrics.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metrics.StartTime.Should().Be(metrics.EndTime.Subtract(duration));
        metrics.AdditionalMetrics.Should().NotBeNull();
    }

    [Fact]
    public void PerformanceMetrics_FromDuration_WithFailure_ShouldSetIsSuccessfulFalse()
    {
        // Arrange
        var operationName = "FailedOperation";
        var duration = TimeSpan.FromMilliseconds(1000);

        // Act
        var metrics = PerformanceMetrics.FromDuration(operationName, duration, false);

        // Assert
        metrics.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void CorrelationContext_Create_ShouldGenerateNewId()
    {
        // Act
        var context = CorrelationContext.Create();

        // Assert
        context.CorrelationId.Should().NotBeNullOrEmpty();
        context.ParentCorrelationId.Should().BeNull();
        context.Properties.Should().NotBeNull();
    }

    [Fact]
    public void CorrelationContext_Create_WithParentId_ShouldSetParentId()
    {
        // Arrange
        var parentId = "parent-123";

        // Act
        var context = CorrelationContext.Create(parentId);

        // Assert
        context.ParentCorrelationId.Should().Be(parentId);
    }

    [Fact]
    public void LogHealthStatus_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var status = new LogHealthStatus();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.IsSeqHealthy.Should().BeFalse();
        status.LogFileAccess.Should().BeFalse();
        status.LastLogEntry.Should().BeNull();
        status.LogVolumeToday.Should().Be(0);
        status.LogLevel.Should().BeEmpty();
        status.ActiveEnrichers.Should().NotBeNull();
        status.ActiveSinks.Should().NotBeNull();
    }

    [Theory]
    [InlineData(LogCategory.Application)]
    [InlineData(LogCategory.Performance)]
    [InlineData(LogCategory.Business)]
    [InlineData(LogCategory.Security)]
    [InlineData(LogCategory.UserActivity)]
    [InlineData(LogCategory.Api)]
    [InlineData(LogCategory.Database)]
    [InlineData(LogCategory.HealthCheck)]
    [InlineData(LogCategory.System)]
    public void LogCategory_AllValues_ShouldBeValid(LogCategory category)
    {
        // Act & Assert
        Enum.IsDefined(typeof(LogCategory), category).Should().BeTrue();
    }

    [Theory]
    [InlineData(SecurityEventType.Authentication)]
    [InlineData(SecurityEventType.Authorization)]
    [InlineData(SecurityEventType.DataAccess)]
    [InlineData(SecurityEventType.ConfigurationChange)]
    [InlineData(SecurityEventType.PrivilegeEscalation)]
    [InlineData(SecurityEventType.SuspiciousActivity)]
    [InlineData(SecurityEventType.PolicyViolation)]
    public void SecurityEventType_AllValues_ShouldBeValid(SecurityEventType eventType)
    {
        // Act & Assert
        Enum.IsDefined(typeof(SecurityEventType), eventType).Should().BeTrue();
    }
}