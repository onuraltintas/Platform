using Xunit;

namespace Gateway.Tests;

public class BasicTests
{
    [Fact]
    public void SampleTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    public void Add_ShouldReturnCorrectSum(int a, int b, int expected)
    {
        // Act
        var result = a + b;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GatewayCore_Models_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var gatewayRequest = new Gateway.Core.Models.GatewayRequest();

        // Act & Assert
        Assert.NotNull(gatewayRequest);
        Assert.NotEmpty(gatewayRequest.RequestId);
        Assert.Equal(DateTime.UtcNow.Date, gatewayRequest.Timestamp.Date);
    }

    [Fact]
    public void GatewayOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new Gateway.Core.Configuration.GatewayOptions();

        // Assert
        Assert.Equal("Development", options.Environment);
        Assert.Equal(5000, options.Port);
        Assert.True(options.EnableSwagger);
    }
}