using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public interface IDataSubjectRightsService
{
    Task<DataSubjectRequest> CreateRequestAsync(string userId, string userEmail, DataSubjectRightType requestType, 
        string description, List<PersonalDataCategory> affectedCategories, string ipAddress, string userAgent);
    
    Task<DataSubjectRequest?> GetRequestAsync(string requestId);
    
    Task<List<DataSubjectRequest>> GetUserRequestsAsync(string userId);
    
    Task<List<DataSubjectRequest>> GetPendingRequestsAsync();
    
    Task<List<DataSubjectRequest>> GetOverdueRequestsAsync();
    
    Task<DataSubjectRequest> UpdateRequestStatusAsync(string requestId, DataSubjectRequestStatus status, 
        string? notes = null, string? updatedBy = null);
    
    Task<DataSubjectRequest> AssignRequestAsync(string requestId, string assignedTo);
    
    Task<DataSubjectRequest> CompleteRequestAsync(string requestId, string completionNotes, 
        List<string>? attachmentUrls = null);
    
    Task<DataSubjectRequest> RejectRequestAsync(string requestId, string rejectionReason);
    
    Task<Dictionary<string, object>> ProcessAccessRequestAsync(string userId);
    
    Task<bool> ProcessErasureRequestAsync(string userId, List<PersonalDataCategory> categories);
    
    Task<Dictionary<string, object>> ProcessPortabilityRequestAsync(string userId, List<PersonalDataCategory> categories);
    
    Task<bool> ProcessRectificationRequestAsync(string userId, Dictionary<string, object> corrections);
    
    Task<bool> CanUserMakeRequestAsync(string userId, DataSubjectRightType requestType);
    
    Task<List<DataSubjectRequestSummary>> GetRequestSummariesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    Task<int> GetUserRequestCountAsync(string userId, DateTime? fromDate = null);
    
    Task SendRequestNotificationAsync(DataSubjectRequest request, string notificationType);
}