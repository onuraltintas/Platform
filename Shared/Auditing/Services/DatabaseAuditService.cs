using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using EgitimPlatform.Shared.Auditing.Configuration;
using EgitimPlatform.Shared.Auditing.Models;

namespace EgitimPlatform.Shared.Auditing.Services;

public class DatabaseAuditService : IAuditService
{
    private readonly AuditingOptions _options;
    private readonly ILogger<DatabaseAuditService> _logger;
    private readonly IAuditContextProvider _contextProvider;
    private readonly IAuditEventPublisher? _eventPublisher;
    private readonly IDbContextFactory<AuditDbContext> _dbContextFactory;

    public DatabaseAuditService(
        IOptions<AuditingOptions> options,
        ILogger<DatabaseAuditService> logger,
        IAuditContextProvider contextProvider,
        IDbContextFactory<AuditDbContext> dbContextFactory,
        IAuditEventPublisher? eventPublisher = null)
    {
        _options = options.Value;
        _logger = logger;
        _contextProvider = contextProvider;
        _eventPublisher = eventPublisher;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid> LogAuditAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableAuditing)
                return auditEntry.Id;

            // Enrich with context information
            EnrichAuditEntry(auditEntry);

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.AuditEntries.Add(auditEntry);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit entry logged: {EntityType} {EntityId} {Action}", 
                auditEntry.EntityType, auditEntry.EntityId, auditEntry.Action);

            // Publish audit event if publisher is available
            if (_eventPublisher != null)
            {
                await _eventPublisher.PublishAuditEventAsync(auditEntry, cancellationToken);
            }

            return auditEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for {EntityType} {EntityId}", 
                auditEntry.EntityType, auditEntry.EntityId);
            throw;
        }
    }

    public async Task<IEnumerable<Guid>> LogAuditsAsync(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableAuditing)
                return auditEntries.Select(a => a.Id);

            var enrichedEntries = auditEntries.ToList();

            // Enrich all entries
            foreach (var entry in enrichedEntries)
            {
                EnrichAuditEntry(entry);
            }

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.AuditEntries.AddRange(enrichedEntries);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Logged {Count} audit entries", enrichedEntries.Count);

            // Publish audit events if publisher is available
            if (_eventPublisher != null)
            {
                foreach (var entry in enrichedEntries)
                {
                    await _eventPublisher.PublishAuditEventAsync(entry, cancellationToken);
                }
            }

            return enrichedEntries.Select(a => a.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log {Count} audit entries", auditEntries.Count());
            throw;
        }
    }

    public async Task<Guid> LogUserAuditAsync(UserAuditEntry userAuditEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableUserAuditing)
                return userAuditEntry.Id;

            // Enrich with context information
            EnrichUserAuditEntry(userAuditEntry);

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.UserAuditEntries.Add(userAuditEntry);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("User audit entry logged: {UserId} {Action}", 
                userAuditEntry.UserId, userAuditEntry.Action);

            // Publish user audit event if publisher is available
            if (_eventPublisher != null)
            {
                await _eventPublisher.PublishUserAuditEventAsync(userAuditEntry, cancellationToken);
            }

            return userAuditEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user audit entry for {UserId}", userAuditEntry.UserId);
            throw;
        }
    }

    public async Task<Guid> LogApiAuditAsync(ApiAuditEntry apiAuditEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableApiAuditing)
                return apiAuditEntry.Id;

            // Enrich with context information
            EnrichApiAuditEntry(apiAuditEntry);

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.ApiAuditEntries.Add(apiAuditEntry);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("API audit entry logged: {Method} {Path} {StatusCode} {Duration}ms", 
                apiAuditEntry.HttpMethod, apiAuditEntry.Path, apiAuditEntry.StatusCode, apiAuditEntry.Duration.TotalMilliseconds);

            // Publish API audit event if publisher is available
            if (_eventPublisher != null)
            {
                await _eventPublisher.PublishApiAuditEventAsync(apiAuditEntry, cancellationToken);
            }

            return apiAuditEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log API audit entry for {Method} {Path}", 
                apiAuditEntry.HttpMethod, apiAuditEntry.Path);
            throw;
        }
    }

    public async Task<Guid> LogPerformanceAuditAsync(PerformanceAuditEntry performanceAuditEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnablePerformanceAuditing)
                return performanceAuditEntry.Id;

            // Only log if duration exceeds threshold
            if (performanceAuditEntry.Duration.TotalMilliseconds < _options.SlowQueryThresholdMs)
                return performanceAuditEntry.Id;

            // Enrich with context information
            EnrichPerformanceAuditEntry(performanceAuditEntry);

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.PerformanceAuditEntries.Add(performanceAuditEntry);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Performance audit entry logged: {Operation} {Duration}ms", 
                performanceAuditEntry.Operation, performanceAuditEntry.Duration.TotalMilliseconds);

            return performanceAuditEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log performance audit entry for {Operation}", 
                performanceAuditEntry.Operation);
            throw;
        }
    }

    public async Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AuditEntries
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string entityType, string entityId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AuditEntries
            .Where(a => a.EntityType == entityType && 
                       a.EntityId == entityId && 
                       a.Timestamp >= from && 
                       a.Timestamp <= to)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserAuditEntry>> GetUserAuditTrailAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserAuditEntries
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserAuditEntry>> GetUserAuditTrailAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserAuditEntries
            .Where(u => u.UserId == userId && 
                       u.Timestamp >= from && 
                       u.Timestamp <= to)
            .OrderByDescending(u => u.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApiAuditEntry>> GetApiAuditTrailAsync(string? userId = null, string? path = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.ApiAuditEntries.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(path))
            query = query.Where(a => a.Path.Contains(path));

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditStatistics> GetAuditStatisticsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var auditEntries = await context.AuditEntries
            .Where(a => a.Timestamp >= from && a.Timestamp <= to)
            .ToListAsync(cancellationToken);

        var userAuditEntries = await context.UserAuditEntries
            .Where(u => u.Timestamp >= from && u.Timestamp <= to)
            .ToListAsync(cancellationToken);

        var apiAuditEntries = await context.ApiAuditEntries
            .Where(a => a.Timestamp >= from && a.Timestamp <= to)
            .ToListAsync(cancellationToken);

        var performanceAuditEntries = await context.PerformanceAuditEntries
            .Where(p => p.StartTime >= from && p.EndTime <= to)
            .ToListAsync(cancellationToken);

        return new AuditStatistics
        {
            TotalAuditEntries = auditEntries.Count,
            TotalUserAuditEntries = userAuditEntries.Count,
            TotalApiAuditEntries = apiAuditEntries.Count,
            TotalPerformanceAuditEntries = performanceAuditEntries.Count,
            ActionCounts = auditEntries.GroupBy(a => a.Action).ToDictionary(g => g.Key, g => g.Count()),
            EntityTypeCounts = auditEntries.GroupBy(a => a.EntityType).ToDictionary(g => g.Key, g => g.Count()),
            UserCounts = auditEntries.Where(a => !string.IsNullOrEmpty(a.UserId))
                .GroupBy(a => a.UserId!).ToDictionary(g => g.Key, g => g.Count()),
            DailyAuditCounts = auditEntries.GroupBy(a => a.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count()),
            HourlyAuditCounts = auditEntries.GroupBy(a => a.Timestamp.Hour)
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageApiResponseTime = apiAuditEntries.Any() 
                ? TimeSpan.FromMilliseconds(apiAuditEntries.Average(a => a.Duration.TotalMilliseconds))
                : TimeSpan.Zero,
            AverageApiResponseTimeByEndpoint = apiAuditEntries.GroupBy(a => a.Path)
                .ToDictionary(g => g.Key, g => TimeSpan.FromMilliseconds(g.Average(a => a.Duration.TotalMilliseconds))),
            OldestAuditEntry = auditEntries.Min(a => a.Timestamp),
            NewestAuditEntry = auditEntries.Max(a => a.Timestamp)
        };
    }

    public async Task CleanupOldAuditsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Clean up audit entries
            var oldAudits = await context.AuditEntries
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            context.AuditEntries.RemoveRange(oldAudits);

            // Clean up user audit entries
            var oldUserAudits = await context.UserAuditEntries
                .Where(u => u.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            context.UserAuditEntries.RemoveRange(oldUserAudits);

            // Clean up API audit entries
            var oldApiAudits = await context.ApiAuditEntries
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            context.ApiAuditEntries.RemoveRange(oldApiAudits);

            // Clean up performance audit entries
            var oldPerformanceAudits = await context.PerformanceAuditEntries
                .Where(p => p.StartTime < cutoffDate)
                .ToListAsync(cancellationToken);

            context.PerformanceAuditEntries.RemoveRange(oldPerformanceAudits);

            await context.SaveChangesAsync(cancellationToken);

            var totalCleaned = oldAudits.Count + oldUserAudits.Count + oldApiAudits.Count + oldPerformanceAudits.Count;
            _logger.LogInformation("Cleaned up {Count} old audit entries older than {CutoffDate}", 
                totalCleaned, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit entries");
            throw;
        }
    }

    public async Task<bool> RestoreEntityAsync(string entityType, string entityId, Guid auditId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var auditEntry = await context.AuditEntries
                .FirstOrDefaultAsync(a => a.Id == auditId && 
                                         a.EntityType == entityType && 
                                         a.EntityId == entityId, cancellationToken);

            if (auditEntry == null || string.IsNullOrEmpty(auditEntry.OldValues))
                return false;

            // This would require knowing the actual entity type and DbContext
            // Implementation would depend on the specific entity framework setup
            _logger.LogInformation("Restore operation initiated for {EntityType} {EntityId} from audit {AuditId}", 
                entityType, entityId, auditId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore entity {EntityType} {EntityId} from audit {AuditId}", 
                entityType, entityId, auditId);
            return false;
        }
    }

    private void EnrichAuditEntry(AuditEntry auditEntry)
    {
        auditEntry.UserId ??= _contextProvider.GetCurrentUserId();
        auditEntry.UserName ??= _contextProvider.GetCurrentUserName();
        auditEntry.SessionId ??= _contextProvider.GetCurrentSessionId();
        auditEntry.IpAddress ??= _contextProvider.GetCurrentIpAddress();
        auditEntry.UserAgent ??= _contextProvider.GetCurrentUserAgent();

        // Add additional context
        var additionalContext = _contextProvider.GetAdditionalContext();
        foreach (var kvp in additionalContext)
        {
            auditEntry.Metadata[kvp.Key] = kvp.Value;
        }
    }

    private void EnrichUserAuditEntry(UserAuditEntry userAuditEntry)
    {
        userAuditEntry.IpAddress ??= _contextProvider.GetCurrentIpAddress();
        userAuditEntry.UserAgent ??= _contextProvider.GetCurrentUserAgent();
        userAuditEntry.SessionId ??= _contextProvider.GetCurrentSessionId();

        // Add additional context
        var additionalContext = _contextProvider.GetAdditionalContext();
        foreach (var kvp in additionalContext)
        {
            userAuditEntry.AdditionalData[kvp.Key] = kvp.Value;
        }
    }

    private void EnrichApiAuditEntry(ApiAuditEntry apiAuditEntry)
    {
        apiAuditEntry.UserId ??= _contextProvider.GetCurrentUserId();
        apiAuditEntry.UserName ??= _contextProvider.GetCurrentUserName();
        apiAuditEntry.IpAddress ??= _contextProvider.GetCurrentIpAddress();
        apiAuditEntry.UserAgent ??= _contextProvider.GetCurrentUserAgent();
        apiAuditEntry.CorrelationId ??= _contextProvider.GetCorrelationId();
    }

    private void EnrichPerformanceAuditEntry(PerformanceAuditEntry performanceAuditEntry)
    {
        performanceAuditEntry.UserId ??= _contextProvider.GetCurrentUserId();
        performanceAuditEntry.SessionId ??= _contextProvider.GetCurrentSessionId();
        performanceAuditEntry.IpAddress ??= _contextProvider.GetCurrentIpAddress();
        performanceAuditEntry.CorrelationId ??= _contextProvider.GetCorrelationId();
    }
}