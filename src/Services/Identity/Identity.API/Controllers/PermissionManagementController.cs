using Identity.Core.Caching;
using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Dynamic permission management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class PermissionManagementController : ControllerBase
{
    private readonly IPermissionCacheService _permissionCacheService;
    private readonly IUserService _userService;
    private readonly ILogger<PermissionManagementController> _logger;

    public PermissionManagementController(
        IPermissionCacheService permissionCacheService,
        IUserService userService,
        ILogger<PermissionManagementController> logger)
    {
        _permissionCacheService = permissionCacheService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get user permissions with caching information
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    public async Task<ActionResult<UserPermissionsResponse>> GetUserPermissions(string userId)
    {
        try
        {
            var permissions = await _permissionCacheService.GetUserPermissionsAsync(userId);
            var isCached = await _permissionCacheService.IsUserPermissionsCachedAsync(userId);

            var response = new UserPermissionsResponse
            {
                UserId = userId,
                Permissions = permissions,
                IsCached = isCached,
                PermissionCount = permissions.Count,
                RetrievedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get user permissions" });
        }
    }

    /// <summary>
    /// Get role permissions with caching information
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    public async Task<ActionResult<RolePermissionsResponse>> GetRolePermissions(string roleId)
    {
        try
        {
            var permissions = await _permissionCacheService.GetRolePermissionsAsync(roleId);

            var response = new RolePermissionsResponse
            {
                RoleId = roleId,
                Permissions = permissions,
                PermissionCount = permissions.Count,
                RetrievedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to get role permissions" });
        }
    }

    /// <summary>
    /// Get permission metadata
    /// </summary>
    [HttpGet("permissions/{permissionCode}/metadata")]
    public async Task<ActionResult<PermissionMetadata>> GetPermissionMetadata(string permissionCode)
    {
        try
        {
            var metadata = await _permissionCacheService.GetPermissionMetadataAsync(permissionCode);

            if (metadata == null)
            {
                return NotFound(new { error = $"Permission '{permissionCode}' not found" });
            }

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for permission {PermissionCode}", permissionCode);
            return StatusCode(500, new { error = "Failed to get permission metadata" });
        }
    }

    /// <summary>
    /// Invalidate user permissions cache
    /// </summary>
    [HttpDelete("users/{userId}/cache")]
    public async Task<IActionResult> InvalidateUserPermissions(string userId)
    {
        try
        {
            await _permissionCacheService.InvalidateUserPermissionsAsync(userId);

            _logger.LogInformation("Invalidated permissions cache for user {UserId} by admin {AdminUser}",
                userId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "User permissions cache invalidated", userId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permissions cache for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to invalidate user permissions cache" });
        }
    }

    /// <summary>
    /// Invalidate role permissions cache
    /// </summary>
    [HttpDelete("roles/{roleId}/cache")]
    public async Task<IActionResult> InvalidateRolePermissions(string roleId)
    {
        try
        {
            await _permissionCacheService.InvalidateRolePermissionsAsync(roleId);

            _logger.LogInformation("Invalidated permissions cache for role {RoleId} by admin {AdminUser}",
                roleId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Role permissions cache invalidated", roleId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permissions cache for role {RoleId}", roleId);
            return StatusCode(500, new { error = "Failed to invalidate role permissions cache" });
        }
    }

    /// <summary>
    /// Bulk invalidate user permissions
    /// </summary>
    [HttpPost("users/bulk-invalidate")]
    public async Task<IActionResult> BulkInvalidateUserPermissions([FromBody] BulkInvalidateRequest request)
    {
        try
        {
            if (request.UserIds == null || !request.UserIds.Any())
            {
                return BadRequest(new { error = "UserIds cannot be empty" });
            }

            await _permissionCacheService.BulkInvalidateUsersAsync(request.UserIds);

            _logger.LogInformation("Bulk invalidated permissions cache for {UserCount} users by admin {AdminUser}",
                request.UserIds.Count(), User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new
            {
                message = "User permissions cache bulk invalidated",
                userCount = request.UserIds.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk invalidating user permissions cache");
            return StatusCode(500, new { error = "Failed to bulk invalidate user permissions cache" });
        }
    }

    /// <summary>
    /// Refresh user permissions (invalidate and reload)
    /// </summary>
    [HttpPost("users/{userId}/refresh")]
    public async Task<IActionResult> RefreshUserPermissions(string userId)
    {
        try
        {
            await _permissionCacheService.RefreshUserPermissionsAsync(userId);

            _logger.LogInformation("Refreshed permissions cache for user {UserId} by admin {AdminUser}",
                userId, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "User permissions refreshed", userId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing permissions for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to refresh user permissions" });
        }
    }

    /// <summary>
    /// Warmup cache for frequently accessed users
    /// </summary>
    [HttpPost("cache/warmup")]
    public async Task<IActionResult> WarmupCache([FromBody] CacheWarmupRequest request)
    {
        try
        {
            if (request.UserIds == null || !request.UserIds.Any())
            {
                return BadRequest(new { error = "UserIds cannot be empty" });
            }

            await _permissionCacheService.WarmupCacheAsync(request.UserIds);

            _logger.LogInformation("Cache warmup initiated for {UserCount} users by admin {AdminUser}",
                request.UserIds.Count(), User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new
            {
                message = "Cache warmup completed",
                userCount = request.UserIds.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up cache");
            return StatusCode(500, new { error = "Failed to warmup cache" });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/statistics")]
    public async Task<ActionResult<PermissionCacheStatistics>> GetCacheStatistics()
    {
        try
        {
            var statistics = await _permissionCacheService.GetCacheStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, new { error = "Failed to get cache statistics" });
        }
    }

    /// <summary>
    /// Clear all permission caches (dangerous operation)
    /// </summary>
    [HttpDelete("cache/clear-all")]
    public async Task<IActionResult> ClearAllCaches()
    {
        try
        {
            await _permissionCacheService.ClearAllCachesAsync();

            _logger.LogWarning("All permission caches cleared by admin {AdminUser}",
                User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "All permission caches cleared", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all caches");
            return StatusCode(500, new { error = "Failed to clear all caches" });
        }
    }

    /// <summary>
    /// Get cache health information
    /// </summary>
    [HttpGet("cache/health")]
    public async Task<ActionResult<CacheHealthResponse>> GetCacheHealth()
    {
        try
        {
            var cachedUserCount = await _permissionCacheService.GetCachedUserCountAsync();
            var statistics = await _permissionCacheService.GetCacheStatisticsAsync();

            var health = new CacheHealthResponse
            {
                IsHealthy = true,
                CachedUserCount = cachedUserCount,
                CacheHitRatio = statistics.CacheHitRatio,
                AverageResponseTime = statistics.AverageResponseTime,
                LastUpdated = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["TotalCacheHits"] = statistics.TotalCacheHits,
                    ["TotalCacheMisses"] = statistics.TotalCacheMisses,
                    ["TotalCachedUsers"] = statistics.TotalCachedUsers,
                    ["TotalCachedRoles"] = statistics.TotalCachedRoles,
                    ["MemoryUsageMB"] = statistics.MemoryUsageBytes / (1024.0 * 1024.0)
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache health");
            return Ok(new CacheHealthResponse
            {
                IsHealthy = false,
                LastUpdated = DateTime.UtcNow,
                Details = new Dictionary<string, object> { ["Error"] = ex.Message }
            });
        }
    }
}

/// <summary>
/// User permissions response model
/// </summary>
public class UserPermissionsResponse
{
    public string UserId { get; set; } = string.Empty;
    public HashSet<string> Permissions { get; set; } = new();
    public bool IsCached { get; set; }
    public int PermissionCount { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Role permissions response model
/// </summary>
public class RolePermissionsResponse
{
    public string RoleId { get; set; } = string.Empty;
    public HashSet<string> Permissions { get; set; } = new();
    public int PermissionCount { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Bulk invalidate request model
/// </summary>
public class BulkInvalidateRequest
{
    public IEnumerable<string> UserIds { get; set; } = [];
}

/// <summary>
/// Cache warmup request model
/// </summary>
public class CacheWarmupRequest
{
    public IEnumerable<string> UserIds { get; set; } = [];
}

/// <summary>
/// Cache health response model
/// </summary>
public class CacheHealthResponse
{
    public bool IsHealthy { get; set; }
    public int CachedUserCount { get; set; }
    public double CacheHitRatio { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}