using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.NotificationService.Models.Entities;

public class EmailNotification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(256)]
    public string ToEmail { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string? ToName { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Body { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Delivered, Opened, Clicked
    
    public Guid? UserId { get; set; }
    
    public Guid? TrackingId { get; set; }
    
    [MaxLength(50)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(100)]
    public string? TemplateName { get; set; }
    
    public string? TemplateData { get; set; } // JSON
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(100)]
    public string? ExternalId { get; set; } // Provider message ID
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    [MaxLength(500)]
    public string? Metadata { get; set; } // JSON for additional data
}