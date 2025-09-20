using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Interfaces;

public interface IDataRetentionService
{
    Task<DataRetentionPolicy> CreateRetentionPolicyAsync(DataRetentionPolicy policy, 
        CancellationToken cancellationToken = default);
    
    Task<DataRetentionPolicy> UpdateRetentionPolicyAsync(DataRetentionPolicy policy, 
        CancellationToken cancellationToken = default);
    
    Task<bool> DeleteRetentionPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    
    Task<DataRetentionPolicy[]> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default);
    
    Task<DataRetentionPolicy?> GetRetentionPolicyAsync(DataCategory category, string? dataType = null, 
        CancellationToken cancellationToken = default);
    
    Task<int> ApplyRetentionPoliciesAsync(CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord[]> GetDataForDeletionAsync(CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord[]> GetDataForArchivalAsync(CancellationToken cancellationToken = default);
    
    Task<int> ArchiveExpiredDataAsync(CancellationToken cancellationToken = default);
    
    Task<int> DeleteExpiredDataAsync(bool forceDelete = false, CancellationToken cancellationToken = default);
    
    Task<bool> ExtendRetentionPeriodAsync(string dataRecordId, int additionalDays, 
        string reason, CancellationToken cancellationToken = default);
    
    Task SetRetentionExpiryAsync(string userId, DataCategory category, DateTime expiryDate, 
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<DataCategory, DateTime?>> GetUserDataExpiryDatesAsync(string userId, 
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, object>> GetRetentionStatisticsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ValidateRetentionComplianceAsync(CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord> AddDataRecordAsync(PersonalDataRecord record, CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord?> GetDataRecordAsync(string recordId, CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord[]> GetUserDataRecordsAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteDataRecordAsync(string recordId, CancellationToken cancellationToken = default);
    
    Task<int> DeleteUserDataAsync(string userId, DataCategory[]? categories = null, CancellationToken cancellationToken = default);
    
    Task<bool> ExtendRetentionAsync(string recordId, int additionalDays, string reason, CancellationToken cancellationToken = default);
    
    Task<Dictionary<DataCategory, RetentionSummary>> GetRetentionSummaryAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ValidateRetentionComplianceAsync(string userId, CancellationToken cancellationToken = default);
}