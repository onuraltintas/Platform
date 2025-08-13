using EgitimPlatform.Shared.Auditing.Models;

namespace EgitimPlatform.Shared.Auditing.Services;

public interface IAuditService
{
    Task<Guid> LogAuditAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> LogAuditsAsync(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken = default);
    
    Task<Guid> LogUserAuditAsync(UserAuditEntry userAuditEntry, CancellationToken cancellationToken = default);
    Task<Guid> LogApiAuditAsync(ApiAuditEntry apiAuditEntry, CancellationToken cancellationToken = default);
    Task<Guid> LogPerformanceAuditAsync(PerformanceAuditEntry performanceAuditEntry, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityType, string entityId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<UserAuditEntry>> GetUserAuditTrailAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserAuditEntry>> GetUserAuditTrailAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ApiAuditEntry>> GetApiAuditTrailAsync(string? userId = null, string? path = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    
    Task<AuditStatistics> GetAuditStatisticsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    
    Task CleanupOldAuditsAsync(int retentionDays, CancellationToken cancellationToken = default);
    
    Task<bool> RestoreEntityAsync(string entityType, string entityId, Guid auditId, CancellationToken cancellationToken = default);
}

public interface IAuditContextProvider
{
    string? GetCurrentUserId();
    string? GetCurrentUserName();
    string? GetCurrentSessionId();
    string? GetCurrentIpAddress();
    string? GetCurrentUserAgent();
    string? GetCorrelationId();
    Dictionary<string, object> GetAdditionalContext();
}

public interface IAuditEventPublisher
{
    Task PublishAuditEventAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default);
    Task PublishUserAuditEventAsync(UserAuditEntry userAuditEntry, CancellationToken cancellationToken = default);
    Task PublishApiAuditEventAsync(ApiAuditEntry apiAuditEntry, CancellationToken cancellationToken = default);
}

public class AuditStatistics
{
    public int TotalAuditEntries { get; set; }
    public int TotalUserAuditEntries { get; set; }
    public int TotalApiAuditEntries { get; set; }
    public int TotalPerformanceAuditEntries { get; set; }
    
    public Dictionary<AuditAction, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public Dictionary<string, int> UserCounts { get; set; } = new();
    
    public Dictionary<DateTime, int> DailyAuditCounts { get; set; } = new();
    public Dictionary<int, int> HourlyAuditCounts { get; set; } = new();
    
    public TimeSpan AverageApiResponseTime { get; set; }
    public Dictionary<string, TimeSpan> AverageApiResponseTimeByEndpoint { get; set; } = new();
    
    public long TotalDataSize { get; set; }
    public DateTime OldestAuditEntry { get; set; }
    public DateTime NewestAuditEntry { get; set; }
}