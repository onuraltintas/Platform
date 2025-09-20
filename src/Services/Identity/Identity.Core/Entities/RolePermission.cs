namespace Identity.Core.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public virtual ApplicationRole Role { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
    
    // Group-specific permission override
    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }
    
    // Conditions
    public string? Conditions { get; set; } // JSON for complex conditions
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}