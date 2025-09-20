namespace Enterprise.Shared.Logging.Tests.Services;

public class EnterpriseLoggerFactoryTests
{
    private readonly ILoggerFactory _mockLoggerFactory;
    private readonly LoggingSettings _settings;
    private readonly ICorrelationContextAccessor _mockCorrelationAccessor;
    private readonly EnterpriseLoggerFactory _factory;

    public EnterpriseLoggerFactoryTests()
    {
        _mockLoggerFactory = Substitute.For<ILoggerFactory>();
        _mockCorrelationAccessor = Substitute.For<ICorrelationContextAccessor>();
        _settings = new LoggingSettings();

        var options = Options.Create(_settings);
        _factory = new EnterpriseLoggerFactory(_mockLoggerFactory, options, _mockCorrelationAccessor);
    }

    [Fact]
    public void CreateLogger_Generic_ShouldReturnEnterpriseLogger()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TestClass>>();
        _mockLoggerFactory.CreateLogger<TestClass>().Returns(mockLogger);

        // Act
        var logger = _factory.CreateLogger<TestClass>();

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<IEnterpriseLogger<TestClass>>();
    }

    [Fact]
    public void CreateLogger_WithName_ShouldReturnEnterpriseLogger()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<object>>();
        _mockLoggerFactory.CreateLogger<object>().Returns(mockLogger);

        // Act
        var logger = _factory.CreateLogger("TestLogger");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<IEnterpriseLogger<object>>();
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act & Assert
        Action act = () => new EnterpriseLoggerFactory(null!, options, _mockCorrelationAccessor);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new EnterpriseLoggerFactory(_mockLoggerFactory, null!, _mockCorrelationAccessor);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullCorrelationAccessor_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act & Assert
        Action act = () => new EnterpriseLoggerFactory(_mockLoggerFactory, options, null);
        act.Should().NotThrow();
    }

    public class TestClass { }
}