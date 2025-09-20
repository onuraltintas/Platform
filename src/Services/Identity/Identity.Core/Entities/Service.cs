namespace Identity.Core.Entities;

public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public ServiceType Type { get; set; }
    
    // Health Check
    public string? HealthCheckEndpoint { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public ServiceStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    
    // Security
    public string? ApiKey { get; set; }
    public bool RequiresAuthentication { get; set; } = true;
    
    // Metadata
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? RegisteredBy { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public virtual ICollection<GroupService> GroupServices { get; set; } = new List<GroupService>();
}

public enum ServiceType
{
    Internal = 1,
    External = 2,
    ThirdParty = 3
}

public enum ServiceStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
    Offline = 4
}