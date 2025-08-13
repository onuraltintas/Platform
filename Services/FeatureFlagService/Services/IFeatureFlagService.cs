using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;

namespace EgitimPlatform.Services.FeatureFlagService.Services;

public interface IFeatureFlagService
{
    // Feature Flag Management
    Task<FeatureFlagResponse> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, string createdBy);
    Task<FeatureFlagResponse> UpdateFeatureFlagAsync(string id, UpdateFeatureFlagRequest request, string updatedBy);
    Task<FeatureFlagResponse?> GetFeatureFlagAsync(string id);
    Task<FeatureFlagResponse?> GetFeatureFlagByKeyAsync(string key, string environment, string applicationId);
    Task<PagedFeatureFlagResponse> GetFeatureFlagsAsync(FeatureFlagListRequest request);
    Task<bool> DeleteFeatureFlagAsync(string id);
    
    // Feature Flag Evaluation
    Task<FeatureFlagEvaluationResponse> EvaluateFeatureFlagAsync(EvaluateFeatureFlagRequest request);
    Task<BatchEvaluationResponse> BatchEvaluateAsync(BatchEvaluateRequest request);
    Task<bool> IsFeatureEnabledAsync(string userId, string featureFlagKey, Dictionary<string, object>? context = null, string environment = "production", string applicationId = "");
    Task<T> GetFeatureValueAsync<T>(string userId, string featureFlagKey, T defaultValue, Dictionary<string, object>? context = null, string environment = "production", string applicationId = "");
    
    // Assignment Management
    Task<FeatureFlagAssignmentResponse> AssignUserToVariationAsync(string featureFlagId, string userId, string variation, string reason, Dictionary<string, object>? context = null);
    Task<FeatureFlagAssignmentResponse?> GetUserAssignmentAsync(string userId, string featureFlagId);
    Task<List<FeatureFlagAssignmentResponse>> GetUserAssignmentsAsync(string userId);
    Task<bool> RemoveUserAssignmentAsync(string userId, string featureFlagId);
    
    // Event Logging
    Task LogEventAsync(LogEventRequest request);
    Task LogExposureAsync(string featureFlagId, string userId, string variation, Dictionary<string, object>? properties = null);
    Task LogConversionAsync(string featureFlagId, string userId, string eventName, Dictionary<string, object>? properties = null);
    
    // Statistics and Analytics
    Task<FeatureFlagStatsResponse> GetFeatureFlagStatsAsync(string featureFlagId, TimeSpan period);
    Task<FeatureFlagUsageResponse> GetFeatureFlagUsageAsync(string featureFlagId, DateTime from, DateTime to);
    Task<Dictionary<string, int>> GetVariationDistributionAsync(string featureFlagId);
    
    // Bulk Operations
    Task<List<FeatureFlagResponse>> BulkUpdateStatusAsync(List<string> featureFlagIds, FeatureFlagStatus status, string updatedBy);
    Task<List<FeatureFlagResponse>> BulkToggleAsync(List<string> featureFlagIds, bool isEnabled, string updatedBy);
    
    // Health and Monitoring
    Task<bool> IsHealthyAsync();
    Task<Dictionary<string, object>> GetServiceStatusAsync();
}