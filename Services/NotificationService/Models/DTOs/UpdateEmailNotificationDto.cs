using System;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class UpdateEmailNotificationDto
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? Status { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }
}
