using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using EgitimPlatform.Services.FeatureFlagService.Data;
using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;
using EgitimPlatform.Services.FeatureFlagService.Repositories;
using EgitimPlatform.Shared.Errors.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace EgitimPlatform.Services.FeatureFlagService.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlagDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IFeatureFlagRepository _repository;
    private readonly IFeatureFlagEvaluationEngine _evaluationEngine;

    public FeatureFlagService(
        FeatureFlagDbContext context,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<FeatureFlagService> logger,
        IFeatureFlagRepository repository,
        IFeatureFlagEvaluationEngine evaluationEngine)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
        _repository = repository;
        _evaluationEngine = evaluationEngine;
    }

    // Feature Flag Management
    public async Task<FeatureFlagResponse> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, string createdBy)
    {
        // Check if key already exists
        var existingFlag = await _repository.GetByKeyAsync(request.Key, request.Environment, request.ApplicationId);
        if (existingFlag != null)
        {
            throw new BusinessException("FEATURE_FLAG_KEY_EXISTS", $"Feature flag with key '{request.Key}' already exists in {request.Environment} environment");
        }

        var featureFlag = _mapper.Map<FeatureFlag>(request);
        featureFlag.CreatedBy = createdBy;
        featureFlag.CreatedAt = DateTime.UtcNow;

        await _repository.CreateAsync(featureFlag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag created: {Key} by {CreatedBy}", request.Key, createdBy);

        // Clear cache
        InvalidateCache(featureFlag.Key, featureFlag.Environment, featureFlag.ApplicationId);

        return _mapper.Map<FeatureFlagResponse>(featureFlag);
    }

    public async Task<FeatureFlagResponse> UpdateFeatureFlagAsync(string id, UpdateFeatureFlagRequest request, string updatedBy)
    {
        var featureFlag = await _repository.GetByIdAsync(id);
        if (featureFlag == null)
        {
            throw new BusinessException("FEATURE_FLAG_NOT_FOUND", $"Feature flag with id '{id}' not found");
        }

        _mapper.Map(request, featureFlag);
        featureFlag.UpdatedBy = updatedBy;
        featureFlag.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(featureFlag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag updated: {Key} by {UpdatedBy}", featureFlag.Key, updatedBy);

        // Clear cache
        InvalidateCache(featureFlag.Key, featureFlag.Environment, featureFlag.ApplicationId);

        return _mapper.Map<FeatureFlagResponse>(featureFlag);
    }

    public async Task<FeatureFlagResponse?> GetFeatureFlagAsync(string id)
    {
        var featureFlag = await _repository.GetByIdAsync(id);
        return featureFlag == null ? null : _mapper.Map<FeatureFlagResponse>(featureFlag);
    }

    public async Task<FeatureFlagResponse?> GetFeatureFlagByKeyAsync(string key, string environment, string applicationId)
    {
        var cacheKey = GetCacheKey(key, environment, applicationId);
        
        if (_cache.TryGetValue(cacheKey, out FeatureFlagResponse? cachedResponse))
        {
            return cachedResponse;
        }

        var featureFlag = await _repository.GetByKeyAsync(key, environment, applicationId);
        if (featureFlag == null)
        {
            return null;
        }

        var response = _mapper.Map<FeatureFlagResponse>(featureFlag);
        
        // Cache for 5 minutes
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return response;
    }

    public async Task<PagedFeatureFlagResponse> GetFeatureFlagsAsync(FeatureFlagListRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(request);
        
        return new PagedFeatureFlagResponse(
            _mapper.Map<List<FeatureFlagResponse>>(items),
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize)
        );
    }

    public async Task<bool> DeleteFeatureFlagAsync(string id)
    {
        var featureFlag = await _repository.GetByIdAsync(id);
        if (featureFlag == null)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag deleted: {Key}", featureFlag.Key);

        // Clear cache
        InvalidateCache(featureFlag.Key, featureFlag.Environment, featureFlag.ApplicationId);

        return true;
    }

    // Feature Flag Evaluation
    public async Task<FeatureFlagEvaluationResponse> EvaluateFeatureFlagAsync(EvaluateFeatureFlagRequest request)
    {
        var featureFlag = await GetFeatureFlagByKeyAsync(request.FeatureFlagKey, request.Environment, request.ApplicationId);
        
        if (featureFlag == null)
        {
            return new FeatureFlagEvaluationResponse(
                request.FeatureFlagKey,
                false,
                "control",
                false,
                "Feature flag not found",
                request.Context
            );
        }

        var evaluation = await _evaluationEngine.EvaluateAsync(featureFlag, request.UserId, request.Context);
        
        // Log the evaluation event
        await LogEventAsync(new LogEventRequest(
            featureFlag.Id,
            request.UserId,
            FeatureFlagEventTypes.Evaluated,
            evaluation.Variation,
            new Dictionary<string, object> { { "reason", evaluation.Reason } },
            "",
            "",
            ""
        ));

        return evaluation;
    }

    public async Task<BatchEvaluationResponse> BatchEvaluateAsync(BatchEvaluateRequest request)
    {
        var evaluations = new Dictionary<string, FeatureFlagEvaluationResponse>();

        foreach (var key in request.FeatureFlagKeys)
        {
            var evaluation = await EvaluateFeatureFlagAsync(new EvaluateFeatureFlagRequest(
                request.UserId,
                key,
                request.Context,
                request.Environment,
                request.ApplicationId
            ));
            
            evaluations[key] = evaluation;
        }

        return new BatchEvaluationResponse(
            request.UserId,
            evaluations,
            request.Context
        );
    }

    public async Task<bool> IsFeatureEnabledAsync(string userId, string featureFlagKey, Dictionary<string, object>? context = null, string environment = "production", string applicationId = "")
    {
        var evaluation = await EvaluateFeatureFlagAsync(new EvaluateFeatureFlagRequest(
            userId,
            featureFlagKey,
            context ?? new Dictionary<string, object>(),
            environment,
            applicationId
        ));

        return evaluation.IsEnabled;
    }

    public async Task<T> GetFeatureValueAsync<T>(string userId, string featureFlagKey, T defaultValue, Dictionary<string, object>? context = null, string environment = "production", string applicationId = "")
    {
        var evaluation = await EvaluateFeatureFlagAsync(new EvaluateFeatureFlagRequest(
            userId,
            featureFlagKey,
            context ?? new Dictionary<string, object>(),
            environment,
            applicationId
        ));

        if (evaluation.Value is T value)
        {
            return value;
        }

        return defaultValue;
    }

    // Assignment Management
    public async Task<FeatureFlagAssignmentResponse> AssignUserToVariationAsync(string featureFlagId, string userId, string variation, string reason, Dictionary<string, object>? context = null)
    {
        var assignment = new FeatureFlagAssignment
        {
            FeatureFlagId = featureFlagId,
            UserId = userId,
            AssignedVariation = variation,
            Reason = reason,
            Context = context ?? new Dictionary<string, object>(),
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.FeatureFlagAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Clear user assignment cache
        _cache.Remove($"assignment:{userId}:{featureFlagId}");

        return _mapper.Map<FeatureFlagAssignmentResponse>(assignment);
    }

    public async Task<FeatureFlagAssignmentResponse?> GetUserAssignmentAsync(string userId, string featureFlagId)
    {
        var cacheKey = $"assignment:{userId}:{featureFlagId}";
        
        if (_cache.TryGetValue(cacheKey, out FeatureFlagAssignmentResponse? cachedAssignment))
        {
            return cachedAssignment;
        }

        var assignment = await _context.FeatureFlagAssignments
            .FirstOrDefaultAsync(a => a.UserId == userId && a.FeatureFlagId == featureFlagId && a.IsActive);

        if (assignment == null)
        {
            return null;
        }

        var response = _mapper.Map<FeatureFlagAssignmentResponse>(assignment);
        
        // Cache for 10 minutes
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
        
        return response;
    }

    public async Task<List<FeatureFlagAssignmentResponse>> GetUserAssignmentsAsync(string userId)
    {
        var assignments = await _context.FeatureFlagAssignments
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        return _mapper.Map<List<FeatureFlagAssignmentResponse>>(assignments);
    }

    public async Task<bool> RemoveUserAssignmentAsync(string userId, string featureFlagId)
    {
        var assignment = await _context.FeatureFlagAssignments
            .FirstOrDefaultAsync(a => a.UserId == userId && a.FeatureFlagId == featureFlagId && a.IsActive);

        if (assignment == null)
        {
            return false;
        }

        assignment.IsActive = false;
        await _context.SaveChangesAsync();

        // Clear user assignment cache
        _cache.Remove($"assignment:{userId}:{featureFlagId}");

        return true;
    }

    // Event Logging
    public async Task LogEventAsync(LogEventRequest request)
    {
        var eventEntity = _mapper.Map<FeatureFlagEvent>(request);
        eventEntity.OccurredAt = DateTime.UtcNow;

        _context.FeatureFlagEvents.Add(eventEntity);
        await _context.SaveChangesAsync();
    }

    public async Task LogExposureAsync(string featureFlagId, string userId, string variation, Dictionary<string, object>? properties = null)
    {
        await LogEventAsync(new LogEventRequest(
            featureFlagId,
            userId,
            FeatureFlagEventTypes.ExposureLogged,
            variation,
            properties ?? new Dictionary<string, object>(),
            "",
            "",
            ""
        ));
    }

    public async Task LogConversionAsync(string featureFlagId, string userId, string eventName, Dictionary<string, object>? properties = null)
    {
        var eventProperties = properties ?? new Dictionary<string, object>();
        eventProperties["conversion_event"] = eventName;

        await LogEventAsync(new LogEventRequest(
            featureFlagId,
            userId,
            FeatureFlagEventTypes.ConversionTracked,
            "",
            eventProperties,
            "",
            "",
            ""
        ));
    }

    // Statistics and Analytics
    public async Task<FeatureFlagStatsResponse> GetFeatureFlagStatsAsync(string featureFlagId, TimeSpan period)
    {
        var fromDate = DateTime.UtcNow.Subtract(period);
        
        var events = await _context.FeatureFlagEvents
            .Where(e => e.FeatureFlagId == featureFlagId && e.OccurredAt >= fromDate)
            .ToListAsync();

        var featureFlag = await _repository.GetByIdAsync(featureFlagId);
        
        var totalEvaluations = events.Count(e => e.EventType == FeatureFlagEventTypes.Evaluated);
        var uniqueUsers = events.Select(e => e.UserId).Distinct().Count();
        
        var variationDistribution = events
            .Where(e => e.EventType == FeatureFlagEventTypes.Evaluated)
            .GroupBy(e => e.Variation)
            .ToDictionary(g => g.Key, g => g.Count());

        var conversionRates = CalculateConversionRates(events);

        return new FeatureFlagStatsResponse(
            featureFlagId,
            featureFlag?.Key ?? "",
            totalEvaluations,
            uniqueUsers,
            variationDistribution,
            conversionRates,
            events.Max(e => e.OccurredAt),
            period
        );
    }

    public async Task<FeatureFlagUsageResponse> GetFeatureFlagUsageAsync(string featureFlagId, DateTime from, DateTime to)
    {
        var events = await _context.FeatureFlagEvents
            .Where(e => e.FeatureFlagId == featureFlagId && e.OccurredAt >= from && e.OccurredAt <= to)
            .ToListAsync();

        var dailyEvaluations = events
            .Where(e => e.EventType == FeatureFlagEventTypes.Evaluated)
            .GroupBy(e => e.OccurredAt.Date)
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        var variationCounts = events
            .Where(e => e.EventType == FeatureFlagEventTypes.Evaluated)
            .GroupBy(e => e.Variation)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FeatureFlagUsageResponse(
            featureFlagId,
            dailyEvaluations,
            variationCounts,
            new List<string>(), // Top applications - would need additional tracking
            new List<string>()  // Top environments - would need additional tracking
        );
    }

    public async Task<Dictionary<string, int>> GetVariationDistributionAsync(string featureFlagId)
    {
        var assignments = await _context.FeatureFlagAssignments
            .Where(a => a.FeatureFlagId == featureFlagId && a.IsActive)
            .GroupBy(a => a.AssignedVariation)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return assignments;
    }

    // Bulk Operations
    public async Task<List<FeatureFlagResponse>> BulkUpdateStatusAsync(List<string> featureFlagIds, FeatureFlagStatus status, string updatedBy)
    {
        var featureFlags = await _context.FeatureFlags
            .Where(f => featureFlagIds.Contains(f.Id))
            .ToListAsync();

        foreach (var flag in featureFlags)
        {
            flag.Status = status;
            flag.UpdatedBy = updatedBy;
            flag.UpdatedAt = DateTime.UtcNow;

            // Clear cache
            InvalidateCache(flag.Key, flag.Environment, flag.ApplicationId);
        }

        await _context.SaveChangesAsync();

        return _mapper.Map<List<FeatureFlagResponse>>(featureFlags);
    }

    public async Task<List<FeatureFlagResponse>> BulkToggleAsync(List<string> featureFlagIds, bool isEnabled, string updatedBy)
    {
        var featureFlags = await _context.FeatureFlags
            .Where(f => featureFlagIds.Contains(f.Id))
            .ToListAsync();

        foreach (var flag in featureFlags)
        {
            flag.IsEnabled = isEnabled;
            flag.UpdatedBy = updatedBy;
            flag.UpdatedAt = DateTime.UtcNow;

            // Clear cache
            InvalidateCache(flag.Key, flag.Environment, flag.ApplicationId);
        }

        await _context.SaveChangesAsync();

        return _mapper.Map<List<FeatureFlagResponse>>(featureFlags);
    }

    // Health and Monitoring
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetServiceStatusAsync()
    {
        var totalFlags = await _context.FeatureFlags.CountAsync();
        var activeFlags = await _context.FeatureFlags.CountAsync(f => f.Status == FeatureFlagStatus.Active);
        var totalAssignments = await _context.FeatureFlagAssignments.CountAsync(a => a.IsActive);

        return new Dictionary<string, object>
        {
            { "total_feature_flags", totalFlags },
            { "active_feature_flags", activeFlags },
            { "total_assignments", totalAssignments },
            { "database_healthy", await IsHealthyAsync() },
            { "cache_entries", "unknown" },
            { "last_checked", DateTime.UtcNow }
        };
    }

    // Private helper methods
    private static string GetCacheKey(string key, string environment, string applicationId)
    {
        return $"feature_flag:{key}:{environment}:{applicationId}";
    }

    private void InvalidateCache(string key, string environment, string applicationId)
    {
        var cacheKey = GetCacheKey(key, environment, applicationId);
        _cache.Remove(cacheKey);
    }

    private static Dictionary<string, double> CalculateConversionRates(List<FeatureFlagEvent> events)
    {
        var exposures = events.Where(e => e.EventType == FeatureFlagEventTypes.ExposureLogged).ToList();
        var conversions = events.Where(e => e.EventType == FeatureFlagEventTypes.ConversionTracked).ToList();

        var conversionRates = new Dictionary<string, double>();

        var exposuresByVariation = exposures.GroupBy(e => e.Variation);
        
        foreach (var group in exposuresByVariation)
        {
            var variation = group.Key;
            var exposureCount = group.Count();
            var conversionCount = conversions.Count(c => 
                exposures.Any(e => e.UserId == c.UserId && e.Variation == variation));

            var rate = exposureCount > 0 ? (double)conversionCount / exposureCount : 0.0;
            conversionRates[variation] = rate;
        }

        return conversionRates;
    }
}