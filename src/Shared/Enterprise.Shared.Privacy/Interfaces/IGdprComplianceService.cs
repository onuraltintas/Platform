using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Interfaces;

public interface IGdprComplianceService
{
    Task<DataExportRequest> ProcessDataExportRequestAsync(string userId, DataCategory[]? categories = null, 
        CancellationToken cancellationToken = default);
    
    Task<DataDeletionRequest> ProcessDataDeletionRequestAsync(string userId, 
        DataSubjectRight requestType, DataCategory[]? categories = null, 
        CancellationToken cancellationToken = default);
    
    Task<DataExportRequest> GetDataExportStatusAsync(string requestId, 
        CancellationToken cancellationToken = default);
    
    Task<DataDeletionRequest> GetDataDeletionStatusAsync(string requestId, 
        CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord[]> GetUserPersonalDataAsync(string userId, 
        CancellationToken cancellationToken = default);
    
    Task<bool> UpdateUserDataAsync(string userId, Dictionary<string, string> updates, 
        CancellationToken cancellationToken = default);
    
    Task<bool> RestrictDataProcessingAsync(string userId, DataCategory[] categories, 
        CancellationToken cancellationToken = default);
    
    Task<bool> ObjectToDataProcessingAsync(string userId, ConsentPurpose[] purposes, 
        CancellationToken cancellationToken = default);
    
    Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, 
        string reportType = "GDPR", CancellationToken cancellationToken = default);
    
    Task<bool> ValidateDataProcessingLegalityAsync(string userId, ConsentPurpose purpose, 
        CancellationToken cancellationToken = default);
    
    Task<ComplianceIssue[]> IdentifyComplianceIssuesAsync(CancellationToken cancellationToken = default);
    
    Task<bool> NotifyDataBreachAsync(string description, DataCategory[] affectedCategories, 
        string[] affectedUserIds, CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, object>> GetComplianceMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> VerifyConsentLegalBasisAsync(string userId, ConsentPurpose purpose, 
        CancellationToken cancellationToken = default);
}