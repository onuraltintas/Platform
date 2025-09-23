using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Core.Entities;

/// <summary>
/// Security policy configuration for Zero Trust
/// </summary>
[Table("SecurityPolicies")]
public class SecurityPolicy
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PolicyType { get; set; } = string.Empty; // Device, Network, Behavior, Authentication

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // Compliance, Security, Access

    [Column(TypeName = "jsonb")]
    public string Rules { get; set; } = "{}"; // JSON policy rules

    [Column(TypeName = "jsonb")]
    public string Conditions { get; set; } = "{}"; // JSON conditions

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinimumTrustScore { get; set; } = 50.0m;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    public bool IsActive { get; set; } = true;

    public bool IsEnforced { get; set; } = true;

    public int Priority { get; set; } = 100;

    public Guid? GroupId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(GroupId))]
    public virtual Group? Group { get; set; }

    public virtual ICollection<PolicyViolation> Violations { get; set; } = new List<PolicyViolation>();
}

/// <summary>
/// Policy violation tracking
/// </summary>
[Table("PolicyViolations")]
public class PolicyViolation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SecurityPolicyId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? DeviceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ViolationType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium";

    [Column(TypeName = "jsonb")]
    public string ViolationData { get; set; } = "{}";

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open, Acknowledged, Resolved, Ignored

    [MaxLength(1000)]
    public string? Resolution { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(450)]
    public string? AcknowledgedBy { get; set; }

    [MaxLength(450)]
    public string? ResolvedBy { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(SecurityPolicyId))]
    public virtual SecurityPolicy? SecurityPolicy { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}