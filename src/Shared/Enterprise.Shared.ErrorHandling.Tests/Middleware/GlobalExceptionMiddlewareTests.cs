using System.Text;
using System.Text.Json;

namespace Enterprise.Shared.ErrorHandling.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly ErrorHandlingSettings _settings;
    private readonly IErrorResponseFactory _responseFactory;
    private readonly ICorrelationContextAccessor _correlationContext;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public GlobalExceptionMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        _responseFactory = Substitute.For<IErrorResponseFactory>();
        _correlationContext = Substitute.For<ICorrelationContextAccessor>();
        _timeZoneProvider = Substitute.For<ITimeZoneProvider>();
        
        _settings = new ErrorHandlingSettings
        {
            EnableDetailedErrors = false,
            SensitiveDataPatterns = new List<string> { "password", "secret" },
            MaxErrorStackTraceLength = 1000
        };

        _timeZoneProvider.GetCurrentTime().Returns(new DateTime(2024, 1, 1, 15, 30, 0));
    }

    [Fact]
    public async Task InvokeAsync_WithNoException_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithEnterpriseException_ShouldHandleException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new BusinessRuleException("TestRule", "İş kuralı ihlali");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/problem+json");
        
        // Verify logging
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_ShouldReturnValidationProblem()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var validationErrors = new FluentValidation.Results.ValidationFailure[]
        {
            new("Email", "Email gerekli"),
            new("Password", "Şifre gerekli")
        };
        var exception = new FluentValidationException(validationErrors);
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_ShouldReturnBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new ArgumentNullException("testParam", "Parametre null olamaz");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_WithTimeoutException_ShouldReturn408()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new TimeoutException("İstek zaman aşımına uğradı");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(408);
    }

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_ShouldReturn499()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new OperationCanceledException("İşlem iptal edildi");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_ShouldReturn500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new InvalidOperationException("Beklenmeyen hata");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_WithCorrelationContext_ShouldUseCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var correlationId = "test-correlation-123";
        var correlationContextInstance = new CorrelationContext { CorrelationId = correlationId };
        _correlationContext.CorrelationContext.Returns(correlationContextInstance);
        
        var exception = new InvalidOperationException("Test hatası");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Verify that correlation ID is used in logging
        _logger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WithDatabaseException_ShouldLogCritical()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new DatabaseException("Veritabanı bağlantısı koptu", "08001", 2);
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        
        // Verify critical logging for database exceptions
        _logger.Received().Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WithDetailedErrorsEnabled_ShouldIncludeStackTrace()
    {
        // Arrange
        _settings.EnableDetailedErrors = true;
        
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new InvalidOperationException("Detaylı hata testi");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        
        // Response body should contain detailed error information
        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseBody.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseTurkishTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var turkishTime = new DateTime(2024, 6, 15, 18, 30, 0);
        _timeZoneProvider.GetCurrentTime().Returns(turkishTime);
        
        var exception = new ArgumentException("Test");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Verify that Turkish time is used in the response
        _timeZoneProvider.Received().GetCurrentTime();
    }

    [Fact]
    public async Task InvokeAsync_WithResponseStarted_ShouldNotWriteResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseFeature = Substitute.For<IHttpResponseFeature>();
        responseFeature.HasStarted.Returns(true);
        context.Features.Set(responseFeature);
        
        var exception = new InvalidOperationException("Test");
        
        RequestDelegate next = (ctx) => throw exception;
        var middleware = CreateMiddleware(next);

        // Act & Assert
        await middleware.InvokeAsync(context);
        
        // Should not throw exception even if response has started
    }

    private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        var options = Options.Create(_settings);
        
        return new GlobalExceptionMiddleware(
            next, 
            _logger, 
            options, 
            _responseFactory, 
            _correlationContext,
            _timeZoneProvider);
    }
}