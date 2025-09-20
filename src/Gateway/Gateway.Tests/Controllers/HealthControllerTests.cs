using Gateway.API.Controllers;
using Gateway.Core.Interfaces;
using Gateway.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Enterprise.Shared.Common.Models;

namespace Gateway.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IServiceHealthService> _mockHealthService;
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockHealthService = new Mock<IServiceHealthService>();
        _mockLogger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_mockHealthService.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetHealth_ShouldReturnOkWithHealthInfo()
    {
        // Act
        var result = _controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the health info structure
        var healthInfo = okResult.Value;
        var healthInfoType = healthInfo.GetType();
        
        Assert.NotNull(healthInfoType.GetProperty("Status"));
        Assert.NotNull(healthInfoType.GetProperty("Timestamp"));
        Assert.NotNull(healthInfoType.GetProperty("Service"));
        Assert.NotNull(healthInfoType.GetProperty("Version"));
        Assert.NotNull(healthInfoType.GetProperty("Environment"));
    }

    [Fact]
    public async Task GetDetailedHealth_WithHealthyServices_ShouldReturnOk()
    {
        // Arrange
        var healthyService = new ServiceHealthResult
        {
            ServiceName = "Identity Service",
            Endpoint = "https://localhost:5001/health",
            IsHealthy = true,
            ResponseTime = TimeSpan.FromMilliseconds(100),
            Message = "Service is healthy",
            CheckedAt = DateTime.UtcNow
        };

        var healthResults = new List<ServiceHealthResult> { healthyService };
        _mockHealthService.Setup(x => x.GetAllServiceHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ServiceHealthResult>>.Success(healthResults));

        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetDetailedHealth_WithUnhealthyServices_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var unhealthyService = new ServiceHealthResult
        {
            ServiceName = "Identity Service",
            Endpoint = "https://localhost:5001/health",
            IsHealthy = false,
            ResponseTime = TimeSpan.FromSeconds(30),
            Message = "Service is not responding",
            CheckedAt = DateTime.UtcNow
        };

        var healthResults = new List<ServiceHealthResult> { unhealthyService };
        _mockHealthService.Setup(x => x.GetAllServiceHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ServiceHealthResult>>.Success(healthResults));

        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task GetDetailedHealth_WithServiceFailure_ShouldReturnServiceUnavailable()
    {
        // Arrange
        _mockHealthService.Setup(x => x.GetAllServiceHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ServiceHealthResult>>.Failure("Service health check failed"));

        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task GetDetailedHealth_WithException_ShouldReturnServiceUnavailable()
    {
        // Arrange
        _mockHealthService.Setup(x => x.GetAllServiceHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task GetServiceHealth_ShouldReturnOkWithServiceInfo()
    {
        // Arrange
        var serviceName = "identity";

        // Act
        var result = await _controller.GetServiceHealth(serviceName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the service health info structure
        var serviceInfo = okResult.Value;
        var serviceInfoType = serviceInfo.GetType();
        
        Assert.NotNull(serviceInfoType.GetProperty("ServiceName"));
        Assert.NotNull(serviceInfoType.GetProperty("Status"));
        Assert.NotNull(serviceInfoType.GetProperty("Timestamp"));
        Assert.NotNull(serviceInfoType.GetProperty("Message"));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("user")]
    [InlineData("notification")]
    public async Task GetServiceHealth_WithDifferentServiceNames_ShouldReturnOk(string serviceName)
    {
        // Act
        var result = await _controller.GetServiceHealth(serviceName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}