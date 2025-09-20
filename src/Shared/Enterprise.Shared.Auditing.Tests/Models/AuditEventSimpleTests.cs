namespace Enterprise.Shared.Auditing.Tests.Models;

public class AuditEventSimpleTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var auditEvent = new AuditEvent();

        auditEvent.Id.Should().NotBeNullOrEmpty();
        auditEvent.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        auditEvent.Action.Should().BeEmpty();
        auditEvent.Resource.Should().BeEmpty();
        auditEvent.Result.Should().BeEmpty();
        auditEvent.Category.Should().Be(AuditEventCategory.Application);
        auditEvent.Severity.Should().Be(AuditSeverity.Information);
        auditEvent.Properties.Should().NotBeNull();
        auditEvent.Tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Create_WithValidParameters_ReturnsConfiguredEvent()
    {
        var action = "TestAction";
        var resource = "TestResource";
        var result = "Success";

        var auditEvent = AuditEvent.Create(action, resource, result);

        auditEvent.Action.Should().Be(action);
        auditEvent.Resource.Should().Be(resource);
        auditEvent.Result.Should().Be(result);
        auditEvent.Id.Should().NotBeNullOrEmpty();
        auditEvent.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithDefaultResult_SetsSuccessResult()
    {
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");

        auditEvent.Result.Should().Be("Success");
    }

    [Fact]
    public void WithUser_SetsUserProperties()
    {
        var userId = "user123";
        var username = "testuser";

        var auditEvent = AuditEvent.Create("TestAction", "TestResource")
            .WithUser(userId, username);

        auditEvent.UserId.Should().Be(userId);
        auditEvent.Username.Should().Be(username);
    }

    [Fact]
    public void WithMetadata_SerializesMetadataToString()
    {
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        var auditEvent = AuditEvent.Create("TestAction", "TestResource")
            .WithMetadata(metadata);

        auditEvent.Metadata.Should().NotBeNullOrEmpty();
        auditEvent.Metadata.Should().Contain("key1");
        auditEvent.Metadata.Should().Contain("value1");
        auditEvent.Metadata.Should().Contain("key2");
    }

    [Fact]
    public void WithCorrelation_SetsCorrelationId()
    {
        var correlationId = "correlation123";

        var auditEvent = AuditEvent.Create("TestAction", "TestResource")
            .WithCorrelation(correlationId);

        auditEvent.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void WithHttpContext_SetsHttpContextProperties()
    {
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var sessionId = "session123";

        var auditEvent = AuditEvent.Create("TestAction", "TestResource")
            .WithHttpContext(ipAddress, userAgent, sessionId);

        auditEvent.IpAddress.Should().Be(ipAddress);
        auditEvent.UserAgent.Should().Be(userAgent);
        auditEvent.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Properties_CanBeModifiedDirectly()
    {
        var auditEvent = AuditEvent.Create("TestAction", "TestResource");

        auditEvent.Properties["custom"] = "value";
        auditEvent.Properties["number"] = 123;

        auditEvent.Properties.Should().ContainKey("custom");
        auditEvent.Properties.Should().ContainKey("number");
        auditEvent.Properties["custom"].Should().Be("value");
        auditEvent.Properties["number"].Should().Be(123);
    }
}