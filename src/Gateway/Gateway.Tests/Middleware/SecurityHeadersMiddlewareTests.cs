using Gateway.API.Middleware;
using Gateway.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Gateway.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<ILogger<SecurityHeadersMiddleware>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly GatewayOptions _gatewayOptions;

    public SecurityHeadersMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<SecurityHeadersMiddleware>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions
        {
            Environment = "Development",
            Security = new SecurityOptions
            {
                RequireHttps = true
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddBasicSecurityHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        
        // Check basic security headers
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        
        Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
        
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddContentSecurityPolicy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
    }

    [Fact]
    public async Task InvokeAsync_WithHttpsRequest_ShouldAddHSTS()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        var hsts = context.Response.Headers["Strict-Transport-Security"].ToString();
        Assert.Contains("max-age=31536000", hsts);
        Assert.Contains("includeSubDomains", hsts);
        Assert.Contains("preload", hsts);
    }

    [Fact]
    public async Task InvokeAsync_WithHttpRequest_ShouldNotAddHSTS()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddPermissionsPolicy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Permissions-Policy"));
        var permissionsPolicy = context.Response.Headers["Permissions-Policy"].ToString();
        
        Assert.Contains("geolocation=()", permissionsPolicy);
        Assert.Contains("microphone=()", permissionsPolicy);
        Assert.Contains("camera=()", permissionsPolicy);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCrossOriginHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"));
        Assert.Equal("require-corp", context.Response.Headers["Cross-Origin-Embedder-Policy"]);
        
        Assert.True(context.Response.Headers.ContainsKey("Cross-Origin-Opener-Policy"));
        Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Opener-Policy"]);
        
        Assert.True(context.Response.Headers.ContainsKey("Cross-Origin-Resource-Policy"));
        Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Resource-Policy"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRemoveServerHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        // Add headers that should be removed
        context.Response.Headers.Add("Server", "Microsoft-IIS/10.0");
        context.Response.Headers.Add("X-Powered-By", "ASP.NET");
        context.Response.Headers.Add("X-AspNet-Version", "4.0.30319");
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Server"));
        Assert.False(context.Response.Headers.ContainsKey("X-Powered-By"));
        Assert.False(context.Response.Headers.ContainsKey("X-AspNet-Version"));
        Assert.False(context.Response.Headers.ContainsKey("X-AspNetMvc-Version"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddPostProcessingHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-request-123";
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Gateway-Processed"));
        Assert.Equal("true", context.Response.Headers["X-Gateway-Processed"]);
        
        Assert.True(context.Response.Headers.ContainsKey("X-Gateway-Version"));
        Assert.Equal("1.0.0", context.Response.Headers["X-Gateway-Version"]);
        
        Assert.True(context.Response.Headers.ContainsKey("X-Request-ID"));
        Assert.Equal("test-request-123", context.Response.Headers["X-Request-ID"]);
        
        Assert.True(context.Response.Headers.ContainsKey("X-Gateway-Security"));
        Assert.Equal("enabled", context.Response.Headers["X-Gateway-Security"]);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/identity/login")]
    [InlineData("/api/users/profile")]
    [InlineData("/health/detailed")]
    public async Task InvokeAsync_WithSecuritySensitiveEndpoints_ShouldAddNoCacheHeaders(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
        Assert.Equal("no-cache, no-store, must-revalidate", context.Response.Headers["Cache-Control"]);
        
        Assert.True(context.Response.Headers.ContainsKey("Pragma"));
        Assert.Equal("no-cache", context.Response.Headers["Pragma"]);
        
        Assert.True(context.Response.Headers.ContainsKey("Expires"));
        Assert.Equal("0", context.Response.Headers["Expires"]);
    }

    [Fact]
    public async Task InvokeAsync_WithJsonContentType_ShouldAddContentTypeValidation()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.Response.ContentType = "application/json";
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Validation"));
        Assert.Equal("strict", context.Response.Headers["X-Content-Type-Validation"]);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_ShouldAddEnvironmentHeader()
    {
        // Arrange
        _gatewayOptions.Environment = "Development";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Gateway-Environment"));
        Assert.Equal("Development", context.Response.Headers["X-Gateway-Environment"]);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_ShouldNotAddEnvironmentHeader()
    {
        // Arrange
        _gatewayOptions.Environment = "Production";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, _mockOptions.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("X-Gateway-Environment"));
    }
}