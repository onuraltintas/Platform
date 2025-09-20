namespace Enterprise.Shared.Logging.Tests.Services;

public class EnterpriseLoggerTests
{
    private readonly ILogger<TestClass> _mockLogger;
    private readonly LoggingSettings _settings;
    private readonly ICorrelationContextAccessor _mockCorrelationAccessor;
    private readonly EnterpriseLogger<TestClass> _enterpriseLogger;

    public EnterpriseLoggerTests()
    {
        _mockLogger = Substitute.For<ILogger<TestClass>>();
        _mockCorrelationAccessor = Substitute.For<ICorrelationContextAccessor>();
        
        _settings = new LoggingSettings
        {
            EnablePerformanceLogging = true,
            EnableSensitiveDataLogging = false,
            SlowQueryThresholdMs = 1000,
            MaxPropertiesPerEvent = 50,
            MaxPropertyLength = 2000,
            MaskingSensitiveFields = new List<string> { "password", "secret" }
        };

        var options = Options.Create(_settings);
        _enterpriseLogger = new EnterpriseLogger<TestClass>(_mockLogger, options, _mockCorrelationAccessor);
    }

    [Fact]
    public void LogPerformance_WhenEnabled_ShouldLogInformationForFastOperation()
    {
        // Arrange
        var operationName = "FastOperation";
        var duration = TimeSpan.FromMilliseconds(500);
        var properties = new Dictionary<string, object> { ["TestProp"] = "TestValue" };

        // Act
        _enterpriseLogger.LogPerformance(operationName, duration, properties);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("FastOperation")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogPerformance_WhenSlowOperation_ShouldLogWarning()
    {
        // Arrange
        var operationName = "SlowOperation";
        var duration = TimeSpan.FromMilliseconds(1500);

        // Act
        _enterpriseLogger.LogPerformance(operationName, duration);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("SlowOperation")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogPerformance_WhenDisabled_ShouldNotLog()
    {
        // Arrange
        _settings.EnablePerformanceLogging = false;
        var operationName = "AnyOperation";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        _enterpriseLogger.LogPerformance(operationName, duration);

        // Assert
        _mockLogger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogBusinessEvent_ShouldLogInformation()
    {
        // Arrange
        var eventName = "UserRegistered";
        var properties = new Dictionary<string, object> 
        { 
            ["UserId"] = "123",
            ["Email"] = "test@example.com"
        };

        // Act
        _enterpriseLogger.LogBusinessEvent(eventName, properties);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("UserRegistered")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogSecurityEvent_ShouldLogWarning()
    {
        // Arrange
        var eventType = "UnauthorizedAccess";
        var properties = new Dictionary<string, object>
        {
            ["UserId"] = "123",
            ["Resource"] = "/admin/panel"
        };

        // Act
        _enterpriseLogger.LogSecurityEvent(eventType, properties);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("UnauthorizedAccess")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogUserActivity_ShouldLogInformation()
    {
        // Arrange
        var action = "Login";
        var userId = "user123";
        var properties = new Dictionary<string, object> { ["IP"] = "192.168.1.1" };

        // Act
        _enterpriseLogger.LogUserActivity(action, userId, properties);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Login") && x.ToString()!.Contains("user123")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogApiCall_WithSuccessStatus_ShouldLogInformation()
    {
        // Arrange
        var method = "GET";
        var endpoint = "/api/users";
        var duration = TimeSpan.FromMilliseconds(200);
        var statusCode = 200;

        // Act
        _enterpriseLogger.LogApiCall(method, endpoint, duration, statusCode);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("GET") && x.ToString()!.Contains("/api/users")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogApiCall_WithErrorStatus_ShouldLogWarning()
    {
        // Arrange
        var method = "POST";
        var endpoint = "/api/orders";
        var duration = TimeSpan.FromMilliseconds(300);
        var statusCode = 500;

        // Act
        _enterpriseLogger.LogApiCall(method, endpoint, duration, statusCode);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("POST") && x.ToString()!.Contains("/api/orders")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDatabaseOperation_ShouldLogDebugForFastQuery()
    {
        // Arrange
        var operation = "SELECT";
        var commandText = "SELECT * FROM Users WHERE Id = @id";
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        _enterpriseLogger.LogDatabaseOperation(operation, commandText, duration);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("SELECT")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDatabaseOperation_ShouldLogWarningForSlowQuery()
    {
        // Arrange
        var operation = "SELECT";
        var commandText = "SELECT * FROM Users";
        var duration = TimeSpan.FromMilliseconds(2000);

        // Act
        _enterpriseLogger.LogDatabaseOperation(operation, commandText, duration);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("SELECT")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogException_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "UserService.GetUser";
        var properties = new Dictionary<string, object> { ["UserId"] = "123" };

        // Act
        _enterpriseLogger.LogException(exception, context, properties);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("UserService.GetUser")),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogHealthCheck_HealthyComponent_ShouldLogInformation()
    {
        // Arrange
        var componentName = "Database";
        var isHealthy = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        _enterpriseLogger.LogHealthCheck(componentName, isHealthy, duration);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Database") && x.ToString()!.Contains("Healthy")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogHealthCheck_UnhealthyComponent_ShouldLogWarning()
    {
        // Arrange
        var componentName = "Redis";
        var isHealthy = false;
        var duration = TimeSpan.FromMilliseconds(5000);

        // Act
        _enterpriseLogger.LogHealthCheck(componentName, isHealthy, duration);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Redis") && x.ToString()!.Contains("Unhealthy")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void BeginTimedScope_ShouldReturnDisposableScope()
    {
        // Arrange
        var operationName = "TestOperation";
        var properties = new Dictionary<string, object> { ["TestProp"] = "TestValue" };

        // Act
        using var scope = _enterpriseLogger.BeginTimedScope(operationName, properties) as ITimedOperationScope;

        // Assert
        scope.Should().NotBeNull();
        scope!.OperationName.Should().Be(operationName);
        scope.Properties.Should().ContainKey("TestProp");
    }

    [Fact]
    public void MaskSensitiveData_ShouldMaskPasswordFields()
    {
        // Arrange
        var properties = new Dictionary<string, object> 
        { 
            ["Username"] = "testuser",
            ["Password"] = "secretpassword123",
            ["Email"] = "test@example.com"
        };

        // Act
        _enterpriseLogger.LogBusinessEvent("Login", properties);

        // Assert
        _mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => !x.ToString()!.Contains("secretpassword123")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void WithCorrelationContext_ShouldIncludeCorrelationId()
    {
        // Arrange
        var correlationContext = new CorrelationContext
        {
            CorrelationId = "test-correlation-id",
            UserId = "user123"
        };
        _mockCorrelationAccessor.CorrelationContext.Returns(correlationContext);

        // Act
        _enterpriseLogger.LogBusinessEvent("TestEvent");

        // Assert
        _mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("test-correlation-id")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // Helper test class
    public class TestClass { }
}