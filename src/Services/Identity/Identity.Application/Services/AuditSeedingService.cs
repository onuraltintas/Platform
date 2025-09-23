using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

/// <summary>
/// Audit system seed data service
/// </summary>
public class AuditSeedingService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<AuditSeedingService> _logger;

    public AuditSeedingService(
        IdentityDbContext context,
        ILogger<AuditSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed sample audit events for demonstration
    /// </summary>
    public async Task SeedSampleAuditEventsAsync()
    {
        var existingEvents = await _context.AuditEvents.CountAsync();
        if (existingEvents > 0)
        {
            _logger.LogInformation("Audit events already exist, skipping seed");
            return;
        }

        var events = GetSampleAuditEvents();

        foreach (var auditEvent in events)
        {
            _context.AuditEvents.Add(auditEvent);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample audit events", events.Count);
    }

    /// <summary>
    /// Seed sample security alerts for demonstration
    /// </summary>
    public async Task SeedSampleSecurityAlertsAsync()
    {
        var existingAlerts = await _context.SecurityAlerts.CountAsync();
        if (existingAlerts > 0)
        {
            _logger.LogInformation("Security alerts already exist, skipping seed");
            return;
        }

        var alerts = GetSampleSecurityAlerts();

        foreach (var alert in alerts)
        {
            _context.SecurityAlerts.Add(alert);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample security alerts", alerts.Count);
    }

    private List<AuditEvent> GetSampleAuditEvents()
    {
        var baseTime = DateTime.UtcNow.AddHours(-24); // 24 hours ago

        return new List<AuditEvent>
        {
            new AuditEvent
            {
                EventType = "User.Login",
                EntityType = "User",
                EntityId = "system-user-1",
                UserId = "system-user-1",
                UserName = "admin@platform.com",
                Action = "Login",
                Description = "User successfully logged in",
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                SessionId = Guid.NewGuid().ToString(),
                Metadata = """{"loginMethod": "password", "rememberMe": false, "location": "Corporate Office"}""",
                Severity = 0, // Info
                Category = 1, // Authentication
                IsSecurityEvent = true,
                RiskLevel = "Low",
                Source = "Identity.API",
                Tags = """{"category": "authentication", "outcome": "success"}""",
                Duration = 1250,
                IsSuccessful = true,
                Timestamp = baseTime.AddHours(1)
            },
            new AuditEvent
            {
                EventType = "User.PermissionGrant",
                EntityType = "Permission",
                EntityId = "perm-001",
                UserId = "system-user-1",
                UserName = "admin@platform.com",
                Action = "Grant",
                Description = "Permission granted to user role",
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                SessionId = Guid.NewGuid().ToString(),
                Metadata = """{"targetUser": "user@platform.com", "permission": "Users.Read", "reason": "Role assignment"}""",
                OldValues = """{"permissions": []}""",
                NewValues = """{"permissions": ["Users.Read"]}""",
                Severity = 1, // Warning
                Category = 2, // Authorization
                IsSecurityEvent = true,
                RiskLevel = "Medium",
                Source = "Identity.API",
                Tags = """{"category": "authorization", "outcome": "success", "critical": true}""",
                Duration = 2100,
                IsSuccessful = true,
                Timestamp = baseTime.AddHours(2)
            },
            new AuditEvent
            {
                EventType = "User.FailedLogin",
                EntityType = "User",
                EntityId = "unknown",
                UserId = "system",
                UserName = "unknown",
                Action = "Login",
                Description = "Failed login attempt with invalid credentials",
                IpAddress = "203.0.113.42",
                UserAgent = "curl/7.68.0",
                SessionId = Guid.NewGuid().ToString(),
                Metadata = """{"attemptedUsername": "admin", "reason": "invalid_credentials", "location": "Unknown"}""",
                Severity = 2, // Error
                Category = 1, // Authentication
                IsSecurityEvent = true,
                RiskLevel = "High",
                Source = "Identity.API",
                Tags = """{"category": "authentication", "outcome": "failure", "suspicious": true}""",
                Duration = 850,
                IsSuccessful = false,
                ErrorMessage = "Invalid username or password",
                Timestamp = baseTime.AddHours(3)
            },
            new AuditEvent
            {
                EventType = "System.ConfigurationChange",
                EntityType = "SecurityPolicy",
                EntityId = "policy-001",
                UserId = "system-user-1",
                UserName = "admin@platform.com",
                Action = "Update",
                Description = "Security policy configuration updated",
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                SessionId = Guid.NewGuid().ToString(),
                Metadata = """{"policyType": "Device Trust", "changeType": "threshold_update"}""",
                OldValues = """{"minimumTrustScore": 60}""",
                NewValues = """{"minimumTrustScore": 70}""",
                Severity = 1, // Warning
                Category = 4, // Configuration
                IsSecurityEvent = true,
                RiskLevel = "Medium",
                Source = "Identity.API",
                Tags = """{"category": "configuration", "outcome": "success", "impact": "high"}""",
                Duration = 1800,
                IsSuccessful = true,
                Timestamp = baseTime.AddHours(4)
            }
        };
    }

    private List<SecurityAlert> GetSampleSecurityAlerts()
    {
        var baseTime = DateTime.UtcNow.AddHours(-12); // 12 hours ago

        return new List<SecurityAlert>
        {
            new SecurityAlert
            {
                AlertType = "MultipleFailedLogins",
                Title = "Multiple Failed Login Attempts",
                Description = "5 consecutive failed login attempts detected from IP 203.0.113.42",
                Severity = "High",
                Status = "New",
                UserId = null,
                DeviceId = null,
                IpAddress = "203.0.113.42",
                SessionId = null,
                AlertData = """{"attemptCount": 5, "timeWindow": "15m", "usernames": ["admin", "administrator", "root"]}""",
                TriggerConditions = """{"failedLogins": {"count": 5, "timeWindow": "15m"}, "sameIP": true}""",
                RuleId = "rule-001",
                RuleName = "Multiple Failed Logins",
                IsAutoGenerated = true,
                CorrelationId = Guid.NewGuid().ToString(),
                Priority = 200,
                ConfidenceScore = 95.0,
                CreatedAt = baseTime.AddHours(1)
            },
            new SecurityAlert
            {
                AlertType = "LowTrustScore",
                Title = "User Trust Score Below Threshold",
                Description = "User trust score dropped to 25, below minimum threshold of 30",
                Severity = "Critical",
                Status = "Acknowledged",
                UserId = "user-at-risk",
                DeviceId = "device-123",
                IpAddress = "198.51.100.10",
                SessionId = Guid.NewGuid().ToString(),
                AlertData = """{"currentScore": 25, "threshold": 30, "factors": ["unusual_location", "new_device", "off_hours"]}""",
                TriggerConditions = """{"trustScore": {"operator": "lt", "value": 30}, "timeWindow": "5m"}""",
                RuleId = "rule-002",
                RuleName = "Low Trust Score Alert",
                AcknowledgedAt = baseTime.AddHours(2),
                AcknowledgedBy = "security-analyst-1",
                IsAutoGenerated = true,
                CorrelationId = Guid.NewGuid().ToString(),
                Priority = 100,
                ConfidenceScore = 88.0,
                CreatedAt = baseTime.AddHours(1.5),
                Notes = "User contacted for verification. Legitimate access confirmed."
            },
            new SecurityAlert
            {
                AlertType = "UnusualLocation",
                Title = "Access from Unusual Location",
                Description = "User login from new geographic location: Tokyo, Japan",
                Severity = "Medium",
                Status = "Resolved",
                UserId = "frequent-traveler",
                DeviceId = "device-456",
                IpAddress = "202.12.27.33",
                SessionId = Guid.NewGuid().ToString(),
                AlertData = """{"newLocation": {"country": "Japan", "city": "Tokyo"}, "lastLocation": {"country": "USA", "city": "New York"}, "distance": 6740}""",
                TriggerConditions = """{"location": {"newCountry": true, "distance": {"gt": 1000}}, "timeWindow": "1h"}""",
                RuleId = "rule-003",
                RuleName = "Unusual Location Access",
                AcknowledgedAt = baseTime.AddHours(3),
                AcknowledgedBy = "security-analyst-2",
                ResolvedAt = baseTime.AddHours(4),
                ResolvedBy = "security-analyst-2",
                Resolution = "User confirmed business travel. Location added to known locations.",
                IsAutoGenerated = true,
                CorrelationId = Guid.NewGuid().ToString(),
                Priority = 300,
                ConfidenceScore = 75.0,
                CreatedAt = baseTime.AddHours(2.5)
            }
        };
    }
}