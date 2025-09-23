using Enterprise.Shared.Caching.Interfaces;

namespace Identity.Core.Caching;

/// <summary>
/// Advanced permission caching service interface that leverages Enterprise.Shared.Caching
/// </summary>
public interface IPermissionCacheService
{
    /// <summary>
    /// Get user permissions with multi-level caching
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role permissions
    /// </summary>
    Task<HashSet<string>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get permission metadata
    /// </summary>
    Task<PermissionMetadata?> GetPermissionMetadataAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache user permissions with intelligent TTL
    /// </summary>
    Task SetUserPermissionsAsync(string userId, HashSet<string> permissions, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache role permissions
    /// </summary>
    Task SetRolePermissionsAsync(string roleId, HashSet<string> permissions, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate user permissions cache
    /// </summary>
    Task InvalidateUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate role permissions cache and all users with that role
    /// </summary>
    Task InvalidateRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate specific permission across all users/roles
    /// </summary>
    Task InvalidatePermissionAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk invalidate multiple users
    /// </summary>
    Task BulkInvalidateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Warm up cache for frequently accessed permissions
    /// </summary>
    Task WarmupCacheAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<PermissionCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user permissions are cached
    /// </summary>
    Task<bool> IsUserPermissionsCachedAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh user permissions from database
    /// </summary>
    Task RefreshUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached user count
    /// </summary>
    Task<int> GetCachedUserCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all permission caches
    /// </summary>
    Task ClearAllCachesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Permission metadata for caching
/// </summary>
public class PermissionMetadata
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CachedAt { get; set; }
}

/// <summary>
/// Permission cache statistics
/// </summary>
public class PermissionCacheStatistics
{
    public int TotalCachedUsers { get; set; }
    public int TotalCachedRoles { get; set; }
    public int TotalCachedPermissions { get; set; }
    public double CacheHitRatio { get; set; }
    public long TotalCacheHits { get; set; }
    public long TotalCacheMisses { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastUpdated { get; set; }
    public long MemoryUsageBytes { get; set; }
    public Dictionary<string, int> TopCachedUsers { get; set; } = new();
    public Dictionary<string, int> TopCachedPermissions { get; set; } = new();
}