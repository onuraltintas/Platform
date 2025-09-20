using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Models;
using FluentAssertions;

namespace Enterprise.Shared.Common.Tests.Models;

[TestFixture]
public class ResultTests
{
    #region Result (Non-Generic) Tests

    [Test]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.Status.Should().Be(OperationStatus.Success);
    }

    [Test]
    public void Success_WithMetadata_CreatesSuccessfulResultWithMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = Result.Success(metadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Metadata.Should().ContainKey("key");
        result.Metadata["key"].Should().Be("value");
    }

    [Test]
    public void Failure_WithErrorMessage_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "Operation failed";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Errors.Should().Contain(errorMessage);
        result.Status.Should().Be(OperationStatus.Failed);
    }

    [Test]
    public void Failure_WithMultipleErrors_CreatesFailedResultWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result.Failure(errors);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Error 1");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Test]
    public void Failure_WithException_CreatesFailedResultWithExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.Failure(exception);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Test exception");
        result.Metadata.Should().ContainKey("ExceptionType");
        result.Metadata["ExceptionType"].Should().Be("InvalidOperationException");
        result.Metadata.Should().ContainKey("StackTrace");
    }

    [Test]
    public void WithMetadata_AddsMetadataToResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        result.WithMetadata("key", "value");

        // Assert
        result.Metadata.Should().ContainKey("key");
        result.Metadata["key"].Should().Be("value");
    }

    [Test]
    public void ImplicitConversion_FromString_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "Error occurred";

        // Act
        Result result = errorMessage;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Test]
    public void ImplicitConversion_FromException_CreatesFailedResult()
    {
        // Arrange
        var exception = new ArgumentException("Test error");

        // Act
        Result result = exception;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Test error");
    }

    [Test]
    public void Combine_WithAllSuccessfulResults_ReturnsSuccessfulResult()
    {
        // Arrange
        var result1 = Result.Success().WithMetadata("key1", "value1");
        var result2 = Result.Success().WithMetadata("key2", "value2");
        var result3 = Result.Success();

        // Act
        var combinedResult = Result.Combine(result1, result2, result3);

        // Assert
        combinedResult.IsSuccess.Should().BeTrue();
        combinedResult.Metadata.Should().ContainKeys("key1", "key2");
    }

    [Test]
    public void Combine_WithSomeFailedResults_ReturnsFailedResult()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Failure("Error 1");
        var result3 = Result.Failure("Error 2");

        // Act
        var combinedResult = Result.Combine(result1, result2, result3);

        // Assert
        combinedResult.IsFailure.Should().BeTrue();
        combinedResult.Errors.Should().Contain("Error 1");
        combinedResult.Errors.Should().Contain("Error 2");
    }

    [Test]
    public void ToString_WithSuccessfulResult_ReturnsSuccessString()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Success");
    }

    [Test]
    public void ToString_WithFailedResult_ReturnsFailureString()
    {
        // Arrange
        var result = Result.Failure("Test error");

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Failure: Test error");
    }

    #endregion

    #region Result<T> (Generic) Tests

    [Test]
    public void Success_WithValue_CreatesSuccessfulResultWithValue()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Test]
    public void Success_WithValueAndMetadata_CreatesSuccessfulResultWithValueAndMetadata()
    {
        // Arrange
        var value = "test value";
        var metadata = new Dictionary<string, object> { { "key", "metadata" } };

        // Act
        var result = Result<string>.Success(value, metadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Metadata.Should().ContainKey("key");
    }

    [Test]
    public void Failure_Generic_CreatesFailedResultWithoutValue()
    {
        // Arrange
        var errorMessage = "Operation failed";

        // Act
        var result = Result<string>.Failure(errorMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.HasValue.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(errorMessage);
    }

    [Test]
    public void Map_WithSuccessfulResult_MapsValueCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be("5");
    }

    [Test]
    public void Map_WithFailedResult_ReturnsFailedResult()
    {
        // Arrange
        var result = Result<int>.Failure("Error");

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be("Error");
    }

    [Test]
    public void Map_WithExceptionInMapper_ReturnsFailedResult()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var mappedResult = result.Map<string>(x => throw new InvalidOperationException("Mapping failed"));

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be("Mapping failed");
    }

    [Test]
    public void Bind_WithSuccessfulResult_BindsCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var boundResult = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        boundResult.IsSuccess.Should().BeTrue();
        boundResult.Value.Should().Be("5");
    }

    [Test]
    public void Bind_WithFailedResult_ReturnsFailedResult()
    {
        // Arrange
        var result = Result<int>.Failure("Error");

        // Act
        var boundResult = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        boundResult.IsFailure.Should().BeTrue();
        boundResult.Error.Should().Be("Error");
    }

    [Test]
    public void OnSuccess_WithSuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Success(5);
        var actionExecuted = false;

        // Act
        result.OnSuccess(x => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeTrue();
    }

    [Test]
    public void OnSuccess_WithFailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure("Error");
        var actionExecuted = false;

        // Act
        result.OnSuccess(x => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
    }

    [Test]
    public void OnFailure_WithFailedResult_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Failure("Test error");
        var actionExecuted = false;
        var capturedError = "";

        // Act
        result.OnFailure(error => 
        {
            actionExecuted = true;
            capturedError = error;
        });

        // Assert
        actionExecuted.Should().BeTrue();
        capturedError.Should().Be("Test error");
    }

    [Test]
    public void OnFailure_WithSuccessfulResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(5);
        var actionExecuted = false;

        // Act
        result.OnFailure(error => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
    }

    [Test]
    public void GetValueOrDefault_WithSuccessfulResult_ReturnsValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var value = result.GetValueOrDefault(0);

        // Assert
        value.Should().Be(42);
    }

    [Test]
    public void GetValueOrDefault_WithFailedResult_ReturnsDefaultValue()
    {
        // Arrange
        var result = Result<int>.Failure("Error");

        // Act
        var value = result.GetValueOrDefault(999);

        // Assert
        value.Should().Be(999);
    }

    [Test]
    public void ImplicitConversion_FromValue_CreatesSuccessfulResult()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Test]
    public void ImplicitConversion_FromString_CreatesFailedGenericResult()
    {
        // Arrange
        var error = "Error message";

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Test]
    public void ToString_WithSuccessfulGenericResult_ReturnsSuccessStringWithValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Success: 42");
    }

    [Test]
    public void ToString_WithFailedGenericResult_ReturnsFailureString()
    {
        // Arrange
        var result = Result<int>.Failure("Test error");

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Failure: Test error");
    }

    #endregion
}