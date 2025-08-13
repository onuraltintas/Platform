namespace EgitimPlatform.Services.NotificationService.Models.DTOs;

public class CreateEmailNotificationDto
{
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public Guid? UserId { get; set; }
}
