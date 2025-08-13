using System;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class EmailNotificationFilterDto
    {
        public string? Status { get; set; }
        public string? RecipientEmail { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
