namespace EgitimPlatform.Services.NotificationService.Models.DTOs;

public class EmailNotificationDto
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? TrackingId { get; set; }
    public DateTime? OpenedAt { get; set; }
}
