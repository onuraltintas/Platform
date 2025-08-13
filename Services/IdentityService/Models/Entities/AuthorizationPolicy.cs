using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.IdentityService.Models.Entities;

public class AuthorizationPolicy
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(256)]
    public string MatchRegex { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized string array of roles
    /// </summary>
    public string? RequiredRolesJson { get; set; }

    /// <summary>
    /// JSON serialized string array of permissions
    /// </summary>
    public string? RequiredPermissionsJson { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(64)]
    public string? TenantId { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string? UpdatedBy { get; set; }
}

