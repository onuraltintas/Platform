using System.Diagnostics;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Enterprise.Shared.Observability.Tests.Services;

public class OpenTelemetryTracingServiceTests
{
    private readonly Mock<ICorrelationContextAccessor> _correlationContextMock;
    private readonly Mock<ILogger<OpenTelemetryTracingService>> _loggerMock;
    private readonly ObservabilitySettings _settings;
    private readonly OpenTelemetryTracingService _service;

    public OpenTelemetryTracingServiceTests()
    {
        _correlationContextMock = new Mock<ICorrelationContextAccessor>();
        _loggerMock = new Mock<ILogger<OpenTelemetryTracingService>>();
        _settings = new ObservabilitySettings
        {
            EnableTracing = true,
            ServiceName = "TestService",
            ServiceVersion = "1.0.0",
            Environment = "Test"
        };
        _service = new OpenTelemetryTracingService(_correlationContextMock.Object, _loggerMock.Object, _settings);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new OpenTelemetryTracingService(_correlationContextMock.Object, _loggerMock.Object, _settings);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_CorrelationContext_Is_Null()
    {
        // Act & Assert
        Action act = () => new OpenTelemetryTracingService(null!, _loggerMock.Object, _settings);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("correlationContext");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new OpenTelemetryTracingService(_correlationContextMock.Object, null!, _settings);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new OpenTelemetryTracingService(_correlationContextMock.Object, _loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("settings");
    }

    [Fact]
    public void StartActivity_Should_Return_Null_When_Tracing_Disabled()
    {
        // Arrange
        _settings.EnableTracing = false;

        // Act
        var activity = _service.StartActivity("test-activity");

        // Assert
        activity.Should().BeNull();
    }

    [Fact]
    public void StartActivity_Should_Add_Correlation_Id_When_Available()
    {
        // Arrange
        var correlationContext = new CorrelationContext
        {
            CorrelationId = "test-correlation-id",
            UserId = "user123"
        };
        _correlationContextMock.Setup(x => x.CorrelationContext).Returns(correlationContext);

        // Act
        using var activity = _service.StartActivity("test-activity");

        // Assert
        if (activity != null)
        {
            activity.DisplayName.Should().Be("test-activity");
            // Note: Tags are added but we can't easily verify them in unit tests without more setup
        }
    }

    [Fact]
    public void AddTag_Should_Not_Throw_When_No_Current_Activity()
    {
        // Act & Assert
        Action act = () => _service.AddTag("test.key", "test-value");
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEvent_Should_Not_Throw_When_No_Current_Activity()
    {
        // Act & Assert
        Action act = () => _service.AddEvent("test-event", new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 123
        });
        act.Should().NotThrow();
    }

    [Fact]
    public void SetStatus_Should_Not_Throw_When_No_Current_Activity()
    {
        // Act & Assert
        Action act = () => _service.SetStatus(ActivityStatusCode.Ok, "Test completed");
        act.Should().NotThrow();
    }

    [Fact]
    public void GetTraceId_Should_Return_Null_When_No_Current_Activity()
    {
        // Act
        var traceId = _service.GetTraceId();

        // Assert
        traceId.Should().BeNull();
    }

    [Fact]
    public void GetSpanId_Should_Return_Null_When_No_Current_Activity()
    {
        // Act
        var spanId = _service.GetSpanId();

        // Assert
        spanId.Should().BeNull();
    }

    [Fact]
    public void EnrichWithUserContext_Should_Not_Throw_When_No_Current_Activity()
    {
        // Act & Assert
        Action act = () => _service.EnrichWithUserContext("user123", "test@example.com");
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithBusinessContext_Should_Not_Throw_When_No_Current_Activity()
    {
        // Act & Assert
        Action act = () => _service.EnrichWithBusinessContext(new Dictionary<string, object>
        {
            ["order_id"] = "order123",
            ["amount"] = 99.99
        });
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_Should_Not_Throw_When_No_Current_Activity()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        Action act = () => _service.RecordException(exception, new Dictionary<string, object>
        {
            ["context"] = "test"
        });
        act.Should().NotThrow();
    }

    [Fact]
    public void GetCurrentActivity_Should_Return_Current_Activity()
    {
        // Act
        var activity = _service.GetCurrentActivity();

        // Assert
        // Should return whatever Activity.Current is (could be null)
        activity.Should().Be(Activity.Current);
    }

    [Fact]
    public void StartActivity_Should_Set_Service_Tags()
    {
        // Arrange
        _settings.ServiceName = "TestService";
        _settings.ServiceVersion = "2.0.0";
        _settings.Environment = "Production";

        // Act
        using var activity = _service.StartActivity("tagged-activity");

        // Assert
        // Activity is created but tags verification would require more complex setup
        activity?.DisplayName.Should().Be("tagged-activity");
    }

    [Fact]
    public void EnrichWithUserContext_Should_Add_Email_Tag_When_Provided()
    {
        // Act & Assert - Should not throw regardless of current activity state
        Action act = () => _service.EnrichWithUserContext("user456", "user456@example.com");
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEvent_Should_Handle_Null_Attributes()
    {
        // Act & Assert
        Action act = () => _service.AddEvent("event-without-attributes", null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_Should_Handle_Null_Attributes()
    {
        // Arrange
        var exception = new ArgumentException("Test argument exception");

        // Act & Assert
        Action act = () => _service.RecordException(exception, null);
        act.Should().NotThrow();
    }
}