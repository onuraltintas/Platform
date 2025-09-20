using Gateway.API.Middleware;
using Gateway.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Text.Json;

namespace Gateway.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly GatewayOptions _gatewayOptions;

    public RateLimitingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions
        {
            RateLimiting = new RateLimitingOptions
            {
                Enabled = true,
                RequestsPerMinute = 5, // Low limit for testing
                RequestsPerHour = 100,
                EnablePerUserLimits = true,
                AuthenticatedUserRequestsPerMinute = 10,
                EndpointLimits = new List<EndpointRateLimit>
                {
                    new EndpointRateLimit
                    {
                        Endpoint = "identity",
                        RequestsPerMinute = 2,
                        RequestsPerHour = 50
                    }
                }
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitingDisabled_ShouldCallNext()
    {
        // Arrange
        _gatewayOptions.RateLimiting.Enabled = false;
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithinRateLimit_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        
        // Check rate limit headers
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Remaining"));
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Reset"));
    }

    [Fact]
    public async Task InvokeAsync_ExceedingRateLimit_ShouldReturnTooManyRequests()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act - Make requests to exceed the limit
        for (int i = 0; i <= _gatewayOptions.RateLimiting.RequestsPerMinute; i++)
        {
            await middleware.InvokeAsync(context);
            
            // Reset response for next iteration
            if (i < _gatewayOptions.RateLimiting.RequestsPerMinute)
            {
                context.Response.Body = new MemoryStream();
                context.Response.StatusCode = 200;
                context.Response.Headers.Clear();
            }
        }

        // Assert
        Assert.Equal(429, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task InvokeAsync_WithApiKey_ShouldUseApiKeyRateLimit()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Add("X-API-Key", "test-api-key-123456");
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        
        // Rate limit should be applied based on API key
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
    }

    [Fact]
    public async Task InvokeAsync_WithIdentityEndpoint_ShouldUseEndpointSpecificLimit()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/identity/login";
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.2");
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act - Make requests to exceed the identity endpoint limit (2 requests per minute)
        await middleware.InvokeAsync(context);
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = 200;
        context.Response.Headers.Clear();
        
        await middleware.InvokeAsync(context);
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = 200;
        context.Response.Headers.Clear();
        
        // This should exceed the identity endpoint limit
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentIPs_ShouldTrackSeparately()
    {
        // Arrange
        var context1 = new DefaultHttpContext();
        context1.Request.Path = "/api/test";
        context1.Response.Body = new MemoryStream();
        context1.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        var context2 = new DefaultHttpContext();
        context2.Request.Path = "/api/test";
        context2.Response.Body = new MemoryStream();
        context2.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.2");
        
        var nextCalled = 0;
        RequestDelegate next = (ctx) =>
        {
            nextCalled++;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        // Assert
        Assert.Equal(2, nextCalled);
        Assert.Equal(200, context1.Response.StatusCode);
        Assert.Equal(200, context2.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/identity/login")]
    [InlineData("/api/users/profile")]
    [InlineData("/api/notifications/send")]
    [InlineData("/health")]
    public async Task InvokeAsync_WithDifferentEndpoints_ShouldApplyCorrectLimits(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
    }

    [Fact]
    public async Task InvokeAsync_WithXForwardedForHeader_ShouldUseForwardedIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Add("X-Forwarded-For", "192.168.1.100, 10.0.0.1");
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        
        // Rate limiting should be based on forwarded IP (192.168.1.100)
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
    }
}