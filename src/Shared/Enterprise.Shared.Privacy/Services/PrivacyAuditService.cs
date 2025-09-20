using System.Collections.Concurrent;
using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Privacy.Services;

public class PrivacyAuditService : IPrivacyAuditService
{
    private readonly PrivacySettings _settings;
    private readonly ILogger<PrivacyAuditService> _logger;
    private readonly ConcurrentBag<PrivacyAuditEvent> _auditEvents = new();

    public PrivacyAuditService(
        IOptions<PrivacySettings> settings,
        ILogger<PrivacyAuditService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogAuditEventAsync(PrivacyAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent == null) throw new ArgumentNullException(nameof(auditEvent));

        try
        {
            auditEvent.Timestamp = DateTime.UtcNow;
            _auditEvents.Add(auditEvent);

            if (_settings.AuditLogging.EnableStructuredLogging)
            {
                _logger.LogInformation("Privacy audit event: {EventType} for user {UserId} - {Description}",
                    auditEvent.EventType, auditEvent.UserId, auditEvent.EventDescription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event");
        }
    }

    public async Task LogDataAccessAsync(string userId, string dataRecordId, DataCategory category, 
        string source, CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.DataAccess,
            EventDescription = $"Data access for category {category}",
            Source = source,
            DataRecordIds = dataRecordId,
            AffectedDataCategories = new[] { category }
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogDataModificationAsync(string userId, string dataRecordId, DataCategory category, 
        string modificationDetails, CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.DataModified,
            EventDescription = modificationDetails,
            DataRecordIds = dataRecordId,
            AffectedDataCategories = new[] { category }
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogConsentChangeAsync(string userId, ConsentPurpose purpose, ConsentStatus oldStatus, 
        ConsentStatus newStatus, string? reason = null, CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = newStatus == ConsentStatus.Granted ? AuditEventType.ConsentGranted : AuditEventType.ConsentWithdrawn,
            EventDescription = $"Consent changed from {oldStatus} to {newStatus} for {purpose}",
            AffectedPurposes = new[] { purpose },
            EventData = new Dictionary<string, object>
            {
                ["OldStatus"] = oldStatus.ToString(),
                ["NewStatus"] = newStatus.ToString(),
                ["Purpose"] = purpose.ToString(),
                ["Reason"] = reason ?? string.Empty
            }
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogDataDeletionAsync(string userId, DataCategory[] categories, int recordsDeleted, 
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.DataDeleted,
            EventDescription = $"Deleted {recordsDeleted} records across categories: {string.Join(", ", categories)}",
            AffectedDataCategories = categories,
            EventData = new Dictionary<string, object>
            {
                ["RecordsDeleted"] = recordsDeleted,
                ["Categories"] = categories.Select(c => c.ToString()).ToArray()
            }
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogUserRightExercisedAsync(string userId, DataSubjectRight right, 
        string details, CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.UserRightExercised,
            EventDescription = $"User exercised right: {right} - {details}",
            EventData = new Dictionary<string, object>
            {
                ["Right"] = right.ToString(),
                ["Details"] = details
            }
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task LogPolicyViolationAsync(string userId, string violation, string severity, 
        Dictionary<string, object>? details = null, CancellationToken cancellationToken = default)
    {
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = userId,
            EventType = AuditEventType.PolicyViolation,
            EventDescription = violation,
            Severity = severity,
            EventData = details ?? new Dictionary<string, object>(),
            RequiresNotification = severity == "Critical" || severity == "High"
        };

        await LogAuditEventAsync(auditEvent, cancellationToken);
    }

    public async Task<PrivacyAuditEvent[]> GetAuditEventsAsync(string userId, DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var events = _auditEvents
            .Where(e => e.UserId == userId)
            .Where(e => fromDate == null || e.Timestamp >= fromDate)
            .Where(e => toDate == null || e.Timestamp <= toDate)
            .OrderByDescending(e => e.Timestamp)
            .ToArray();

        return events;
    }

    public async Task<PrivacyAuditEvent[]> GetAuditEventsByTypeAsync(AuditEventType eventType, 
        DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var events = _auditEvents
            .Where(e => e.EventType == eventType)
            .Where(e => fromDate == null || e.Timestamp >= fromDate)
            .Where(e => toDate == null || e.Timestamp <= toDate)
            .OrderByDescending(e => e.Timestamp)
            .ToArray();

        return events;
    }

    public async Task<PrivacyMetrics> GeneratePrivacyMetricsAsync(DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var events = _auditEvents
            .Where(e => fromDate == null || e.Timestamp >= fromDate)
            .Where(e => toDate == null || e.Timestamp <= toDate)
            .ToArray();

        return new PrivacyMetrics
        {
            TotalAuditEvents = events.Length,
            ConsentsByPurpose = events
                .Where(e => e.AffectedPurposes != null)
                .SelectMany(e => e.AffectedPurposes!)
                .GroupBy(p => p)
                .ToDictionary(g => g.Key, g => g.Count()),
            DataRecordsByCategory = events
                .Where(e => e.AffectedDataCategories != null)
                .SelectMany(e => e.AffectedDataCategories!)
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<Dictionary<string, int>> GetAuditEventSummaryAsync(DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var events = _auditEvents
            .Where(e => fromDate == null || e.Timestamp >= fromDate)
            .Where(e => toDate == null || e.Timestamp <= toDate)
            .ToArray();

        return events
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
    }

    public async Task<PrivacyAuditEvent[]> SearchAuditEventsAsync(string searchTerm, 
        AuditEventType[]? eventTypes = null, DateTime? fromDate = null, DateTime? toDate = null, 
        CancellationToken cancellationToken = default)
    {
        var events = _auditEvents
            .Where(e => eventTypes == null || eventTypes.Contains(e.EventType))
            .Where(e => fromDate == null || e.Timestamp >= fromDate)
            .Where(e => toDate == null || e.Timestamp <= toDate)
            .Where(e => string.IsNullOrEmpty(searchTerm) || 
                       e.EventDescription.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       e.UserId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp)
            .ToArray();

        return events;
    }

    public async Task<int> CleanupOldAuditEventsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var eventsToRemove = _auditEvents
            .Where(e => e.Timestamp < cutoffDate)
            .ToArray();

        // Note: ConcurrentBag doesn't support removal, so this is a simplified implementation
        // In production, you'd use a proper database with DELETE operations
        
        _logger.LogInformation("Would cleanup {Count} audit events older than {CutoffDate}", 
            eventsToRemove.Length, cutoffDate);

        return eventsToRemove.Length;
    }

    public async Task<bool> ArchiveAuditEventsAsync(DateTime beforeDate, string archivePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventsToArchive = _auditEvents
                .Where(e => e.Timestamp < beforeDate)
                .ToArray();

            if (eventsToArchive.Length == 0)
                return true;

            // In a real implementation, you would serialize and save to the archive path
            _logger.LogInformation("Would archive {Count} audit events to {ArchivePath}", 
                eventsToArchive.Length, archivePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive audit events");
            return false;
        }
    }
}