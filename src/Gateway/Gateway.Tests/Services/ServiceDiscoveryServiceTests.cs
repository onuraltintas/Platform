using Gateway.Core.Services;
using Gateway.Core.Configuration;
using Gateway.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gateway.Tests.Services;

public class ServiceDiscoveryServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<ServiceDiscoveryService>> _mockLogger;
    private readonly Mock<IOptions<GatewayOptions>> _mockOptions;
    private readonly ServiceDiscoveryService _service;
    private readonly GatewayOptions _gatewayOptions;

    public ServiceDiscoveryServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<ServiceDiscoveryService>>();
        _mockOptions = new Mock<IOptions<GatewayOptions>>();
        
        _gatewayOptions = new GatewayOptions
        {
            DownstreamServices = new DownstreamServicesOptions
            {
                Identity = new IdentityServiceOptions
                {
                    BaseUrl = "https://localhost:5001",
                    HealthEndpoint = "/health",
                    TimeoutSeconds = 30,
                    RetryCount = 3,
                    EnableCircuitBreaker = true,
                    FailureThreshold = 0.5,
                    CircuitBreakerTimeoutSeconds = 60
                },
                User = new IdentityServiceOptions
                {
                    BaseUrl = "https://localhost:5002",
                    HealthEndpoint = "/health",
                    TimeoutSeconds = 30,
                    RetryCount = 3,
                    EnableCircuitBreaker = true,
                    FailureThreshold = 0.5,
                    CircuitBreakerTimeoutSeconds = 60
                },
                Notification = new IdentityServiceOptions
                {
                    BaseUrl = "https://localhost:5003",
                    HealthEndpoint = "/health",
                    TimeoutSeconds = 25,
                    RetryCount = 2,
                    EnableCircuitBreaker = true,
                    FailureThreshold = 0.3,
                    CircuitBreakerTimeoutSeconds = 45
                }
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_gatewayOptions);
        
        var httpClient = new HttpClient();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _service = new ServiceDiscoveryService(httpClient, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task DiscoverServicesAsync_ShouldReturnConfiguredServices()
    {
        // Act
        var result = await _service.DiscoverServicesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var services = result.Value.ToList();
        Assert.Equal(3, services.Count);
        
        Assert.Contains(services, s => s.ServiceName == "Identity" && s.BaseUrl == "https://localhost:5001");
        Assert.Contains(services, s => s.ServiceName == "User" && s.BaseUrl == "https://localhost:5002");
        Assert.Contains(services, s => s.ServiceName == "Notification" && s.BaseUrl == "https://localhost:5003");
    }

    [Fact]
    public async Task GetServiceEndpointAsync_WithValidServiceName_ShouldReturnEndpoint()
    {
        // Arrange
        await _service.DiscoverServicesAsync();

        // Act
        var result = await _service.GetServiceEndpointAsync("Identity");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Identity", result.Value.ServiceName);
        Assert.Equal("https://localhost:5001", result.Value.BaseUrl);
    }

    [Fact]
    public async Task GetServiceEndpointAsync_WithInvalidServiceName_ShouldReturnFailure()
    {
        // Arrange
        await _service.DiscoverServicesAsync();

        // Act
        var result = await _service.GetServiceEndpointAsync("NonExistentService");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task RegisterServiceAsync_ShouldAddServiceToRegistry()
    {
        // Arrange
        var newService = new ServiceEndpoint
        {
            ServiceName = "TestService",
            BaseUrl = "https://localhost:5004",
            HealthEndpoint = "/health",
            IsHealthy = true
        };

        // Act
        var result = await _service.RegisterServiceAsync(newService);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify service was added
        var retrieveResult = await _service.GetServiceEndpointAsync("TestService");
        Assert.True(retrieveResult.IsSuccess);
        Assert.Equal("TestService", retrieveResult.Value!.ServiceName);
    }

    [Fact]
    public async Task DeregisterServiceAsync_ShouldRemoveServiceFromRegistry()
    {
        // Arrange
        var service = new ServiceEndpoint
        {
            ServiceName = "TestService",
            BaseUrl = "https://localhost:5004",
            HealthEndpoint = "/health",
            IsHealthy = true
        };

        await _service.RegisterServiceAsync(service);

        // Act
        var result = await _service.DeregisterServiceAsync("TestService", "https://localhost:5004");

        // Assert
        Assert.True(result.IsSuccess);

        // Verify service was removed
        var retrieveResult = await _service.GetServiceEndpointAsync("TestService");
        Assert.False(retrieveResult.IsSuccess);
    }

    [Fact]
    public async Task GetAllServicesAsync_ShouldReturnAllRegisteredServices()
    {
        // Arrange
        await _service.DiscoverServicesAsync();

        var additionalService = new ServiceEndpoint
        {
            ServiceName = "TestService",
            BaseUrl = "https://localhost:5004",
            HealthEndpoint = "/health",
            IsHealthy = true
        };
        await _service.RegisterServiceAsync(additionalService);

        // Act
        var result = await _service.GetAllServicesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var services = result.Value.ToList();
        Assert.Equal(4, services.Count); // 3 configured + 1 additional
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("User")]
    [InlineData("Notification")]
    public async Task GetServiceEndpointAsync_WithValidServices_ShouldReturnCorrectMetadata(string serviceName)
    {
        // Arrange
        await _service.DiscoverServicesAsync();

        // Act
        var result = await _service.GetServiceEndpointAsync(serviceName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Metadata);
        Assert.True(result.Value.Metadata.ContainsKey("TimeoutSeconds"));
        Assert.True(result.Value.Metadata.ContainsKey("RetryCount"));
        Assert.True(result.Value.Metadata.ContainsKey("EnableCircuitBreaker"));
    }
}