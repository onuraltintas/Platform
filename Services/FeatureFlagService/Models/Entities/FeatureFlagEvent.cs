using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.FeatureFlagService.Models.Entities;

public class FeatureFlagEvent
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string FeatureFlagId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Variation { get; set; } = string.Empty;

    public Dictionary<string, object> Properties { get; set; } = new();

    [Required]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    // Navigation properties
    public virtual FeatureFlag FeatureFlag { get; set; } = null!;
}

public static class FeatureFlagEventTypes
{
    public const string Evaluated = "feature_flag_evaluated";
    public const string Enabled = "feature_flag_enabled";
    public const string Disabled = "feature_flag_disabled";
    public const string VariationChanged = "variation_changed";
    public const string ExposureLogged = "exposure_logged";
    public const string ConversionTracked = "conversion_tracked";
}