using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public interface IConsentService
{
    Task<ConsentRecord> CreateConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose, 
        string consentText, string ipAddress, string userAgent, DateTime? expiryDate = null);
    
    Task<ConsentRecord?> GetConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose);
    
    Task<List<ConsentRecord>> GetUserConsentsAsync(string userId);
    
    Task<ConsentSummary> GetConsentSummaryAsync(string userId);
    
    Task<bool> HasValidConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose);
    
    Task<ConsentRecord> WithdrawConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose, string reason);
    
    Task<ConsentRecord> UpdateConsentAsync(string consentId, ConsentStatus status, string? reason = null);
    
    Task<List<ConsentRecord>> GetExpiringConsentsAsync(int daysBeforeExpiry = 30);
    
    Task<List<ConsentRecord>> GetExpiredConsentsAsync();
    
    Task<int> CleanupExpiredConsentsAsync();
    
    Task<bool> ValidateConsentRequirementsAsync(object entity);
    
    Task<Dictionary<PersonalDataCategory, bool>> GetConsentStatusByUserAsync(string userId);
    
    Task<List<ConsentRecord>> GetConsentHistoryAsync(string userId, PersonalDataCategory? dataCategory = null);
    
    Task<ConsentRecord> RenewConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose, 
        string newConsentText, string ipAddress, string userAgent);
}