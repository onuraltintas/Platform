using Identity.Core.Entities;

namespace Identity.Core.DTOs;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    
    // Service Association
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public ServiceDto? Service { get; set; }
    
    // Classification
    public PermissionType Type { get; set; }
    public string TypeDisplay => Type.ToString();
    public int Priority { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // Usage Statistics
    public int RoleCount { get; set; }
    public int UserCount { get; set; }
}

public class CreatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public PermissionType Type { get; set; } = PermissionType.Custom;
    public int Priority { get; set; } = 0;
}

public class UpdatePermissionRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; }
}

public class PermissionCheckRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string? Resource { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class PermissionCheckResponse
{
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public IEnumerable<string> RequiredRoles { get; set; } = new List<string>();
    public DateTime CheckedAt { get; set; }
}

public class BulkPermissionCheckRequest
{
    public string UserId { get; set; } = string.Empty;
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
    public Guid? GroupId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class BulkPermissionCheckResponse
{
    public Dictionary<string, bool> Results { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

public class PermissionMatrixDto
{
    public IEnumerable<ServicePermissionGroup> Services { get; set; } = new List<ServicePermissionGroup>();
    public IEnumerable<RolePermissionGroup> Roles { get; set; } = new List<RolePermissionGroup>();
    public DateTime GeneratedAt { get; set; }
}

public class ServicePermissionGroup
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public IEnumerable<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
}

public class RolePermissionGroup
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public IEnumerable<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
}