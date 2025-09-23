using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

/// <summary>
/// Service for auditing permission-related operations
/// </summary>
public interface IPermissionAuditService
{
    /// <summary>
    /// Log a permission-related action
    /// </summary>
    Task LogPermissionActionAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log permission check result
    /// </summary>
    Task LogPermissionCheckAsync(
        string permissionCode,
        string userId,
        bool allowed,
        string? reason = null,
        Guid? groupId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log bulk permission operation
    /// </summary>
    Task LogBulkPermissionOperationAsync(
        PermissionAuditAction action,
        int affectedCount,
        string? performedBy = null,
        string? description = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    Task<Result<PagedResult<PermissionAuditLog>>> GetUserAuditLogsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a permission
    /// </summary>
    Task<Result<PagedResult<PermissionAuditLog>>> GetPermissionAuditLogsAsync(
        string permissionCode,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a role
    /// </summary>
    Task<Result<PagedResult<PermissionAuditLog>>> GetRoleAuditLogsAsync(
        Guid roleId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security-related audit logs (failed permissions, suspicious activity)
    /// </summary>
    Task<Result<PagedResult<PermissionAuditLog>>> GetSecurityAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old audit logs based on retention policy
    /// </summary>
    Task<Result<int>> CleanupOldAuditLogsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit statistics
    /// </summary>
    Task<Result<PermissionAuditStatistics>> GetAuditStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Permission audit statistics
/// </summary>
public class PermissionAuditStatistics
{
    public int TotalPermissionChecks { get; set; }
    public int SuccessfulChecks { get; set; }
    public int FailedChecks { get; set; }
    public int PermissionChanges { get; set; }
    public int UserPermissionGrants { get; set; }
    public int RolePermissionGrants { get; set; }
    public Dictionary<string, int> TopPermissions { get; set; } = new();
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}