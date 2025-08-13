using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public interface IDataProcessingActivityService
{
    Task<DataProcessingActivity> CreateActivityAsync(string name, string description, string controller,
        DataProcessingLawfulBasis lawfulBasis, List<PersonalDataCategory> dataCategories,
        List<string> processingPurposes, List<string> dataSubjectCategories);
    
    Task<DataProcessingActivity?> GetActivityAsync(string activityId);
    
    Task<List<DataProcessingActivity>> GetAllActivitiesAsync();
    
    Task<List<ProcessingActivitySummary>> GetActivitySummariesAsync();
    
    Task<DataProcessingActivity> UpdateActivityAsync(string activityId, Dictionary<string, object> updates);
    
    Task<bool> DeactivateActivityAsync(string activityId);
    
    Task<bool> ActivateActivityAsync(string activityId);
    
    Task<List<DataProcessingActivity>> GetActivitiesByLawfulBasisAsync(DataProcessingLawfulBasis lawfulBasis);
    
    Task<List<DataProcessingActivity>> GetActivitiesByDataCategoryAsync(PersonalDataCategory dataCategory);
    
    Task<List<DataProcessingActivity>> GetActivitiesRequiringDPIAAsync();
    
    Task<DataProcessingActivity> AddThirdCountryTransferAsync(string activityId, string country, string safeguards);
    
    Task<DataProcessingActivity> RemoveThirdCountryTransferAsync(string activityId, string country);
    
    Task<DataProcessingActivity> UpdateRetentionPeriodAsync(string activityId, PersonalDataCategory dataCategory, int retentionDays);
    
    Task<DataProcessingActivity> AddRecipientAsync(string activityId, string recipient);
    
    Task<DataProcessingActivity> RemoveRecipientAsync(string activityId, string recipient);
    
    Task<Dictionary<DataProcessingLawfulBasis, int>> GetActivitiesCountByLawfulBasisAsync();
    
    Task<Dictionary<PersonalDataCategory, int>> GetActivitiesCountByDataCategoryAsync();
    
    Task<bool> ValidateActivityComplianceAsync(string activityId);
}