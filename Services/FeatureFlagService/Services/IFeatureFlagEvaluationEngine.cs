using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;

namespace EgitimPlatform.Services.FeatureFlagService.Services;

public interface IFeatureFlagEvaluationEngine
{
    Task<FeatureFlagEvaluationResponse> EvaluateAsync(FeatureFlagResponse featureFlag, string userId, Dictionary<string, object> context);
    Task<string> DetermineVariationAsync(FeatureFlagResponse featureFlag, string userId, Dictionary<string, object> context);
    Task<object> GetVariationValueAsync(FeatureFlagResponse featureFlag, string variation);
    Task<bool> EvaluateConditionsAsync(Dictionary<string, object> conditions, string userId, Dictionary<string, object> context);
    Task<bool> IsUserInAudienceAsync(List<string> targetAudiences, List<string> excludedAudiences, string userId, Dictionary<string, object> context);
    Task<bool> IsWithinRolloutPercentageAsync(int rolloutPercentage, string userId, string featureFlagKey);
}