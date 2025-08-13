using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.NotificationService.Models.Entities;

public class PushNotificationDevice
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid PushNotificationId { get; set; }
    
    [Required]
    public string UserDeviceId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Delivered, Clicked
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(100)]
    public string? ExternalId { get; set; }
    
    // Navigation properties
    public virtual PushNotification PushNotification { get; set; } = null!;
    public virtual UserDevice UserDevice { get; set; } = null!;
}