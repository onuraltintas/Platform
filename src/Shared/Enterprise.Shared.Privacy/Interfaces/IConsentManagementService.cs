using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Interfaces;

public interface IConsentManagementService
{
    Task<ConsentRecord> GrantConsentAsync(ConsentRequest request, CancellationToken cancellationToken = default);
    
    Task<ConsentRecord[]> GrantMultipleConsentsAsync(ConsentRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<bool> WithdrawConsentAsync(ConsentWithdrawalRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<ConsentRecord?> GetConsentAsync(string userId, ConsentPurpose purpose, 
        CancellationToken cancellationToken = default);
    
    Task<ConsentRecord[]> GetUserConsentsAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<ConsentSummary> GetConsentSummaryAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<bool> HasValidConsentAsync(string userId, ConsentPurpose purpose, 
        CancellationToken cancellationToken = default);
    
    Task<bool> HasValidConsentAsync(string userId, ConsentPurpose[] purposes, 
        CancellationToken cancellationToken = default);
    
    Task<ConsentRecord> UpdateConsentAsync(string consentId, ConsentStatus newStatus, 
        string? reason = null, CancellationToken cancellationToken = default);
    
    Task<ConsentHistory[]> GetConsentHistoryAsync(string userId, CancellationToken cancellationToken = default);
    
    Task<int> ProcessExpiredConsentsAsync(CancellationToken cancellationToken = default);
    
    Task<ConsentRecord[]> GetExpiringConsentsAsync(int daysFromNow, 
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateConsentRequirementsAsync(string userId, ConsentPurpose[] purposes, 
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, object>> GetConsentStatisticsAsync(CancellationToken cancellationToken = default);
}