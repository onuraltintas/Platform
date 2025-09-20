using Microsoft.AspNetCore.Identity;

namespace Identity.Core.Entities;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Group/Tenant Support
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }
    
    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}