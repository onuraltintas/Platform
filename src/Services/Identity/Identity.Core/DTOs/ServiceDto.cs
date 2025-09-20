using Identity.Core.Entities;

namespace Identity.Core.DTOs;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ServiceType Type { get; set; }
    public string TypeDisplay => Type.ToString();
    
    // Health Check
    public string? HealthCheckEndpoint { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public ServiceStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public string? StatusMessage { get; set; }
    
    // Security
    public bool RequiresAuthentication { get; set; }
    
    // Metadata
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? RegisteredBy { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    
    // Statistics
    public int PermissionCount { get; set; }
    public int GroupCount { get; set; }
}

public class CreateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public ServiceType Type { get; set; } = ServiceType.Internal;
    public string? HealthCheckEndpoint { get; set; }
    public bool RequiresAuthentication { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class UpdateServiceRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? HealthCheckEndpoint { get; set; }
    public bool RequiresAuthentication { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class ServiceRegistrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public ServiceType Type { get; set; } = ServiceType.Internal;
    public string? HealthCheckEndpoint { get; set; }
    public string RegistrationKey { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public IEnumerable<CreatePermissionRequest> Permissions { get; set; } = new List<CreatePermissionRequest>();
}

public class ServiceHealthCheckRequest
{
    public Guid ServiceId { get; set; }
    public ServiceStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public Dictionary<string, object> HealthData { get; set; } = new();
}