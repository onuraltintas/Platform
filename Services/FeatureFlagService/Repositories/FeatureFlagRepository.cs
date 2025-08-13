using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Services.FeatureFlagService.Data;
using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;

namespace EgitimPlatform.Services.FeatureFlagService.Repositories;

public class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly FeatureFlagDbContext _context;

    public FeatureFlagRepository(FeatureFlagDbContext context)
    {
        _context = context;
    }

    public async Task<FeatureFlag?> GetByIdAsync(string id)
    {
        return await _context.FeatureFlags
            .Include(f => f.Assignments)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<FeatureFlag?> GetByKeyAsync(string key, string environment, string applicationId)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Key == key && f.Environment == environment && f.ApplicationId == applicationId);
    }

    public async Task<List<FeatureFlag>> GetAllAsync()
    {
        return await _context.FeatureFlags
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<FeatureFlag> items, int totalCount)> GetPagedAsync(FeatureFlagListRequest request)
    {
        var query = _context.FeatureFlags.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(f => f.Name.Contains(request.Search) || f.Key.Contains(request.Search) || f.Description.Contains(request.Search));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(f => f.Type == request.Type);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(f => f.Status == request.Status);
        }

        if (!string.IsNullOrEmpty(request.Environment))
        {
            query = query.Where(f => f.Environment == request.Environment);
        }

        if (!string.IsNullOrEmpty(request.ApplicationId))
        {
            query = query.Where(f => f.ApplicationId == request.ApplicationId);
        }

        if (request.Tags != null && request.Tags.Any())
        {
            // This would need a more complex implementation for JSON arrays
            // For now, we'll use a simple contains check
            foreach (var tag in request.Tags)
            {
                query = query.Where(f => f.Tags.Contains(tag));
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDirection);

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<FeatureFlag> CreateAsync(FeatureFlag featureFlag)
    {
        _context.FeatureFlags.Add(featureFlag);
        return featureFlag;
    }

    public async Task<FeatureFlag> UpdateAsync(FeatureFlag featureFlag)
    {
        _context.FeatureFlags.Update(featureFlag);
        return featureFlag;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var featureFlag = await _context.FeatureFlags.FindAsync(id);
        if (featureFlag == null)
        {
            return false;
        }

        _context.FeatureFlags.Remove(featureFlag);
        return true;
    }

    public async Task<bool> ExistsAsync(string key, string environment, string applicationId)
    {
        return await _context.FeatureFlags
            .AnyAsync(f => f.Key == key && f.Environment == environment && f.ApplicationId == applicationId);
    }

    public async Task<List<FeatureFlag>> GetByEnvironmentAsync(string environment)
    {
        return await _context.FeatureFlags
            .Where(f => f.Environment == environment)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<FeatureFlag>> GetByApplicationIdAsync(string applicationId)
    {
        return await _context.FeatureFlags
            .Where(f => f.ApplicationId == applicationId)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<FeatureFlag>> GetByStatusAsync(FeatureFlagStatus status)
    {
        return await _context.FeatureFlags
            .Where(f => f.Status == status)
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FeatureFlag>> GetByTagsAsync(List<string> tags)
    {
        var query = _context.FeatureFlags.AsQueryable();

        foreach (var tag in tags)
        {
            query = query.Where(f => f.Tags.Contains(tag));
        }

        return await query
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<FeatureFlag>> GetExpiredAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.FeatureFlags
            .Where(f => f.EndDate.HasValue && f.EndDate < now && f.Status == FeatureFlagStatus.Active)
            .ToListAsync();
    }

    public async Task<List<FeatureFlag>> GetExpiringAsync(int daysFromNow)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysFromNow);
        return await _context.FeatureFlags
            .Where(f => f.EndDate.HasValue && f.EndDate <= futureDate && f.Status == FeatureFlagStatus.Active)
            .ToListAsync();
    }

    private static IQueryable<FeatureFlag> ApplySorting(IQueryable<FeatureFlag> query, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLowerInvariant() == "desc";

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
            "key" => isDescending ? query.OrderByDescending(f => f.Key) : query.OrderBy(f => f.Key),
            "status" => isDescending ? query.OrderByDescending(f => f.Status) : query.OrderBy(f => f.Status),
            "type" => isDescending ? query.OrderByDescending(f => f.Type) : query.OrderBy(f => f.Type),
            "isenabled" => isDescending ? query.OrderByDescending(f => f.IsEnabled) : query.OrderBy(f => f.IsEnabled),
            "rolloutpercentage" => isDescending ? query.OrderByDescending(f => f.RolloutPercentage) : query.OrderBy(f => f.RolloutPercentage),
            "environment" => isDescending ? query.OrderByDescending(f => f.Environment) : query.OrderBy(f => f.Environment),
            "createdat" => isDescending ? query.OrderByDescending(f => f.CreatedAt) : query.OrderBy(f => f.CreatedAt),
            "updatedat" => isDescending ? query.OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt) : query.OrderBy(f => f.UpdatedAt ?? f.CreatedAt),
            _ => isDescending ? query.OrderByDescending(f => f.CreatedAt) : query.OrderBy(f => f.CreatedAt)
        };
    }
}