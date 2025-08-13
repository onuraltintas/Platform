using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EgitimPlatform.Services.NotificationService.Models.DTOs;

namespace EgitimPlatform.Services.NotificationService.Services
{
    public interface IEmailNotificationService
    {
        Task<IEnumerable<EmailNotificationDto>> GetFilteredAsync(EmailNotificationFilterDto filter);
        Task<EmailNotificationDto> GetByIdAsync(string id);
        Task<IEnumerable<EmailNotificationDto>> GetByUserIdAsync(string userId);
        Task<EmailNotificationDto> CreateAsync(CreateEmailNotificationDto createDto);
        Task<IEnumerable<EmailNotificationDto>> CreateBulkAsync(BulkEmailNotificationDto bulkDto);
        Task<EmailNotificationDto> CreateFromTemplateAsync(string templateName, string? userId, string toEmail, Dictionary<string, string> templateData, string? subject, DateTime? scheduledAt);
        Task<IEnumerable<EmailNotificationDto>> CreateBulkFromTemplateAsync(string templateName, List<string>? userIds, List<string>? emailAddresses, Dictionary<string, string> templateData, string? subject, DateTime? scheduledAt);
        Task<EmailNotificationDto> UpdateAsync(string id, UpdateEmailNotificationDto updateDto);
        Task<bool> DeleteAsync(string id);
        Task<bool> SendAsync(string id);
        Task<int> SendBulkAsync(IEnumerable<string> ids);
        Task<int> SendPendingNotificationsAsync();
        Task<int> SendScheduledNotificationsAsync();
        Task<int> RetryFailedNotificationsAsync();
        Task<int> MarkAsReadAsync(string userId, List<string> notificationIds);
        Task<EmailNotificationStatsDto> GetStatsAsync(DateTime? fromDate, DateTime? toDate, string? userId);
        Task<int> GetCountAsync(EmailNotificationFilterDto? filter);
        Task<bool> ValidateNotificationAsync(CreateEmailNotificationDto notification);
        Task<List<string>> ValidateRecipientsAsync(IEnumerable<string> emailAddresses);
        Task<int> CleanupOldNotificationsAsync(int olderThanDays);
        Task<int> CleanupFailedNotificationsAsync(int failedOlderThanDays);
    }
}
