using Gateway.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gateway.API.Controllers;

/// <summary>
/// Gateway authorization management controller
/// </summary>
[ApiController]
[Route("api/gateway/authorization")]
[Authorize(Roles = "SuperAdmin")]
public class AuthorizationController : ControllerBase
{
    private readonly IGatewayPermissionService _permissionService;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IConfiguration _configuration;

    public AuthorizationController(
        IGatewayPermissionService permissionService,
        ILogger<AuthorizationController> logger,
        IConfiguration configuration)
    {
        _permissionService = permissionService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get current route permission configurations
    /// </summary>
    [HttpGet("routes")]
    public async Task<ActionResult<Dictionary<string, RoutePermissionConfig>>> GetRoutePermissions()
    {
        try
        {
            var routes = _permissionService.GetRoutePermissions();
            return Ok(new { data = routes, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route permissions");
            return StatusCode(500, new { error = "Failed to get route permissions" });
        }
    }

    /// <summary>
    /// Update route permission configuration
    /// </summary>
    [HttpPut("routes/{*route}")]
    public async Task<IActionResult> UpdateRoutePermission(string route, [FromBody] RoutePermissionConfig config)
    {
        try
        {
            await _permissionService.UpdateRoutePermissionAsync(route, config);

            _logger.LogInformation("Updated route permission configuration for {Route} by {User}",
                route, User.FindFirst(ClaimTypes.Name)?.Value);

            return Ok(new { message = "Route permission updated successfully", route, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating route permission for {Route}", route);
            return StatusCode(500, new { error = "Failed to update route permission" });
        }
    }

    /// <summary>
    /// Test permission for a specific user and route
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<PermissionCheckResult>> TestPermission([FromBody] PermissionTestRequest request)
    {
        try
        {
            // Create a ClaimsPrincipal for testing
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, request.UserId),
                new(ClaimTypes.Name, request.UserName ?? request.UserId)
            };

            if (request.Roles != null)
            {
                claims.AddRange(request.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var hasPermission = await _permissionService.HasPermissionAsync(
                principal, request.Route, request.Method);

            var result = new PermissionCheckResult
            {
                IsAllowed = hasPermission,
                UserId = request.UserId,
                Route = request.Route,
                Method = request.Method,
                CheckedAt = DateTime.UtcNow,
                Reason = hasPermission ? "Permission granted" : "Permission denied"
            };

            return Ok(new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing permission for user {UserId} route {Route}",
                request.UserId, request.Route);
            return StatusCode(500, new { error = "Failed to test permission" });
        }
    }

    /// <summary>
    /// Get user permissions
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    public async Task<ActionResult<HashSet<string>>> GetUserPermissions(string userId)
    {
        try
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId);
            return Ok(new { data = permissions, userId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get user permissions" });
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
            await _permissionService.InvalidateUserPermissionsAsync(userId);

            _logger.LogInformation("Invalidated permissions cache for user {UserId} by {AdminUser}",
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
    /// Internal endpoint to invalidate user permissions cache via API key (service-to-service)
    /// </summary>
    [HttpDelete("users/{userId}/cache/internal")]
    [AllowAnonymous]
    public async Task<IActionResult> InvalidateUserPermissionsInternal(string userId)
    {
        try
        {
            var providedKey = Request.Headers["X-Internal-API-Key"].FirstOrDefault();
            var expectedKey = _configuration["GATEWAY_INTERNAL_API_KEY"] ?? Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_API_KEY");

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                return Unauthorized(new { error = "Invalid internal API key" });
            }

            await _permissionService.InvalidateUserPermissionsAsync(userId);

            _logger.LogInformation("[Internal] Invalidated permissions cache for user {UserId}", userId);

            return Ok(new { message = "User permissions cache invalidated", userId, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating permissions cache for user {UserId} (internal)", userId);
            return StatusCode(500, new { error = "Failed to invalidate user permissions cache" });
        }
    }

    /// <summary>
    /// Internal bulk invalidation via API key
    /// </summary>
    [HttpPost("users/bulk-invalidate/internal")]
    [AllowAnonymous]
    public async Task<IActionResult> BulkInvalidateInternal([FromBody] BulkInvalidateRequest request)
    {
        try
        {
            var providedKey = Request.Headers["X-Internal-API-Key"].FirstOrDefault();
            var expectedKey = _configuration["GATEWAY_INTERNAL_API_KEY"] ?? Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_API_KEY");

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                return Unauthorized(new { error = "Invalid internal API key" });
            }

            if (request.UserIds == null || !request.UserIds.Any())
            {
                return BadRequest(new { error = "UserIds cannot be empty" });
            }

            foreach (var userId in request.UserIds)
            {
                await _permissionService.InvalidateUserPermissionsAsync(userId);
            }

            _logger.LogInformation("[Internal] Bulk invalidated permissions cache for {Count} users", request.UserIds.Count());

            return Ok(new { message = "Bulk invalidation completed", count = request.UserIds.Count(), timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal bulk invalidation");
            return StatusCode(500, new { error = "Failed to bulk invalidate user permissions cache" });
        }
    }

    public class BulkInvalidateRequest
    {
        public IEnumerable<string> UserIds { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Get gateway authorization statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<GatewayAuthorizationStats>> GetAuthorizationStatistics()
    {
        try
        {
            // This would typically come from a metrics service
            // For now, return basic statistics
            var stats = new GatewayAuthorizationStats
            {
                TotalRoutes = _permissionService.GetRoutePermissions().Count,
                ProtectedRoutes = _permissionService.GetRoutePermissions().Values.Count(r => r.RequireAuthentication),
                AnonymousRoutes = _permissionService.GetRoutePermissions().Values.Count(r => r.AllowAnonymous),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new { data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authorization statistics");
            return StatusCode(500, new { error = "Failed to get authorization statistics" });
        }
    }

    /// <summary>
    /// Health check for authorization system
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "Gateway Authorization",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

/// <summary>
/// Permission test request model
/// </summary>
public class PermissionTestRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string[]? Roles { get; set; }
}

/// <summary>
/// Gateway authorization statistics
/// </summary>
public class GatewayAuthorizationStats
{
    public int TotalRoutes { get; set; }
    public int ProtectedRoutes { get; set; }
    public int AnonymousRoutes { get; set; }
    public DateTime LastUpdated { get; set; }
}