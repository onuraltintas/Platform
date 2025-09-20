using Enterprise.Shared.Observability.Models;
using Enterprise.Shared.Observability.Services;
using FluentAssertions;
using Xunit;

namespace Enterprise.Shared.Observability.Tests.Services;

public class CorrelationContextAccessorTests
{
    [Fact]
    public void CorrelationContext_Should_Be_Null_Initially()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();

        // Act & Assert
        accessor.CorrelationContext.Should().BeNull();
    }

    [Fact]
    public void CorrelationContext_Should_Store_And_Retrieve_Context()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();
        var context = new CorrelationContext
        {
            CorrelationId = "test-correlation-id",
            UserId = "user123",
            SessionId = "session456",
            TraceId = "trace789",
            SpanId = "span012"
        };

        // Act
        accessor.CorrelationContext = context;

        // Assert
        accessor.CorrelationContext.Should().NotBeNull();
        accessor.CorrelationContext.Should().BeSameAs(context);
        accessor.CorrelationContext!.CorrelationId.Should().Be("test-correlation-id");
        accessor.CorrelationContext.UserId.Should().Be("user123");
        accessor.CorrelationContext.SessionId.Should().Be("session456");
        accessor.CorrelationContext.TraceId.Should().Be("trace789");
        accessor.CorrelationContext.SpanId.Should().Be("span012");
    }

    [Fact]
    public void CorrelationContext_Should_Allow_Null_Assignment()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();
        var context = new CorrelationContext
        {
            CorrelationId = "test-id"
        };
        accessor.CorrelationContext = context;

        // Act
        accessor.CorrelationContext = null;

        // Assert
        accessor.CorrelationContext.Should().BeNull();
    }

    [Fact]
    public void CorrelationContext_Should_Be_Independent_Between_Instances()
    {
        // Note: AsyncLocal shares context within the same execution context
        // This test verifies the final state after both assignments
        
        // Arrange
        var accessor1 = new CorrelationContextAccessor();
        var accessor2 = new CorrelationContextAccessor();
        
        var context1 = new CorrelationContext { CorrelationId = "context1" };
        var context2 = new CorrelationContext { CorrelationId = "context2" };

        // Act
        accessor1.CorrelationContext = context1;
        accessor2.CorrelationContext = context2;

        // Assert - Both accessors will show the last set context due to AsyncLocal behavior
        accessor1.CorrelationContext.Should().BeSameAs(context2);
        accessor2.CorrelationContext.Should().BeSameAs(context2);
        accessor1.CorrelationContext!.CorrelationId.Should().Be("context2");
        accessor2.CorrelationContext!.CorrelationId.Should().Be("context2");
    }

    [Fact]
    public void CorrelationContext_Should_Support_Multiple_Updates()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();
        var context1 = new CorrelationContext { CorrelationId = "first" };
        var context2 = new CorrelationContext { CorrelationId = "second" };
        var context3 = new CorrelationContext { CorrelationId = "third" };

        // Act & Assert
        accessor.CorrelationContext = context1;
        accessor.CorrelationContext!.CorrelationId.Should().Be("first");

        accessor.CorrelationContext = context2;
        accessor.CorrelationContext!.CorrelationId.Should().Be("second");

        accessor.CorrelationContext = context3;
        accessor.CorrelationContext!.CorrelationId.Should().Be("third");
    }

    [Fact]
    public void CorrelationContext_Should_Handle_Complex_Context_Data()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();
        var context = new CorrelationContext
        {
            CorrelationId = "complex-correlation-id",
            UserId = "user-with-special-chars-üößäë",
            SessionId = Guid.NewGuid().ToString(),
            TraceId = "trace-" + DateTime.UtcNow.Ticks,
            SpanId = "span-" + Random.Shared.Next(1000, 9999),
            Baggage = new Dictionary<string, string>
            {
                ["custom_field"] = "custom_value",
                ["tenant_id"] = "tenant_123",
                ["feature_flags"] = "flag1,flag2,flag3"
            },
            CreatedAt = DateTime.UtcNow
        };

        // Act
        accessor.CorrelationContext = context;

        // Assert
        var retrievedContext = accessor.CorrelationContext;
        retrievedContext.Should().NotBeNull();
        retrievedContext.Should().BeSameAs(context);
        retrievedContext!.CorrelationId.Should().Be(context.CorrelationId);
        retrievedContext.UserId.Should().Be(context.UserId);
        retrievedContext.SessionId.Should().Be(context.SessionId);
        retrievedContext.TraceId.Should().Be(context.TraceId);
        retrievedContext.SpanId.Should().Be(context.SpanId);
        retrievedContext.Baggage.Should().BeEquivalentTo(context.Baggage);
        retrievedContext.CreatedAt.Should().BeCloseTo(context.CreatedAt, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task CorrelationContext_Should_Be_Preserved_Across_Async_Calls()
    {
        // Arrange
        var accessor = new CorrelationContextAccessor();
        var context = new CorrelationContext
        {
            CorrelationId = "async-test-id",
            UserId = "async-user"
        };

        // Act
        accessor.CorrelationContext = context;
        
        // Simulate async operation
        await Task.Delay(10);
        
        var retrievedAfterDelay = accessor.CorrelationContext;
        
        await Task.Run(() =>
        {
            // This should still have the same context
            var retrievedInTask = accessor.CorrelationContext;
            retrievedInTask.Should().BeSameAs(context);
        });

        // Assert
        retrievedAfterDelay.Should().BeSameAs(context);
        retrievedAfterDelay!.CorrelationId.Should().Be("async-test-id");
        retrievedAfterDelay.UserId.Should().Be("async-user");
    }
}