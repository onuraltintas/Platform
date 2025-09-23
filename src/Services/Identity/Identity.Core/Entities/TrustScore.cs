using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Core.Entities;

/// <summary>
/// Trust score entity for Zero Trust architecture
/// </summary>
[Table("TrustScores")]
public class TrustScore
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } // 0.00 to 100.00

    [Required]
    public int TrustLevel { get; set; } // Maps to TrustLevel enum

    [Column(TypeName = "decimal(5,2)")]
    public decimal DeviceScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal NetworkScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal BehaviorScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AuthenticationScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal LocationScore { get; set; }

    [Column(TypeName = "jsonb")]
    public string Factors { get; set; } = "[]"; // JSON serialized trust factors

    [Column(TypeName = "jsonb")]
    public string Risks { get; set; } = "[]"; // JSON serialized risks

    [Column(TypeName = "jsonb")]
    public string Recommendations { get; set; } = "[]"; // JSON serialized recommendations

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<TrustScoreHistory> History { get; set; } = new List<TrustScoreHistory>();
}

/// <summary>
/// Trust score history for tracking changes over time
/// </summary>
[Table("TrustScoreHistory")]
public class TrustScoreHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TrustScoreId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PreviousScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal NewScore { get; set; }

    [Required]
    [MaxLength(100)]
    public string ChangeReason { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string EventData { get; set; } = "{}";

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(TrustScoreId))]
    public virtual TrustScore? TrustScore { get; set; }
}