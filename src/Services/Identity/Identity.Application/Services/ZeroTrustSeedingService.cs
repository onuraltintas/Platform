using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

/// <summary>
/// Zero Trust system seed data service
/// </summary>
public class ZeroTrustSeedingService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<ZeroTrustSeedingService> _logger;

    public ZeroTrustSeedingService(
        IdentityDbContext context,
        ILogger<ZeroTrustSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed default security policies
    /// </summary>
    public async Task SeedSecurityPoliciesAsync()
    {
        var existingPolicies = await _context.SecurityPolicies.CountAsync();
        if (existingPolicies > 0)
        {
            _logger.LogInformation("Security policies already exist, skipping seed");
            return;
        }

        var policies = GetDefaultSecurityPolicies();

        foreach (var policy in policies)
        {
            _context.SecurityPolicies.Add(policy);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default security policies", policies.Count);
    }

    /// <summary>
    /// Seed default alert rules
    /// </summary>
    public async Task SeedAlertRulesAsync()
    {
        var existingRules = await _context.AlertRules.CountAsync();
        if (existingRules > 0)
        {
            _logger.LogInformation("Alert rules already exist, skipping seed");
            return;
        }

        var rules = GetDefaultAlertRules();

        foreach (var rule in rules)
        {
            _context.AlertRules.Add(rule);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default alert rules", rules.Count);
    }

    private List<SecurityPolicy> GetDefaultSecurityPolicies()
    {
        return new List<SecurityPolicy>
        {
            new SecurityPolicy
            {
                Name = "Device Trust Policy",
                Description = "Minimum trust score required for device access",
                Category = "Device",
                PolicyType = "Device",
                MinimumTrustScore = 70.0m,
                Severity = "High",
                Rules = """{"minimumTrustScore": 70, "requireKnownDevice": true, "allowUnmanagedDevices": false}""",
                Conditions = """{"deviceCompliance": true, "certificateRequired": false, "locationVerification": false}""",
                IsActive = true,
                IsEnforced = true,
                Priority = 100,
                CreatedBy = "System"
            },
            new SecurityPolicy
            {
                Name = "Network Access Policy",
                Description = "Network-based access control policy",
                Category = "Network",
                PolicyType = "Network",
                MinimumTrustScore = 60.0m,
                Severity = "Medium",
                Rules = """{"allowedNetworks": ["corporate", "vpn"], "blockSuspiciousIPs": true, "requireVPN": false}""",
                Conditions = """{"ipWhitelist": false, "geoBlocking": false, "threatIntelligence": true}""",
                IsActive = true,
                IsEnforced = true,
                Priority = 200,
                CreatedBy = "System"
            },
            new SecurityPolicy
            {
                Name = "Behavioral Analysis Policy",
                Description = "User behavior monitoring and anomaly detection",
                Category = "Behavior",
                PolicyType = "Behavior",
                MinimumTrustScore = 50.0m,
                Severity = "Medium",
                Rules = """{"trackLoginPatterns": true, "detectAnomalies": true, "suspiciousActivityThreshold": 80}""",
                Conditions = """{"timeBasedAccess": false, "unusualLocationAlert": true, "multipleFailedAttempts": 5}""",
                IsActive = true,
                IsEnforced = false, // Monitor only initially
                Priority = 300,
                CreatedBy = "System"
            },
            new SecurityPolicy
            {
                Name = "Authentication Strength Policy",
                Description = "Multi-factor authentication requirements",
                Category = "Authentication",
                PolicyType = "Authentication",
                MinimumTrustScore = 80.0m,
                Severity = "High",
                Rules = """{"requireMFA": true, "passwordComplexity": "high", "sessionTimeout": 3600}""",
                Conditions = """{"privilegedAccess": true, "sensitiveOperations": true, "adminRoles": true}""",
                IsActive = true,
                IsEnforced = true,
                Priority = 50,
                CreatedBy = "System"
            }
        };
    }

    private List<AlertRule> GetDefaultAlertRules()
    {
        return new List<AlertRule>
        {
            new AlertRule
            {
                Name = "Low Trust Score Alert",
                Description = "Alert when trust score drops below threshold",
                Category = "Security",
                Severity = "High",
                Conditions = """{"trustScore": {"operator": "lt", "value": 30}, "timeWindow": "5m"}""",
                Actions = """{"notifications": ["email", "webhook"], "autoBlock": false, "requireReview": true}""",
                NotificationChannels = """["security-team@platform.com", "soc@platform.com"]""",
                CooldownPeriod = TimeSpan.FromMinutes(10),
                MaxAlertsPerHour = 5,
                IsActive = true,
                Priority = 100,
                CreatedBy = "System"
            },
            new AlertRule
            {
                Name = "Multiple Failed Logins",
                Description = "Alert on multiple failed login attempts",
                Category = "Authentication",
                Severity = "Medium",
                Conditions = """{"failedLogins": {"count": 5, "timeWindow": "15m"}, "sameIP": true}""",
                Actions = """{"notifications": ["email"], "temporaryLock": true, "duration": "30m"}""",
                NotificationChannels = """["security-team@platform.com"]""",
                CooldownPeriod = TimeSpan.FromMinutes(15),
                MaxAlertsPerHour = 10,
                IsActive = true,
                Priority = 200,
                CreatedBy = "System"
            },
            new AlertRule
            {
                Name = "Unusual Location Access",
                Description = "Alert when access from unusual geographic location",
                Category = "Security",
                Severity = "Medium",
                Conditions = """{"location": {"newCountry": true, "distance": {"gt": 1000}}, "timeWindow": "1h"}""",
                Actions = """{"notifications": ["email"], "requireAdditionalAuth": true}""",
                NotificationChannels = """["user", "security-team@platform.com"]""",
                CooldownPeriod = TimeSpan.FromHours(6),
                MaxAlertsPerHour = 3,
                IsActive = true,
                Priority = 300,
                CreatedBy = "System"
            },
            new AlertRule
            {
                Name = "Device Compliance Violation",
                Description = "Alert when device fails compliance checks",
                Category = "Compliance",
                Severity = "High",
                Conditions = """{"device": {"compliance": false}, "criticalViolations": {"gt": 0}}""",
                Actions = """{"notifications": ["email", "webhook"], "blockDevice": true, "requireCompliance": true}""",
                NotificationChannels = """["security-team@platform.com", "compliance@platform.com"]""",
                CooldownPeriod = TimeSpan.FromHours(1),
                MaxAlertsPerHour = 2,
                IsActive = true,
                Priority = 50,
                CreatedBy = "System"
            },
            new AlertRule
            {
                Name = "Privileged Account Activity",
                Description = "Monitor all privileged account activities",
                Category = "Audit",
                Severity = "High",
                Conditions = """{"userRoles": ["SuperAdmin", "Admin"], "sensitiveOperations": true}""",
                Actions = """{"notifications": ["email", "sms"], "auditLog": true, "requireApproval": false}""",
                NotificationChannels = """["audit-team@platform.com", "ciso@platform.com"]""",
                CooldownPeriod = TimeSpan.FromMinutes(5),
                MaxAlertsPerHour = 20,
                IsActive = true,
                Priority = 10,
                CreatedBy = "System"
            }
        };
    }
}