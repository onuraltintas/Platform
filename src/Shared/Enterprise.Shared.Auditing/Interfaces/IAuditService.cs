namespace Enterprise.Shared.Auditing.Interfaces;

/// <summary>
/// Service for auditing system events and user activities
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs a generic audit event
    /// </summary>
    /// <param name="auditEvent">The audit event to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the audit logging operation</returns>
    Task<Result> LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a security-specific audit event
    /// </summary>
    /// <param name="securityEvent">The security audit event to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the audit logging operation</returns>
    Task<Result> LogSecurityEventAsync(SecurityAuditEvent securityEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs multiple audit events in a batch
    /// </summary>
    /// <param name="auditEvents">The audit events to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the batch audit logging operation</returns>
    Task<Result> LogEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for audit events based on criteria
    /// </summary>
    /// <param name="criteria">The search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results containing matching audit events</returns>
    Task<AuditSearchResult> SearchEventsAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific audit event by ID
    /// </summary>
    /// <param name="id">The audit event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audit event if found, null otherwise</returns>
    Task<AuditEvent?> GetEventAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit events for a specific correlation ID
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of related audit events</returns>
    Task<List<AuditEvent>> GetEventsByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit statistics for a given time period
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit statistics</returns>
    Task<AuditStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges audit events older than the specified date
    /// </summary>
    /// <param name="olderThan">The cutoff date for purging</param>
    /// <param name="categories">Optional categories to purge (all categories if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the number of events purged</returns>
    Task<Result<int>> PurgeEventsAsync(DateTime olderThan, AuditEventCategory[]? categories = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives audit events older than the specified date
    /// </summary>
    /// <param name="olderThan">The cutoff date for archiving</param>
    /// <param name="archiveLocation">Location to archive the events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the number of events archived</returns>
    Task<Result<int>> ArchiveEventsAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the health of the audit system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for audit events
/// </summary>
public class AuditStatistics
{
    /// <summary>
    /// Start date for the statistics
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for the statistics
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total number of events in the period
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of events by category
    /// </summary>
    public Dictionary<AuditEventCategory, int> EventsByCategory { get; set; } = new();

    /// <summary>
    /// Number of events by severity
    /// </summary>
    public Dictionary<AuditSeverity, int> EventsBySeverity { get; set; } = new();

    /// <summary>
    /// Number of events by result
    /// </summary>
    public Dictionary<string, int> EventsByResult { get; set; } = new();

    /// <summary>
    /// Number of unique users who performed actions
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Most common actions
    /// </summary>
    public Dictionary<string, int> CommonActions { get; set; } = new();

    /// <summary>
    /// Most active users
    /// </summary>
    public Dictionary<string, int> ActiveUsers { get; set; } = new();

    /// <summary>
    /// Most accessed resources
    /// </summary>
    public Dictionary<string, int> AccessedResources { get; set; } = new();

    /// <summary>
    /// Events by day
    /// </summary>
    public Dictionary<DateTime, int> EventsByDay { get; set; } = new();

    /// <summary>
    /// Events by hour (0-23)
    /// </summary>
    public Dictionary<int, int> EventsByHour { get; set; } = new();

    /// <summary>
    /// Top IP addresses by event count
    /// </summary>
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();

    /// <summary>
    /// Average response time for operations with duration
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Number of security alerts
    /// </summary>
    public int SecurityAlerts { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailedOperations { get; set; }

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRate { get; set; }
}