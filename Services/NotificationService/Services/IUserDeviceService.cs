using System.Collections.Generic;
using System.Threading.Tasks;
using EgitimPlatform.Services.NotificationService.Models.DTOs;

namespace EgitimPlatform.Services.NotificationService.Services
{
    public interface IUserDeviceService
    {
        Task<UserDeviceDto> GetByIdAsync(string id);
        Task<IEnumerable<UserDeviceDto>> GetByUserIdAsync(string userId);
        Task<IEnumerable<UserDeviceDto>> GetActiveDevicesByUserIdAsync(string userId);
        Task<UserDeviceDto> CreateAsync(CreateUserDeviceDto dto);
        Task<UserDeviceDto> RegisterOrUpdateDeviceAsync(CreateUserDeviceDto dto);
        Task<UserDeviceDto> UpdateAsync(string id, UpdateUserDeviceDto dto);
        Task<bool> DeleteAsync(string id);
        Task DeactivateAsync(string id);
        Task UpdateLastActiveAsync(string id);
        Task UpdateNotificationEnabledAsync(string id, bool isEnabled);
        Task<int> GetActiveDeviceCountAsync();
        Task<int> CleanupInactiveDevicesAsync(int olderThanDays);

    }
}
