using Gateway.API.Middleware;
using Gateway.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Gateway.Tests.Middleware;

public class CircuitBreakerMiddlewareTests
{
    private readonly Mock<ILogger<CircuitBreakerMiddleware>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly GatewayOptions _gatewayOptions;

    public CircuitBreakerMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<CircuitBreakerMiddleware>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions();
        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
    }

    [Fact]
    public async Task InvokeAsync_WithNonProxyPath_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CircuitBreakerMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithIdentityPath_ShouldProcessThroughCircuitBreaker()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/identity/users";
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new CircuitBreakerMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUserPath_ShouldProcessThroughCircuitBreaker()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/users/profile";
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new CircuitBreakerMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithNotificationPath_ShouldProcessThroughCircuitBreaker()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/notifications/send";
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new CircuitBreakerMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public void CircuitBreakerState_RecordSuccess_ShouldIncrementSuccessCount()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act
        state.RecordSuccess();

        // Assert
        Assert.Equal(CircuitState.Closed, state.State);
    }

    [Fact]
    public void CircuitBreakerState_RecordFailure_ShouldIncrementFailureCount()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act
        state.RecordFailure();

        // Assert
        Assert.Equal(CircuitState.Closed, state.State); // Should still be closed with 1 failure
    }

    [Fact]
    public void CircuitBreakerState_MultipleFailures_ShouldOpenCircuit()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act - Record 5 failures to trigger circuit open
        for (int i = 0; i < 5; i++)
        {
            state.RecordFailure();
        }

        // Assert
        Assert.Equal(CircuitState.Open, state.State);
    }

    [Theory]
    [InlineData("/api/identity/login")]
    [InlineData("/api/users/register")]
    [InlineData("/api/notifications/webhook")]
    public async Task InvokeAsync_WithValidProxyPaths_ShouldProcessCorrectly(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new CircuitBreakerMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }
}