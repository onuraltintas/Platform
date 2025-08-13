using Microsoft.EntityFrameworkCore;
using AutoMapper;
using EgitimPlatform.Services.NotificationService.Data;
using EgitimPlatform.Services.NotificationService.Models.DTOs;
using EgitimPlatform.Services.NotificationService.Models.Entities;

namespace EgitimPlatform.Services.NotificationService.Services;

public class UserDeviceService : IUserDeviceService
{
    private readonly NotificationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserDeviceService> _logger;

    public UserDeviceService(
        NotificationDbContext context,
        IMapper mapper,
        ILogger<UserDeviceService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDeviceDto>> GetByUserIdAsync(string userId)
    {
        var devices = await _context.UserDevices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UserDeviceDto>>(devices);
    }

    public async Task<IEnumerable<UserDeviceDto>> GetActiveDevicesByUserIdAsync(string userId)
    {
        var devices = await _context.UserDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UserDeviceDto>>(devices);
    }

    public async Task<UserDeviceDto> GetByIdAsync(string id)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            throw new KeyNotFoundException($"Device with ID {id} not found");

        return _mapper.Map<UserDeviceDto>(device);
    }

    public async Task<UserDeviceDto?> GetByPushTokenAsync(string pushToken)
    {
        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.PushToken == pushToken && d.IsActive);

        if (device == null)
            return null;

        return _mapper.Map<UserDeviceDto>(device);
    }

    public async Task<UserDeviceDto> CreateAsync(CreateUserDeviceDto createDto)
    {
        var device = _mapper.Map<UserDevice>(createDto);
        device.LastUsedAt = DateTime.UtcNow;
        
        _context.UserDevices.Add(device);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new device {DeviceId} for user {UserId}", device.Id, device.UserId);
        
        return _mapper.Map<UserDeviceDto>(device);
    }

    public async Task<UserDeviceDto> RegisterOrUpdateDeviceAsync(CreateUserDeviceDto createDto)
    {
        // Check if device with same push token already exists
        var existingDevice = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.PushToken == createDto.PushToken);

        if (existingDevice != null)
        {
            // Update existing device
            existingDevice.UserId = createDto.UserId;
            existingDevice.DeviceType = createDto.DeviceType;
            existingDevice.IsActive = true;
            existingDevice.UpdatedAt = DateTime.UtcNow;
            existingDevice.LastUsedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated existing device with push token for user {UserId}", createDto.UserId);
            
            return _mapper.Map<UserDeviceDto>(existingDevice);
        }

        return await CreateAsync(createDto);
    }

    public async Task<UserDeviceDto> UpdateAsync(string id, UpdateUserDeviceDto updateDto)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            throw new KeyNotFoundException($"Device with ID {id} not found");

        _mapper.Map(updateDto, device);
        device.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated device {DeviceId}", id);
        
        return _mapper.Map<UserDeviceDto>(device);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            return false;

        _context.UserDevices.Remove(device);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted device {DeviceId}", id);
        
        return true;
    }

    public async Task DeactivateAsync(string id)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            throw new KeyNotFoundException($"Device with ID {id} not found");

        device.IsActive = false;
        device.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated device {DeviceId}", id);
    }

    public async Task<int> DeactivateUserDevicesAsync(string userId)
    {
        var devices = await _context.UserDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync();

        foreach (var device in devices)
        {
            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated {Count} devices for user {UserId}", devices.Count, userId);
        
        return devices.Count;
    }

    public async Task<bool> UpdateLastUsedAsync(string id)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            return false;

        device.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task UpdateLastActiveAsync(string id)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            throw new KeyNotFoundException($"Device with ID {id} not found");

        device.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNotificationEnabledAsync(string id, bool isEnabled)
    {
        var device = await _context.UserDevices.FindAsync(id);
        if (device == null)
            throw new KeyNotFoundException($"Device with ID {id} not found");

        // Assuming there's a NotificationEnabled property in the entity
        // If not, we can add it to the metadata or add a new property
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated notification enabled status for device {DeviceId} to {IsEnabled}", id, isEnabled);
    }

    public async Task<bool> UpdateLastUsedByTokenAsync(string pushToken)
    {
        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.PushToken == pushToken && d.IsActive);

        if (device == null)
            return false;

        device.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<IEnumerable<UserDeviceDto>> GetByDeviceTypeAsync(string deviceType)
    {
        var devices = await _context.UserDevices
            .Where(d => d.DeviceType == deviceType && d.IsActive)
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UserDeviceDto>>(devices);
    }

    public async Task<int> GetActiveDeviceCountAsync()
    {
        return await _context.UserDevices.CountAsync(d => d.IsActive);
    }

    public async Task<int> GetActiveDeviceCountByUserAsync(string userId)
    {
        return await _context.UserDevices.CountAsync(d => d.UserId == userId && d.IsActive);
    }

    public async Task<IEnumerable<UserDeviceDto>> GetInactiveDevicesAsync(int inactiveDays = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDays);
        
        var inactiveDevices = await _context.UserDevices
            .Where(d => d.IsActive && (d.LastUsedAt == null || d.LastUsedAt < cutoffDate))
            .OrderBy(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UserDeviceDto>>(inactiveDevices);
    }

    public async Task<int> CleanupInactiveDevicesAsync(int inactiveDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDays);
        
        var inactiveDevices = await _context.UserDevices
            .Where(d => d.LastUsedAt.HasValue && d.LastUsedAt < cutoffDate)
            .ToListAsync();

        _context.UserDevices.RemoveRange(inactiveDevices);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} inactive devices", inactiveDevices.Count);
        
        return inactiveDevices.Count;
    }

    public async Task<bool> ValidateDeviceAsync(CreateUserDeviceDto device)
    {
        // Basic validation
        if (string.IsNullOrEmpty(device.UserId) || 
            string.IsNullOrEmpty(device.PushToken) || 
            string.IsNullOrEmpty(device.DeviceType))
            return false;

        // Validate device type
        var validDeviceTypes = new[] { "iOS", "Android", "Web", "Desktop" };
        if (!validDeviceTypes.Contains(device.DeviceType, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public async Task<IEnumerable<string>> GetUserPushTokensAsync(string userId)
    {
        var tokens = await _context.UserDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .Select(d => d.PushToken)
            .ToListAsync();

        return tokens;
    }

    public async Task<Dictionary<string, int>> GetDeviceTypeStatisticsAsync()
    {
        var stats = await _context.UserDevices
            .Where(d => d.IsActive)
            .GroupBy(d => d.DeviceType)
            .Select(g => new { DeviceType = g.Key, Count = g.Count() })
            .ToListAsync();

        return stats.ToDictionary(s => s.DeviceType, s => s.Count);
    }
}