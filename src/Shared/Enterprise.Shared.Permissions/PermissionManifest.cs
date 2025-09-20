using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Permissions;

public class PermissionManifest
{
    [Required]
    public ManifestServiceInfo Service { get; set; } = new ManifestServiceInfo();
    
    [Required]
    public List<PermissionManifestEntry> Permissions { get; set; } = new List<PermissionManifestEntry>();
    
    public string? SchemaVersion { get; set; } = "1.0";
    public string? Signature { get; set; }
}

public class ManifestServiceInfo
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Version { get; set; }
    public string? Endpoint { get; set; }
}

public class PermissionManifestEntry
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Resource { get; set; } = string.Empty;
    [Required]
    public string Action { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

