using System.ComponentModel.DataAnnotations;
using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Models;

public class PersonalDataInventory
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public PersonalDataCategory DataCategory { get; set; }

    [Required]
    [MaxLength(200)]
    public string DataField { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StorageLocation { get; set; } = string.Empty;

    [Required]
    public DataProcessingLawfulBasis LawfulBasis { get; set; }

    [Required]
    public DateTime CollectedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public DateTime? ScheduledForDeletionAt { get; set; }

    [Required]
    public DataRetentionReason RetentionReason { get; set; }

    public int RetentionPeriodDays { get; set; }

    [MaxLength(500)]
    public string RetentionJustification { get; set; } = string.Empty;

    public bool IsEncrypted { get; set; }

    public bool IsPseudonymized { get; set; }

    public bool IsAnonymized { get; set; }

    [MaxLength(200)]
    public string ProcessingSystem { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string ProcessingPurpose { get; set; } = string.Empty;

    public List<string> SharedWith { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public class PersonalDataSummary
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<PersonalDataCategory, int> DataCountByCategory { get; set; } = new();
    public Dictionary<DataProcessingLawfulBasis, int> DataCountByLawfulBasis { get; set; } = new();
    public DateTime OldestDataPoint { get; set; }
    public DateTime NewestDataPoint { get; set; }
    public int TotalDataPoints { get; set; }
    public int EncryptedDataPoints { get; set; }
    public int PseudonymizedDataPoints { get; set; }
    public int AnonymizedDataPoints { get; set; }
    public List<string> StorageLocations { get; set; } = new();
    public List<string> ProcessingSystems { get; set; } = new();
}