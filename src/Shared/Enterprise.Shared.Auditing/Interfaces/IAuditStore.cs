namespace Enterprise.Shared.Auditing.Interfaces;

/// <summary>
/// Interface for audit event storage and retrieval
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Stores a single audit event
    /// </summary>
    /// <param name="auditEvent">The audit event to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the storage operation</returns>
    Task<Result> StoreEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores multiple audit events
    /// </summary>
    /// <param name="auditEvents">The audit events to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the storage operation</returns>
    Task<Result> StoreEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit events based on search criteria
    /// </summary>
    /// <param name="criteria">The search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching audit events and total count</returns>
    Task<(List<AuditEvent> Events, int TotalCount)> QueryEventsAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific audit event by ID
    /// </summary>
    /// <param name="id">The audit event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audit event if found, null otherwise</returns>
    Task<AuditEvent?> GetEventAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events by correlation ID
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of related audit events</returns>
    Task<List<AuditEvent>> GetEventsByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes audit events older than the specified date
    /// </summary>
    /// <param name="olderThan">The cutoff date</param>
    /// <param name="categories">Optional categories to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events deleted</returns>
    Task<int> DeleteEventsAsync(DateTime olderThan, AuditEventCategory[]? categories = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for stored audit events
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit statistics</returns>
    Task<AuditStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the audit store is healthy and accessible
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of events for a specific time period
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of events</returns>
    Task<int> GetEventCountAsync(DateTime startDate, DateTime endDate, AuditEventCategory? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit events to a specific format
    /// </summary>
    /// <param name="criteria">Search criteria for events to export</param>
    /// <param name="format">Export format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported data as byte array</returns>
    Task<byte[]> ExportEventsAsync(AuditSearchCriteria criteria, AuditExportFormat format, CancellationToken cancellationToken = default);
}

/// <summary>
/// Export formats for audit events
/// </summary>
public enum AuditExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv = 1,

    /// <summary>
    /// Excel format
    /// </summary>
    Excel = 2,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 3,

    /// <summary>
    /// PDF format
    /// </summary>
    Pdf = 4
}