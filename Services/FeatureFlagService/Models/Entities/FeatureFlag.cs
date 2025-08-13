using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.FeatureFlagService.Models.Entities;

public class FeatureFlag
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public FeatureFlagType Type { get; set; }

    [Required]
    public bool IsEnabled { get; set; }

    [Required]
    public FeatureFlagStatus Status { get; set; } = FeatureFlagStatus.Draft;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public int RolloutPercentage { get; set; } = 0;

    public List<string> TargetAudiences { get; set; } = new();

    public List<string> ExcludedAudiences { get; set; } = new();

    public Dictionary<string, object> Conditions { get; set; } = new();

    public Dictionary<string, object> Variations { get; set; } = new();

    [MaxLength(100)]
    public string DefaultVariation { get; set; } = "control";

    [MaxLength(100)]
    public string Environment { get; set; } = "production";

    [MaxLength(200)]
    public string ApplicationId { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<FeatureFlagAssignment> Assignments { get; set; } = new List<FeatureFlagAssignment>();
    public virtual ICollection<FeatureFlagEvent> Events { get; set; } = new List<FeatureFlagEvent>();
}

public enum FeatureFlagType
{
    Boolean = 1,
    String = 2,
    Number = 3,
    Json = 4,
    Rollout = 5
}

public enum FeatureFlagStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    Completed = 4,
    Archived = 5
}