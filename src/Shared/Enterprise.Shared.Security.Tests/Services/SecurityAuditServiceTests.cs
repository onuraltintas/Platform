namespace Enterprise.Shared.Security.Tests.Services;

[TestFixture]
public class SecurityAuditServiceTests
{
    private Mock<ILogger<SecurityAuditService>> _logger = null!;
    private IMemoryCache _cache = null!;
    private SecuritySettings _settings = null!;
    private SecurityAuditService _securityAuditService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<SecurityAuditService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _settings = new SecuritySettings
        {
            MaxFailedLoginAttempts = 5,
            FailedLoginWindowMinutes = 30,
            IpBlockDurationMinutes = 60
        };

        var options = Options.Create(_settings);
        _securityAuditService = new SecurityAuditService(_logger.Object, _cache, options);
    }

    [TearDown]
    public void TearDown()
    {
        _cache?.Dispose();
    }

    #region Authentication Success Tests

    [Test]
    public async Task LogAuthenticationSuccessAsync_WithValidData_LogsEvent()
    {
        // Arrange
        var userId = "user123";
        var method = "Password";
        var additionalData = new Dictionary<string, object> { ["ip"] = "192.168.1.1" };

        // Act
        await _securityAuditService.LogAuthenticationSuccessAsync(userId, method, additionalData);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(SecurityEventType.Authentication);
        securityEvent.UserId.Should().Be(userId);
        securityEvent.Description.Should().Contain(method);
        securityEvent.Severity.Should().Be(SecurityEventSeverity.Information);
        securityEvent.AdditionalData.Should().BeEquivalentTo(additionalData);
    }

    [Test]
    public async Task LogAuthenticationSuccessAsync_WithNullAdditionalData_LogsEvent()
    {
        // Arrange
        var userId = "user123";
        var method = "OAuth";

        // Act
        await _securityAuditService.LogAuthenticationSuccessAsync(userId, method);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.AdditionalData.Should().BeEmpty();
    }

    #endregion

    #region Authentication Failure Tests

    [Test]
    public async Task LogAuthenticationFailureAsync_WithValidData_LogsEvent()
    {
        // Arrange
        var identifier = "user@example.com";
        var method = "Password";
        var reason = "Invalid password";

        // Act
        await _securityAuditService.LogAuthenticationFailureAsync(identifier, method, reason);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(SecurityEventType.Authentication);
        securityEvent.UserId.Should().Be(identifier);
        securityEvent.Description.Should().Contain(method).And.Contain(reason);
        securityEvent.Severity.Should().Be(SecurityEventSeverity.Medium);
    }

    [Test]
    public async Task LogAuthenticationFailureAsync_MultipleFailures_TriggersLockout()
    {
        // Arrange
        var identifier = "user@example.com";
        var method = "Password";
        var reason = "Invalid password";

        // Act - Generate multiple failures to trigger lockout
        for (int i = 0; i < 5; i++)
        {
            await _securityAuditService.LogAuthenticationFailureAsync(identifier, method, reason);
        }

        // Verify events were logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(6); // 5 failures + 1 lockout
        events.Should().Contain(e => e.EventType == SecurityEventType.AccountLockout);
    }

    #endregion

    #region Authorization Failure Tests

    [Test]
    public async Task LogAuthorizationFailureAsync_WithValidData_LogsEvent()
    {
        // Arrange
        var userId = "user123";
        var resource = "AdminPanel";
        var action = "View";

        // Act
        await _securityAuditService.LogAuthorizationFailureAsync(userId, resource, action);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(SecurityEventType.Authorization);
        securityEvent.UserId.Should().Be(userId);
        securityEvent.Description.Should().Contain(action).And.Contain(resource);
        securityEvent.Severity.Should().Be(SecurityEventSeverity.Medium);
    }

    #endregion

    #region Security Event Tests

    [Test]
    public async Task LogSecurityEventAsync_WithCriticalSeverity_LogsEvent()
    {
        // Arrange
        var eventType = SecurityEventType.SecurityPolicyViolation;
        var description = "Critical security violation detected";
        var severity = SecurityEventSeverity.Critical;

        // Act
        await _securityAuditService.LogSecurityEventAsync(eventType, description, severity);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(eventType);
        securityEvent.Description.Should().Be(description);
        securityEvent.Severity.Should().Be(severity);
    }

    [Test]
    public async Task LogSecurityEventAsync_WithManyEvents_LimitsEventCount()
    {
        // Act - Log more than 10000 events to test memory management
        for (int i = 0; i < 10100; i++)
        {
            await _securityAuditService.LogSecurityEventAsync(
                SecurityEventType.DataAccess, 
                $"Event {i}", 
                SecurityEventSeverity.Information);
        }

        // Verify events are limited
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddHours(-1), 
            DateTime.UtcNow.AddHours(1));

        // Assert
        events.Should().HaveCount(10000); // Should be limited to 10000
    }

    #endregion

    #region Suspicious Activity Tests

    [Test]
    public async Task LogSuspiciousActivityAsync_WithValidData_LogsEvent()
    {
        // Arrange
        var source = "192.168.1.100";
        var activity = "Multiple failed login attempts";

        // Act
        await _securityAuditService.LogSuspiciousActivityAsync(source, activity);

        // Verify event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(SecurityEventType.SuspiciousActivity);
        securityEvent.Description.Should().Contain(source).And.Contain(activity);
        securityEvent.Severity.Should().Be(SecurityEventSeverity.High);
    }

    #endregion

    #region Get Security Events Tests

    [Test]
    public async Task GetSecurityEventsAsync_WithDateRange_ReturnsFilteredEvents()
    {
        // Arrange
        var from = DateTime.UtcNow.AddMinutes(-5);
        var to = DateTime.UtcNow.AddMinutes(5);

        await _securityAuditService.LogSecurityEventAsync(
            SecurityEventType.Authentication, 
            "Test event", 
            SecurityEventSeverity.Information);

        // Act
        var events = await _securityAuditService.GetSecurityEventsAsync(from, to);

        // Assert
        events.Should().HaveCount(1);
        events.First().OccurredAt.Should().BeOnOrAfter(from).And.BeOnOrBefore(to);
    }

    [Test]
    public async Task GetSecurityEventsAsync_WithEventTypeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        await _securityAuditService.LogSecurityEventAsync(
            SecurityEventType.Authentication, 
            "Auth event", 
            SecurityEventSeverity.Information);
        
        await _securityAuditService.LogSecurityEventAsync(
            SecurityEventType.Authorization, 
            "Auth event", 
            SecurityEventSeverity.Medium);

        var from = DateTime.UtcNow.AddMinutes(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        // Act
        var authEvents = await _securityAuditService.GetSecurityEventsAsync(from, to, SecurityEventType.Authentication);

        // Assert
        authEvents.Should().HaveCount(1);
        authEvents.First().EventType.Should().Be(SecurityEventType.Authentication);
    }

    [Test]
    public async Task GetSecurityEventsAsync_WithNoEvents_ReturnsEmpty()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(-1).AddHours(1);

        // Act
        var events = await _securityAuditService.GetSecurityEventsAsync(from, to);

        // Assert
        events.Should().BeEmpty();
    }

    #endregion

    #region Failed Login Attempts Tests

    [Test]
    public async Task GetFailedLoginAttemptsAsync_WithRecentAttempts_ReturnsCount()
    {
        // Arrange
        var identifier = "user@example.com";
        var window = TimeSpan.FromMinutes(30);

        // Log some failed attempts
        for (int i = 0; i < 3; i++)
        {
            await _securityAuditService.LogAuthenticationFailureAsync(identifier, "Password", "Invalid");
        }

        // Act
        var count = await _securityAuditService.GetFailedLoginAttemptsAsync(identifier, window);

        // Assert
        count.Should().Be(3);
    }

    [Test]
    public async Task GetFailedLoginAttemptsAsync_WithEmptyIdentifier_ReturnsZero()
    {
        // Act
        var count = await _securityAuditService.GetFailedLoginAttemptsAsync("", TimeSpan.FromMinutes(30));

        // Assert
        count.Should().Be(0);
    }

    [Test]
    public async Task GetFailedLoginAttemptsAsync_WithOldAttempts_ReturnsZero()
    {
        // Arrange
        var identifier = "user@example.com";
        var shortWindow = TimeSpan.FromMilliseconds(1);

        // Log failed attempt
        await _securityAuditService.LogAuthenticationFailureAsync(identifier, "Password", "Invalid");

        // Wait to make sure the attempt is outside the window
        await Task.Delay(10);

        // Act
        var count = await _securityAuditService.GetFailedLoginAttemptsAsync(identifier, shortWindow);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region IP Blocking Tests

    [Test]
    public async Task BlockIpAddressAsync_WithValidData_BlocksIp()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var duration = TimeSpan.FromMinutes(60);
        var reason = "Suspicious activity";

        // Act
        await _securityAuditService.BlockIpAddressAsync(ipAddress, duration, reason);
        var isBlocked = await _securityAuditService.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeTrue();
    }

    [Test]
    public async Task IsIpBlockedAsync_WithNonBlockedIp_ReturnsFalse()
    {
        // Arrange
        var ipAddress = "192.168.1.101";

        // Act
        var isBlocked = await _securityAuditService.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeFalse();
    }

    [Test]
    public async Task IsIpBlockedAsync_WithEmptyIp_ReturnsFalse()
    {
        // Act
        var isBlocked = await _securityAuditService.IsIpBlockedAsync("");

        // Assert
        isBlocked.Should().BeFalse();
    }

    [Test]
    public async Task UnblockIpAddressAsync_WithBlockedIp_UnblocksIp()
    {
        // Arrange
        var ipAddress = "192.168.1.102";
        var duration = TimeSpan.FromMinutes(60);
        var reason = "Test block";

        await _securityAuditService.BlockIpAddressAsync(ipAddress, duration, reason);

        // Act
        await _securityAuditService.UnblockIpAddressAsync(ipAddress);
        var isBlocked = await _securityAuditService.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeFalse();
    }

    [Test]
    public async Task BlockIpAddressAsync_WithEmptyIp_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _securityAuditService.BlockIpAddressAsync("", TimeSpan.FromMinutes(60), "reason"));
    }

    [Test]
    public async Task UnblockIpAddressAsync_WithEmptyIp_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _securityAuditService.UnblockIpAddressAsync(""));
    }

    [Test]
    public async Task BlockIpAddressAsync_LogsSecurityEvent()
    {
        // Arrange
        var ipAddress = "192.168.1.103";
        var duration = TimeSpan.FromMinutes(60);
        var reason = "Multiple failed attempts";

        // Act
        await _securityAuditService.BlockIpAddressAsync(ipAddress, duration, reason);

        // Verify security event was logged
        var events = await _securityAuditService.GetSecurityEventsAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        events.Should().HaveCount(1);
        var securityEvent = events.First();
        securityEvent.EventType.Should().Be(SecurityEventType.SuspiciousActivity);
        securityEvent.Description.Should().Contain(ipAddress).And.Contain(reason);
        securityEvent.Severity.Should().Be(SecurityEventSeverity.High);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task GetSecurityEventsAsync_OrdersByDateDescending()
    {
        // Arrange
        await _securityAuditService.LogSecurityEventAsync(
            SecurityEventType.Authentication, 
            "First event", 
            SecurityEventSeverity.Information);

        await Task.Delay(10); // Ensure different timestamps

        await _securityAuditService.LogSecurityEventAsync(
            SecurityEventType.Authorization, 
            "Second event", 
            SecurityEventSeverity.Medium);

        var from = DateTime.UtcNow.AddMinutes(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        // Act
        var events = await _securityAuditService.GetSecurityEventsAsync(from, to);

        // Assert
        events.Should().HaveCount(2);
        events.First().Description.Should().Be("Second event"); // Most recent first
        events.Last().Description.Should().Be("First event");
    }

    [Test]
    public async Task IsIpBlockedAsync_WithExpiredBlock_ReturnsFalse()
    {
        // Arrange
        var ipAddress = "192.168.1.104";
        var shortDuration = TimeSpan.FromMilliseconds(1);
        var reason = "Test expiration";

        await _securityAuditService.BlockIpAddressAsync(ipAddress, shortDuration, reason);

        // Wait for block to expire
        await Task.Delay(10);

        // Act
        var isBlocked = await _securityAuditService.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeFalse();
    }

    [Test]
    public async Task IsIpBlockedAsync_UsesCaching()
    {
        // Arrange
        var ipAddress = "192.168.1.105";
        var duration = TimeSpan.FromMinutes(60);
        var reason = "Cache test";

        await _securityAuditService.BlockIpAddressAsync(ipAddress, duration, reason);

        // Act - Multiple calls should use cache
        var isBlocked1 = await _securityAuditService.IsIpBlockedAsync(ipAddress);
        var isBlocked2 = await _securityAuditService.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked1.Should().BeTrue();
        isBlocked2.Should().BeTrue();
    }

    #endregion
}