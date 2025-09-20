namespace Identity.Core.Entities;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType Type { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    
    // Settings
    public int MaxUsers { get; set; } = 100;
    public DateTime? SubscriptionExpiresAt { get; set; }
    public string? SubscriptionPlan { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation Properties
    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public virtual ICollection<GroupService> GroupServices { get; set; } = new List<GroupService>();
    public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
}

public enum GroupType
{
    Organization = 1,
    Department = 2,
    Team = 3,
    Project = 4,
    Customer = 5,
    Partner = 6,
    Vendor = 7
}