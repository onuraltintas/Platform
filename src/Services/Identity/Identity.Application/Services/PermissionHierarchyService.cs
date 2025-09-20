using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Identity.Application.Services;

public interface IPermissionHierarchyService
{
    Task<IEnumerable<string>> ExpandPermissionsAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetEffectivePermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionWithHierarchyAsync(string userId, string permission, Guid? groupId = null, CancellationToken cancellationToken = default);
    bool IsWildcardMatch(string pattern, string permission);
    Task InvalidateHierarchyCacheAsync();
}

public class PermissionHierarchyService : IPermissionHierarchyService
{
    private readonly IPermissionService _permissionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionHierarchyService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    private readonly string _hierarchyCacheKey = "permission_hierarchy";

    public PermissionHierarchyService(
        IPermissionService permissionService,
        IMemoryCache cache,
        ILogger<PermissionHierarchyService> logger)
    {
        _permissionService = permissionService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> ExpandPermissionsAsync(
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expandedPermissions = new HashSet<string>();

            foreach (var permission in permissions)
            {
                expandedPermissions.Add(permission);

                if (IsWildcardPermission(permission))
                {
                    var wildcardMatches = await GetWildcardMatchesAsync(permission, cancellationToken);
                    foreach (var match in wildcardMatches)
                    {
                        expandedPermissions.Add(match);
                    }
                }
            }

            _logger.LogDebug("Expanded {InputCount} permissions to {OutputCount} permissions",
                permissions.Count(), expandedPermissions.Count);

            return expandedPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding permissions");
            return permissions;
        }
    }

    public async Task<IEnumerable<string>> GetEffectivePermissionsAsync(
        string userId,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"effective_permissions:{userId}:{groupId}";

            if (_cache.TryGetValue<IEnumerable<string>>(cacheKey, out var cachedPermissions))
            {
                return cachedPermissions!;
            }

            var userPermissionsResult = await _permissionService.GetUserPermissionNamesAsync(
                userId, groupId, cancellationToken);

            if (!userPermissionsResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get user permissions for {UserId}: {Error}",
                    userId, userPermissionsResult.Error);
                return new List<string>();
            }

            var effectivePermissions = await ExpandPermissionsAsync(
                userPermissionsResult.Value, cancellationToken);

            var shortCacheExpiration = TimeSpan.FromMinutes(5);
            _cache.Set(cacheKey, effectivePermissions, shortCacheExpiration);

            _logger.LogInformation("Retrieved {Count} effective permissions for user {UserId}",
                effectivePermissions.Count(), userId);

            return effectivePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> HasPermissionWithHierarchyAsync(
        string userId,
        string permission,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var effectivePermissions = await GetEffectivePermissionsAsync(
                userId, groupId, cancellationToken);

            if (effectivePermissions.Contains(permission))
            {
                return true;
            }

            foreach (var userPermission in effectivePermissions)
            {
                if (IsWildcardPermission(userPermission) && IsWildcardMatch(userPermission, permission))
                {
                    _logger.LogDebug("Permission {Permission} granted via wildcard {Wildcard} for user {UserId}",
                        permission, userPermission, userId);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking hierarchical permission {Permission} for user {UserId}",
                permission, userId);
            return false;
        }
    }

    public bool IsWildcardMatch(string pattern, string permission)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(permission))
                return false;

            if (pattern == permission)
                return true;

            var regexPattern = pattern
                .Replace(".", @"\.")
                .Replace("**", "DOUBLE_WILDCARD")
                .Replace("*", "[^.]*")
                .Replace("DOUBLE_WILDCARD", ".*");

            regexPattern = "^" + regexPattern + "$";

            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return regex.IsMatch(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching wildcard pattern {Pattern} against {Permission}",
                pattern, permission);
            return false;
        }
    }

    public async Task InvalidateHierarchyCacheAsync()
    {
        _cache.Remove(_hierarchyCacheKey);
        _logger.LogInformation("Permission hierarchy cache invalidated");
    }

    private bool IsWildcardPermission(string permission)
    {
        return permission.Contains('*');
    }

    private async Task<IEnumerable<string>> GetWildcardMatchesAsync(string pattern, CancellationToken cancellationToken)
    {
        // In a real implementation, this would query all permissions and match against the pattern
        // For now, return empty list
        await Task.Delay(1, cancellationToken);
        return new List<string>();
    }
}