namespace Enterprise.Shared.Logging.Tests.Extensions;

public class TimedOperationScopeTests
{
    [Fact]
    public void TimedOperationScope_ShouldTrackElapsedTime()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);
        var operationName = "TestOperation";

        // Act
        using (var scope = enterpriseLogger.BeginTimedScope(operationName) as ITimedOperationScope)
        {
            // Wait a small amount to ensure elapsed time > 0
            Thread.Sleep(10);
            
            // Assert during scope
            scope!.OperationName.Should().Be(operationName);
            scope.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
            scope.Properties.Should().NotBeNull();
        }

        // Assert after disposal - should have logged performance
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("TestOperation")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TimedOperationScope_AddProperty_ShouldAddToProperties()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);

        // Act
        using (var scope = enterpriseLogger.BeginTimedScope("TestOperation") as ITimedOperationScope)
        {
            scope!.AddProperty("TestKey", "TestValue");
            
            // Assert
            scope.Properties.Should().ContainKey("TestKey");
            scope.Properties["TestKey"].Should().Be("TestValue");
        }
    }

    [Fact]
    public void TimedOperationScope_MarkAsFailed_ShouldSetFailedState()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);
        var exception = new InvalidOperationException("Test error");

        // Act
        using (var scope = enterpriseLogger.BeginTimedScope("TestOperation"))
        {
            (scope as ITimedOperationScope)!.MarkAsFailed(exception);
        }

        // Assert - should have logged with failure information
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Success") && x.ToString()!.Contains("False")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TimedOperationScope_WithInitialProperties_ShouldIncludeThemInLog()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);
        var initialProperties = new Dictionary<string, object>
        {
            ["InitialProp"] = "InitialValue"
        };

        // Act
        using (var scope = enterpriseLogger.BeginTimedScope("TestOperation", initialProperties))
        {
            // Scope automatically disposes and logs
        }

        // Assert
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("InitialProp")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TimedOperationScope_MultipleDispose_ShouldNotLogMultipleTimes()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);

        // Act
        var scope = enterpriseLogger.BeginTimedScope("TestOperation");
        scope.Dispose();
        scope.Dispose(); // Second dispose should be ignored

        // Assert - should only log once
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TimedOperationScope_WithException_ShouldIncludeErrorInProperties()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        var settings = Options.Create(new LoggingSettings { EnablePerformanceLogging = true });
        var enterpriseLogger = new EnterpriseLogger<TestClass>(mockLogger, settings);
        var exception = new ArgumentException("Test exception message");

        // Act
        using (var scope = enterpriseLogger.BeginTimedScope("TestOperation"))
        {
            (scope as ITimedOperationScope)!.MarkAsFailed(exception);
        }

        // Assert
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Test exception message")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    public class TestClass { }
}