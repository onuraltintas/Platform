using Microsoft.EntityFrameworkCore;
using AutoMapper;
using EgitimPlatform.Services.NotificationService.Data;
using EgitimPlatform.Services.NotificationService.Models.DTOs;
using EgitimPlatform.Services.NotificationService.Models.Entities;
using EgitimPlatform.Shared.Email.Services;
using EgitimPlatform.Shared.Email.Models;
using System.Text.Json;

namespace EgitimPlatform.Services.NotificationService.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        NotificationDbContext context,
        IMapper mapper,
        IEmailService emailService,
        ILogger<EmailNotificationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<EmailNotificationDto>> GetFilteredAsync(EmailNotificationFilterDto filter)
    {
        var query = _context.EmailNotifications.AsQueryable();

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(e => e.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.RecipientEmail))
            query = query.Where(e => e.ToEmail.Contains(filter.RecipientEmail));

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.EndDate.Value);

        query = query.OrderByDescending(e => e.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize);

        var notifications = await query.ToListAsync();
        return _mapper.Map<IEnumerable<EmailNotificationDto>>(notifications);
    }

    public async Task<EmailNotificationDto> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
            throw new ArgumentException("Invalid ID format");

        var notification = await _context.EmailNotifications.FindAsync(guidId);
        if (notification == null)
            throw new KeyNotFoundException($"Email notification with ID {id} not found");

        return _mapper.Map<EmailNotificationDto>(notification);
    }

    public async Task<IEnumerable<EmailNotificationDto>> GetByUserIdAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var guidUserId))
            throw new ArgumentException("Invalid User ID format");

        var notifications = await _context.EmailNotifications
            .Where(e => e.UserId == guidUserId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EmailNotificationDto>>(notifications);
    }

    public async Task<EmailNotificationDto> CreateAsync(CreateEmailNotificationDto createDto)
    {
        var notification = _mapper.Map<EmailNotification>(createDto);
        
        _context.EmailNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email notification created with ID: {NotificationId}", notification.Id);
        
        return _mapper.Map<EmailNotificationDto>(notification);
    }

    public async Task<IEnumerable<EmailNotificationDto>> CreateBulkAsync(BulkEmailNotificationDto bulkDto)
    {
        var notifications = _mapper.Map<List<EmailNotification>>(bulkDto);
        
        _context.EmailNotifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk email notifications created. Count: {Count}", notifications.Count);
        
        return _mapper.Map<IEnumerable<EmailNotificationDto>>(notifications);
    }

    public async Task<EmailNotificationDto> CreateFromTemplateAsync(string templateName, string? userId, string toEmail, Dictionary<string, string> templateData, string? subject, DateTime? scheduledAt)
    {
        var notification = new EmailNotification
        {
            ToEmail = toEmail,
            Subject = subject ?? "Notification",
            Body = $"Template: {templateName} - Data: {JsonSerializer.Serialize(templateData)}",
            TemplateName = templateName,
            TemplateData = JsonSerializer.Serialize(templateData),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ScheduledAt = scheduledAt,
            RetryCount = 0,
            MaxRetries = 3,
            TrackingId = Guid.NewGuid()
        };

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var guidUserId))
        {
            notification.UserId = guidUserId;
        }
        
        _context.EmailNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template-based email notification created with ID: {NotificationId}", notification.Id);
        
        return _mapper.Map<EmailNotificationDto>(notification);
    }

    public async Task<IEnumerable<EmailNotificationDto>> CreateBulkFromTemplateAsync(string templateName, List<string>? userIds, List<string>? emailAddresses, Dictionary<string, string> templateData, string? subject, DateTime? scheduledAt)
    {
        var notifications = new List<EmailNotification>();
        var trackingId = Guid.NewGuid();
        var templateDataJson = JsonSerializer.Serialize(templateData);
        
        if (emailAddresses != null)
        {
            foreach (var email in emailAddresses)
            {
                var notification = new EmailNotification
                {
                    ToEmail = email,
                    Subject = subject ?? "Notification",
                    Body = $"Template: {templateName} - Data: {templateDataJson}",
                    TemplateName = templateName,
                    TemplateData = templateDataJson,
                    Status = "Pending",
                    TrackingId = trackingId,
                    CreatedAt = DateTime.UtcNow,
                    ScheduledAt = scheduledAt,
                    RetryCount = 0,
                    MaxRetries = 3
                };
                notifications.Add(notification);
            }
        }
        
        _context.EmailNotifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk template-based email notifications created. Count: {Count}", notifications.Count);
        
        return _mapper.Map<IEnumerable<EmailNotificationDto>>(notifications);
    }

    public async Task<EmailNotificationDto> UpdateAsync(string id, UpdateEmailNotificationDto updateDto)
    {
        if (!Guid.TryParse(id, out var guidId))
            throw new ArgumentException("Invalid ID format");

        var notification = await _context.EmailNotifications.FindAsync(guidId);
        if (notification == null)
            throw new KeyNotFoundException($"Email notification with ID {id} not found");

        _mapper.Map(updateDto, notification);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email notification updated with ID: {NotificationId}", notification.Id);
        
        return _mapper.Map<EmailNotificationDto>(notification);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
            throw new ArgumentException("Invalid ID format");

        var notification = await _context.EmailNotifications.FindAsync(guidId);
        if (notification == null)
            return false;

        _context.EmailNotifications.Remove(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email notification deleted with ID: {NotificationId}", guidId);
        
        return true;
    }

    public async Task<bool> SendAsync(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
            throw new ArgumentException("Invalid ID format");

        var notification = await _context.EmailNotifications.FindAsync(guidId);
        if (notification == null)
            return false;

        try
        {
            var emailMessage = new EmailMessage(notification.ToEmail, notification.Subject, notification.Body);

            var result = await _emailService.SendEmailAsync(emailMessage);
            
            if (result.IsSuccess)
            {
                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                notification.ExternalId = result.MessageId;
            }
            else
            {
                notification.Status = "Failed";
                notification.ErrorMessage = result.ErrorMessage;
                notification.RetryCount++;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Email notification send attempt completed. ID: {NotificationId}, Success: {Success}", 
                notification.Id, result.IsSuccess);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            notification.Status = "Failed";
            notification.ErrorMessage = ex.Message;
            notification.RetryCount++;
            
            await _context.SaveChangesAsync();
            
            _logger.LogError(ex, "Failed to send email notification with ID: {NotificationId}", notification.Id);
            
            return false;
        }
    }

    public async Task<int> SendBulkAsync(IEnumerable<string> ids)
    {
        var sentCount = 0;
        
        foreach (var id in ids)
        {
            if (await SendAsync(id))
                sentCount++;
        }
        
        _logger.LogInformation("Bulk email send completed. Sent: {SentCount} out of {TotalCount}", sentCount, ids.Count());
        
        return sentCount;
    }

    public async Task<int> SendPendingNotificationsAsync()
    {
        var pendingNotifications = await _context.EmailNotifications
            .Where(e => e.Status == "Pending" && (e.ScheduledAt == null || e.ScheduledAt <= DateTime.UtcNow))
            .Take(100) // Process in batches
            .ToListAsync();

        var sentCount = 0;
        
        foreach (var notification in pendingNotifications)
        {
            if (await SendAsync(notification.Id.ToString()))
                sentCount++;
        }
        
        return sentCount;
    }

    public async Task<int> SendScheduledNotificationsAsync()
    {
        var scheduledNotifications = await _context.EmailNotifications
            .Where(e => e.Status == "Pending" && e.ScheduledAt.HasValue && e.ScheduledAt <= DateTime.UtcNow)
            .Take(100)
            .ToListAsync();

        var sentCount = 0;
        
        foreach (var notification in scheduledNotifications)
        {
            if (await SendAsync(notification.Id.ToString()))
                sentCount++;
        }
        
        return sentCount;
    }

    public async Task<int> RetryFailedNotificationsAsync()
    {
        var failedNotifications = await _context.EmailNotifications
            .Where(e => e.Status == "Failed" && e.RetryCount < e.MaxRetries)
            .Take(50) // Limit retries
            .ToListAsync();

        var retriedCount = 0;
        
        foreach (var notification in failedNotifications)
        {
            if (await SendAsync(notification.Id.ToString()))
                retriedCount++;
        }
        
        return retriedCount;
    }

    public async Task<int> MarkAsReadAsync(string userId, List<string> notificationIds)
    {
        if (!Guid.TryParse(userId, out var guidUserId))
            throw new ArgumentException("Invalid User ID format");

        var guidIds = notificationIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(id => Guid.Parse(id))
            .ToList();

        var notifications = await _context.EmailNotifications
            .Where(e => e.UserId == guidUserId && guidIds.Contains(e.Id) && !e.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        return notifications.Count;
    }

    public async Task<EmailNotificationStatsDto> GetStatsAsync(DateTime? fromDate, DateTime? toDate, string? userId)
    {
        var query = _context.EmailNotifications.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var guidUserId))
            query = query.Where(e => e.UserId == guidUserId);

        var stats = await query
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new EmailNotificationStatsDto
        {
            TotalPending = stats.FirstOrDefault(s => s.Status == "Pending")?.Count ?? 0,
            TotalSent = stats.FirstOrDefault(s => s.Status == "Sent")?.Count ?? 0,
            TotalFailed = stats.FirstOrDefault(s => s.Status == "Failed")?.Count ?? 0
        };
    }

    public async Task<int> GetCountAsync(EmailNotificationFilterDto? filter)
    {
        var query = _context.EmailNotifications.AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.CreatedAt <= filter.EndDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<bool> ValidateNotificationAsync(CreateEmailNotificationDto notification)
    {
        // Basic validation
        if (string.IsNullOrEmpty(notification.ToEmail) || string.IsNullOrEmpty(notification.Subject))
            return false;

        // Email format validation
        try
        {
            var addr = new System.Net.Mail.MailAddress(notification.ToEmail);
            return addr.Address == notification.ToEmail;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ValidateRecipientsAsync(IEnumerable<string> emailAddresses)
    {
        var validEmails = new List<string>();

        foreach (var email in emailAddresses)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address == email)
                    validEmails.Add(email);
            }
            catch
            {
                // Invalid email, skip
            }
        }

        return validEmails;
    }

    public async Task<int> CleanupOldNotificationsAsync(int olderThanDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var oldNotifications = await _context.EmailNotifications
            .Where(e => e.CreatedAt < cutoffDate && (e.Status == "Sent" || e.Status == "Delivered"))
            .ToListAsync();

        _context.EmailNotifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old email notifications", oldNotifications.Count);
        
        return oldNotifications.Count;
    }

    public async Task<int> CleanupFailedNotificationsAsync(int failedOlderThanDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-failedOlderThanDays);
        
        var failedNotifications = await _context.EmailNotifications
            .Where(e => e.CreatedAt < cutoffDate && e.Status == "Failed" && e.RetryCount >= e.MaxRetries)
            .ToListAsync();

        _context.EmailNotifications.RemoveRange(failedNotifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} failed email notifications", failedNotifications.Count);
        
        return failedNotifications.Count;
    }
}