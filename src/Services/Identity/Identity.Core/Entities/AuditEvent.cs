using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Identity.Core.Entities;

/// <summary>
/// Audit event entity for comprehensive logging
/// </summary>
[Table("AuditEvents")]
[Index(nameof(UserId))]
[Index(nameof(EventType))]
[Index(nameof(Timestamp))]
[Index(nameof(IsSecurityEvent))]
[Index(nameof(Severity))]
public class AuditEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(128)]
    public string? SessionId { get; set; }

    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string OldValues { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string NewValues { get; set; } = "{}";

    public int Severity { get; set; } = 0; // Maps to AuditSeverity enum

    public int Category { get; set; } = 0; // Maps to AuditCategory enum

    public bool IsSecurityEvent { get; set; } = false;

    [MaxLength(20)]
    public string? RiskLevel { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string Tags { get; set; } = "{}";

    public Guid? GroupId { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public long? Duration { get; set; } // Operation duration in milliseconds

    public bool IsSuccessful { get; set; } = true;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual Group? Group { get; set; }

    public virtual ICollection<AuditEventAttachment> Attachments { get; set; } = new List<AuditEventAttachment>();
}

/// <summary>
/// Audit event attachments for additional context
/// </summary>
[Table("AuditEventAttachments")]
public class AuditEventAttachment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AuditEventId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    [MaxLength(1000)]
    public string? FilePath { get; set; }

    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(AuditEventId))]
    public virtual AuditEvent? AuditEvent { get; set; }
}