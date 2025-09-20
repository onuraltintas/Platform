using Microsoft.AspNetCore.Http.Features;

namespace Enterprise.Shared.ErrorHandling.Tests.Handlers;

public class ErrorResponseFactoryTests
{
    private readonly ErrorResponseFactory _factory;
    private readonly ILogger<ErrorResponseFactory> _logger;
    private readonly ErrorHandlingSettings _settings;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public ErrorResponseFactoryTests()
    {
        _logger = Substitute.For<ILogger<ErrorResponseFactory>>();
        _timeZoneProvider = Substitute.For<ITimeZoneProvider>();
        _timeZoneProvider.GetCurrentTime().Returns(new DateTime(2024, 1, 1, 12, 0, 0));
        
        _settings = new ErrorHandlingSettings
        {
            EnableDetailedErrors = false,
            ErrorCodes = new Dictionary<string, string>
            {
                ["ValidationFailed"] = "ERR_VALIDATION_001",
                ["ResourceNotFound"] = "ERR_NOTFOUND_001"
            }
        };

        var options = Options.Create(_settings);
        _factory = new ErrorResponseFactory(options, _logger, null, _timeZoneProvider);
    }

    [Fact]
    public void CreateErrorResponse_WithEnterpriseException_ShouldReturnCorrectResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new BusinessRuleException("TestRule", "İş kuralı ihlali")
            .WithCorrelationId("test-123");

        // Act
        var response = _factory.CreateErrorResponse(exception, context);

        // Assert
        response.ErrorCode.Should().Be("BUSINESS_RULE_VIOLATION");
        response.Message.Should().Be("BUSINESS_RULE_VIOLATION"); // Would be localized in real scenario
        response.StatusCode.Should().Be(400);
        response.Path.Should().Be("/test");
        response.Method.Should().Be("GET");
        response.Data.Should().ContainKey("severity");
        response.Data.Should().ContainKey("ruleName");
    }

    [Fact]
    public void CreateErrorResponse_WithArgumentException_ShouldReturnValidationError()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ArgumentException("Geçersiz argüman");

        // Act
        var response = _factory.CreateErrorResponse(exception, context);

        // Assert
        response.ErrorCode.Should().Be("ERR_VALIDATION_001");
        response.StatusCode.Should().Be(400);
    }

    [Fact]
    public void CreateValidationErrorResponse_ShouldReturnValidationResponse()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError { Field = "Email", Message = "Email gerekli" },
            new ValidationError { Field = "Password", Message = "Şifre gerekli" }
        };
        var context = CreateHttpContext();

        // Act
        var response = _factory.CreateValidationErrorResponse(errors, context);

        // Assert
        response.ErrorCode.Should().Be("VALIDATION_FAILED");
        response.Message.Should().Be("One or more validation errors occurred");
        response.Errors.Should().HaveCount(2);
        response.StatusCode.Should().Be(400);
    }

    [Fact]
    public void CreateProblemDetails_WithEnterpriseException_ShouldReturnProblemDetails()
    {
        // Arrange
        var exception = new ResourceNotFoundException("Kullanıcı", "123");
        var correlationId = "test-correlation";

        // Act
        var problemDetails = _factory.CreateProblemDetails(exception, correlationId);

        // Assert
        problemDetails.Title.Should().Be("Resource Not Found");
        problemDetails.Status.Should().Be(404);
        problemDetails.Instance.Should().Be(correlationId);
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("data");
    }

    [Fact]
    public void CreateProblemDetails_WithStandardException_ShouldReturnGenericProblemDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Geçersiz işlem");
        var correlationId = "test-correlation";

        // Act
        var problemDetails = _factory.CreateProblemDetails(exception, correlationId);

        // Assert
        problemDetails.Title.Should().Be("Invalid Operation");
        problemDetails.Status.Should().Be(500);
        problemDetails.Instance.Should().Be(correlationId);
        problemDetails.Extensions.Should().ContainKey("correlationId");
        problemDetails.Extensions.Should().ContainKey("timestamp");
    }

    [Fact]
    public void CreateErrorResponse_WithTimeoutException_ShouldReturn408()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new TimeoutException("İstek zaman aşımına uğradı");

        // Act
        var response = _factory.CreateErrorResponse(exception, context);

        // Assert
        response.StatusCode.Should().Be(408);
        response.ErrorCode.Should().Be("UNKNOWN_ERROR");
    }

    [Fact]
    public void CreateErrorResponse_WithOperationCancelledException_ShouldReturn499()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new OperationCanceledException("İstek iptal edildi");

        // Act
        var response = _factory.CreateErrorResponse(exception, context);

        // Assert
        response.StatusCode.Should().Be(499);
    }

    [Fact]
    public void CreateErrorResponse_ShouldUseTurkishTime()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ArgumentException("Test");
        var turkishTime = new DateTime(2024, 1, 1, 15, 30, 0); // Turkish time
        _timeZoneProvider.GetCurrentTime().Returns(turkishTime);

        // Act
        var response = _factory.CreateErrorResponse(exception, context);

        // Assert
        response.Timestamp.Should().Be(turkishTime);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Request.Method = "GET";
        context.TraceIdentifier = "trace-123";
        return context;
    }
}