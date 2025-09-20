using Gateway.Core.Configuration;
using Xunit;

namespace Gateway.Tests.Configuration;

public class GatewayOptionsTests
{
    [Fact]
    public void GatewayOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var options = new GatewayOptions();

        // Assert
        Assert.Equal("Development", options.Environment);
        Assert.Equal(5000, options.Port);
        Assert.True(options.EnableSwagger);
        Assert.True(options.EnableRequestLogging);
        Assert.True(options.EnableResponseLogging);
        Assert.True(options.EnableHealthChecks);
        Assert.True(options.EnableMetrics);
        Assert.NotNull(options.Security);
        Assert.NotNull(options.RateLimiting);
        Assert.NotNull(options.Cors);
        Assert.NotNull(options.DownstreamServices);
    }

    [Fact]
    public void SecurityOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var options = new SecurityOptions();

        // Assert
        Assert.Empty(options.JwtSecret);
        Assert.Empty(options.JwtIssuer);
        Assert.Empty(options.JwtAudience);
        Assert.True(options.RequireHttps);
        Assert.False(options.EnableApiKeyAuthentication);
        Assert.Equal(60, options.TokenExpirationMinutes);
        Assert.Equal(10 * 1024 * 1024, options.MaxRequestSizeBytes); // 10MB
    }

    [Fact]
    public void RateLimitingOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var options = new RateLimitingOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(100, options.RequestsPerMinute);
        Assert.Equal(1000, options.RequestsPerHour);
        Assert.Equal(10000, options.RequestsPerDay);
        Assert.True(options.EnablePerUserLimits);
        Assert.Equal(200, options.AuthenticatedUserRequestsPerMinute);
        Assert.NotNull(options.EndpointLimits);
        Assert.Empty(options.EndpointLimits);
    }

    [Fact]
    public void CorsOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var options = new CorsOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Single(options.AllowedOrigins);
        Assert.Equal("https://localhost:3000", options.AllowedOrigins[0]);
        Assert.Equal(6, options.AllowedMethods.Count);
        Assert.Contains("GET", options.AllowedMethods);
        Assert.Contains("POST", options.AllowedMethods);
        Assert.Contains("PUT", options.AllowedMethods);
        Assert.Contains("DELETE", options.AllowedMethods);
        Assert.Contains("PATCH", options.AllowedMethods);
        Assert.Contains("OPTIONS", options.AllowedMethods);
        Assert.Single(options.AllowedHeaders);
        Assert.Equal("*", options.AllowedHeaders[0]);
        Assert.True(options.AllowCredentials);
        Assert.Equal(86400, options.PreflightMaxAge); // 24 hours
    }

    [Fact]
    public void IdentityServiceOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var options = new IdentityServiceOptions();

        // Assert
        Assert.Empty(options.BaseUrl);
        Assert.Equal("/health", options.HealthEndpoint);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(3, options.RetryCount);
        Assert.True(options.EnableCircuitBreaker);
        Assert.Equal(0.5, options.FailureThreshold);
        Assert.Equal(60, options.CircuitBreakerTimeoutSeconds);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void GatewayOptions_Environment_ShouldAcceptValidValues(string environment)
    {
        // Arrange & Act
        var options = new GatewayOptions
        {
            Environment = environment
        };

        // Assert
        Assert.Equal(environment, options.Environment);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void GatewayOptions_Port_ShouldAcceptValidValues(int port)
    {
        // Arrange & Act
        var options = new GatewayOptions
        {
            Port = port
        };

        // Assert
        Assert.Equal(port, options.Port);
    }
}