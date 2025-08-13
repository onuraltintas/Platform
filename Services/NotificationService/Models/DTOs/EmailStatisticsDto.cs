namespace EgitimPlatform.Services.NotificationService.Models.DTOs;

public class EmailStatisticsDto
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public double DeliveryRate { get; set; }
    public double OpenRate { get; set; }
}
