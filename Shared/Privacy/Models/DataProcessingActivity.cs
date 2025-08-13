using System.ComponentModel.DataAnnotations;
using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Models;

public class DataProcessingActivity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Controller { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Processor { get; set; } = string.Empty;

    [Required]
    public DataProcessingLawfulBasis LawfulBasis { get; set; }

    [MaxLength(1000)]
    public string LawfulBasisDetails { get; set; } = string.Empty;

    [Required]
    public List<PersonalDataCategory> DataCategories { get; set; } = new();

    [Required]
    public List<string> DataSubjectCategories { get; set; } = new();

    [Required]
    public List<string> ProcessingPurposes { get; set; } = new();

    public List<string> Recipients { get; set; } = new();

    public List<string> ThirdCountryTransfers { get; set; } = new();

    public Dictionary<PersonalDataCategory, int> RetentionPeriods { get; set; } = new();

    [MaxLength(1000)]
    public string SecurityMeasures { get; set; } = string.Empty;

    public bool RequiresDataProtectionImpactAssessment { get; set; }

    public string? DataProtectionImpactAssessmentUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class ProcessingActivitySummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DataProcessingLawfulBasis LawfulBasis { get; set; }
    public int DataCategoriesCount { get; set; }
    public int DataSubjectsCount { get; set; }
    public bool RequiresDPIA { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastUpdated { get; set; }
}