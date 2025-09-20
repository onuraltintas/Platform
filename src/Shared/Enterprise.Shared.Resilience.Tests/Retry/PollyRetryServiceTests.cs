using Enterprise.Shared.Resilience.Models;
using Enterprise.Shared.Resilience.Retry;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Resilience.Tests.Retry;

public class PollyRetryServiceTests
{
    private readonly Mock<ILogger<PollyRetryService>> _loggerMock;
    private readonly IOptions<ResilienceSettings> _settings;
    private readonly PollyRetryService _service;

    public PollyRetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<PollyRetryService>>();
        
        _settings = Options.Create(new ResilienceSettings
        {
            Retry = new RetrySettings
            {
                MaxRetryAttempts = 3,
                BaseDelayMs = 100,
                MaxDelayMs = 1000,
                BackoffType = "Exponential",
                UseJitter = false, // Disable jitter for predictable tests
                EnableRetryLogging = true
            }
        });

        _service = new PollyRetryService(_settings, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act & Assert - Constructor should not throw
        var service = new PollyRetryService(_settings, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        Action act = () => new PollyRetryService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        Action act = () => new PollyRetryService(_settings, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Success_Result_Without_Retry_For_Valid_Operation()
    {
        // Arrange
        const string expectedResult = "success";
        var attemptCount = 0;

        // Act
        var result = await _service.ExecuteAsync(async () =>
        {
            attemptCount++;
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Execute_Void_Operation_Successfully()
    {
        // Arrange
        var executed = false;
        var attemptCount = 0;

        // Act
        await _service.ExecuteAsync(async () =>
        {
            attemptCount++;
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Retry_On_Transient_Exception()
    {
        // Arrange
        var attemptCount = 0;
        const string expectedResult = "success";

        // Act
        var result = await _service.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                await Task.Delay(10);
                throw new HttpRequestException("Transient error");
            }
            return expectedResult;
        });

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(3); // First attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Retry_On_Business_Exception()
    {
        // Arrange
        var attemptCount = 0;
        var businessException = new BusinessRuleException("test-rule");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ExecuteAsync<string>(async () =>
            {
                attemptCount++;
                await Task.Delay(10);
                throw businessException;
            }));

        exception.Should().BeSameAs(businessException);
        attemptCount.Should().Be(1); // No retries for business exceptions
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Retry_On_Validation_Exception()
    {
        // Arrange
        var attemptCount = 0;
        var validationException = new ValidationException("test-field");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ExecuteAsync<string>(async () =>
            {
                attemptCount++;
                await Task.Delay(10);
                throw validationException;
            }));

        exception.Should().BeSameAs(validationException);
        attemptCount.Should().Be(1); // No retries for validation exceptions
    }

    [Fact]
    public async Task ExecuteAsync_Should_Exhaust_All_Retry_Attempts_For_Persistent_Error()
    {
        // Arrange
        var attemptCount = 0;
        var persistentException = new TimeoutException("Persistent timeout");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            _service.ExecuteAsync<string>(async () =>
            {
                attemptCount++;
                await Task.Delay(10);
                throw persistentException;
            }));

        exception.Should().BeSameAs(persistentException);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    [Fact]
    public async Task ExecuteWithCustomPolicyAsync_Should_Use_Custom_Retry_Policy()
    {
        // Arrange
        var customPolicy = new RetryPolicy
        {
            MaxAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            BackoffType = "Linear",
            UseJitter = false
        };

        var attemptCount = 0;
        const string expectedResult = "custom-success";

        // Act
        var result = await _service.ExecuteWithCustomPolicyAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                await Task.Delay(10);
                throw new HttpRequestException("Transient error");
            }
            return expectedResult;
        }, customPolicy);

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(2); // Initial attempt + 1 retry (max 2 attempts)
    }

    [Fact]
    public async Task ExecuteWithCustomPolicyAsync_Should_Execute_Void_Operation_With_Custom_Policy()
    {
        // Arrange
        var customPolicy = new RetryPolicy
        {
            MaxAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(50)
        };

        var attemptCount = 0;
        var executed = false;

        // Act
        await _service.ExecuteWithCustomPolicyAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                await Task.Delay(10);
                throw new TaskCanceledException("Transient error");
            }
            executed = true;
        }, customPolicy);

        // Assert
        executed.Should().BeTrue();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithCustomPolicyAsync_Should_Use_Custom_ShouldRetry_Predicate()
    {
        // Arrange
        var customPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            ShouldRetry = ex => ex is InvalidOperationException // Only retry on InvalidOperationException
        };

        var attemptCount = 0;

        // Act & Assert - Should retry on InvalidOperationException
        var result = await _service.ExecuteWithCustomPolicyAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                await Task.Delay(5);
                throw new InvalidOperationException("Should retry");
            }
            return "success";
        }, customPolicy);

        result.Should().Be("success");
        attemptCount.Should().Be(2);

        // Reset for second test
        attemptCount = 0;

        // Act & Assert - Should NOT retry on ArgumentException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ExecuteWithCustomPolicyAsync<string>(async () =>
            {
                attemptCount++;
                await Task.Delay(5);
                throw new ArgumentException("Should not retry");
            }, customPolicy));

        attemptCount.Should().Be(1); // No retries
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ExecuteAsync(async () =>
            {
                await Task.Delay(1000);
                return "result";
            }, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Specified_Retry_Key()
    {
        // Arrange
        const string retryKey = "custom-retry-key";
        const string expectedResult = "keyed-success";

        // Act
        var result = await _service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        }, retryKey);

        // Assert
        result.Should().Be(expectedResult);
    }
}