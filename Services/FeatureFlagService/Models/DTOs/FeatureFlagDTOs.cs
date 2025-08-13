using EgitimPlatform.Services.FeatureFlagService.Models.Entities;

namespace EgitimPlatform.Services.FeatureFlagService.Models.DTOs;

// Request DTOs
public record CreateFeatureFlagRequest(
    string Name,
    string Key,
    string Description,
    FeatureFlagType Type,
    bool IsEnabled,
    int RolloutPercentage,
    List<string> TargetAudiences,
    List<string> ExcludedAudiences,
    Dictionary<string, object> Conditions,
    Dictionary<string, object> Variations,
    string DefaultVariation,
    string Environment,
    string ApplicationId,
    List<string> Tags,
    DateTime? StartDate,
    DateTime? EndDate
);

public record UpdateFeatureFlagRequest(
    string Name,
    string Description,
    bool IsEnabled,
    FeatureFlagStatus Status,
    int RolloutPercentage,
    List<string> TargetAudiences,
    List<string> ExcludedAudiences,
    Dictionary<string, object> Conditions,
    Dictionary<string, object> Variations,
    string DefaultVariation,
    List<string> Tags,
    DateTime? StartDate,
    DateTime? EndDate
);

public record EvaluateFeatureFlagRequest(
    string UserId,
    string FeatureFlagKey,
    Dictionary<string, object> Context,
    string Environment = "production",
    string ApplicationId = ""
);

public record BatchEvaluateRequest(
    string UserId,
    List<string> FeatureFlagKeys,
    Dictionary<string, object> Context,
    string Environment = "production",
    string ApplicationId = ""
);

public record LogEventRequest(
    string FeatureFlagId,
    string UserId,
    string EventType,
    string Variation,
    Dictionary<string, object> Properties,
    string SessionId,
    string IpAddress,
    string UserAgent
);

// Response DTOs
public record FeatureFlagResponse(
    string Id,
    string Name,
    string Key,
    string Description,
    FeatureFlagType Type,
    bool IsEnabled,
    FeatureFlagStatus Status,
    DateTime? StartDate,
    DateTime? EndDate,
    int RolloutPercentage,
    List<string> TargetAudiences,
    List<string> ExcludedAudiences,
    Dictionary<string, object> Conditions,
    Dictionary<string, object> Variations,
    string DefaultVariation,
    string Environment,
    string ApplicationId,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string UpdatedBy
);

public record FeatureFlagEvaluationResponse(
    string FeatureFlagKey,
    bool IsEnabled,
    string Variation,
    object Value,
    string Reason,
    Dictionary<string, object> Context
);

public record BatchEvaluationResponse(
    string UserId,
    Dictionary<string, FeatureFlagEvaluationResponse> Evaluations,
    Dictionary<string, object> Context
);

public record FeatureFlagAssignmentResponse(
    string Id,
    string FeatureFlagId,
    string UserId,
    string AssignedVariation,
    DateTime AssignedAt,
    DateTime? ExpiresAt,
    string Reason,
    Dictionary<string, object> Context,
    bool IsActive
);

public record FeatureFlagEventResponse(
    string Id,
    string FeatureFlagId,
    string UserId,
    string EventType,
    string Variation,
    Dictionary<string, object> Properties,
    DateTime OccurredAt,
    string SessionId,
    string IpAddress
);


// Statistics and Analytics DTOs
public record FeatureFlagStatsResponse(
    string FeatureFlagId,
    string FeatureFlagKey,
    int TotalEvaluations,
    int UniqueUsers,
    Dictionary<string, int> VariationDistribution,
    Dictionary<string, double> ConversionRates,
    DateTime LastEvaluated,
    TimeSpan ReportPeriod
);

public record FeatureFlagUsageResponse(
    string FeatureFlagId,
    Dictionary<string, int> DailyEvaluations,
    Dictionary<string, int> VariationCounts,
    List<string> TopApplications,
    List<string> TopEnvironments
);

// List and Pagination DTOs
public record FeatureFlagListRequest(
    int Page = 1,
    int PageSize = 50,
    string? Search = null,
    FeatureFlagType? Type = null,
    FeatureFlagStatus? Status = null,
    string? Environment = null,
    string? ApplicationId = null,
    List<string>? Tags = null,
    string SortBy = "CreatedAt",
    string SortDirection = "desc"
);

public record PagedFeatureFlagResponse(
    List<FeatureFlagResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);