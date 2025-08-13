using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.NotificationService.Models.Entities;

public class PushNotification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    [MaxLength(500)]
    public string? ClickActionUrl { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Delivered, Clicked
    
    [MaxLength(50)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public string? Data { get; set; } // JSON payload for custom data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(100)]
    public string? ExternalId { get; set; }
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<PushNotificationDevice> PushNotificationDevices { get; set; } = new List<PushNotificationDevice>();
}