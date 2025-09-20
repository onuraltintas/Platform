using Serilog.Events;
using Serilog.Parsing;

namespace Enterprise.Shared.Logging.Tests.Enrichers;

public class EnricherTests
{
    [Fact]
    public void CorrelationIdEnricher_WithCorrelationContext_ShouldAddProperties()
    {
        // Arrange
        var mockAccessor = Substitute.For<ICorrelationContextAccessor>();
        var correlationContext = new CorrelationContext
        {
            CorrelationId = "test-correlation-id",
            ParentCorrelationId = "parent-id",
            RequestId = "request-123",
            SessionId = "session-456"
        };
        correlationContext.Properties["CustomProp"] = "CustomValue";

        mockAccessor.CorrelationContext.Returns(correlationContext);
        var enricher = new CorrelationIdEnricher(mockAccessor);

        var logEvent = CreateLogEvent();
        ILogEventPropertyFactory propertyFactory = Substitute.For<ILogEventPropertyFactory>();
        propertyFactory.CreateProperty(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<bool>())
            .Returns(x => new LogEventProperty((string)x[0], new ScalarValue(x[1])));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("CorrelationId");
        logEvent.Properties.Should().ContainKey("ParentCorrelationId");
        logEvent.Properties.Should().ContainKey("RequestId");
        logEvent.Properties.Should().ContainKey("SessionId");
        logEvent.Properties.Should().ContainKey("CustomProp");

        logEvent.Properties["CorrelationId"].ToString().Should().Contain("test-correlation-id");
        logEvent.Properties["ParentCorrelationId"].ToString().Should().Contain("parent-id");
        logEvent.Properties["RequestId"].ToString().Should().Contain("request-123");
        logEvent.Properties["SessionId"].ToString().Should().Contain("session-456");
    }

    [Fact]
    public void CorrelationIdEnricher_WithoutCorrelationContext_ShouldNotAddProperties()
    {
        // Arrange
        var mockAccessor = Substitute.For<ICorrelationContextAccessor>();
        mockAccessor.CorrelationContext.Returns((CorrelationContext?)null);

        var enricher = new CorrelationIdEnricher(mockAccessor);
        var logEvent = CreateLogEvent();
        ILogEventPropertyFactory propertyFactory = Substitute.For<ILogEventPropertyFactory>();
        propertyFactory.CreateProperty(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<bool>())
            .Returns(x => new LogEventProperty((string)x[0], new ScalarValue(x[1])));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void ServiceEnricher_ShouldAddServiceProperties()
    {
        // Arrange
        var serviceName = "TestService";
        var serviceVersion = "1.0.0";
        var environment = "Test";

        var enricher = new ServiceEnricher(serviceName, serviceVersion, environment);
        var logEvent = CreateLogEvent();
        ILogEventPropertyFactory propertyFactory = Substitute.For<ILogEventPropertyFactory>();
        propertyFactory.CreateProperty(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<bool>())
            .Returns(x => new LogEventProperty((string)x[0], new ScalarValue(x[1])));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("ServiceName");
        logEvent.Properties.Should().ContainKey("ServiceVersion");
        logEvent.Properties.Should().ContainKey("Environment");
        logEvent.Properties.Should().ContainKey("MachineName");
        logEvent.Properties.Should().ContainKey("ProcessId");
        logEvent.Properties.Should().ContainKey("ProcessName");
        logEvent.Properties.Should().ContainKey("ThreadId");

        logEvent.Properties["ServiceName"].ToString().Should().Contain("TestService");
        logEvent.Properties["ServiceVersion"].ToString().Should().Contain("1.0.0");
        logEvent.Properties["Environment"].ToString().Should().Contain("Test");
    }

    [Fact]
    public void UserEnricher_WithHttpContext_ShouldAddUserProperties()
    {
        // Arrange
        var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        var mockIdentity = Substitute.For<System.Security.Claims.ClaimsIdentity>();

        // Setup claims
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user123"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
        };

        mockIdentity.IsAuthenticated.Returns(true);
        mockIdentity.Claims.Returns(claims);
        mockUser.Identity.Returns(mockIdentity);
        mockUser.Claims.Returns(claims);
        mockUser.FindFirst(Arg.Any<string>()).Returns(x =>
        {
            var claimType = (string)x[0];
            return claims.FirstOrDefault(c => c.Type == claimType);
        });
        mockUser.FindAll(Arg.Any<string>()).Returns(x =>
        {
            var claimType = (string)x[0];
            return claims.Where(c => c.Type == claimType);
        });

        var mockRequest = Substitute.For<HttpRequest>();
        var mockConnection = Substitute.For<ConnectionInfo>();
        var headers = new HeaderDictionary { ["User-Agent"] = "TestAgent" };

        mockRequest.Headers.Returns(headers);
        mockHttpContext.User.Returns(mockUser);
        mockHttpContext.Request.Returns(mockRequest);
        mockHttpContext.Connection.Returns(mockConnection);
        mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        var enricher = new UserEnricher(mockHttpContextAccessor);
        var logEvent = CreateLogEvent();
        ILogEventPropertyFactory propertyFactory = Substitute.For<ILogEventPropertyFactory>();
        propertyFactory.CreateProperty(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<bool>())
            .Returns(x => new LogEventProperty((string)x[0], new ScalarValue(x[1])));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("UserId");
        logEvent.Properties.Should().ContainKey("UserEmail");
        logEvent.Properties.Should().ContainKey("UserName");
        logEvent.Properties.Should().ContainKey("UserRoles");
        logEvent.Properties.Should().ContainKey("UserAgent");

        logEvent.Properties["UserId"].ToString().Should().Contain("user123");
        logEvent.Properties["UserEmail"].ToString().Should().Contain("test@example.com");
        logEvent.Properties["UserName"].ToString().Should().Contain("Test User");
    }

    [Fact]
    public void UserEnricher_WithoutHttpContext_ShouldNotAddProperties()
    {
        // Arrange
        var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var enricher = new UserEnricher(mockHttpContextAccessor);
        var logEvent = CreateLogEvent();
        ILogEventPropertyFactory propertyFactory = Substitute.For<ILogEventPropertyFactory>();
        propertyFactory.CreateProperty(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<bool>())
            .Returns(x => new LogEventProperty((string)x[0], new ScalarValue(x[1])));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().NotContainKey("UserId");
        logEvent.Properties.Should().NotContainKey("UserEmail");
    }

    private static LogEvent CreateLogEvent()
    {
        var template = new MessageTemplateParser().Parse("Test message");
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            template,
            new LogEventProperty[0]);
    }
}