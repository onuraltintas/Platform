using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Core.Entities;

/// <summary>
/// Device trust information for Zero Trust architecture
/// </summary>
[Table("DeviceTrusts")]
public class DeviceTrust
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty; // Mobile, Desktop, Tablet

    [Required]
    [MaxLength(100)]
    public string OperatingSystem { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Browser { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? DeviceName { get; set; }

    public bool IsTrusted { get; set; } = false;

    public bool IsManaged { get; set; } = false;

    public bool IsCompliant { get; set; } = false;

    public bool IsJailbroken { get; set; } = false;

    [MaxLength(128)]
    public string? CertificateFingerprint { get; set; }

    [MaxLength(64)]
    public string? DeviceFingerprint { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TrustScore { get; set; } = 50.0m;

    [Column(TypeName = "jsonb")]
    public string CompliancePolicies { get; set; } = "{}"; // JSON policy check results

    [Column(TypeName = "jsonb")]
    public string SecurityFeatures { get; set; } = "{}"; // JSON security features

    [Column(TypeName = "jsonb")]
    public string AdditionalInfo { get; set; } = "{}"; // JSON additional device info

    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public DateTime? LastComplianceCheck { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<DeviceActivity> Activities { get; set; } = new List<DeviceActivity>();
}

/// <summary>
/// Device activity tracking
/// </summary>
[Table("DeviceActivities")]
public class DeviceActivity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DeviceTrustId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty; // Login, DataAccess, PolicyCheck

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }

    [Column(TypeName = "jsonb")]
    public string ActivityData { get; set; } = "{}";

    public bool IsSuccessful { get; set; } = true;

    [MaxLength(255)]
    public string? ErrorMessage { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(DeviceTrustId))]
    public virtual DeviceTrust? DeviceTrust { get; set; }
}