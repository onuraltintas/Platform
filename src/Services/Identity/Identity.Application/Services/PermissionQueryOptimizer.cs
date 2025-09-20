using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public interface IPermissionQueryOptimizer
{
    Task<Dictionary<string, bool>> CheckMultiplePermissionsAsync(string userId, IEnumerable<string> permissions, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, IEnumerable<string>>> GetBulkUserPermissionsAsync(IEnumerable<string> userIds, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task PreloadPermissionCacheAsync(CancellationToken cancellationToken = default);
}

public class PermissionQueryOptimizer : IPermissionQueryOptimizer
{
    private readonly IdentityDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionQueryOptimizer> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public PermissionQueryOptimizer(
        IdentityDbContext context,
        IMemoryCache cache,
        ILogger<PermissionQueryOptimizer> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Dictionary<string, bool>> CheckMultiplePermissionsAsync(
        string userId,
        IEnumerable<string> permissions,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permissionsList = permissions.ToList();
            var cacheKey = $"multi_check:{userId}:{string.Join(",", permissionsList.Take(10))}:{groupId}";

            if (_cache.TryGetValue<Dictionary<string, bool>>(cacheKey, out var cachedResult))
            {
                return cachedResult!;
            }

            var userPermissions = await GetUserPermissionNamesAsync(userId, groupId, cancellationToken);

            var results = permissionsList.ToDictionary(
                permission => permission,
                permission => userPermissions.Contains(permission)
            );

            var shortExpiration = TimeSpan.FromMinutes(3);
            _cache.Set(cacheKey, results, shortExpiration);

            _logger.LogDebug("Checked {PermissionCount} permissions for user {UserId}: {GrantedCount} granted",
                permissionsList.Count, userId, results.Count(r => r.Value));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multiple permission check for user {UserId}", userId);
            return permissions.ToDictionary(p => p, p => false);
        }
    }

    public async Task<Dictionary<string, IEnumerable<string>>> GetBulkUserPermissionsAsync(
        IEnumerable<string> userIds,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdList = userIds.ToList();
            var cacheKey = $"bulk_user_permissions:{string.Join(",", userIdList.Take(5))}:{groupId}";

            if (_cache.TryGetValue<Dictionary<string, IEnumerable<string>>>(cacheKey, out var cachedResult))
            {
                return cachedResult!;
            }

            var results = new Dictionary<string, List<string>>();
            foreach (var userId in userIdList)
            {
                results[userId] = new List<string>();
            }

            var query = from ur in _context.UserRoles
                       join r in _context.Roles on ur.RoleId equals r.Id
                       join rp in _context.RolePermissions on r.Id equals rp.RoleId
                       join p in _context.Permissions on rp.PermissionId equals p.Id
                       where userIdList.Contains(ur.UserId)
                             && p.IsActive
                             && r.IsActive
                             && (groupId == null || rp.GroupId == null || rp.GroupId == groupId)
                             && (rp.ValidFrom == null || rp.ValidFrom <= DateTime.UtcNow)
                             && (rp.ValidUntil == null || rp.ValidUntil >= DateTime.UtcNow)
                       select new { ur.UserId, p.Name };

            var permissionData = await query.ToListAsync(cancellationToken);

            foreach (var item in permissionData)
            {
                if (results.ContainsKey(item.UserId))
                {
                    results[item.UserId].Add(item.Name);
                }
            }

            var finalResults = results.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Distinct().AsEnumerable()
            );

            _cache.Set(cacheKey, finalResults, _cacheExpiration);

            _logger.LogInformation("Retrieved bulk permissions for {UserCount} users", userIdList.Count);

            return finalResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk user permissions query");
            return userIds.ToDictionary(id => id, id => Enumerable.Empty<string>());
        }
    }

    public async Task PreloadPermissionCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting permission cache preload");

            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.Id, p.Name, p.Resource, p.Action, p.ServiceId })
                .ToListAsync(cancellationToken);

            var permissionLookup = allPermissions.ToDictionary(p => p.Name, p => p);
            _cache.Set("all_permissions_lookup", permissionLookup, TimeSpan.FromHours(1));

            _logger.LogInformation("Permission cache preload completed: {PermissionCount} permissions",
                allPermissions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission cache preload");
        }
    }

    private async Task<HashSet<string>> GetUserPermissionNamesAsync(string userId, Guid? groupId, CancellationToken cancellationToken)
    {
        var query = from ur in _context.UserRoles
                   join r in _context.Roles on ur.RoleId equals r.Id
                   join rp in _context.RolePermissions on r.Id equals rp.RoleId
                   join p in _context.Permissions on rp.PermissionId equals p.Id
                   where ur.UserId == userId
                         && p.IsActive
                         && r.IsActive
                         && (groupId == null || rp.GroupId == null || rp.GroupId == groupId)
                         && (rp.ValidFrom == null || rp.ValidFrom <= DateTime.UtcNow)
                         && (rp.ValidUntil == null || rp.ValidUntil >= DateTime.UtcNow)
                   select p.Name;

        var permissions = await query.ToListAsync(cancellationToken);
        return permissions.ToHashSet();
    }
}