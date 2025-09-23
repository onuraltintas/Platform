using System.ComponentModel.DataAnnotations;

namespace Identity.Core.Entities;

/// <summary>
/// Audit log for permission-related changes and access attempts
/// </summary>
public class PermissionAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public PermissionAuditAction Action { get; set; }

    [StringLength(100)]
    public string? PermissionCode { get; set; }

    public Guid? PermissionId { get; set; }
    public virtual Permission? Permission { get; set; }

    // Subject (who performed the action)
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public Guid? RoleId { get; set; }
    public virtual ApplicationRole? Role { get; set; }

    // Target (who was affected)
    public string? TargetUserId { get; set; }
    public virtual ApplicationUser? TargetUser { get; set; }

    public Guid? TargetRoleId { get; set; }
    public virtual ApplicationRole? TargetRole { get; set; }

    // Context
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    // Technical details
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [StringLength(100)]
    public string? RequestId { get; set; }

    // Result
    public bool Success { get; set; } = true;

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Additional metadata (JSON)
    public string? Metadata { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Retention policy
    public DateTime? DeleteAfter { get; set; }
}

public enum PermissionAuditAction
{
    // Permission management
    PermissionCreated = 1,
    PermissionUpdated = 2,
    PermissionDeleted = 3,

    // Role-Permission assignments
    PermissionGrantedToRole = 10,
    PermissionRevokedFromRole = 11,

    // User-Permission assignments
    PermissionGrantedToUser = 20,
    PermissionRevokedFromUser = 21,

    // Permission checks/usage
    PermissionChecked = 30,
    PermissionDenied = 31,
    PermissionAllowed = 32,

    // Wildcard permissions
    WildcardPermissionGranted = 40,
    WildcardPermissionRevoked = 41,

    // Bulk operations
    BulkPermissionUpdate = 50,
    PermissionMigration = 51,

    // System events
    PermissionCacheCleared = 60,
    PermissionSystemReset = 61
}