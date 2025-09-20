namespace Identity.Core.Entities;

public class GroupService
{
    public Guid GroupId { get; set; }
    public virtual Group Group { get; set; } = null!;
    
    public Guid ServiceId { get; set; }
    public virtual Service Service { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Limits
    public int? MaxRequests { get; set; }
    public int? MaxUsers { get; set; }
    public decimal? MaxStorage { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}