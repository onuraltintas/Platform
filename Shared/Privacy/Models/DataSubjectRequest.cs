using System.ComponentModel.DataAnnotations;
using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Models;

public class DataSubjectRequest
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public DataSubjectRightType RequestType { get; set; }

    [Required]
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Pending;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public List<PersonalDataCategory> AffectedDataCategories { get; set; } = new();

    [Required]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletionDate { get; set; }

    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [MaxLength(100)]
    public string AssignedTo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ProcessingNotes { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string CompletionNotes { get; set; } = string.Empty;

    [MaxLength(45)]
    public string RequestorIpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string RequestorUserAgent { get; set; } = string.Empty;

    public List<string> AttachmentUrls { get; set; } = new();

    public Dictionary<string, object> ProcessingMetadata { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class DataSubjectRequestSummary
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DataSubjectRightType RequestType { get; set; }
    public DataSubjectRequestStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue => DateTime.UtcNow > DueDate && Status != DataSubjectRequestStatus.Completed;
    public int DaysRemaining => (DueDate - DateTime.UtcNow).Days;
}