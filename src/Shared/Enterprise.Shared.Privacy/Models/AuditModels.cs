namespace Enterprise.Shared.Privacy.Models;

public class PrivacyAuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public AuditEventType EventType { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty; // Application, API, User, System
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
    public DataCategory[]? AffectedDataCategories { get; set; }
    public string? DataRecordIds { get; set; }
    public ConsentPurpose[]? AffectedPurposes { get; set; }
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
    public bool RequiresNotification { get; set; } = false;
}

public class ComplianceReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ReportType { get; set; } = string.Empty; // GDPR, CCPA, etc.
    public string ReportPeriod { get; set; } = string.Empty;
    public string DataController { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public double ComplianceScore { get; set; }
    public ComplianceMetrics Metrics { get; set; } = new();
    public ComplianceIssue[] Issues { get; set; } = Array.Empty<ComplianceIssue>();
    public Dictionary<string, object> DetailedData { get; set; } = new();
    public string Status { get; set; } = "Generated";
    public string? FilePath { get; set; }
}

public class ComplianceMetrics
{
    public int TotalUsers { get; set; }
    public int TotalDataRecords { get; set; }
    public int TotalConsentRecords { get; set; }
    public int ActiveConsents { get; set; }
    public int WithdrawnConsents { get; set; }
    public int ExpiredConsents { get; set; }
    public int DataExportRequests { get; set; }
    public int DataDeletionRequests { get; set; }
    public int TotalDataSubjectRequests { get; set; }
    public int RequestsProcessedOnTime { get; set; }
    public int PendingRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public int CompletedDeletions { get; set; }
    public int PolicyViolations { get; set; }
    public int SecurityBreaches { get; set; }
    public Dictionary<DataCategory, int> DataByCategory { get; set; } = new();
    public Dictionary<ConsentPurpose, int> ConsentsByPurpose { get; set; } = new();
    public Dictionary<AuditEventType, int> AuditEventsByType { get; set; } = new();
}

public class ComplianceIssue
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Description { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserId { get; set; }
    public string? AffectedUserId { get; set; }
    public string? AffectedDataRecordId { get; set; }
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string Status { get; set; } = "Open"; // Open, In Progress, Resolved
    public Dictionary<string, string> IssueDetails { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

public class PrivacyMetrics
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalActiveUsers { get; set; }
    public int TotalConsentRecords { get; set; }
    public double ConsentRate { get; set; }
    public double WithdrawalRate { get; set; }
    public int DataRetentionViolations { get; set; }
    public int AnonymizedRecords { get; set; }
    public int DeletedRecords { get; set; }
    public Dictionary<ConsentPurpose, int> ConsentsByPurpose { get; set; } = new();
    public Dictionary<DataCategory, int> DataRecordsByCategory { get; set; } = new();
    public Dictionary<string, int> Top10DataAccessPatterns { get; set; } = new();
    public int TotalAuditEvents { get; set; }
    public DateTime? LastComplianceReview { get; set; }
}