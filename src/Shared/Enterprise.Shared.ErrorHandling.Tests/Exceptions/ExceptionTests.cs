namespace Enterprise.Shared.ErrorHandling.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void BusinessRuleException_ShouldHaveCorrectProperties()
    {
        // Arrange
        var ruleName = "MinimumAge";
        var message = "Kullanıcı en az 18 yaşında olmalıdır";
        var errorCode = "BUSINESS_RULE_VIOLATION";

        // Act
        var exception = new BusinessRuleException(ruleName, message);

        // Assert
        exception.RuleName.Should().Be(ruleName);
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.HttpStatusCode.Should().Be(400);
        exception.Severity.Should().Be(ErrorSeverity.Medium);
        exception.ErrorData.Should().ContainKey("ruleName");
        exception.ErrorData["ruleName"].Should().Be(ruleName);
    }

    [Fact]
    public void ResourceNotFoundException_ShouldHaveCorrectProperties()
    {
        // Arrange
        var resourceType = "Kullanıcı";
        var resourceId = "123";

        // Act
        var exception = new ResourceNotFoundException(resourceType, resourceId);

        // Assert
        exception.ResourceType.Should().Be(resourceType);
        exception.ResourceId.Should().Be(resourceId);
        exception.ErrorCode.Should().Be("RESOURCE_NOT_FOUND");
        exception.HttpStatusCode.Should().Be(404);
        exception.Severity.Should().Be(ErrorSeverity.Low);
        exception.Message.Should().Be($"{resourceType} with ID '{resourceId}' was not found");
        exception.ErrorData.Should().ContainKey("resourceType");
        exception.ErrorData.Should().ContainKey("resourceId");
    }

    [Fact]
    public void ValidationException_WithSingleError_ShouldWork()
    {
        // Arrange
        var field = "Email";
        var message = "Email adresi geçersiz";

        // Act
        var exception = new ValidationException(field, message);

        // Assert
        exception.Errors.Should().HaveCount(1);
        exception.Errors[0].Field.Should().Be(field);
        exception.Errors[0].Message.Should().Be(message);
        exception.ErrorCode.Should().Be("VALIDATION_FAILED");
        exception.HttpStatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidationException_WithMultipleErrors_ShouldWork()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError { Field = "Email", Message = "Email gerekli" },
            new ValidationError { Field = "Password", Message = "Şifre en az 8 karakter olmalı" }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.Field == "Email");
        exception.Errors.Should().Contain(e => e.Field == "Password");
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveCorrectDefaults()
    {
        // Act
        var exception = new UnauthorizedException();

        // Assert
        exception.ErrorCode.Should().Be("UNAUTHORIZED");
        exception.HttpStatusCode.Should().Be(401);
        exception.Message.Should().Be("User is not authenticated");
        exception.Severity.Should().Be(ErrorSeverity.Medium);
    }

    [Fact]
    public void ForbiddenException_WithPermission_ShouldIncludeInErrorData()
    {
        // Arrange
        var permission = "user:delete";
        var message = "Bu işlem için yetkiniz yok";

        // Act
        var exception = new ForbiddenException(message, permission);

        // Assert
        exception.RequiredPermission.Should().Be(permission);
        exception.ErrorData.Should().ContainKey("requiredPermission");
        exception.ErrorData["requiredPermission"].Should().Be(permission);
        exception.HttpStatusCode.Should().Be(403);
    }

    [Fact]
    public void ConflictException_ShouldHaveConflictType()
    {
        // Arrange
        var message = "Email adresi zaten kullanılıyor";
        var conflictType = "EMAIL_CONFLICT";

        // Act
        var exception = new ConflictException(message, conflictType);

        // Assert
        exception.ConflictType.Should().Be(conflictType);
        exception.ErrorData.Should().ContainKey("conflictType");
        exception.HttpStatusCode.Should().Be(409);
    }

    [Fact]
    public void ExternalServiceException_ShouldHaveServiceInfo()
    {
        // Arrange
        var serviceName = "PaymentGateway";
        var message = "Ödeme servisi mevcut değil";
        var statusCode = 503;

        // Act
        var exception = new ExternalServiceException(serviceName, message, statusCode);

        // Assert
        exception.ServiceName.Should().Be(serviceName);
        exception.ServiceStatusCode.Should().Be(statusCode);
        exception.ErrorData.Should().ContainKey("serviceName");
        exception.ErrorData.Should().ContainKey("serviceStatusCode");
        exception.Severity.Should().Be(ErrorSeverity.High);
        exception.HttpStatusCode.Should().Be(502);
    }

    [Fact]
    public void DatabaseException_ShouldHaveSqlInfo()
    {
        // Arrange
        var message = "Veritabanı bağlantısı koptu";
        var sqlState = "08001";
        var sqlErrorNumber = 2;

        // Act
        var exception = new DatabaseException(message, sqlState, sqlErrorNumber);

        // Assert
        exception.SqlState.Should().Be(sqlState);
        exception.SqlErrorNumber.Should().Be(sqlErrorNumber);
        exception.ErrorData.Should().ContainKey("sqlState");
        exception.ErrorData.Should().ContainKey("sqlErrorNumber");
        exception.Severity.Should().Be(ErrorSeverity.Critical);
    }

    [Fact]
    public void EnterpriseException_ToProblemDetails_ShouldReturnCorrectStructure()
    {
        // Arrange
        var exception = new BusinessRuleException("TestRule", "Test mesajı")
            .WithCorrelationId("test-correlation-123")
            .WithSeverity(ErrorSeverity.High)
            .WithData("additionalInfo", "test-data");

        // Act
        var problemDetails = exception.ToProblemDetails();

        // Assert
        problemDetails.Title.Should().Be("Business Rule Violation");
        problemDetails.Detail.Should().Be("Test mesajı");
        problemDetails.Status.Should().Be(400);
        problemDetails.Type.Should().Be("https://enterprise.com/errors/BUSINESS_RULE_VIOLATION");
        problemDetails.Instance.Should().Be("test-correlation-123");
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("occurredAt");
        problemDetails.Extensions.Should().ContainKey("severity");
        problemDetails.Extensions.Should().ContainKey("data");
    }

    [Fact]
    public void EnterpriseException_FluentInterface_ShouldWork()
    {
        // Arrange & Act
        var exception = new BusinessRuleException("TestRule", "Test")
            .WithCorrelationId("correlation-123")
            .WithSeverity(ErrorSeverity.Critical)
            .WithData("key1", "value1")
            .WithData("key2", 42);

        // Assert
        exception.CorrelationId.Should().Be("correlation-123");
        exception.Severity.Should().Be(ErrorSeverity.Critical);
        exception.ErrorData.Should().ContainKey("key1");
        exception.ErrorData.Should().ContainKey("key2");
        exception.ErrorData["key1"].Should().Be("value1");
        exception.ErrorData["key2"].Should().Be(42);
    }

    [Fact]
    public void ValidationException_FromModelState_ShouldWork()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email gerekli");
        modelState.AddModelError("Password", "Şifre çok kısa");

        // Act
        var exception = ValidationException.FromModelState(modelState);

        // Assert
        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.Field == "Email" && e.Message == "Email gerekli");
        exception.Errors.Should().Contain(e => e.Field == "Password" && e.Message == "Şifre çok kısa");
    }
}