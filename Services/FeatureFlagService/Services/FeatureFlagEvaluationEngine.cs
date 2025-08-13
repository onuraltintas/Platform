using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EgitimPlatform.Services.FeatureFlagService.Services;

public class FeatureFlagEvaluationEngine : IFeatureFlagEvaluationEngine
{
    private readonly ILogger<FeatureFlagEvaluationEngine> _logger;

    public FeatureFlagEvaluationEngine(ILogger<FeatureFlagEvaluationEngine> logger)
    {
        _logger = logger;
    }

    public async Task<FeatureFlagEvaluationResponse> EvaluateAsync(FeatureFlagResponse featureFlag, string userId, Dictionary<string, object> context)
    {
        try
        {
            // Check if feature flag is enabled
            if (!featureFlag.IsEnabled)
            {
                return CreateDisabledResponse(featureFlag, context, "Feature flag is disabled");
            }

            // Check if feature flag is active
            if (featureFlag.Status != FeatureFlagStatus.Active)
            {
                return CreateDisabledResponse(featureFlag, context, $"Feature flag status is {featureFlag.Status}");
            }

            // Check date boundaries
            var now = DateTime.UtcNow;
            if (featureFlag.StartDate.HasValue && now < featureFlag.StartDate.Value)
            {
                return CreateDisabledResponse(featureFlag, context, "Feature flag not yet started");
            }

            if (featureFlag.EndDate.HasValue && now > featureFlag.EndDate.Value)
            {
                return CreateDisabledResponse(featureFlag, context, "Feature flag has ended");
            }

            // Check audience targeting
            if (!await IsUserInAudienceAsync(featureFlag.TargetAudiences, featureFlag.ExcludedAudiences, userId, context))
            {
                return CreateDisabledResponse(featureFlag, context, "User not in target audience");
            }

            // Evaluate conditions
            if (featureFlag.Conditions.Any() && !await EvaluateConditionsAsync(featureFlag.Conditions, userId, context))
            {
                return CreateDisabledResponse(featureFlag, context, "Conditions not met");
            }

            // Check rollout percentage
            if (!await IsWithinRolloutPercentageAsync(featureFlag.RolloutPercentage, userId, featureFlag.Key))
            {
                return CreateDisabledResponse(featureFlag, context, "User not in rollout percentage");
            }

            // Determine variation
            var variation = await DetermineVariationAsync(featureFlag, userId, context);
            var value = await GetVariationValueAsync(featureFlag, variation);

            return new FeatureFlagEvaluationResponse(
                featureFlag.Key,
                true,
                variation,
                value,
                "Feature flag enabled for user",
                context
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag {Key} for user {UserId}", featureFlag.Key, userId);
            return CreateDisabledResponse(featureFlag, context, "Evaluation error");
        }
    }

    public async Task<string> DetermineVariationAsync(FeatureFlagResponse featureFlag, string userId, Dictionary<string, object> context)
    {
        // For rollout flags, use consistent hashing
        if (featureFlag.Type == FeatureFlagType.Rollout && featureFlag.Variations.Any())
        {
            return await DetermineRolloutVariationAsync(featureFlag, userId);
        }

        // For rollout features, determine based on rollout strategy
        if (featureFlag.Type == FeatureFlagType.Rollout && featureFlag.Variations.Any())
        {
            return await DetermineRolloutVariationAsync(featureFlag, userId, context);
        }

        // Default to the configured default variation
        return featureFlag.DefaultVariation;
    }

    public async Task<object> GetVariationValueAsync(FeatureFlagResponse featureFlag, string variation)
    {
        if (featureFlag.Variations.TryGetValue(variation, out var value))
        {
            return value;
        }

        // Return type-specific default values
        return featureFlag.Type switch
        {
            FeatureFlagType.Boolean => variation == "enabled" || variation == "true",
            FeatureFlagType.String => variation,
            FeatureFlagType.Number => TryParseNumber(variation),
            FeatureFlagType.Json => TryParseJson(variation),
            _ => variation == "enabled" || variation == "true"
        };
    }

    public async Task<bool> EvaluateConditionsAsync(Dictionary<string, object> conditions, string userId, Dictionary<string, object> context)
    {
        foreach (var condition in conditions)
        {
            if (!await EvaluateSingleConditionAsync(condition.Key, condition.Value, userId, context))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> IsUserInAudienceAsync(List<string> targetAudiences, List<string> excludedAudiences, string userId, Dictionary<string, object> context)
    {
        // If no target audiences are specified, everyone is included
        if (!targetAudiences.Any())
        {
            // Check exclusions
            return !await IsUserInExcludedAudienceAsync(excludedAudiences, userId, context);
        }

        // Check if user is in target audience
        var inTargetAudience = await IsUserInTargetAudienceAsync(targetAudiences, userId, context);
        if (!inTargetAudience)
        {
            return false;
        }

        // Check if user is in excluded audience
        var inExcludedAudience = await IsUserInExcludedAudienceAsync(excludedAudiences, userId, context);
        return !inExcludedAudience;
    }

    public async Task<bool> IsWithinRolloutPercentageAsync(int rolloutPercentage, string userId, string featureFlagKey)
    {
        if (rolloutPercentage >= 100)
        {
            return true;
        }

        if (rolloutPercentage <= 0)
        {
            return false;
        }

        // Use consistent hashing to determine if user is in rollout
        var hash = ComputeHash($"{userId}:{featureFlagKey}");
        var userPercentile = (hash % 100) + 1;

        return userPercentile <= rolloutPercentage;
    }

    // Private helper methods
    private static FeatureFlagEvaluationResponse CreateDisabledResponse(FeatureFlagResponse featureFlag, Dictionary<string, object> context, string reason)
    {
        return new FeatureFlagEvaluationResponse(
            featureFlag.Key,
            false,
            featureFlag.DefaultVariation,
            GetDefaultValue(featureFlag.Type),
            reason,
            context
        );
    }

    private static object GetDefaultValue(FeatureFlagType type)
    {
        return type switch
        {
            FeatureFlagType.Boolean => false,
            FeatureFlagType.String => "",
            FeatureFlagType.Number => 0,
            FeatureFlagType.Json => new Dictionary<string, object>(),
            _ => false
        };
    }

    private async Task<string> DetermineRolloutVariationAsync(FeatureFlagResponse featureFlag, string userId)
    {
        var variations = featureFlag.Variations.Keys.ToList();
        if (!variations.Any())
        {
            return featureFlag.DefaultVariation;
        }

        // Use consistent hashing to assign user to variation
        var hash = ComputeHash($"{userId}:{featureFlag.Key}:abtest");
        var variationIndex = hash % variations.Count;

        return variations[variationIndex];
    }

    private async Task<string> DetermineRolloutVariationAsync(FeatureFlagResponse featureFlag, string userId, Dictionary<string, object> context)
    {
        // For rollout, use the default variation or "enabled"
        return featureFlag.DefaultVariation != "control" ? featureFlag.DefaultVariation : "enabled";
    }

    private async Task<bool> EvaluateSingleConditionAsync(string conditionKey, object conditionValue, string userId, Dictionary<string, object> context)
    {
        try
        {
            // Get the value from context
            if (!context.TryGetValue(conditionKey, out var contextValue))
            {
                return false;
            }

            // Handle different condition types
            if (conditionValue is JsonElement jsonElement)
            {
                return await EvaluateJsonConditionAsync(jsonElement, contextValue);
            }

            // Simple equality check
            return contextValue?.ToString() == conditionValue?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating condition {Key}", conditionKey);
            return false;
        }
    }

    private async Task<bool> EvaluateJsonConditionAsync(JsonElement condition, object contextValue)
    {
        // Handle complex condition objects (operators, arrays, etc.)
        if (condition.ValueKind == JsonValueKind.Object)
        {
            // Support for operators like { "in": ["value1", "value2"] }
            if (condition.TryGetProperty("in", out var inElement) && inElement.ValueKind == JsonValueKind.Array)
            {
                var allowedValues = inElement.EnumerateArray().Select(e => e.GetString()).ToList();
                return allowedValues.Contains(contextValue?.ToString());
            }

            // Support for range operators like { "gte": 18, "lt": 65 }
            if (condition.TryGetProperty("gte", out var gteElement) && double.TryParse(contextValue?.ToString(), out var numValue))
            {
                var minValue = gteElement.GetDouble();
                if (numValue < minValue) return false;

                if (condition.TryGetProperty("lt", out var ltElement))
                {
                    var maxValue = ltElement.GetDouble();
                    return numValue < maxValue;
                }

                return true;
            }
        }

        // Default to string comparison
        return condition.GetString() == contextValue?.ToString();
    }

    private async Task<bool> IsUserInTargetAudienceAsync(List<string> targetAudiences, string userId, Dictionary<string, object> context)
    {
        foreach (var audience in targetAudiences)
        {
            if (await EvaluateAudienceAsync(audience, userId, context))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsUserInExcludedAudienceAsync(List<string> excludedAudiences, string userId, Dictionary<string, object> context)
    {
        foreach (var audience in excludedAudiences)
        {
            if (await EvaluateAudienceAsync(audience, userId, context))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> EvaluateAudienceAsync(string audience, string userId, Dictionary<string, object> context)
    {
        // Built-in audience types
        return audience switch
        {
            "all" => true,
            "premium_users" => context.ContainsKey("is_premium") && context["is_premium"].ToString() == "true",
            "beta_users" => context.ContainsKey("is_beta") && context["is_beta"].ToString() == "true",
            "new_users" => IsNewUser(context),
            "mobile_users" => context.ContainsKey("platform") && context["platform"].ToString() == "mobile",
            "web_users" => context.ContainsKey("platform") && context["platform"].ToString() == "web",
            _ => context.ContainsKey("audiences") && 
                 context["audiences"] is List<string> userAudiences && 
                 userAudiences.Contains(audience)
        };
    }

    private static bool IsNewUser(Dictionary<string, object> context)
    {
        if (context.TryGetValue("registration_date", out var regDateObj) && 
            DateTime.TryParse(regDateObj.ToString(), out var regDate))
        {
            return regDate > DateTime.UtcNow.AddDays(-30); // New user if registered within 30 days
        }

        return false;
    }

    private static int ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Math.Abs(BitConverter.ToInt32(hashBytes, 0));
    }

    private static object TryParseNumber(string variation)
    {
        if (int.TryParse(variation, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(variation, out var doubleValue))
        {
            return doubleValue;
        }

        return 0;
    }

    private static object TryParseJson(string variation)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(variation) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}