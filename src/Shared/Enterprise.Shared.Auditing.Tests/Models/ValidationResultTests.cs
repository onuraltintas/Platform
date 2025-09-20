namespace Enterprise.Shared.Auditing.Tests.Models;

public class ValidationResultTests
{
    [Fact]
    public void Constructor_WithSuccess_SetsCorrectValues()
    {
        var result = new ValidationResult(true);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithFailure_SetsCorrectValues()
    {
        var error = "Validation failed";
        var result = new ValidationResult(false, error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_ReturnsSuccessfulResult()
    {
        var result = ValidationResult.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessage_ReturnsFailedResult()
    {
        var error = "Something went wrong";
        var result = ValidationResult.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }
}