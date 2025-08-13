namespace EgitimPlatform.Services.NotificationService.Models.DTOs;

public class BulkSendDto
{
    public List<Guid> NotificationIds { get; set; } = new();
}
