using Enterprise.Shared.Caching.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gateway.Core.Authorization;

/// <summary>
/// Gateway permission service with caching and dynamic configuration
/// </summary>
public class GatewayPermissionService : IGatewayPermissionService
{
    private readonly ICacheService _cacheService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GatewayPermissionService> _logger;
    private readonly string _identityServiceUrl;

    // Static route permission mappings (can be overridden dynamically)
    private readonly Dictionary<string, RoutePermissionConfig> _routePermissions;

    public GatewayPermissionService(
        ICacheService cacheService,
        IHttpClientFactory httpClientFactory,
        ILogger<GatewayPermissionService> logger)
    {
        _cacheService = cacheService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _identityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://localhost:5001";

        _routePermissions = InitializeRoutePermissions();
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string route, string method, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user ID from claims
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims for route {Route}", route);
                return false;
            }

            // Check if user is SuperAdmin (bypass all checks)
            if (user.IsInRole("SuperAdmin"))
            {
                _logger.LogDebug("SuperAdmin access granted for route {Route}", route);
                return true;
            }

            // Find matching route configuration
            var routeConfig = FindRouteConfig(route);
            if (routeConfig == null)
            {
                _logger.LogWarning("No route configuration found for {Route}", route);
                return false;
            }

            // Check if route allows anonymous access
            if (routeConfig.AllowAnonymous)
            {
                return true;
            }

            // Check authentication requirement
            if (routeConfig.RequireAuthentication && !user.Identity!.IsAuthenticated)
            {
                return false;
            }

            // Check role-based access
            if (routeConfig.AllowedRoles.Length > 0)
            {
                var hasRole = routeConfig.AllowedRoles.Any(role => user.IsInRole(role));
                if (hasRole)
                {
                    return true;
                }
            }

            // Check method-specific permissions
            if (routeConfig.MethodPermissions.TryGetValue(method.ToUpper(), out var requiredPermissions))
            {
                if (requiredPermissions.Length == 0)
                {
                    return true; // No specific permissions required for this method
                }

                var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
                return requiredPermissions.Any(permission => HasWildcardPermission(userPermissions, permission));
            }

            // Default: allow if authenticated
            return user.Identity!.IsAuthenticated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for route {Route} method {Method}", route, method);
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // SuperAdmin has all permissions
        if (user.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Any(permission => HasWildcardPermission(userPermissions, permission));
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"gateway:user_permissions:{userId}";

        try
        {
            // Try to get from cache first
            var cachedPermissions = await _cacheService.GetAsync<HashSet<string>>(cacheKey, cancellationToken);
            if (cachedPermissions != null)
            {
                return cachedPermissions;
            }

            // Fetch from Identity service
            var permissions = await FetchUserPermissionsFromIdentityServiceAsync(userId, cancellationToken);

            // Cache for 15 minutes
            await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(15), cancellationToken);

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for user {UserId}", userId);
            return new HashSet<string>();
        }
    }

    public async Task InvalidateUserPermissionsAsync(string userId)
    {
        var cacheKey = $"gateway:user_permissions:{userId}";
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogDebug("Invalidated permissions cache for user {UserId}", userId);
    }

    public Dictionary<string, RoutePermissionConfig> GetRoutePermissions()
    {
        return new Dictionary<string, RoutePermissionConfig>(_routePermissions);
    }

    public async Task UpdateRoutePermissionAsync(string route, RoutePermissionConfig config)
    {
        _routePermissions[route] = config;

        // Cache the updated configuration
        var cacheKey = $"gateway:route_permissions";
        await _cacheService.SetAsync(cacheKey, _routePermissions, TimeSpan.FromHours(1));

        _logger.LogInformation("Updated route permission configuration for {Route}", route);
    }

    private RoutePermissionConfig? FindRouteConfig(string route)
    {
        // First, try exact match
        if (_routePermissions.TryGetValue(route, out var config))
        {
            return config;
        }

        // Then try pattern matching
        foreach (var kvp in _routePermissions)
        {
            if (IsRouteMatch(route, kvp.Key))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private static bool IsRouteMatch(string actualRoute, string configRoute)
    {
        // Convert YARP route pattern to regex
        var pattern = configRoute
            .Replace("{**catch-all}", ".*")
            .Replace("{*catch-all}", ".*")
            .Replace("{id}", @"[^/]+")
            .Replace("{userId}", @"[^/]+")
            .Replace("{roleId}", @"[^/]+");

        return Regex.IsMatch(actualRoute, $"^{pattern}$", RegexOptions.IgnoreCase);
    }

    private static bool HasWildcardPermission(HashSet<string> userPermissions, string requiredPermission)
    {
        // Direct permission check
        if (userPermissions.Contains(requiredPermission))
        {
            return true;
        }

        // Wildcard permission check
        foreach (var userPermission in userPermissions)
        {
            if (userPermission.Contains("*"))
            {
                if (MatchesWildcard(requiredPermission, userPermission))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesWildcard(string permission, string wildcard)
    {
        if (wildcard == "*.*.*" || wildcard == "*")
        {
            return true; // SuperAdmin wildcard
        }

        var permissionParts = permission.Split('.');
        var wildcardParts = wildcard.Split('.');

        if (wildcardParts.Length > permissionParts.Length)
        {
            return false;
        }

        for (int i = 0; i < wildcardParts.Length; i++)
        {
            if (wildcardParts[i] != "*" && wildcardParts[i] != permissionParts[i])
            {
                return false;
            }
        }

        return true;
    }

    private async Task<HashSet<string>> FetchUserPermissionsFromIdentityServiceAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_identityServiceUrl);

            var response = await httpClient.GetAsync($"/api/v1/users/{userId}/permissions", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var permissionsResponse = JsonSerializer.Deserialize<UserPermissionsResponse>(json);

                if (permissionsResponse?.Data != null)
                {
                    var allPermissions = new HashSet<string>();
                    allPermissions.UnionWith(permissionsResponse.Data.DirectPermissions);
                    allPermissions.UnionWith(permissionsResponse.Data.RolePermissions);
                    allPermissions.UnionWith(permissionsResponse.Data.WildcardPermissions);

                    return allPermissions;
                }
            }
            else
            {
                _logger.LogWarning("Failed to fetch permissions for user {UserId}. Status: {StatusCode}",
                    userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permissions from Identity service for user {UserId}", userId);
        }

        return new HashSet<string>();
    }

    private Dictionary<string, RoutePermissionConfig> InitializeRoutePermissions()
    {
        return new Dictionary<string, RoutePermissionConfig>
        {
            // Authentication routes (anonymous)
            ["/api/v1/auth/{**catch-all}"] = new()
            {
                Route = "/api/v1/auth/{**catch-all}",
                AllowAnonymous = true,
                RequireAuthentication = false,
                Service = "Identity"
            },

            ["/api/auth/{**catch-all}"] = new()
            {
                Route = "/api/auth/{**catch-all}",
                AllowAnonymous = true,
                RequireAuthentication = false,
                Service = "Identity"
            },

            // Account routes (authenticated users only)
            ["/api/v1/account/{**catch-all}"] = new()
            {
                Route = "/api/v1/account/{**catch-all}",
                RequireAuthentication = true,
                Service = "Identity",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = Array.Empty<string>(), // Authenticated users can read their account
                    ["PUT"] = Array.Empty<string>(), // Authenticated users can update their account
                    ["PATCH"] = Array.Empty<string>()
                }
            },

            // User management routes
            ["/api/v1/users/{**catch-all}"] = new()
            {
                Route = "/api/v1/users/{**catch-all}",
                RequireAuthentication = true,
                Service = "Identity",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "Identity.Users.Read" },
                    ["POST"] = new[] { "Identity.Users.Create" },
                    ["PUT"] = new[] { "Identity.Users.Update" },
                    ["PATCH"] = new[] { "Identity.Users.Update" },
                    ["DELETE"] = new[] { "Identity.Users.Delete" }
                }
            },

            // Role management routes
            ["/api/v1/roles/{**catch-all}"] = new()
            {
                Route = "/api/v1/roles/{**catch-all}",
                RequireAuthentication = true,
                Service = "Identity",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "Identity.Roles.Read" },
                    ["POST"] = new[] { "Identity.Roles.Create" },
                    ["PUT"] = new[] { "Identity.Roles.Update" },
                    ["PATCH"] = new[] { "Identity.Roles.Update" },
                    ["DELETE"] = new[] { "Identity.Roles.Delete" }
                }
            },

            // Permission management routes
            ["/api/v1/permissions/{**catch-all}"] = new()
            {
                Route = "/api/v1/permissions/{**catch-all}",
                RequireAuthentication = true,
                Service = "Identity",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "Identity.Permissions.Read" },
                    ["POST"] = new[] { "Identity.Permissions.Create" },
                    ["PUT"] = new[] { "Identity.Permissions.Update" },
                    ["DELETE"] = new[] { "Identity.Permissions.Delete" }
                }
            },

            // Group management routes
            ["/api/v1/groups/{**catch-all}"] = new()
            {
                Route = "/api/v1/groups/{**catch-all}",
                RequireAuthentication = true,
                Service = "Identity",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "Identity.Groups.Read" },
                    ["POST"] = new[] { "Identity.Groups.Create" },
                    ["PUT"] = new[] { "Identity.Groups.Update" },
                    ["DELETE"] = new[] { "Identity.Groups.Delete" }
                }
            },

            // Speed Reading routes
            ["/api/v1/exercises/{**catch-all}"] = new()
            {
                Route = "/api/v1/exercises/{**catch-all}",
                RequireAuthentication = true,
                Service = "SpeedReading",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "SpeedReading.Exercises.Read" },
                    ["POST"] = new[] { "SpeedReading.Exercises.Create" },
                    ["PUT"] = new[] { "SpeedReading.Exercises.Update" },
                    ["DELETE"] = new[] { "SpeedReading.Exercises.Delete" }
                }
            },

            ["/api/v1/reading-texts/{**catch-all}"] = new()
            {
                Route = "/api/v1/reading-texts/{**catch-all}",
                RequireAuthentication = true,
                Service = "SpeedReading",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "SpeedReading.ReadingTexts.Read" },
                    ["POST"] = new[] { "SpeedReading.ReadingTexts.Create" },
                    ["PUT"] = new[] { "SpeedReading.ReadingTexts.Update" },
                    ["DELETE"] = new[] { "SpeedReading.ReadingTexts.Delete" }
                }
            },

            ["/api/v1/speedreading/analytics/{**catch-all}"] = new()
            {
                Route = "/api/v1/speedreading/analytics/{**catch-all}",
                RequireAuthentication = true,
                Service = "SpeedReading",
                MethodPermissions = new Dictionary<string, string[]>
                {
                    ["GET"] = new[] { "SpeedReading.Analytics.Read" },
                    ["POST"] = new[] { "SpeedReading.Analytics.Export" }
                }
            }
        };
    }

    private class UserPermissionsResponse
    {
        public UserPermissionsData? Data { get; set; }
    }

    private class UserPermissionsData
    {
        public string[] DirectPermissions { get; set; } = Array.Empty<string>();
        public string[] RolePermissions { get; set; } = Array.Empty<string>();
        public string[] WildcardPermissions { get; set; } = Array.Empty<string>();
    }
}