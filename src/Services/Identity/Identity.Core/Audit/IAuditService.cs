using Identity.Core.Entities;

namespace Identity.Core.Audit;

/// <summary>
/// Advanced audit and monitoring service interface
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log audit event
    /// </summary>
    Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    Task<List<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit events with filtering and pagination
    /// </summary>
    Task<PagedAuditResult> GetAuditEventsAsync(AuditFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate audit report
    /// </summary>
    Task<AuditReport> GenerateAuditReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security analytics
    /// </summary>
    Task<SecurityAnalytics> GetSecurityAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect anomalies in audit data
    /// </summary>
    Task<List<AuditAnomaly>> DetectAnomaliesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get compliance report
    /// </summary>
    Task<ComplianceReport> GetComplianceReportAsync(ComplianceReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export audit data
    /// </summary>
    Task<byte[]> ExportAuditDataAsync(AuditExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time monitoring dashboard data
    /// </summary>
    Task<MonitoringDashboard> GetMonitoringDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create alert rule
    /// </summary>
    Task<AlertRule> CreateAlertRuleAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active alerts
    /// </summary>
    Task<List<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive old audit data
    /// </summary>
    Task<ArchiveResult> ArchiveAuditDataAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit event information
/// </summary>
public class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object> OldValues { get; set; } = new();
    public Dictionary<string, object> NewValues { get; set; } = new();
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
    public AuditCategory Category { get; set; } = AuditCategory.General;
    public bool IsSecurityEvent { get; set; }
    public string? RiskLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Audit severity levels
/// </summary>
public enum AuditSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Audit categories
/// </summary>
public enum AuditCategory
{
    General = 0,
    Authentication = 1,
    Authorization = 2,
    DataAccess = 3,
    DataModification = 4,
    SecurityEvent = 5,
    SystemEvent = 6,
    ComplianceEvent = 7
}

/// <summary>
/// Audit filter for queries
/// </summary>
public class AuditFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EventType { get; set; }
    public AuditSeverity? Severity { get; set; }
    public AuditCategory? Category { get; set; }
    public bool? IsSecurityEvent { get; set; }
    public string? IpAddress { get; set; }
    public string? SearchTerm { get; set; }
    public List<string> Tags { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Timestamp";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Paged audit result
/// </summary>
public class PagedAuditResult
{
    public List<AuditEvent> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public AuditSummary Summary { get; set; } = new();
}

/// <summary>
/// Audit summary statistics
/// </summary>
public class AuditSummary
{
    public int TotalEvents { get; set; }
    public int SecurityEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public Dictionary<string, int> EventsByCategory { get; set; } = new();
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();
}

/// <summary>
/// Audit report request
/// </summary>
public class AuditReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string ReportType { get; set; } = string.Empty; // Summary, Detailed, Security, Compliance
    public List<AuditCategory> Categories { get; set; } = new();
    public List<string> UserIds { get; set; } = new();
    public string? Format { get; set; } = "PDF"; // PDF, Excel, CSV
    public bool IncludeCharts { get; set; } = true;
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Audit report result
/// </summary>
public class AuditReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public AuditSummary Summary { get; set; } = new();
    public List<AuditReportSection> Sections { get; set; } = new();
    public List<AuditChart> Charts { get; set; } = new();
    public byte[]? FileData { get; set; }
    public string FileType { get; set; } = string.Empty;
}

/// <summary>
/// Audit report section
/// </summary>
public class AuditReportSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<AuditEvent> Events { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Audit chart data
/// </summary>
public class AuditChart
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Line, Bar, Pie, Area
    public List<ChartDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Chart data point
/// </summary>
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime? Date { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Security analytics result
/// </summary>
public class SecurityAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public SecurityMetrics Metrics { get; set; } = new();
    public List<SecurityTrend> Trends { get; set; } = new();
    public List<SecurityIncident> Incidents { get; set; } = new();
    public List<RiskIndicator> RiskIndicators { get; set; } = new();
    public ThreatLandscape ThreatLandscape { get; set; } = new();
}

/// <summary>
/// Security metrics
/// </summary>
public class SecurityMetrics
{
    public int TotalSecurityEvents { get; set; }
    public int FailedLogins { get; set; }
    public int SuccessfulLogins { get; set; }
    public int PrivilegeEscalations { get; set; }
    public int DataAccessViolations { get; set; }
    public int SuspiciousActivities { get; set; }
    public double SecurityScore { get; set; }
    public Dictionary<string, int> ThreatsByType { get; set; } = new();
    public Dictionary<string, int> AttacksBySource { get; set; } = new();
}

/// <summary>
/// Security trend data
/// </summary>
public class SecurityTrend
{
    public string MetricName { get; set; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; set; } = new();
    public string Trend { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    public double ChangePercentage { get; set; }
}

/// <summary>
/// Trend data point
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// Security incident
/// </summary>
public class SecurityIncident
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string DetectedBy { get; set; } = string.Empty;
    public List<AuditEvent> RelatedEvents { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Risk indicator
/// </summary>
public class RiskIndicator
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> Indicators { get; set; } = new();
    public string? Recommendation { get; set; }
}

/// <summary>
/// Threat landscape overview
/// </summary>
public class ThreatLandscape
{
    public List<ThreatCategory> ThreatCategories { get; set; } = new();
    public List<AttackVector> AttackVectors { get; set; } = new();
    public List<string> GeographicSources { get; set; } = new();
    public Dictionary<string, int> TimePatterns { get; set; } = new();
}

/// <summary>
/// Threat category
/// </summary>
public class ThreatCategory
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
}

/// <summary>
/// Attack vector information
/// </summary>
public class AttackVector
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double SuccessRate { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Audit anomaly detection result
/// </summary>
public class AuditAnomaly
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double AnomalyScore { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public List<AuditEvent> RelatedEvents { get; set; } = new();
    public Dictionary<string, object> AnalysisData { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Compliance report request
/// </summary>
public class ComplianceReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Standard { get; set; } = string.Empty; // GDPR, HIPAA, SOX, etc.
    public List<string> Requirements { get; set; } = new();
    public string Format { get; set; } = "PDF";
}

/// <summary>
/// Compliance report result
/// </summary>
public class ComplianceReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Standard { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public double ComplianceScore { get; set; }
    public List<ComplianceRequirement> Requirements { get; set; } = new();
    public List<ComplianceViolation> Violations { get; set; } = new();
    public List<ComplianceRecommendation> Recommendations { get; set; } = new();
    public byte[]? FileData { get; set; }
}

/// <summary>
/// Compliance requirement
/// </summary>
public class ComplianceRequirement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompliant { get; set; }
    public double CompliancePercentage { get; set; }
    public List<string> EvidenceEvents { get; set; } = new();
}

/// <summary>
/// Compliance violation
/// </summary>
public class ComplianceViolation
{
    public string RequirementId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public List<AuditEvent> RelatedEvents { get; set; } = new();
}

/// <summary>
/// Compliance recommendation
/// </summary>
public class ComplianceRecommendation
{
    public string RequirementId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
}

/// <summary>
/// Audit export request
/// </summary>
public class AuditExportRequest
{
    public AuditFilter Filter { get; set; } = new();
    public string Format { get; set; } = "CSV"; // CSV, JSON, XML
    public bool IncludeMetadata { get; set; } = true;
    public string? FileName { get; set; }
}

/// <summary>
/// Monitoring dashboard data
/// </summary>
public class MonitoringDashboard
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public RealTimeMetrics RealTimeMetrics { get; set; } = new();
    public List<RecentEvent> RecentEvents { get; set; } = new();
    public List<Alert> ActiveAlerts { get; set; } = new();
    public List<DashboardWidget> Widgets { get; set; } = new();
    public SystemHealth SystemHealth { get; set; } = new();
}

/// <summary>
/// Real-time metrics
/// </summary>
public class RealTimeMetrics
{
    public int ActiveUsers { get; set; }
    public int EventsPerMinute { get; set; }
    public int FailedLoginsLastHour { get; set; }
    public int SecurityAlertsToday { get; set; }
    public double SystemLoad { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
}

/// <summary>
/// Recent event for dashboard
/// </summary>
public class RecentEvent
{
    public string EventType { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard widget
/// </summary>
public class DashboardWidget
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Chart, Metric, List, Table
    public Dictionary<string, object> Data { get; set; } = new();
    public string Size { get; set; } = "Medium"; // Small, Medium, Large
}

/// <summary>
/// System health information
/// </summary>
public class SystemHealth
{
    public string Status { get; set; } = string.Empty; // Healthy, Warning, Critical
    public double OverallScore { get; set; }
    public List<HealthIndicator> Indicators { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health indicator
/// </summary>
public class HealthIndicator
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Alert rule
/// </summary>
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Create alert rule request
/// </summary>
public class CreateAlertRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Alert information
/// </summary>
public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // New, Acknowledged, Resolved
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? ResolvedBy { get; set; }
    public List<AuditEvent> TriggerEvents { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Archive result
/// </summary>
public class ArchiveResult
{
    public int ArchivedEventCount { get; set; }
    public DateTime ArchiveDate { get; set; } = DateTime.UtcNow;
    public string ArchiveLocation { get; set; } = string.Empty;
    public long ArchiveSizeBytes { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}