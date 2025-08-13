using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public interface IPersonalDataInventoryService
{
    Task<PersonalDataInventory> RecordPersonalDataAsync(string userId, PersonalDataCategory dataCategory, 
        string dataField, string storageLocation, DataProcessingLawfulBasis lawfulBasis, 
        string processingPurpose, string processingSystem, int retentionPeriodDays = 365);
    
    Task<List<PersonalDataInventory>> GetUserPersonalDataAsync(string userId);
    
    Task<PersonalDataSummary> GetPersonalDataSummaryAsync(string userId);
    
    Task<List<PersonalDataInventory>> GetPersonalDataByCategory(string userId, PersonalDataCategory category);
    
    Task<bool> UpdatePersonalDataAsync(string inventoryId, Dictionary<string, object> updates);
    
    Task<bool> DeletePersonalDataAsync(string inventoryId);
    
    Task<bool> MarkForDeletionAsync(string userId, List<PersonalDataCategory> categories, DateTime deletionDate);
    
    Task<List<PersonalDataInventory>> GetDataScheduledForDeletionAsync(DateTime? beforeDate = null);
    
    Task<int> ProcessScheduledDeletionsAsync();
    
    Task<bool> UpdateLastAccessedAsync(string inventoryId);
    
    Task<List<PersonalDataInventory>> GetStaleDataAsync(int daysSinceLastAccess = 365);
    
    Task<List<PersonalDataInventory>> GetDataByRetentionReasonAsync(DataRetentionReason reason);
    
    Task<Dictionary<string, int>> GetDataCountByStorageLocationAsync();
    
    Task<Dictionary<string, int>> GetDataCountByProcessingSystemAsync();
    
    Task<bool> AnonymizePersonalDataAsync(string inventoryId);
    
    Task<bool> PseudonymizePersonalDataAsync(string inventoryId, string pseudonymizationKey);
}