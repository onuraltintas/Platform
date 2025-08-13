using System;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class UserDeviceDto
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string PushToken { get; set; } = null!;
        public string DeviceType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
