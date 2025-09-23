using System.Security.Claims;

namespace Gateway.Core.Authorization;

/// <summary>
/// Gateway-level permission service interface
/// </summary>
public interface IGatewayPermissionService
{
    /// <summary>
    /// Check if user has permission for the requested route
    /// </summary>
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string route, string method, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has any of the required permissions
    /// </summary>
    Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user permissions from cache or Identity service
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate user permissions cache
    /// </summary>
    Task InvalidateUserPermissionsAsync(string userId);

    /// <summary>
    /// Get route-to-permission mappings
    /// </summary>
    Dictionary<string, RoutePermissionConfig> GetRoutePermissions();

    /// <summary>
    /// Update route permission configuration (dynamic)
    /// </summary>
    Task UpdateRoutePermissionAsync(string route, RoutePermissionConfig config);
}

/// <summary>
/// Route permission configuration
/// </summary>
public class RoutePermissionConfig
{
    public string Route { get; set; } = string.Empty;
    public Dictionary<string, string[]> MethodPermissions { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public bool AllowAnonymous { get; set; } = false;
    public string[] AllowedRoles { get; set; } = Array.Empty<string>();
    public string? Service { get; set; }
    public int CacheDurationMinutes { get; set; } = 15;
}

/// <summary>
/// Permission check result
/// </summary>
public class PermissionCheckResult
{
    public bool IsAllowed { get; set; }
    public string? Reason { get; set; }
    public string? RequiredPermission { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? Route { get; set; }
    public string? Method { get; set; }
}