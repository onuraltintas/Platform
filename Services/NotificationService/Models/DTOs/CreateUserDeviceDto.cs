namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class CreateUserDeviceDto
    {
        public string UserId { get; set; } = null!;
        public string PushToken { get; set; } = null!;
        public string DeviceType { get; set; } = null!;
    }
}
