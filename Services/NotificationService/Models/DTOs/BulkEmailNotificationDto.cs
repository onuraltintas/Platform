using System.Collections.Generic;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class BulkEmailNotificationDto
    {
        public List<CreateEmailNotificationDto> Notifications { get; set; } = new();
    }
}
