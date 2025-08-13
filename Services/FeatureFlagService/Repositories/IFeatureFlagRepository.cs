using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;

namespace EgitimPlatform.Services.FeatureFlagService.Repositories;

public interface IFeatureFlagRepository
{
    Task<FeatureFlag?> GetByIdAsync(string id);
    Task<FeatureFlag?> GetByKeyAsync(string key, string environment, string applicationId);
    Task<List<FeatureFlag>> GetAllAsync();
    Task<(List<FeatureFlag> items, int totalCount)> GetPagedAsync(FeatureFlagListRequest request);
    Task<FeatureFlag> CreateAsync(FeatureFlag featureFlag);
    Task<FeatureFlag> UpdateAsync(FeatureFlag featureFlag);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string key, string environment, string applicationId);
    Task<List<FeatureFlag>> GetByEnvironmentAsync(string environment);
    Task<List<FeatureFlag>> GetByApplicationIdAsync(string applicationId);
    Task<List<FeatureFlag>> GetByStatusAsync(FeatureFlagStatus status);
    Task<List<FeatureFlag>> GetByTagsAsync(List<string> tags);
    Task<List<FeatureFlag>> GetExpiredAsync();
    Task<List<FeatureFlag>> GetExpiringAsync(int daysFromNow);
}