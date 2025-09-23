using System.ComponentModel.DataAnnotations;

namespace Identity.Core.Entities;

/// <summary>
/// Direct user-permission assignments (overrides role permissions)
/// </summary>
public class UserPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid? PermissionId { get; set; }
    public virtual Permission? Permission { get; set; }

    // Support for wildcard permissions (when PermissionId is null)
    [StringLength(100)]
    public string? PermissionPattern { get; set; }

    public bool IsWildcard { get; set; } = false;

    // Permission type (grant or deny)
    public UserPermissionType Type { get; set; } = UserPermissionType.Grant;

    // Audit fields
    public string GrantedBy { get; set; } = "System";
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Optional: Group context for multi-tenancy
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }

    // Conditional permissions
    [StringLength(1000)]
    public string? Conditions { get; set; } // JSON for complex conditions

    // Reason for direct assignment
    [StringLength(500)]
    public string? Reason { get; set; }
}

public enum UserPermissionType
{
    /// <summary>
    /// Grants the permission to the user
    /// </summary>
    Grant = 1,

    /// <summary>
    /// Explicitly denies the permission (overrides role permissions)
    /// </summary>
    Deny = 2
}