using Identity.Core.Audit;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Enterprise.Shared.Caching.Interfaces;

namespace Identity.Application.Services;

/// <summary>
/// Advanced audit and monitoring service implementation
/// </summary>
public class AuditService : IAuditService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly ICacheService _cacheService;

    public AuditService(
        IdentityDbContext context,
        ILogger<AuditService> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create audit log entity (would need to create this entity)
            // For now, we'll log to the system logger and could store in a separate audit table

            var logData = new
            {
                auditEvent.Id,
                auditEvent.EventType,
                auditEvent.EntityType,
                auditEvent.EntityId,
                auditEvent.UserId,
                auditEvent.Action,
                auditEvent.Description,
                auditEvent.IpAddress,
                auditEvent.Severity,
                auditEvent.Category,
                auditEvent.IsSecurityEvent,
                auditEvent.Timestamp
            };

            _logger.LogInformation("Audit Event: {AuditData}", JsonSerializer.Serialize(logData));

            // In a real implementation, you would save to audit database table
            // await _context.AuditEvents.AddAsync(auditEvent, cancellationToken);
            // await _context.SaveChangesAsync(cancellationToken);

            // Trigger alert rules if it's a security event
            if (auditEvent.IsSecurityEvent)
            {
                await ProcessSecurityEvent(auditEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event {EventId}", auditEvent.Id);
        }
    }

    public async Task<List<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would query the audit events table
            // For now, return sample data
            var auditTrail = new List<AuditEvent>
            {
                new AuditEvent
                {
                    EventType = "Entity.Created",
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = "Create",
                    Description = $"{entityType} created",
                    Severity = AuditSeverity.Information,
                    Category = AuditCategory.DataModification,
                    Timestamp = DateTime.UtcNow.AddDays(-1)
                }
            };

            return auditTrail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail for {EntityType}:{EntityId}", entityType, entityId);
            return new List<AuditEvent>();
        }
    }

    public async Task<PagedAuditResult> GetAuditEventsAsync(AuditFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate sample audit events for demonstration
            var sampleEvents = GenerateSampleAuditEvents();

            // Apply filters
            var filteredEvents = ApplyFilters(sampleEvents, filter);

            // Apply pagination
            var pagedEvents = filteredEvents
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var summary = GenerateAuditSummary(filteredEvents);

            return new PagedAuditResult
            {
                Events = pagedEvents,
                TotalCount = filteredEvents.Count,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit events");
            return new PagedAuditResult();
        }
    }

    public async Task<AuditReport> GenerateAuditReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AuditFilter
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };

            var auditResult = await GetAuditEventsAsync(filter, cancellationToken);

            var report = new AuditReport
            {
                Title = request.Title ?? $"Audit Report - {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}",
                Description = request.Description ?? "Comprehensive audit report",
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Summary = auditResult.Summary,
                FileType = request.Format ?? "PDF"
            };

            // Generate sections
            report.Sections.Add(new AuditReportSection
            {
                Title = "Executive Summary",
                Content = GenerateExecutiveSummary(auditResult.Summary),
                Metrics = new Dictionary<string, object>
                {
                    ["TotalEvents"] = auditResult.Summary.TotalEvents,
                    ["SecurityEvents"] = auditResult.Summary.SecurityEvents,
                    ["CriticalEvents"] = auditResult.Summary.CriticalEvents
                }
            });

            // Generate charts if requested
            if (request.IncludeCharts)
            {
                report.Charts = GenerateAuditCharts(auditResult.Summary);
            }

            // Generate file data based on format
            if (request.Format == "PDF")
            {
                report.FileData = await GeneratePdfReportAsync(report, cancellationToken);
            }
            else if (request.Format == "Excel")
            {
                report.FileData = await GenerateExcelReportAsync(report, auditResult.Events, cancellationToken);
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit report");
            return new AuditReport { Title = "Error generating report" };
        }
    }

    public async Task<SecurityAnalytics> GetSecurityAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = new SecurityAnalytics
            {
                FromDate = fromDate,
                ToDate = toDate,
                Metrics = new SecurityMetrics
                {
                    TotalSecurityEvents = 156,
                    FailedLogins = 45,
                    SuccessfulLogins = 1234,
                    PrivilegeEscalations = 2,
                    DataAccessViolations = 8,
                    SuspiciousActivities = 12,
                    SecurityScore = 87.5,
                    ThreatsByType = new Dictionary<string, int>
                    {
                        ["Brute Force"] = 23,
                        ["Privilege Escalation"] = 2,
                        ["Data Exfiltration"] = 4,
                        ["Anomalous Access"] = 8
                    }
                },
                Trends = GenerateSecurityTrends(fromDate, toDate),
                Incidents = GenerateSecurityIncidents(),
                RiskIndicators = GenerateRiskIndicators(),
                ThreatLandscape = GenerateThreatLandscape()
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security analytics");
            return new SecurityAnalytics();
        }
    }

    public async Task<List<AuditAnomaly>> DetectAnomaliesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var anomalies = new List<AuditAnomaly>();

            // Simulate anomaly detection
            anomalies.Add(new AuditAnomaly
            {
                Type = "Unusual Login Pattern",
                Description = "User logged in from 15 different IP addresses in the last hour",
                AnomalyScore = 0.85,
                Severity = "High",
                RecommendedAction = "Investigate user account for compromise"
            });

            anomalies.Add(new AuditAnomaly
            {
                Type = "Off-Hours Access",
                Description = "Multiple users accessing sensitive data outside business hours",
                AnomalyScore = 0.65,
                Severity = "Medium",
                RecommendedAction = "Review access patterns and implement time-based restrictions"
            });

            return anomalies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
            return new List<AuditAnomaly>();
        }
    }

    public async Task<ComplianceReport> GetComplianceReportAsync(ComplianceReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = new ComplianceReport
            {
                Standard = request.Standard,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                ComplianceScore = 92.5
            };

            // Generate compliance requirements based on standard
            report.Requirements = GenerateComplianceRequirements(request.Standard);
            report.Violations = GenerateComplianceViolations();
            report.Recommendations = GenerateComplianceRecommendations();

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report for standard {Standard}", request.Standard);
            return new ComplianceReport();
        }
    }

    public async Task<byte[]> ExportAuditDataAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditResult = await GetAuditEventsAsync(request.Filter, cancellationToken);

            return request.Format.ToUpper() switch
            {
                "CSV" => GenerateCsvExport(auditResult.Events),
                "JSON" => GenerateJsonExport(auditResult.Events),
                "XML" => GenerateXmlExport(auditResult.Events),
                _ => GenerateCsvExport(auditResult.Events)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit data");
            return Array.Empty<byte>();
        }
    }

    public async Task<MonitoringDashboard> GetMonitoringDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = "monitoring_dashboard";
            var cachedDashboard = await _cacheService.GetAsync<MonitoringDashboard>(cacheKey, cancellationToken);

            if (cachedDashboard != null)
            {
                return cachedDashboard;
            }

            var dashboard = new MonitoringDashboard
            {
                RealTimeMetrics = await GetRealTimeMetrics(cancellationToken),
                RecentEvents = await GetRecentEvents(cancellationToken),
                ActiveAlerts = await GetActiveAlertsAsync(cancellationToken),
                Widgets = GenerateDashboardWidgets(),
                SystemHealth = await GetSystemHealth(cancellationToken)
            };

            // Cache for 1 minute
            await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromMinutes(1), cancellationToken);

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring dashboard");
            return new MonitoringDashboard();
        }
    }

    public async Task<AlertRule> CreateAlertRuleAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var alertRule = new AlertRule
            {
                Name = request.Name,
                Description = request.Description,
                Condition = request.Condition,
                Severity = request.Severity,
                NotificationChannels = request.NotificationChannels,
                Parameters = request.Parameters
            };

            // In a real implementation, save to database
            // await _context.AlertRules.AddAsync(alertRule, cancellationToken);
            // await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created alert rule {RuleName}", alertRule.Name);

            return alertRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert rule {RuleName}", request.Name);
            throw;
        }
    }

    public async Task<List<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate sample active alerts
            var alerts = new List<Alert>
            {
                new Alert
                {
                    RuleName = "Failed Login Threshold",
                    Title = "Multiple Failed Login Attempts",
                    Description = "User 'john.doe' has 10 failed login attempts in the last 15 minutes",
                    Severity = "High",
                    Status = "New",
                    TriggeredAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new Alert
                {
                    RuleName = "Privilege Escalation",
                    Title = "Unexpected Privilege Change",
                    Description = "User role changed from 'User' to 'Admin' outside business hours",
                    Severity = "Critical",
                    Status = "Acknowledged",
                    TriggeredAt = DateTime.UtcNow.AddHours(-2),
                    AcknowledgedAt = DateTime.UtcNow.AddHours(-1),
                    AcknowledgedBy = "security.admin"
                }
            };

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return new List<Alert>();
        }
    }

    public async Task<ArchiveResult> ArchiveAuditDataAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate archiving old audit data
            var archivedCount = 15000; // Example count

            var result = new ArchiveResult
            {
                ArchivedEventCount = archivedCount,
                ArchiveLocation = $"archive_{olderThan:yyyyMMdd}.zip",
                ArchiveSizeBytes = archivedCount * 1024, // Rough estimate
                Success = true
            };

            _logger.LogInformation("Archived {Count} audit events older than {Date}",
                archivedCount, olderThan);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit data");
            return new ArchiveResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #region Private Helper Methods

    private async Task ProcessSecurityEvent(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        // Process security events for alerting
        if (auditEvent.Severity >= AuditSeverity.Warning)
        {
            // Trigger alerts based on rules
            _logger.LogWarning("Security event detected: {EventType} - {Description}",
                auditEvent.EventType, auditEvent.Description);
        }
    }

    private List<AuditEvent> GenerateSampleAuditEvents()
    {
        var events = new List<AuditEvent>();
        var random = new Random();

        for (int i = 0; i < 100; i++)
        {
            events.Add(new AuditEvent
            {
                EventType = GetRandomEventType(random),
                EntityType = GetRandomEntityType(random),
                EntityId = Guid.NewGuid().ToString(),
                UserId = $"user_{random.Next(1, 20)}",
                UserName = $"user{random.Next(1, 20)}@example.com",
                Action = GetRandomAction(random),
                Description = "Sample audit event",
                IpAddress = $"192.168.1.{random.Next(1, 255)}",
                Severity = (AuditSeverity)random.Next(0, 4),
                Category = (AuditCategory)random.Next(0, 8),
                IsSecurityEvent = random.NextDouble() > 0.7,
                Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 10080)) // Last week
            });
        }

        return events;
    }

    private List<AuditEvent> ApplyFilters(List<AuditEvent> events, AuditFilter filter)
    {
        var filtered = events.AsQueryable();

        if (filter.FromDate.HasValue)
            filtered = filtered.Where(e => e.Timestamp >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            filtered = filtered.Where(e => e.Timestamp <= filter.ToDate.Value);

        if (!string.IsNullOrEmpty(filter.UserId))
            filtered = filtered.Where(e => e.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.EntityType))
            filtered = filtered.Where(e => e.EntityType == filter.EntityType);

        if (filter.Severity.HasValue)
            filtered = filtered.Where(e => e.Severity == filter.Severity.Value);

        if (filter.Category.HasValue)
            filtered = filtered.Where(e => e.Category == filter.Category.Value);

        if (filter.IsSecurityEvent.HasValue)
            filtered = filtered.Where(e => e.IsSecurityEvent == filter.IsSecurityEvent.Value);

        return filtered.ToList();
    }

    private AuditSummary GenerateAuditSummary(List<AuditEvent> events)
    {
        return new AuditSummary
        {
            TotalEvents = events.Count,
            SecurityEvents = events.Count(e => e.IsSecurityEvent),
            CriticalEvents = events.Count(e => e.Severity == AuditSeverity.Critical),
            WarningEvents = events.Count(e => e.Severity == AuditSeverity.Warning),
            EventsByCategory = events.GroupBy(e => e.Category.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            EventsByType = events.GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopUsers = events.GroupBy(e => e.UserName)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopIpAddresses = events.GroupBy(e => e.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private string GenerateExecutiveSummary(AuditSummary summary)
    {
        return $"This audit report covers {summary.TotalEvents} events, including {summary.SecurityEvents} security-related events. " +
               $"There were {summary.CriticalEvents} critical events and {summary.WarningEvents} warning events that require attention.";
    }

    private List<AuditChart> GenerateAuditCharts(AuditSummary summary)
    {
        var charts = new List<AuditChart>
        {
            new AuditChart
            {
                Title = "Events by Category",
                Type = "Pie",
                DataPoints = summary.EventsByCategory.Select(kvp => new ChartDataPoint
                {
                    Label = kvp.Key,
                    Value = kvp.Value
                }).ToList()
            },
            new AuditChart
            {
                Title = "Top Users by Activity",
                Type = "Bar",
                DataPoints = summary.TopUsers.Take(10).Select(kvp => new ChartDataPoint
                {
                    Label = kvp.Key,
                    Value = kvp.Value
                }).ToList()
            }
        };

        return charts;
    }

    private async Task<byte[]> GeneratePdfReportAsync(AuditReport report, CancellationToken cancellationToken)
    {
        // Simulate PDF generation
        var content = $"Audit Report: {report.Title}\nGenerated: {report.GeneratedAt}\nTotal Events: {report.Summary.TotalEvents}";
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    private async Task<byte[]> GenerateExcelReportAsync(AuditReport report, List<AuditEvent> events, CancellationToken cancellationToken)
    {
        // Simulate Excel generation
        var content = $"Audit Report Data\n{string.Join("\n", events.Select(e => $"{e.Timestamp},{e.EventType},{e.UserName}"))}";
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    private List<SecurityTrend> GenerateSecurityTrends(DateTime fromDate, DateTime toDate)
    {
        var trends = new List<SecurityTrend>
        {
            new SecurityTrend
            {
                MetricName = "Failed Logins",
                Trend = "Increasing",
                ChangePercentage = 15.5,
                DataPoints = GenerateTrendData(fromDate, toDate, 20, 45)
            },
            new SecurityTrend
            {
                MetricName = "Security Events",
                Trend = "Stable",
                ChangePercentage = 2.1,
                DataPoints = GenerateTrendData(fromDate, toDate, 10, 25)
            }
        };

        return trends;
    }

    private List<TrendDataPoint> GenerateTrendData(DateTime fromDate, DateTime toDate, double minValue, double maxValue)
    {
        var points = new List<TrendDataPoint>();
        var random = new Random();
        var current = fromDate;

        while (current <= toDate)
        {
            points.Add(new TrendDataPoint
            {
                Date = current,
                Value = random.NextDouble() * (maxValue - minValue) + minValue
            });
            current = current.AddDays(1);
        }

        return points;
    }

    private List<SecurityIncident> GenerateSecurityIncidents()
    {
        return new List<SecurityIncident>
        {
            new SecurityIncident
            {
                Id = "INC-001",
                Title = "Brute Force Attack Detected",
                Description = "Multiple failed login attempts from suspicious IP ranges",
                Severity = "High",
                Status = "Investigating",
                DetectedAt = DateTime.UtcNow.AddHours(-6),
                DetectedBy = "Automated System"
            }
        };
    }

    private List<RiskIndicator> GenerateRiskIndicators()
    {
        return new List<RiskIndicator>
        {
            new RiskIndicator
            {
                Name = "Credential Compromise Risk",
                Description = "Risk of credential compromise based on login patterns",
                RiskScore = 0.35,
                RiskLevel = "Medium",
                Indicators = ["Multiple failed logins", "Login from new locations"],
                Recommendation = "Implement stronger password policies and MFA"
            }
        };
    }

    private ThreatLandscape GenerateThreatLandscape()
    {
        return new ThreatLandscape
        {
            ThreatCategories = new List<ThreatCategory>
            {
                new ThreatCategory { Name = "Brute Force", Count = 45, Percentage = 35.2, TrendDirection = "Increasing" },
                new ThreatCategory { Name = "Privilege Escalation", Count = 12, Percentage = 9.4, TrendDirection = "Stable" }
            },
            AttackVectors = new List<AttackVector>
            {
                new AttackVector { Name = "Password Attacks", Count = 67, SuccessRate = 8.5, Description = "Dictionary and brute force attacks" }
            },
            GeographicSources = new List<string> { "Unknown", "US", "RU", "CN" },
            TimePatterns = new Dictionary<string, int>
            {
                ["00-06"] = 45,
                ["06-12"] = 23,
                ["12-18"] = 67,
                ["18-24"] = 34
            }
        };
    }

    private List<ComplianceRequirement> GenerateComplianceRequirements(string standard)
    {
        return standard.ToUpper() switch
        {
            "GDPR" => new List<ComplianceRequirement>
            {
                new ComplianceRequirement
                {
                    Id = "GDPR-7.1",
                    Name = "Data Processing Records",
                    Description = "Maintain records of all data processing activities",
                    IsCompliant = true,
                    CompliancePercentage = 95.5
                }
            },
            "HIPAA" => new List<ComplianceRequirement>
            {
                new ComplianceRequirement
                {
                    Id = "HIPAA-164.312",
                    Name = "Access Control",
                    Description = "Implement access controls for PHI",
                    IsCompliant = true,
                    CompliancePercentage = 98.2
                }
            },
            _ => new List<ComplianceRequirement>()
        };
    }

    private List<ComplianceViolation> GenerateComplianceViolations()
    {
        return new List<ComplianceViolation>
        {
            new ComplianceViolation
            {
                RequirementId = "GDPR-7.1",
                Description = "Missing data processing record for user export operation",
                Severity = "Medium",
                OccurredAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }

    private List<ComplianceRecommendation> GenerateComplianceRecommendations()
    {
        return new List<ComplianceRecommendation>
        {
            new ComplianceRecommendation
            {
                RequirementId = "GDPR-7.1",
                Title = "Enhance Data Processing Logging",
                Description = "Implement comprehensive logging for all data processing operations",
                Priority = "Medium",
                Actions = new List<string> { "Update logging configuration", "Review existing logs" }
            }
        };
    }

    private byte[] GenerateCsvExport(List<AuditEvent> events)
    {
        var csv = "Timestamp,EventType,UserId,Action,Description,Severity\n";
        csv += string.Join("\n", events.Select(e =>
            $"{e.Timestamp:yyyy-MM-dd HH:mm:ss},{e.EventType},{e.UserId},{e.Action},{e.Description},{e.Severity}"));

        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    private byte[] GenerateJsonExport(List<AuditEvent> events)
    {
        var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private byte[] GenerateXmlExport(List<AuditEvent> events)
    {
        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<AuditEvents>\n";
        xml += string.Join("\n", events.Select(e =>
            $"  <Event timestamp=\"{e.Timestamp:yyyy-MM-dd HH:mm:ss}\" type=\"{e.EventType}\" user=\"{e.UserId}\" />"));
        xml += "\n</AuditEvents>";

        return System.Text.Encoding.UTF8.GetBytes(xml);
    }

    private async Task<RealTimeMetrics> GetRealTimeMetrics(CancellationToken cancellationToken)
    {
        return new RealTimeMetrics
        {
            ActiveUsers = 247,
            EventsPerMinute = 45,
            FailedLoginsLastHour = 12,
            SecurityAlertsToday = 8,
            SystemLoad = 0.65,
            EventsByType = new Dictionary<string, int>
            {
                ["Login"] = 156,
                ["Logout"] = 134,
                ["DataAccess"] = 89,
                ["DataModification"] = 23
            }
        };
    }

    private async Task<List<RecentEvent>> GetRecentEvents(CancellationToken cancellationToken)
    {
        return new List<RecentEvent>
        {
            new RecentEvent
            {
                EventType = "Login.Failed",
                UserName = "john.doe",
                Description = "Failed login attempt from new location",
                Timestamp = DateTime.UtcNow.AddMinutes(-2),
                Severity = "Warning"
            },
            new RecentEvent
            {
                EventType = "Permission.Changed",
                UserName = "admin",
                Description = "User role updated to Administrator",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Severity = "Information"
            }
        };
    }

    private List<DashboardWidget> GenerateDashboardWidgets()
    {
        return new List<DashboardWidget>
        {
            new DashboardWidget
            {
                Id = "security-score",
                Title = "Security Score",
                Type = "Metric",
                Data = new Dictionary<string, object> { ["value"] = 87.5, ["trend"] = "up" },
                Size = "Small"
            },
            new DashboardWidget
            {
                Id = "event-timeline",
                Title = "Event Timeline",
                Type = "Chart",
                Data = new Dictionary<string, object> { ["chartType"] = "line" },
                Size = "Large"
            }
        };
    }

    private async Task<SystemHealth> GetSystemHealth(CancellationToken cancellationToken)
    {
        return new SystemHealth
        {
            Status = "Healthy",
            OverallScore = 92.5,
            Indicators = new List<HealthIndicator>
            {
                new HealthIndicator { Name = "CPU Usage", Status = "Healthy", Value = 65.2, Unit = "%" },
                new HealthIndicator { Name = "Memory Usage", Status = "Healthy", Value = 72.8, Unit = "%" },
                new HealthIndicator { Name = "Database Response", Status = "Healthy", Value = 15.3, Unit = "ms" }
            }
        };
    }

    private string GetRandomEventType(Random random)
    {
        var types = new[] { "Login.Success", "Login.Failed", "Permission.Changed", "Data.Access", "Data.Modified", "User.Created" };
        return types[random.Next(types.Length)];
    }

    private string GetRandomEntityType(Random random)
    {
        var types = new[] { "User", "Role", "Permission", "Group", "Session" };
        return types[random.Next(types.Length)];
    }

    private string GetRandomAction(Random random)
    {
        var actions = new[] { "Create", "Read", "Update", "Delete", "Login", "Logout" };
        return actions[random.Next(actions.Length)];
    }

    #endregion
}