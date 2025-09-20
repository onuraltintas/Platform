using Enterprise.Shared.Resilience.Bulkhead;
using Enterprise.Shared.Resilience.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Resilience.Tests.Bulkhead;

public class BulkheadServiceTests
{
    private readonly Mock<ILogger<BulkheadService>> _loggerMock;
    private readonly IOptions<ResilienceSettings> _settings;
    private readonly BulkheadService _service;

    public BulkheadServiceTests()
    {
        _loggerMock = new Mock<ILogger<BulkheadService>>();
        
        _settings = Options.Create(new ResilienceSettings
        {
            Bulkhead = new BulkheadSettings
            {
                MaxParallelization = 2,
                MaxQueuedActions = 5,
                EnableBulkheadLogging = true
            },
            Timeout = new TimeoutSettings
            {
                DefaultTimeoutMs = 5000
            }
        });

        _service = new BulkheadService(_settings, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new BulkheadService(_settings, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new BulkheadService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new BulkheadService(_settings, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Success_Result_For_Valid_Operation()
    {
        // Arrange
        const string bulkheadKey = "test-bulkhead";
        const string expectedResult = "success";

        // Act
        var result = await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        }, bulkheadKey);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Execute_Void_Operation_Successfully()
    {
        // Arrange
        const string bulkheadKey = "test-bulkhead-void";
        var executed = false;

        // Act
        await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        }, bulkheadKey);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Allow_Concurrent_Operations_Within_Limit()
    {
        // Arrange
        const string bulkheadKey = "concurrent-bulkhead";
        var startedTasks = 0;
        var completedTasks = 0;
        var taskStartedSignal = new SemaphoreSlim(0);
        var continueSignal = new SemaphoreSlim(0);

        // Act - Start two tasks concurrently (within the limit of 2)
        var task1 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref startedTasks);
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
                Interlocked.Increment(ref completedTasks);
            }, bulkheadKey);
        });

        var task2 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref startedTasks);
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
                Interlocked.Increment(ref completedTasks);
            }, bulkheadKey);
        });

        // Wait for both tasks to start
        await taskStartedSignal.WaitAsync();
        await taskStartedSignal.WaitAsync();

        // Assert both tasks are running
        startedTasks.Should().Be(2);
        completedTasks.Should().Be(0);

        // Release both tasks
        continueSignal.Release(2);
        await Task.WhenAll(task1, task2);

        // Assert both completed
        completedTasks.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Reject_Operation_When_At_Capacity()
    {
        // Arrange
        const string bulkheadKey = "capacity-bulkhead";
        var taskStartedSignal = new SemaphoreSlim(0);
        var continueSignal = new SemaphoreSlim(0);

        // Start two long-running operations to fill the bulkhead
        var task1 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
            }, bulkheadKey);
        });

        var task2 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
            }, bulkheadKey);
        });

        // Wait for both tasks to start
        await taskStartedSignal.WaitAsync();
        await taskStartedSignal.WaitAsync();

        // Act & Assert - Third operation should be rejected
        await Assert.ThrowsAsync<BulkheadRejectedException>(() =>
            _service.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                return "should be rejected";
            }, bulkheadKey));

        // Cleanup
        continueSignal.Release(2);
        await Task.WhenAll(task1, task2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Update_Health_Info_On_Success()
    {
        // Arrange
        const string bulkheadKey = "success-bulkhead";

        // Act
        await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return "success";
        }, bulkheadKey);

        // Assert
        var stats = _service.GetBulkheadStats(bulkheadKey);
        stats.TotalExecutions.Should().Be(1);
        stats.TotalFailures.Should().Be(0);
        stats.TotalRejections.Should().Be(0);
        stats.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Update_Health_Info_On_Failure()
    {
        // Arrange
        const string bulkheadKey = "failure-bulkhead";

        // Act & Assert
        try
        {
            await _service.ExecuteAsync<string>(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test failure");
            }, bulkheadKey);
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        var stats = _service.GetBulkheadStats(bulkheadKey);
        stats.TotalExecutions.Should().Be(1);
        stats.TotalFailures.Should().Be(1);
        stats.SuccessRate.Should().Be(0.0);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Update_Health_Info_On_Rejection()
    {
        // Arrange
        const string bulkheadKey = "rejection-bulkhead";
        var taskStartedSignal = new SemaphoreSlim(0);
        var continueSignal = new SemaphoreSlim(0);

        // Fill the bulkhead capacity
        var task1 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
            }, bulkheadKey);
        });

        var task2 = Task.Run(async () =>
        {
            await _service.ExecuteAsync(async () =>
            {
                taskStartedSignal.Release();
                await continueSignal.WaitAsync();
            }, bulkheadKey);
        });

        await taskStartedSignal.WaitAsync();
        await taskStartedSignal.WaitAsync();

        // Act & Assert
        try
        {
            await _service.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                return "rejected";
            }, bulkheadKey);
        }
        catch (BulkheadRejectedException)
        {
            // Expected rejection
        }

        var stats = _service.GetBulkheadStats(bulkheadKey);
        stats.TotalRejections.Should().Be(1);
        stats.RejectionRate.Should().BeGreaterThan(0);

        // Cleanup
        continueSignal.Release(2);
        await Task.WhenAll(task1, task2);
    }

    [Fact]
    public async Task GetHealthInfoAsync_Should_Return_Valid_Health_Info()
    {
        // Arrange
        const string bulkheadKey = "health-bulkhead";

        // Act
        var healthInfo = await _service.GetHealthInfoAsync(bulkheadKey);

        // Assert
        healthInfo.Should().NotBeNull();
        healthInfo.BulkheadKey.Should().Be(bulkheadKey);
        healthInfo.MaxParallelization.Should().Be(2);
        healthInfo.CurrentExecutions.Should().Be(0);
    }

    [Fact]
    public void GetBulkheadStats_Should_Return_Valid_Statistics()
    {
        // Arrange
        const string bulkheadKey = "stats-bulkhead";

        // Act
        var stats = _service.GetBulkheadStats(bulkheadKey);

        // Assert
        stats.Should().NotBeNull();
        stats.BulkheadKey.Should().Be(bulkheadKey);
        stats.MaxParallelization.Should().Be(2);
        stats.AvailableSlots.Should().Be(2);
        stats.TotalExecutions.Should().Be(0);
        stats.TotalRejections.Should().Be(0);
        stats.TotalFailures.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.RejectionRate.Should().Be(0);
    }

    [Fact]
    public void GetAllBulkheadStats_Should_Return_All_Registered_Bulkheads()
    {
        // Arrange
        const string bulkhead1 = "bulkhead-1";
        const string bulkhead2 = "bulkhead-2";

        // Act
        _ = _service.GetBulkheadStats(bulkhead1); // This creates the bulkhead
        _ = _service.GetBulkheadStats(bulkhead2); // This creates the bulkhead
        var allStats = _service.GetAllBulkheadStats();

        // Assert
        allStats.Should().ContainKey(bulkhead1);
        allStats.Should().ContainKey(bulkhead2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        const string bulkheadKey = "cancellation-bulkhead";
        using var cts = new CancellationTokenSource();
        
        // Act
        var task = _service.ExecuteAsync(async () =>
        {
            await Task.Delay(100); // Let the operation start
            cts.Cancel(); // Cancel after starting
            cts.Token.ThrowIfCancellationRequested(); // This should throw
            return "result";
        }, bulkheadKey, cts.Token);

        // Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => task);
        Assert.True(exception is OperationCanceledException || exception is TaskCanceledException);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Track_Execution_Time()
    {
        // Arrange
        const string bulkheadKey = "timing-bulkhead";
        const int delayMs = 100;

        // Act
        await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(delayMs);
            return "timed";
        }, bulkheadKey);

        // Assert
        var stats = _service.GetBulkheadStats(bulkheadKey);
        stats.AverageExecutionTime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(delayMs - 50));
        stats.AverageExecutionTime.Should().BeLessThan(TimeSpan.FromMilliseconds(delayMs + 200));
    }
}