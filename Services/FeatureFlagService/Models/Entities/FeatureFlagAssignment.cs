using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.FeatureFlagService.Models.Entities;

public class FeatureFlagAssignment
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string FeatureFlagId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AssignedVariation { get; set; } = string.Empty;

    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public Dictionary<string, object> Context { get; set; } = new();

    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual FeatureFlag FeatureFlag { get; set; } = null!;
}