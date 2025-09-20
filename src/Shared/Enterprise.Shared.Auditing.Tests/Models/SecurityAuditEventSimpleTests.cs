namespace Enterprise.Shared.Auditing.Tests.Models;

public class SecurityAuditEventSimpleTests
{
    [Fact]
    public void Constructor_SetsSecurityDefaults()
    {
        var securityEvent = new SecurityAuditEvent();

        // Category is set in base class as Application by default
        securityEvent.EventType.Should().Be(SecurityEventType.Authentication);
        securityEvent.Outcome.Should().Be(SecurityOutcome.Success);
        securityEvent.RiskScore.Should().Be(0);
        securityEvent.IsAlert.Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidParameters_ReturnsConfiguredEvent()
    {
        var eventType = SecurityEventType.Authorization;
        var action = "AccessResource";
        var resource = "SecureDocument";

        var securityEvent = SecurityAuditEvent.Create(eventType, action, resource);

        securityEvent.EventType.Should().Be(eventType);
        securityEvent.Action.Should().Be(action);
        securityEvent.Resource.Should().Be(resource);
        securityEvent.Category.Should().Be(AuditEventCategory.Security);
        securityEvent.Result.Should().Be("Success");
    }

    [Fact]
    public void WithAuthentication_SetsAuthenticationProperties()
    {
        var method = "JWT";
        var role = "Manager";

        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.Authentication, "Login", "Application")
            .WithAuthentication(method, role);

        securityEvent.AuthenticationMethod.Should().Be(method);
        securityEvent.Role.Should().Be(role);
    }

    [Fact]
    public void WithAuthorization_SetsAuthorizationProperties()
    {
        var permission = "read:documents";
        var role = "Manager";

        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.Authorization, "AccessResource", "Document")
            .WithAuthorization(permission, role);

        securityEvent.Permission.Should().Be(permission);
        securityEvent.Role.Should().Be(role);
    }

    [Fact]
    public void WithRisk_SetsRiskProperties()
    {
        var riskScore = 85;
        var isHighRisk = true;

        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.DataAccess, "ViewData", "CustomerRecords")
            .WithRisk(riskScore, isHighRisk);

        securityEvent.RiskScore.Should().Be(riskScore);
        securityEvent.IsAlert.Should().Be(isHighRisk);
    }

    [Fact]
    public void FactoryMethod_CreateFailedLogin_SetsCorrectProperties()
    {
        var username = "testuser";
        var reason = "InvalidPassword";

        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.Authentication, "Login", "Authentication");
        securityEvent.Outcome = SecurityOutcome.Failed;
        securityEvent.Username = username;
        securityEvent.Properties["FailureReason"] = reason;

        securityEvent.EventType.Should().Be(SecurityEventType.Authentication);
        securityEvent.Action.Should().Be("Login");
        securityEvent.Resource.Should().Be("Authentication");
        securityEvent.Outcome.Should().Be(SecurityOutcome.Failed);
        securityEvent.Username.Should().Be(username);
        securityEvent.Properties["FailureReason"].Should().Be(reason);
    }

    [Fact]
    public void FactoryMethod_CreateSuccessfulLogin_SetsCorrectProperties()
    {
        var userId = "user123";
        var username = "testuser";
        var sessionId = "session456";

        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.Authentication, "Login", "Authentication");
        securityEvent.Outcome = SecurityOutcome.Success;
        securityEvent.UserId = userId;
        securityEvent.Username = username;
        securityEvent.SessionId = sessionId;

        securityEvent.EventType.Should().Be(SecurityEventType.Authentication);
        securityEvent.Action.Should().Be("Login");
        securityEvent.Resource.Should().Be("Authentication");
        securityEvent.Outcome.Should().Be(SecurityOutcome.Success);
        securityEvent.UserId.Should().Be(userId);
        securityEvent.Username.Should().Be(username);
        securityEvent.SessionId.Should().Be(sessionId);
    }

    [Theory]
    [InlineData(SecurityEventType.Authentication)]
    [InlineData(SecurityEventType.Authorization)]
    [InlineData(SecurityEventType.DataAccess)]
    [InlineData(SecurityEventType.AccountManagement)]
    public void Create_WithDifferentEventTypes_SetsCorrectType(SecurityEventType eventType)
    {
        var securityEvent = SecurityAuditEvent.Create(eventType, "TestAction", "TestResource");

        securityEvent.EventType.Should().Be(eventType);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(50, false)]
    [InlineData(74, false)]
    [InlineData(75, true)]
    [InlineData(100, true)]
    public void WithRisk_SetIsAlertBasedOnThreshold(int riskScore, bool expectedAlert)
    {
        var securityEvent = SecurityAuditEvent.Create(SecurityEventType.DataAccess, "ViewData", "Records")
            .WithRisk(riskScore, expectedAlert);

        securityEvent.IsAlert.Should().Be(expectedAlert);
    }
}