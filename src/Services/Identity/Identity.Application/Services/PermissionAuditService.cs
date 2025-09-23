using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Enterprise.Shared.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Identity.Application.Services;

/// <summary>
/// Implementation of permission audit service
/// </summary>
public class PermissionAuditService : IPermissionAuditService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<PermissionAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuditService(
        IdentityDbContext context,
        ILogger<PermissionAuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogPermissionActionAsync(
        PermissionAuditAction action,
        string? permissionCode = null,
        string? userId = null,
        string? targetUserId = null,
        Guid? roleId = null,
        Guid? targetRoleId = null,
        Guid? groupId = null,
        bool success = true,
        string? errorMessage = null,
        string? description = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new PermissionAuditLog
            {
                Action = action,
                PermissionCode = permissionCode,
                UserId = userId,
                TargetUserId = targetUserId,
                RoleId = roleId,
                TargetRoleId = targetRoleId,
                GroupId = groupId,
                Success = success,
                ErrorMessage = errorMessage,
                Description = description,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                RequestId = httpContext?.TraceIdentifier,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                Timestamp = DateTime.UtcNow
            };

            // Set retention policy (keep audit logs for 1 year by default)
            auditLog.DeleteAfter = DateTime.UtcNow.AddYears(1);

            _context.PermissionAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Permission audit logged: {Action} for {Permission} by {User}",
                action, permissionCode, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission audit: {Action} for {Permission}",
                action, permissionCode);
            // Don't throw - audit logging should not break the main flow
        }
    }

    public async Task LogPermissionCheckAsync(
        string permissionCode,
        string userId,
        bool allowed,
        string? reason = null,
        Guid? groupId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var action = allowed ? PermissionAuditAction.PermissionAllowed : PermissionAuditAction.PermissionDenied;

        await LogPermissionActionAsync(
            action: action,
            permissionCode: permissionCode,
            userId: userId,
            groupId: groupId,
            success: allowed,
            description: reason,
            metadata: new Dictionary<string, object>
            {
                ["check_result"] = allowed,
                ["ip_address"] = ipAddress ?? GetClientIpAddress(_httpContextAccessor.HttpContext),
                ["user_agent"] = userAgent ?? _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                ["request_id"] = requestId ?? _httpContextAccessor.HttpContext?.TraceIdentifier
            },
            cancellationToken: cancellationToken);
    }

    public async Task LogBulkPermissionOperationAsync(
        PermissionAuditAction action,
        int affectedCount,
        string? performedBy = null,
        string? description = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var bulkMetadata = metadata ?? new Dictionary<string, object>();
        bulkMetadata["affected_count"] = affectedCount;
        bulkMetadata["operation_type"] = "bulk";

        await LogPermissionActionAsync(
            action: action,
            userId: performedBy,
            description: description ?? $"Bulk operation affected {affectedCount} items",
            metadata: bulkMetadata,
            cancellationToken: cancellationToken);
    }

    public async Task<Result<PagedResult<PermissionAuditLog>>> GetUserAuditLogsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PermissionAuditLogs
                .Where(log => log.UserId == userId || log.TargetUserId == userId);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.Permission)
                .Include(log => log.User)
                .Include(log => log.TargetUser)
                .Include(log => log.Role)
                .Include(log => log.TargetRole)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<PermissionAuditLog>(logs, totalCount, page, pageSize);

            return Result<PagedResult<PermissionAuditLog>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user audit logs for user {UserId}", userId);
            return Result<PagedResult<PermissionAuditLog>>.Failure("Failed to retrieve audit logs");
        }
    }

    public async Task<Result<PagedResult<PermissionAuditLog>>> GetPermissionAuditLogsAsync(
        string permissionCode,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PermissionAuditLogs
                .Where(log => log.PermissionCode == permissionCode);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User)
                .Include(log => log.TargetUser)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<PermissionAuditLog>(logs, totalCount, page, pageSize);

            return Result<PagedResult<PermissionAuditLog>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission audit logs for {Permission}", permissionCode);
            return Result<PagedResult<PermissionAuditLog>>.Failure("Failed to retrieve audit logs");
        }
    }

    public async Task<Result<PagedResult<PermissionAuditLog>>> GetRoleAuditLogsAsync(
        Guid roleId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PermissionAuditLogs
                .Where(log => log.RoleId == roleId || log.TargetRoleId == roleId);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.Role)
                .Include(log => log.TargetRole)
                .Include(log => log.User)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<PermissionAuditLog>(logs, totalCount, page, pageSize);

            return Result<PagedResult<PermissionAuditLog>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role audit logs for role {RoleId}", roleId);
            return Result<PagedResult<PermissionAuditLog>>.Failure("Failed to retrieve audit logs");
        }
    }

    public async Task<Result<PagedResult<PermissionAuditLog>>> GetSecurityAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var securityActions = new[]
            {
                PermissionAuditAction.PermissionDenied,
                PermissionAuditAction.PermissionSystemReset,
                PermissionAuditAction.BulkPermissionUpdate
            };

            var query = _context.PermissionAuditLogs
                .Where(log => securityActions.Contains(log.Action) || !log.Success);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<PermissionAuditLog>(logs, totalCount, page, pageSize);

            return Result<PagedResult<PermissionAuditLog>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security audit logs");
            return Result<PagedResult<PermissionAuditLog>>.Failure("Failed to retrieve security audit logs");
        }
    }

    public async Task<Result<int>> CleanupOldAuditLogsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logsToDelete = await _context.PermissionAuditLogs
                .Where(log => log.DeleteAfter.HasValue && log.DeleteAfter.Value < olderThan)
                .ToListAsync(cancellationToken);

            var deletedCount = logsToDelete.Count;

            if (deletedCount > 0)
            {
                _context.PermissionAuditLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} old audit logs older than {Date}",
                    deletedCount, olderThan);
            }

            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
            return Result<int>.Failure("Failed to cleanup audit logs");
        }
    }

    public async Task<Result<PermissionAuditStatistics>> GetAuditStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _context.PermissionAuditLogs
                .Where(log => log.Timestamp >= fromDate && log.Timestamp <= toDate)
                .ToListAsync(cancellationToken);

            var stats = new PermissionAuditStatistics
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalPermissionChecks = logs.Count(l => l.Action == PermissionAuditAction.PermissionChecked ||
                                                       l.Action == PermissionAuditAction.PermissionAllowed ||
                                                       l.Action == PermissionAuditAction.PermissionDenied),
                SuccessfulChecks = logs.Count(l => l.Action == PermissionAuditAction.PermissionAllowed),
                FailedChecks = logs.Count(l => l.Action == PermissionAuditAction.PermissionDenied),
                PermissionChanges = logs.Count(l => l.Action == PermissionAuditAction.PermissionGrantedToRole ||
                                                  l.Action == PermissionAuditAction.PermissionRevokedFromRole ||
                                                  l.Action == PermissionAuditAction.PermissionGrantedToUser ||
                                                  l.Action == PermissionAuditAction.PermissionRevokedFromUser),
                UserPermissionGrants = logs.Count(l => l.Action == PermissionAuditAction.PermissionGrantedToUser),
                RolePermissionGrants = logs.Count(l => l.Action == PermissionAuditAction.PermissionGrantedToRole),
                TopPermissions = logs
                    .Where(l => !string.IsNullOrEmpty(l.PermissionCode))
                    .GroupBy(l => l.PermissionCode!)
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(kv => kv.Value)
                    .Take(10)
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                ActionBreakdown = logs
                    .GroupBy(l => l.Action.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<PermissionAuditStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit statistics");
            return Result<PermissionAuditStatistics>.Failure("Failed to get audit statistics");
        }
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}