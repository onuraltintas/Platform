using System.ComponentModel.DataAnnotations;
using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Models;

public class ConsentRecord
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    public PersonalDataCategory DataCategory { get; set; }

    [Required]
    public ConsentStatus Status { get; set; }

    [Required]
    public DateTime ConsentGiven { get; set; }

    public DateTime? ConsentWithdrawn { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(1000)]
    public string ConsentText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Version { get; set; } = "1.0";

    [MaxLength(200)]
    public string CollectionMethod { get; set; } = string.Empty;

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    public bool IsWithdrawable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class ConsentSummary
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<PersonalDataCategory, ConsentStatus> ConsentsByCategory { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public int TotalConsents { get; set; }
    public int ActiveConsents { get; set; }
    public int WithdrawnConsents { get; set; }
}