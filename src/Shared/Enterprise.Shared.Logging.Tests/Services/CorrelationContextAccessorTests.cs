namespace Enterprise.Shared.Logging.Tests.Services;

public class CorrelationContextAccessorTests
{
    private readonly CorrelationContextAccessor _accessor;

    public CorrelationContextAccessorTests()
    {
        _accessor = new CorrelationContextAccessor();
    }

    [Fact]
    public void CorrelationContext_InitiallyNull()
    {
        // Act & Assert
        _accessor.CorrelationContext.Should().BeNull();
    }

    [Fact]
    public void SetCorrelationContext_ShouldSetContext()
    {
        // Arrange
        var context = new CorrelationContext
        {
            CorrelationId = "test-id",
            UserId = "user123"
        };

        // Act
        _accessor.SetCorrelationContext(context);

        // Assert
        _accessor.CorrelationContext.Should().NotBeNull();
        _accessor.CorrelationContext!.CorrelationId.Should().Be("test-id");
        _accessor.CorrelationContext.UserId.Should().Be("user123");
    }

    [Fact]
    public void ClearCorrelationContext_ShouldSetToNull()
    {
        // Arrange
        var context = new CorrelationContext { CorrelationId = "test-id" };
        _accessor.SetCorrelationContext(context);

        // Act
        _accessor.ClearCorrelationContext();

        // Assert
        _accessor.CorrelationContext.Should().BeNull();
    }

    [Fact]
    public void CreateAndSetContext_ShouldCreateNewContext()
    {
        // Act
        var context = _accessor.CreateAndSetContext();

        // Assert
        context.Should().NotBeNull();
        context.CorrelationId.Should().NotBeNullOrEmpty();
        _accessor.CorrelationContext.Should().Be(context);
    }

    [Fact]
    public void CreateAndSetContext_WithParentId_ShouldSetParentId()
    {
        // Arrange
        var parentId = "parent-id";

        // Act
        var context = _accessor.CreateAndSetContext(parentId);

        // Assert
        context.ParentCorrelationId.Should().Be(parentId);
        _accessor.CorrelationContext.Should().Be(context);
    }

    [Fact]
    public void SetCorrelationContext_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _accessor.SetCorrelationContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}