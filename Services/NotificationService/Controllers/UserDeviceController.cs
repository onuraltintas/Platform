using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Services.NotificationService.Services;
using EgitimPlatform.Services.NotificationService.Models.DTOs;
using EgitimPlatform.Shared.Security.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Errors.Common;
using System.Security.Claims;

namespace EgitimPlatform.Services.NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserDeviceController : ControllerBase
{
    private readonly IUserDeviceService _deviceService;
    private readonly ILogger<UserDeviceController> _logger;

    public UserDeviceController(
        IUserDeviceService deviceService,
        ILogger<UserDeviceController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<UserDeviceDto>>> GetDevice(string id)
    {
        try
        {
            var device = await _deviceService.GetByIdAsync(id);
            if (device == null)
                return NotFound(ApiResponse<UserDeviceDto>.Fail("ERROR", "Device not found"));

            return Ok(ApiResponse<UserDeviceDto>.Ok(device));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return StatusCode(500, ApiResponse<UserDeviceDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("user/{userId}")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDeviceDto>>>> GetUserDevices(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != userId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var devices = await _deviceService.GetByUserIdAsync(userId);
            return Ok(ApiResponse<IEnumerable<UserDeviceDto>>.Ok(devices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices for user {UserId}", userId);
            return StatusCode(500, ApiResponse<IEnumerable<UserDeviceDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("user/{userId}/active")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDeviceDto>>>> GetActiveUserDevices(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != userId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var devices = await _deviceService.GetActiveDevicesByUserIdAsync(userId);
            return Ok(ApiResponse<IEnumerable<UserDeviceDto>>.Ok(devices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active devices for user {UserId}", userId);
            return StatusCode(500, ApiResponse<IEnumerable<UserDeviceDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("register")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<UserDeviceDto>>> RegisterDevice([FromBody] CreateUserDeviceDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDeviceDto>.Fail("ERROR", "Invalid model state"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != createDto.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var device = await _deviceService.RegisterOrUpdateDeviceAsync(createDto);
            return Ok(ApiResponse<UserDeviceDto>.Ok(device));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", createDto.UserId);
            return StatusCode(500, ApiResponse<UserDeviceDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<UserDeviceDto>>> CreateDevice([FromBody] CreateUserDeviceDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDeviceDto>.Fail("ERROR", "Invalid model state"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != createDto.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var device = await _deviceService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, 
                ApiResponse<UserDeviceDto>.Ok(device));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device for user {UserId}", createDto.UserId);
            return StatusCode(500, ApiResponse<UserDeviceDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPut("{id}")]
    [Permission(Permissions.Notifications.NotificationUpdate)]
    public async Task<ActionResult<ApiResponse<UserDeviceDto>>> UpdateDevice(
        string id, [FromBody] UpdateUserDeviceDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDeviceDto>.Fail("ERROR", "Invalid model state"));

            var existingDevice = await _deviceService.GetByIdAsync(id);
            if (existingDevice == null)
                return NotFound(ApiResponse<UserDeviceDto>.Fail("ERROR", "Device not found"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != existingDevice.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var device = await _deviceService.UpdateAsync(id, updateDto);
            return Ok(ApiResponse<UserDeviceDto>.Ok(device));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<UserDeviceDto>.Fail("ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId}", id);
            return StatusCode(500, ApiResponse<UserDeviceDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpDelete("{id}")]
    [Permission(Permissions.Notifications.NotificationDelete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDevice(string id)
    {
        try
        {
            var existingDevice = await _deviceService.GetByIdAsync(id);
            if (existingDevice == null)
                return NotFound(ApiResponse<bool>.Fail("ERROR","Device not found"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != existingDevice.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var result = await _deviceService.DeleteAsync(id);
            return Ok(ApiResponse<bool>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR","Internal server error"));
        }
    }

    [HttpPost("{id}/deactivate")]
    [Permission(Permissions.Notifications.NotificationUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateDevice(string id)
    {
        try
        {
            var existingDevice = await _deviceService.GetByIdAsync(id);
            if (existingDevice == null)
                return NotFound(ApiResponse<bool>.Fail("ERROR","Device not found"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != existingDevice.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            await _deviceService.DeactivateAsync(id);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating device {DeviceId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR","Internal server error"));
        }
    }

    [HttpPost("{id}/update-last-active")]
    [Permission(Permissions.Notifications.NotificationUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateLastActive(string id)
    {
        try
        {
            var existingDevice = await _deviceService.GetByIdAsync(id);
            if (existingDevice == null)
                return NotFound(ApiResponse<bool>.Fail("ERROR","Device not found"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != existingDevice.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            await _deviceService.UpdateLastActiveAsync(id);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last active for device {DeviceId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR","Internal server error"));
        }
    }

    [HttpPost("{id}/notification-enabled")]
    [Permission(Permissions.Notifications.NotificationUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateNotificationEnabled(string id, [FromBody] bool isEnabled)
    {
        try
        {
            var existingDevice = await _deviceService.GetByIdAsync(id);
            if (existingDevice == null)
                return NotFound(ApiResponse<bool>.Fail("ERROR","Device not found"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != existingDevice.UserId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            await _deviceService.UpdateNotificationEnabledAsync(id, isEnabled);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification enabled for device {DeviceId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR","Internal server error"));
        }
    }

    [HttpGet("count/active")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<int>>> GetActiveDeviceCount([FromQuery] string? userId = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(userId))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != userId && !User.IsInRole(Roles.Admin))
                {
                    return Forbid();
                }
            }

            var count = await _deviceService.GetActiveDeviceCountAsync();
            return Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active device count");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("cleanup")]
    [Permission(Permissions.Notifications.NotificationManage)]
    public async Task<ActionResult<ApiResponse<int>>> CleanupInactiveDevices([FromQuery] int inactiveDays = 90)
    {
        try
        {
            var result = await _deviceService.CleanupInactiveDevicesAsync(inactiveDays);
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive devices");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }
}