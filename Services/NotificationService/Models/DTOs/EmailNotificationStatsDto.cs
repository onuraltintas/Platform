namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class EmailNotificationStatsDto
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public int TotalPending { get; set; }
    }
}
