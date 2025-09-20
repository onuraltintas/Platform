namespace Enterprise.Shared.ErrorHandling.Tests.Models;

public class ErrorHandlingModelsTests
{
    [Fact]
    public void ErrorHandlingSettings_ShouldHaveCorrectDefaults()
    {
        // Act
        var settings = new ErrorHandlingSettings();

        // Assert
        settings.EnableDetailedErrors.Should().BeFalse();
        settings.EnableDeveloperExceptionPage.Should().BeFalse();
        settings.EnableProblemDetails.Should().BeTrue();
        settings.EnableCorrelationId.Should().BeTrue();
        settings.EnableLocalization.Should().BeTrue();
        settings.DefaultLanguage.Should().Be("tr-TR");
        settings.DefaultCulture.Should().Be("tr-TR");
        settings.TimeZoneId.Should().Be("Turkey Standard Time");
        settings.MaxErrorStackTraceLength.Should().Be(5000);
        settings.SensitiveDataPatterns.Should().Contain("password");
        settings.SensitiveDataPatterns.Should().Contain("secret");
    }

    [Fact]
    public void ErrorHandlingSettings_SectionName_ShouldBeCorrect()
    {
        // Assert
        ErrorHandlingSettings.SectionName.Should().Be("ErrorHandlingSettings");
    }

    [Fact]
    public void RetryPolicySettings_ShouldHaveCorrectDefaults()
    {
        // Act
        var settings = new RetryPolicySettings();

        // Assert
        settings.MaxRetryAttempts.Should().Be(3);
        settings.InitialDelayMs.Should().Be(1000);
        settings.MaxDelayMs.Should().Be(30000);
        settings.BackoffMultiplier.Should().Be(2);
    }

    [Fact]
    public void CircuitBreakerSettings_ShouldHaveCorrectDefaults()
    {
        // Act
        var settings = new CircuitBreakerSettings();

        // Assert
        settings.FailureThreshold.Should().Be(5);
        settings.SamplingDuration.Should().Be(TimeSpan.FromMinutes(1));
        settings.MinimumThroughput.Should().Be(10);
        settings.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ErrorHandlingSettings_ErrorCodes_ShouldHaveDefaults()
    {
        // Act
        var settings = new ErrorHandlingSettings();

        // Assert
        settings.ErrorCodes.Should().ContainKey("ValidationFailed");
        settings.ErrorCodes.Should().ContainKey("ResourceNotFound");
        settings.ErrorCodes.Should().ContainKey("Unauthorized");
        settings.ErrorCodes.Should().ContainKey("Forbidden");
        settings.ErrorCodes.Should().ContainKey("Conflict");
        settings.ErrorCodes.Should().ContainKey("BusinessRule");
        settings.ErrorCodes.Should().ContainKey("ExternalService");
        settings.ErrorCodes.Should().ContainKey("Database");
        
        settings.ErrorCodes["ValidationFailed"].Should().Be("ERR_VALIDATION_001");
        settings.ErrorCodes["ResourceNotFound"].Should().Be("ERR_NOTFOUND_001");
        settings.ErrorCodes["Unauthorized"].Should().Be("ERR_AUTH_001");
    }

    [Fact]
    public void ErrorResponse_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new ErrorResponse();

        // Assert
        response.ErrorCode.Should().Be(string.Empty);
        response.Message.Should().Be(string.Empty);
        response.CorrelationId.Should().Be(string.Empty);
        response.Path.Should().Be(string.Empty);
        response.Method.Should().Be(string.Empty);
        response.StatusCode.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public void ValidationErrorResponse_ShouldInheritFromErrorResponse()
    {
        // Act
        var response = new ValidationErrorResponse();

        // Assert
        response.Should().BeAssignableTo<ErrorResponse>();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationError_ShouldHaveProperties()
    {
        // Arrange
        var error = new ValidationError
        {
            Field = "Email",
            Message = "Email gerekli",
            Code = "REQUIRED",
            AttemptedValue = ""
        };

        // Assert
        error.Field.Should().Be("Email");
        error.Message.Should().Be("Email gerekli");
        error.Code.Should().Be("REQUIRED");
        error.AttemptedValue.Should().Be("");
    }

    [Theory]
    [InlineData(ErrorSeverity.Low)]
    [InlineData(ErrorSeverity.Medium)]
    [InlineData(ErrorSeverity.High)]
    [InlineData(ErrorSeverity.Critical)]
    public void ErrorSeverity_AllValues_ShouldBeValid(ErrorSeverity severity)
    {
        // Act & Assert
        Enum.IsDefined(typeof(ErrorSeverity), severity).Should().BeTrue();
    }

    [Fact]
    public void ErrorStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new ErrorStatistics();

        // Assert
        stats.TotalErrors.Should().Be(0);
        stats.ErrorsByType.Should().NotBeNull();
        stats.ErrorsByType.Should().BeEmpty();
        stats.ErrorsByStatusCode.Should().NotBeNull();
        stats.ErrorsByStatusCode.Should().BeEmpty();
        stats.TopErrors.Should().NotBeNull();
        stats.TopErrors.Should().BeEmpty();
        stats.ErrorRate.Should().Be(0);
    }

    [Fact]
    public void TopErrorInfo_ShouldHaveProperties()
    {
        // Arrange
        var topError = new TopErrorInfo
        {
            ErrorCode = "VALIDATION_FAILED",
            Message = "Validation hatası",
            Count = 10,
            LastOccurrence = DateTime.Now
        };

        // Assert
        topError.ErrorCode.Should().Be("VALIDATION_FAILED");
        topError.Message.Should().Be("Validation hatası");
        topError.Count.Should().Be(10);
        topError.LastOccurrence.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ErrorHandlingSettings_ShouldAllowCustomization()
    {
        // Arrange & Act
        var settings = new ErrorHandlingSettings
        {
            EnableDetailedErrors = true,
            DefaultLanguage = "en-US",
            DefaultCulture = "en-US",
            TimeZoneId = "UTC",
            MaxErrorStackTraceLength = 2000,
            SensitiveDataPatterns = new List<string> { "customPattern" }
        };

        // Assert
        settings.EnableDetailedErrors.Should().BeTrue();
        settings.DefaultLanguage.Should().Be("en-US");
        settings.DefaultCulture.Should().Be("en-US");
        settings.TimeZoneId.Should().Be("UTC");
        settings.MaxErrorStackTraceLength.Should().Be(2000);
        settings.SensitiveDataPatterns.Should().Contain("customPattern");
        settings.SensitiveDataPatterns.Should().HaveCount(1);
    }

    [Fact]
    public void RetryPolicySettings_ShouldAllowCustomization()
    {
        // Arrange & Act
        var settings = new RetryPolicySettings
        {
            MaxRetryAttempts = 5,
            InitialDelayMs = 500,
            MaxDelayMs = 60000,
            BackoffMultiplier = 1.5
        };

        // Assert
        settings.MaxRetryAttempts.Should().Be(5);
        settings.InitialDelayMs.Should().Be(500);
        settings.MaxDelayMs.Should().Be(60000);
        settings.BackoffMultiplier.Should().Be(1.5);
    }

    [Fact]
    public void CircuitBreakerSettings_ShouldAllowCustomization()
    {
        // Arrange & Act
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 10,
            SamplingDuration = TimeSpan.FromMinutes(5),
            MinimumThroughput = 20,
            BreakDuration = TimeSpan.FromMinutes(2)
        };

        // Assert
        settings.FailureThreshold.Should().Be(10);
        settings.SamplingDuration.Should().Be(TimeSpan.FromMinutes(5));
        settings.MinimumThroughput.Should().Be(20);
        settings.BreakDuration.Should().Be(TimeSpan.FromMinutes(2));
    }
}