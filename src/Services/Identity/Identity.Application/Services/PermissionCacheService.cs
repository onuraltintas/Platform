using Identity.Core.Caching;
using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Identity.Application.Services;

/// <summary>
/// Advanced permission caching service leveraging Enterprise.Shared.Caching system
/// </summary>
public class PermissionCacheService : IPermissionCacheService
{
    private readonly ICacheService _cacheService;
    private readonly IBulkCacheService _bulkCacheService;
    private readonly ICacheMetricsService _metricsService;
    private readonly IdentityDbContext _context;
    private readonly ILogger<PermissionCacheService> _logger;

    // Cache key prefixes
    private const string USER_PERMISSIONS_PREFIX = "perm:user:";
    private const string ROLE_PERMISSIONS_PREFIX = "perm:role:";
    private const string PERMISSION_METADATA_PREFIX = "perm:meta:";
    private const string CACHE_STATS_KEY = "perm:stats";
    private const string USER_ROLES_PREFIX = "perm:user_roles:";

    // Cache TTL configurations
    private readonly TimeSpan _defaultUserPermissionsTtl = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _defaultRolePermissionsTtl = TimeSpan.FromHours(2);
    private readonly TimeSpan _defaultPermissionMetadataTtl = TimeSpan.FromHours(6);

    public PermissionCacheService(
        ICacheService cacheService,
        IBulkCacheService bulkCacheService,
        ICacheMetricsService metricsService,
        IdentityDbContext context,
        ILogger<PermissionCacheService> logger)
    {
        _cacheService = cacheService;
        _bulkCacheService = bulkCacheService;
        _metricsService = metricsService;
        _context = context;
        _logger = logger;
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"{USER_PERMISSIONS_PREFIX}{userId}";

        try
        {
            // Try to get from cache first using Enterprise.Shared.Caching
            var cachedPermissions = await _cacheService.GetAsync<HashSet<string>>(cacheKey, cancellationToken);
            if (cachedPermissions != null)
            {
                _metricsService.RecordHit();
                _metricsService.RecordOperationTime("get", stopwatch.Elapsed);
                _logger.LogDebug("Cache hit for user permissions: {UserId}", userId);
                return cachedPermissions;
            }

            _metricsService.RecordMiss();

            // Load from database
            var permissions = await LoadUserPermissionsFromDatabaseAsync(userId, cancellationToken);

            // Cache the result with intelligent TTL
            var ttl = CalculateIntelligentTtl(permissions.Count, _defaultUserPermissionsTtl);
            await SetUserPermissionsAsync(userId, permissions, ttl, cancellationToken);

            _metricsService.RecordOperationTime("get", stopwatch.Elapsed);
            _logger.LogDebug("Cache miss for user permissions: {UserId}, loaded {Count} permissions", userId, permissions.Count);

            return permissions;
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error getting user permissions for {UserId}", userId);
            return new HashSet<string>();
        }
    }

    public async Task<HashSet<string>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"{ROLE_PERMISSIONS_PREFIX}{roleId}";

        try
        {
            var cachedPermissions = await _cacheService.GetAsync<HashSet<string>>(cacheKey, cancellationToken);
            if (cachedPermissions != null)
            {
                _metricsService.RecordHit();
                _metricsService.RecordOperationTime("get", stopwatch.Elapsed);
                return cachedPermissions;
            }

            _metricsService.RecordMiss();

            var permissions = await LoadRolePermissionsFromDatabaseAsync(roleId, cancellationToken);

            var ttl = CalculateIntelligentTtl(permissions.Count, _defaultRolePermissionsTtl);
            await SetRolePermissionsAsync(roleId, permissions, ttl, cancellationToken);

            _metricsService.RecordOperationTime("get", stopwatch.Elapsed);
            return permissions;
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error getting role permissions for {RoleId}", roleId);
            return new HashSet<string>();
        }
    }

    public async Task<PermissionMetadata?> GetPermissionMetadataAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PERMISSION_METADATA_PREFIX}{permissionCode}";

        try
        {
            var cachedMetadata = await _cacheService.GetAsync<PermissionMetadata>(cacheKey, cancellationToken);
            if (cachedMetadata != null)
            {
                return cachedMetadata;
            }

            var permission = await _context.Permissions
                .Include(p => p.Service)
                .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

            if (permission == null)
                return null;

            var metadata = new PermissionMetadata
            {
                Code = permission.Code,
                Name = permission.Name,
                Description = permission.Description,
                Category = permission.Category,
                Service = permission.Service?.Name ?? "Unknown",
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedAt,
                CachedAt = DateTime.UtcNow
            };

            await _cacheService.SetAsync(cacheKey, metadata, _defaultPermissionMetadataTtl, cancellationToken);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission metadata for {PermissionCode}", permissionCode);
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(string userId, HashSet<string> permissions, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{USER_PERMISSIONS_PREFIX}{userId}";
        var effectiveTtl = ttl ?? _defaultUserPermissionsTtl;

        try
        {
            await _cacheService.SetAsync(cacheKey, permissions, effectiveTtl, cancellationToken);
            _logger.LogDebug("Cached user permissions for {UserId} with TTL {TTL}", userId, effectiveTtl);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error caching user permissions for {UserId}", userId);
        }
    }

    public async Task SetRolePermissionsAsync(string roleId, HashSet<string> permissions, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ROLE_PERMISSIONS_PREFIX}{roleId}";
        var effectiveTtl = ttl ?? _defaultRolePermissionsTtl;

        try
        {
            await _cacheService.SetAsync(cacheKey, permissions, effectiveTtl, cancellationToken);
            _logger.LogDebug("Cached role permissions for {RoleId} with TTL {TTL}", roleId, effectiveTtl);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error caching role permissions for {RoleId}", roleId);
        }
    }

    public async Task InvalidateUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{USER_PERMISSIONS_PREFIX}{userId}";

        try
        {
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogDebug("Invalidated user permissions cache for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error invalidating user permissions for {UserId}", userId);
        }
    }

    public async Task InvalidateRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Invalidate role permissions
            var roleCacheKey = $"{ROLE_PERMISSIONS_PREFIX}{roleId}";
            await _cacheService.RemoveAsync(roleCacheKey, cancellationToken);

            // Find all users with this role and invalidate their permissions
            var usersWithRole = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            await BulkInvalidateUsersAsync(usersWithRole, cancellationToken);

            _logger.LogInformation("Invalidated role permissions cache for {RoleId} and {UserCount} affected users",
                roleId, usersWithRole.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating role permissions for {RoleId}", roleId);
        }
    }

    public async Task InvalidatePermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a complex operation - we need to invalidate all users/roles that have this permission
            // For simplicity, we'll clear all caches when a permission is modified
            await ClearAllCachesAsync(cancellationToken);

            _logger.LogWarning("Cleared all permission caches due to permission modification: {PermissionCode}", permissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permission {PermissionCode}", permissionCode);
        }
    }

    public async Task BulkInvalidateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = userIds.Select(userId => $"{USER_PERMISSIONS_PREFIX}{userId}").ToList();
            var removedCount = await _bulkCacheService.RemoveMultipleAsync(keys, cancellationToken);
            _logger.LogDebug("Bulk invalidated {RemovedCount} user permission caches", removedCount);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error bulk invalidating user permissions");
        }
    }

    public async Task WarmupCacheAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = userIds.Select(async userId =>
            {
                try
                {
                    await GetUserPermissionsAsync(userId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warmup cache for user {UserId}", userId);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cache warmup completed for {UserCount} users", userIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warmup");
        }
    }

    public async Task<PermissionCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _metricsService.GetMetricsAsync(null, cancellationToken);
            var cachedUserCount = await GetCachedUserCountAsync(cancellationToken);

            return new PermissionCacheStatistics
            {
                TotalCachedUsers = cachedUserCount,
                TotalCachedRoles = await GetCachedKeysCount($"{ROLE_PERMISSIONS_PREFIX}*", cancellationToken),
                TotalCachedPermissions = await GetCachedKeysCount($"{PERMISSION_METADATA_PREFIX}*", cancellationToken),
                CacheHitRatio = metrics.HitOrani,
                TotalCacheHits = metrics.HitSayisi,
                TotalCacheMisses = metrics.MissSayisi,
                AverageResponseTime = metrics.OrtalamaGetSuresi,
                LastUpdated = DateTime.UtcNow,
                MemoryUsageBytes = (long)(metrics.BellekKullanimiMB * 1024 * 1024),
                TopCachedUsers = new Dictionary<string, int>(),
                TopCachedPermissions = new Dictionary<string, int>()
            };
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error getting cache statistics");
            return new PermissionCacheStatistics { LastUpdated = DateTime.UtcNow };
        }
    }

    public async Task<bool> IsUserPermissionsCachedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{USER_PERMISSIONS_PREFIX}{userId}";
        return await _cacheService.ExistsAsync(cacheKey, cancellationToken);
    }

    public async Task RefreshUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await InvalidateUserPermissionsAsync(userId, cancellationToken);
        await GetUserPermissionsAsync(userId, cancellationToken);
        _logger.LogDebug("Refreshed permissions cache for user {UserId}", userId);
    }

    public async Task<int> GetCachedUserCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedKeysCount($"{USER_PERMISSIONS_PREFIX}*", cancellationToken);
    }

    public async Task ClearAllCachesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userPattern = $"{USER_PERMISSIONS_PREFIX}*";
            var rolePattern = $"{ROLE_PERMISSIONS_PREFIX}*";
            var metadataPattern = $"{PERMISSION_METADATA_PREFIX}*";

            var removedUserKeys = await _cacheService.RemovePatternAsync(userPattern, cancellationToken);
            var removedRoleKeys = await _cacheService.RemovePatternAsync(rolePattern, cancellationToken);
            var removedMetadataKeys = await _cacheService.RemovePatternAsync(metadataPattern, cancellationToken);

            _logger.LogInformation("Cleared all permission caches: {UserKeys} user caches, {RoleKeys} role caches, {MetadataKeys} metadata caches",
                removedUserKeys, removedRoleKeys, removedMetadataKeys);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError();
            _logger.LogError(ex, "Error clearing all caches");
        }
    }

    private async Task<HashSet<string>> LoadUserPermissionsFromDatabaseAsync(string userId, CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>();

        try
        {
            // Get role-based permissions
            var rolePermissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp)
                .Where(rp => rp.IsActive)
                .Include(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            foreach (var rolePermission in rolePermissions)
            {
                if (rolePermission.IsWildcard && !string.IsNullOrEmpty(rolePermission.PermissionPattern))
                {
                    permissions.Add(rolePermission.PermissionPattern);
                }
                else if (rolePermission.Permission != null)
                {
                    permissions.Add(rolePermission.Permission.Code);
                }
            }

            // Get direct user permissions
            var directPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId && up.IsActive)
                .Include(up => up.Permission)
                .ToListAsync(cancellationToken);

            foreach (var userPermission in directPermissions)
            {
                if (userPermission.Type == UserPermissionType.Grant)
                {
                    if (userPermission.IsWildcard && !string.IsNullOrEmpty(userPermission.PermissionPattern))
                    {
                        permissions.Add(userPermission.PermissionPattern);
                    }
                    else if (userPermission.Permission != null)
                    {
                        permissions.Add(userPermission.Permission.Code);
                    }
                }
                else if (userPermission.Type == UserPermissionType.Deny)
                {
                    // Remove denied permissions
                    if (userPermission.Permission != null)
                    {
                        permissions.Remove(userPermission.Permission.Code);
                    }
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user permissions from database for {UserId}", userId);
            return permissions;
        }
    }

    private async Task<HashSet<string>> LoadRolePermissionsFromDatabaseAsync(string roleId, CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>();

        try
        {
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId && rp.IsActive)
                .Include(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            foreach (var rolePermission in rolePermissions)
            {
                if (rolePermission.IsWildcard && !string.IsNullOrEmpty(rolePermission.PermissionPattern))
                {
                    permissions.Add(rolePermission.PermissionPattern);
                }
                else if (rolePermission.Permission != null)
                {
                    permissions.Add(rolePermission.Permission.Code);
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role permissions from database for {RoleId}", roleId);
            return permissions;
        }
    }

    private TimeSpan CalculateIntelligentTtl(int permissionCount, TimeSpan baseTtl)
    {
        // More permissions = longer cache time (they're more expensive to load)
        var multiplier = Math.Min(2.0, 1.0 + (permissionCount / 100.0));
        return TimeSpan.FromTicks((long)(baseTtl.Ticks * multiplier));
    }

    private async Task<int> GetCachedKeysCount(string pattern, CancellationToken cancellationToken)
    {
        try
        {
            if (_cacheService is IAdvancedCacheService advancedCache)
            {
                var keys = await advancedCache.GetKeysAsync(pattern, 10000, cancellationToken);
                return keys.Count;
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cached keys count for pattern: {Pattern}", pattern);
            return 0;
        }
    }
}