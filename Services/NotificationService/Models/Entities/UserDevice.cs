using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.NotificationService.Models.Entities;

public class UserDevice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string PushToken { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty; // iOS, Android, Web
    
    [MaxLength(100)]
    public string? DeviceModel { get; set; }
    
    [MaxLength(50)]
    public string? OperatingSystem { get; set; }
    
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    
    [MaxLength(50)]
    public string? Language { get; set; } = "en";
    
    [MaxLength(100)]
    public string? TimeZone { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    [MaxLength(500)]
    public string? Metadata { get; set; } // JSON for additional device info
}