using Enterprise.Shared.Resilience.CircuitBreaker;
using Enterprise.Shared.Resilience.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Resilience.Tests.CircuitBreaker;

public class PollyCircuitBreakerServiceTests
{
    private readonly Mock<ILogger<PollyCircuitBreakerService>> _loggerMock;
    private readonly IOptions<ResilienceSettings> _settings;
    private readonly PollyCircuitBreakerService _service;

    public PollyCircuitBreakerServiceTests()
    {
        _loggerMock = new Mock<ILogger<PollyCircuitBreakerService>>();
        
        _settings = Options.Create(new ResilienceSettings
        {
            CircuitBreaker = new CircuitBreakerSettings
            {
                FailureThreshold = 50, // 50% failure rate
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(5),
                EnableLogging = true
            }
        });

        _service = new PollyCircuitBreakerService(_settings, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new PollyCircuitBreakerService(_settings, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new PollyCircuitBreakerService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new PollyCircuitBreakerService(_settings, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Success_Result_For_Valid_Operation()
    {
        // Arrange
        const string testKey = "test-circuit";
        const string expectedResult = "success";

        // Act
        var result = await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        }, testKey);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Execute_Void_Operation_Successfully()
    {
        // Arrange
        const string testKey = "test-circuit-void";
        var executed = false;

        // Act
        await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        }, testKey);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Exception_From_Operation()
    {
        // Arrange
        const string testKey = "test-circuit-error";
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync<string>(async () =>
            {
                await Task.Delay(10);
                throw expectedException;
            }, testKey));

        exception.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task GetCircuitBreakerState_Should_Return_Closed_For_New_Circuit()
    {
        // Arrange
        const string testKey = "new-circuit";

        // Act
        var state = _service.GetCircuitBreakerState(testKey);

        // Assert
        state.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void ResetCircuitBreaker_Should_Complete_Without_Exception()
    {
        // Arrange
        const string testKey = "reset-circuit";

        // Act & Assert
        Action act = () => _service.ResetCircuitBreaker(testKey);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsolateCircuitBreaker_Should_Set_State_To_Isolated()
    {
        // Arrange
        const string testKey = "isolate-circuit";

        // Act
        _service.IsolateCircuitBreaker(testKey);
        var healthInfo = _service.GetCircuitBreakerHealthInfo(testKey);

        // Assert
        healthInfo.State.Should().Be(CircuitBreakerState.Isolated);
    }

    [Fact]
    public void CloseCircuitBreaker_Should_Set_State_To_Closed()
    {
        // Arrange
        const string testKey = "close-circuit";

        // Act
        _service.CloseCircuitBreaker(testKey);
        var healthInfo = _service.GetCircuitBreakerHealthInfo(testKey);

        // Assert
        healthInfo.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void GetCircuitBreakerHealthInfo_Should_Return_Valid_Health_Info()
    {
        // Arrange
        const string testKey = "health-circuit";

        // Act
        var healthInfo = _service.GetCircuitBreakerHealthInfo(testKey);

        // Assert
        healthInfo.Should().NotBeNull();
        healthInfo.CircuitBreakerKey.Should().Be(testKey);
        healthInfo.State.Should().Be(CircuitBreakerState.Closed);
        healthInfo.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Update_Health_Info_On_Success()
    {
        // Arrange
        const string testKey = "success-circuit";

        // Act
        await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return "success";
        }, testKey);

        // Assert
        var healthInfo = _service.GetCircuitBreakerHealthInfo(testKey);
        healthInfo.TotalRequests.Should().Be(1);
        healthInfo.SuccessfulRequests.Should().Be(1);
        healthInfo.FailedRequests.Should().Be(0);
        healthInfo.LastSuccessTime.Should().NotBeNull();
        healthInfo.LastSuccessTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Update_Health_Info_On_Failure()
    {
        // Arrange
        const string testKey = "failure-circuit";

        // Act & Assert
        try
        {
            await _service.ExecuteAsync<string>(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test failure");
            }, testKey);
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        var healthInfo = _service.GetCircuitBreakerHealthInfo(testKey);
        healthInfo.TotalRequests.Should().Be(1);
        healthInfo.SuccessfulRequests.Should().Be(0);
        healthInfo.FailedRequests.Should().Be(1);
        healthInfo.LastFailureTime.Should().NotBeNull();
        healthInfo.LastFailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetAllCircuitBreakerStates_Should_Return_All_Registered_Circuits()
    {
        // Arrange
        const string circuit1 = "circuit-1";
        const string circuit2 = "circuit-2";

        // Act
        _service.CloseCircuitBreaker(circuit1);
        _service.IsolateCircuitBreaker(circuit2);
        var allStates = _service.GetAllCircuitBreakerStates();

        // Assert
        allStates.Should().ContainKey(circuit1);
        allStates.Should().ContainKey(circuit2);
        allStates[circuit1].Should().Be(CircuitBreakerState.Closed);
        allStates[circuit2].Should().Be(CircuitBreakerState.Isolated);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        const string testKey = "cancellation-circuit";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ExecuteAsync(async () =>
            {
                await Task.Delay(1000);
                return "result";
            }, testKey, cts.Token));
    }
}