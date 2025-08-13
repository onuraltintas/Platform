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
public class EmailNotificationController : ControllerBase
{
    private readonly IEmailNotificationService _emailNotificationService;

    private readonly ILogger<EmailNotificationController> _logger;

    public EmailNotificationController(
        IEmailNotificationService emailNotificationService,

        ILogger<EmailNotificationController> logger)
    {
        _emailNotificationService = emailNotificationService;

        _logger = logger;
    }

    [HttpGet]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailNotificationDto>>>> GetNotifications(
        [FromQuery] EmailNotificationFilterDto filter)
    {
        try
        {
            var notifications = await _emailNotificationService.GetFilteredAsync(filter);
            return Ok(ApiResponse<IEnumerable<EmailNotificationDto>>.Ok(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications with filter");
            return StatusCode(500, ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("{id}")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<EmailNotificationDto>>> GetNotification(string id)
    {
        try
        {
            var notification = await _emailNotificationService.GetByIdAsync(id);
            if (notification == null)
                return NotFound(ApiResponse<EmailNotificationDto>.Fail("ERROR", "Notification not found"));

            return Ok(ApiResponse<EmailNotificationDto>.Ok(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<EmailNotificationDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("user/{userId}")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailNotificationDto>>>> GetUserNotifications(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != userId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var notifications = await _emailNotificationService.GetByUserIdAsync(userId);
            return Ok(ApiResponse<IEnumerable<EmailNotificationDto>>.Ok(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return StatusCode(500, ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<EmailNotificationDto>>> CreateNotification(
        [FromBody] CreateEmailNotificationDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<EmailNotificationDto>.Fail("ERROR", "Invalid model state"));

            var notification = await _emailNotificationService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, 
                ApiResponse<EmailNotificationDto>.Ok(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, ApiResponse<EmailNotificationDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("bulk")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailNotificationDto>>>> CreateBulkNotifications(
        [FromBody] BulkEmailNotificationDto bulkDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Invalid model state"));

            var notifications = await _emailNotificationService.CreateBulkAsync(bulkDto);
            return Ok(ApiResponse<IEnumerable<EmailNotificationDto>>.Ok(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk notifications");
            return StatusCode(500, ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("from-template")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<EmailNotificationDto>>> CreateFromTemplate(
        [FromBody] CreateFromTemplateDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<EmailNotificationDto>.Fail("ERROR", "Invalid model state"));

            var notification = await _emailNotificationService.CreateFromTemplateAsync(
                createDto.TemplateName,
                createDto.UserId,
                createDto.ToEmail,
                createDto.TemplateData,
                createDto.Subject,
                createDto.ScheduledAt);

            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, 
                ApiResponse<EmailNotificationDto>.Ok(notification));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Template not found: {TemplateName}", createDto.TemplateName);
            return BadRequest(ApiResponse<EmailNotificationDto>.Fail("ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification from template");
            return StatusCode(500, ApiResponse<EmailNotificationDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("bulk-from-template")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailNotificationDto>>>> CreateBulkFromTemplate(
        [FromBody] CreateBulkFromTemplateDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Invalid model state"));

            var notifications = await _emailNotificationService.CreateBulkFromTemplateAsync(
                createDto.TemplateName,
                createDto.UserIds,
                createDto.EmailAddresses,
                createDto.TemplateData,
                createDto.Subject,
                createDto.ScheduledAt);

            return Ok(ApiResponse<IEnumerable<EmailNotificationDto>>.Ok(notifications));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Template not found: {TemplateName}", createDto.TemplateName);
            return BadRequest(ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk notifications from template");
            return StatusCode(500, ApiResponse<IEnumerable<EmailNotificationDto>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPut("{id}")]
    [Permission(Permissions.Notifications.NotificationUpdate)]
    public async Task<ActionResult<ApiResponse<EmailNotificationDto>>> UpdateNotification(
        string id, [FromBody] UpdateEmailNotificationDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<EmailNotificationDto>.Fail("ERROR", "Invalid model state"));

            var notification = await _emailNotificationService.UpdateAsync(id, updateDto);
            return Ok(ApiResponse<EmailNotificationDto>.Ok(notification));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<EmailNotificationDto>.Fail("ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<EmailNotificationDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpDelete("{id}")]
    [Permission(Permissions.Notifications.NotificationDelete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(string id)
    {
        try
        {
            var result = await _emailNotificationService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.Fail("ERROR", "Notification not found"));

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("{id}/send")]
    [Permission(Permissions.Notifications.NotificationSend)]
    public async Task<ActionResult<ApiResponse<bool>>> SendNotification(string id)
    {
        try
        {
            var result = await _emailNotificationService.SendAsync(id);
            return Ok(ApiResponse<bool>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("send-bulk")]
    [Permission(Permissions.Notifications.NotificationSend)]
    public async Task<ActionResult<ApiResponse<int>>> SendBulkNotifications([FromBody] IEnumerable<string> ids)
    {
        try
        {
            var result = await _emailNotificationService.SendBulkAsync(ids);
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("send-pending")]
    [Permission(Permissions.Notifications.NotificationManage)]
    public async Task<ActionResult<ApiResponse<int>>> SendPendingNotifications()
    {
        try
        {
            var result = await _emailNotificationService.SendPendingNotificationsAsync();
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending pending notifications");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("send-scheduled")]
    [Permission(Permissions.Notifications.NotificationManage)]
    public async Task<ActionResult<ApiResponse<int>>> SendScheduledNotifications()
    {
        try
        {
            var result = await _emailNotificationService.SendScheduledNotificationsAsync();
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending scheduled notifications");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("retry-failed")]
    [Permission(Permissions.Notifications.NotificationManage)]
    public async Task<ActionResult<ApiResponse<int>>> RetryFailedNotifications()
    {
        try
        {
            var result = await _emailNotificationService.RetryFailedNotificationsAsync();
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed notifications");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("{id}/mark-read")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _emailNotificationService.MarkAsReadAsync(userId, new List<string> { id });
            return Ok(ApiResponse<bool>.Ok(result > 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("mark-read-bulk")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<int>>> MarkAsReadBulk([FromBody] List<string> notificationIds)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _emailNotificationService.MarkAsReadAsync(userId, notificationIds);
            return Ok(ApiResponse<int>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notifications as read");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("stats")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<EmailNotificationStatsDto>>> GetStats(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId) && currentUserId != userId && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var stats = await _emailNotificationService.GetStatsAsync(fromDate, toDate, userId);
            return Ok(ApiResponse<EmailNotificationStatsDto>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification stats");
            return StatusCode(500, ApiResponse<EmailNotificationStatsDto>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpGet("count")]
    [Permission(Permissions.Notifications.NotificationRead)]
    public async Task<ActionResult<ApiResponse<int>>> GetCount([FromQuery] EmailNotificationFilterDto? filter)
    {
        try
        {
            var count = await _emailNotificationService.GetCountAsync(filter);
            return Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification count");
            return StatusCode(500, ApiResponse<int>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("validate")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateNotification([FromBody] CreateEmailNotificationDto notification)
    {
        try
        {
            var isValid = await _emailNotificationService.ValidateNotificationAsync(notification);
            return Ok(ApiResponse<bool>.Ok(isValid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating notification");
            return StatusCode(500, ApiResponse<bool>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("validate-recipients")]
    [Permission(Permissions.Notifications.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<List<string>>>> ValidateRecipients([FromBody] IEnumerable<string> emailAddresses)
    {
        try
        {
            var validEmails = await _emailNotificationService.ValidateRecipientsAsync(emailAddresses);
            return Ok(ApiResponse<List<string>>.Ok(validEmails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating recipients");
            return StatusCode(500, ApiResponse<List<string>>.Fail("ERROR", "Internal server error"));
        }
    }

    [HttpPost("cleanup")]
    [Permission(Permissions.Notifications.NotificationManage)]
    public async Task<ActionResult<ApiResponse<object>>> CleanupNotifications(
        [FromQuery] int olderThanDays = 90,
        [FromQuery] int failedOlderThanDays = 30)
    {
        try
        {
            var oldCleaned = await _emailNotificationService.CleanupOldNotificationsAsync(olderThanDays);
            var failedCleaned = await _emailNotificationService.CleanupFailedNotificationsAsync(failedOlderThanDays);

            var result = new { oldCleaned, failedCleaned };
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up notifications");
            return StatusCode(500, ApiResponse<object>.Fail("ERROR", "Internal server error"));
        }
    }
}