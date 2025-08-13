using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public class DataSubjectRightsService : IDataSubjectRightsService
{
    private readonly ILogger<DataSubjectRightsService> _logger;
    private readonly PrivacyOptions _options;
    private readonly List<DataSubjectRequest> _requests; // In-memory storage for demo

    public DataSubjectRightsService(ILogger<DataSubjectRightsService> logger, IOptions<PrivacyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _requests = new List<DataSubjectRequest>();
    }

    public async Task<DataSubjectRequest> CreateRequestAsync(string userId, string userEmail, 
        DataSubjectRightType requestType, string description, List<PersonalDataCategory> affectedCategories, 
        string ipAddress, string userAgent)
    {
        // Check if user can make this type of request
        var canMakeRequest = await CanUserMakeRequestAsync(userId, requestType);
        if (!canMakeRequest)
        {
            throw new InvalidOperationException($"User has exceeded the maximum number of {requestType} requests allowed");
        }

        var request = new DataSubjectRequest
        {
            UserId = userId,
            UserEmail = userEmail,
            RequestType = requestType,
            Description = description,
            AffectedDataCategories = affectedCategories,
            RequestorIpAddress = ipAddress,
            RequestorUserAgent = userAgent,
            DueDate = DateTime.UtcNow.AddDays(_options.DataSubjectRights.RequestProcessingTimeoutDays)
        };

        _requests.Add(request);

        _logger.LogInformation("Data subject request created: {RequestId} for user {UserId} - Type: {RequestType}", 
            request.Id, userId, requestType);

        // Send notification
        await SendRequestNotificationAsync(request, "created");

        return request;
    }

    public async Task<DataSubjectRequest?> GetRequestAsync(string requestId)
    {
        var request = _requests.FirstOrDefault(r => r.Id == requestId);
        return await Task.FromResult(request);
    }

    public async Task<List<DataSubjectRequest>> GetUserRequestsAsync(string userId)
    {
        var userRequests = _requests.Where(r => r.UserId == userId).OrderByDescending(r => r.RequestDate).ToList();
        return await Task.FromResult(userRequests);
    }

    public async Task<List<DataSubjectRequest>> GetPendingRequestsAsync()
    {
        var pendingRequests = _requests
            .Where(r => r.Status == DataSubjectRequestStatus.Pending || r.Status == DataSubjectRequestStatus.InProgress)
            .OrderBy(r => r.DueDate)
            .ToList();
        
        return await Task.FromResult(pendingRequests);
    }

    public async Task<List<DataSubjectRequest>> GetOverdueRequestsAsync()
    {
        var overdueRequests = _requests
            .Where(r => r.Status != DataSubjectRequestStatus.Completed && 
                       r.Status != DataSubjectRequestStatus.Rejected && 
                       r.DueDate < DateTime.UtcNow)
            .ToList();
        
        return await Task.FromResult(overdueRequests);
    }

    public async Task<DataSubjectRequest> UpdateRequestStatusAsync(string requestId, DataSubjectRequestStatus status, 
        string? notes = null, string? updatedBy = null)
    {
        var request = await GetRequestAsync(requestId);
        if (request == null)
            throw new InvalidOperationException("Request not found");

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = updatedBy ?? string.Empty;

        if (!string.IsNullOrEmpty(notes))
        {
            request.ProcessingNotes += $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {notes}";
        }

        if (status == DataSubjectRequestStatus.Completed)
        {
            request.CompletionDate = DateTime.UtcNow;
        }

        _logger.LogInformation("Request {RequestId} status updated to {Status} by {UpdatedBy}", 
            requestId, status, updatedBy);

        await SendRequestNotificationAsync(request, "status_updated");

        return request;
    }

    public async Task<DataSubjectRequest> AssignRequestAsync(string requestId, string assignedTo)
    {
        var request = await GetRequestAsync(requestId);
        if (request == null)
            throw new InvalidOperationException("Request not found");

        request.AssignedTo = assignedTo;
        request.Status = DataSubjectRequestStatus.InProgress;
        request.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Request {RequestId} assigned to {AssignedTo}", requestId, assignedTo);

        return request;
    }

    public async Task<DataSubjectRequest> CompleteRequestAsync(string requestId, string completionNotes, 
        List<string>? attachmentUrls = null)
    {
        var request = await UpdateRequestStatusAsync(requestId, DataSubjectRequestStatus.Completed, completionNotes);
        
        request.CompletionNotes = completionNotes;
        request.CompletionDate = DateTime.UtcNow;

        if (attachmentUrls != null)
        {
            request.AttachmentUrls.AddRange(attachmentUrls);
        }

        await SendRequestNotificationAsync(request, "completed");

        return request;
    }

    public async Task<DataSubjectRequest> RejectRequestAsync(string requestId, string rejectionReason)
    {
        var request = await UpdateRequestStatusAsync(requestId, DataSubjectRequestStatus.Rejected, rejectionReason);
        
        request.CompletionNotes = rejectionReason;
        request.CompletionDate = DateTime.UtcNow;

        await SendRequestNotificationAsync(request, "rejected");

        return request;
    }

    public async Task<Dictionary<string, object>> ProcessAccessRequestAsync(string userId)
    {
        // This would integrate with your data services to collect all user data
        var userData = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["requestDate"] = DateTime.UtcNow,
            ["personalData"] = new Dictionary<string, object>
            {
                ["profile"] = "User profile data would be here",
                ["preferences"] = "User preferences data would be here",
                ["activity"] = "User activity data would be here"
            },
            ["consentHistory"] = "Consent history would be here",
            ["dataProcessingActivities"] = "Processing activities would be here"
        };

        _logger.LogInformation("Access request processed for user {UserId}", userId);

        return await Task.FromResult(userData);
    }

    public async Task<bool> ProcessErasureRequestAsync(string userId, List<PersonalDataCategory> categories)
    {
        // This would integrate with your data services to delete user data
        foreach (var category in categories)
        {
            _logger.LogInformation("Erasing data category {Category} for user {UserId}", category, userId);
            // Actual deletion logic would go here
        }

        _logger.LogInformation("Erasure request processed for user {UserId} - Categories: {Categories}", 
            userId, string.Join(", ", categories));

        return await Task.FromResult(true);
    }

    public async Task<Dictionary<string, object>> ProcessPortabilityRequestAsync(string userId, 
        List<PersonalDataCategory> categories)
    {
        // This would extract user data in a portable format
        var portableData = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["exportDate"] = DateTime.UtcNow,
            ["format"] = "JSON",
            ["categories"] = categories,
            ["data"] = new Dictionary<string, object>()
        };

        foreach (var category in categories)
        {
            portableData["data"] = $"Portable data for {category} would be here";
        }

        _logger.LogInformation("Portability request processed for user {UserId} - Categories: {Categories}", 
            userId, string.Join(", ", categories));

        return await Task.FromResult(portableData);
    }

    public async Task<bool> ProcessRectificationRequestAsync(string userId, Dictionary<string, object> corrections)
    {
        // This would update user data with the corrections
        foreach (var correction in corrections)
        {
            _logger.LogInformation("Correcting field {Field} for user {UserId}", correction.Key, userId);
            // Actual correction logic would go here
        }

        _logger.LogInformation("Rectification request processed for user {UserId} - {Count} corrections", 
            userId, corrections.Count);

        return await Task.FromResult(true);
    }

    public async Task<bool> CanUserMakeRequestAsync(string userId, DataSubjectRightType requestType)
    {
        var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day);
        var requestCount = await GetUserRequestCountAsync(userId, currentMonth);

        var maxRequests = _options.DataSubjectRights.MaxRequestsPerUserPerMonth;
        return requestCount < maxRequests;
    }

    public async Task<List<DataSubjectRequestSummary>> GetRequestSummariesAsync(DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _requests.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(r => r.RequestDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.RequestDate <= toDate.Value);

        var summaries = query.Select(r => new DataSubjectRequestSummary
        {
            Id = r.Id,
            UserId = r.UserId,
            RequestType = r.RequestType,
            Status = r.Status,
            RequestDate = r.RequestDate,
            DueDate = r.DueDate
        }).ToList();

        return await Task.FromResult(summaries);
    }

    public async Task<int> GetUserRequestCountAsync(string userId, DateTime? fromDate = null)
    {
        var query = _requests.Where(r => r.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(r => r.RequestDate >= fromDate.Value);

        var count = query.Count();
        return await Task.FromResult(count);
    }

    public async Task SendRequestNotificationAsync(DataSubjectRequest request, string notificationType)
    {
        // This would integrate with your notification system
        _logger.LogInformation("Sending {NotificationType} notification for request {RequestId}", 
            notificationType, request.Id);

        // Send email to configured notification addresses
        foreach (var email in _options.DataSubjectRights.NotificationEmails)
        {
            _logger.LogInformation("Notification sent to {Email} for request {RequestId}", email, request.Id);
        }

        await Task.CompletedTask;
    }
}