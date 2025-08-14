using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.IdentityService.Models.Entities;

public class UserRole
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RoleId { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}