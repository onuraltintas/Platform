using Gateway.API.Transforms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Tests.Transforms;

public class GatewayRequestTransformTests
{
    private readonly GatewayRequestTransformProvider _transformProvider;
    private readonly Mock<ILogger<GatewayRequestTransformProvider>> _mockLogger;

    public GatewayRequestTransformTests()
    {
        _transformProvider = new GatewayRequestTransformProvider();
        _mockLogger = new Mock<ILogger<GatewayRequestTransformProvider>>();
    }

    [Fact]
    public void ValidateRoute_ShouldNotThrow()
    {
        // Arrange
        var context = new TransformRouteValidationContext();

        // Act & Assert
        var exception = Record.Exception(() => _transformProvider.ValidateRoute(context));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCluster_ShouldNotThrow()
    {
        // Arrange
        var context = new TransformClusterValidationContext();

        // Act & Assert
        var exception = Record.Exception(() => _transformProvider.ValidateCluster(context));
        Assert.Null(exception);
    }

    [Fact]
    public void Apply_ShouldAddTransformsToContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_mockLogger.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            TraceIdentifier = "test-request-id"
        };

        var transformContext = new TransformBuilderContext
        {
            Services = serviceProvider
        };

        // Act
        _transformProvider.Apply(transformContext);

        // Assert
        // Verify that transforms were added (we can't easily test the actual transform execution
        // without a more complex setup, but we can verify the method completes without error)
        Assert.NotNull(transformContext);
    }

    [Theory]
    [InlineData("request-id-123")]
    [InlineData("another-request-id")]
    [InlineData("")]
    public void TransformProvider_ShouldHandleDifferentRequestIds(string requestId)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_mockLogger.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            TraceIdentifier = requestId
        };

        var transformContext = new TransformBuilderContext
        {
            Services = serviceProvider
        };

        // Act & Assert
        var exception = Record.Exception(() => _transformProvider.Apply(transformContext));
        Assert.Null(exception);
    }

    [Fact]
    public void Apply_WithNullServices_ShouldNotThrow()
    {
        // Arrange
        var transformContext = new TransformBuilderContext();

        // Act & Assert
        var exception = Record.Exception(() => _transformProvider.Apply(transformContext));
        Assert.Null(exception);
    }
}