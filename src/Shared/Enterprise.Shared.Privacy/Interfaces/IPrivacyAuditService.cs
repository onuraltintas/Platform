using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Interfaces;

public interface IPrivacyAuditService
{
    Task LogAuditEventAsync(PrivacyAuditEvent auditEvent, CancellationToken cancellationToken = default);
    
    Task LogDataAccessAsync(string userId, string dataRecordId, DataCategory category, 
        string source, CancellationToken cancellationToken = default);
    
    Task LogDataModificationAsync(string userId, string dataRecordId, DataCategory category, 
        string modificationDetails, CancellationToken cancellationToken = default);
    
    Task LogConsentChangeAsync(string userId, ConsentPurpose purpose, ConsentStatus oldStatus, 
        ConsentStatus newStatus, string? reason = null, CancellationToken cancellationToken = default);
    
    Task LogDataDeletionAsync(string userId, DataCategory[] categories, int recordsDeleted, 
        CancellationToken cancellationToken = default);
    
    Task LogUserRightExercisedAsync(string userId, DataSubjectRight right, 
        string details, CancellationToken cancellationToken = default);
    
    Task LogPolicyViolationAsync(string userId, string violation, string severity, 
        Dictionary<string, object>? details = null, CancellationToken cancellationToken = default);
    
    Task<PrivacyAuditEvent[]> GetAuditEventsAsync(string userId, DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    Task<PrivacyAuditEvent[]> GetAuditEventsByTypeAsync(AuditEventType eventType, 
        DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    Task<PrivacyMetrics> GeneratePrivacyMetricsAsync(DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, int>> GetAuditEventSummaryAsync(DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    Task<PrivacyAuditEvent[]> SearchAuditEventsAsync(string searchTerm, 
        AuditEventType[]? eventTypes = null, DateTime? fromDate = null, DateTime? toDate = null, 
        CancellationToken cancellationToken = default);
    
    Task<int> CleanupOldAuditEventsAsync(int retentionDays, CancellationToken cancellationToken = default);
    
    Task<bool> ArchiveAuditEventsAsync(DateTime beforeDate, string archivePath, 
        CancellationToken cancellationToken = default);
}