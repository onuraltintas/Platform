using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enterprise.Shared.Authorization.Attributes;
using Identity.Core.Interfaces;
using Identity.Application.Services;
using System.Security.Claims;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/test-authorization")]
[Authorize]
public class TestAuthorizationController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IPermissionHierarchyService _hierarchyService;
    private readonly IPermissionQueryOptimizer _queryOptimizer;
    private readonly ILogger<TestAuthorizationController> _logger;

    public TestAuthorizationController(
        IPermissionService permissionService,
        IPermissionHierarchyService hierarchyService,
        IPermissionQueryOptimizer queryOptimizer,
        ILogger<TestAuthorizationController> logger)
    {
        _permissionService = permissionService;
        _hierarchyService = hierarchyService;
        _queryOptimizer = queryOptimizer;
        _logger = logger;
    }

    [HttpGet("simple-permission")]
    [RequirePermission("test.read")]
    public IActionResult TestSimplePermission()
    {
        return Ok(new { message = "Simple permission test passed", permission = "test.read" });
    }

    [HttpGet("multiple-permissions")]
    [RequireAllPermissions("test.read", "test.write")]
    public IActionResult TestMultiplePermissions()
    {
        return Ok(new { message = "Multiple permissions test passed", permissions = new[] { "test.read", "test.write" } });
    }

    [HttpGet("wildcard-permission")]
    [RequirePermission("admin.*")]
    public IActionResult TestWildcardPermission()
    {
        return Ok(new { message = "Wildcard permission test passed", pattern = "admin.*" });
    }

    [HttpGet("dynamic-policy")]
    [Authorize(Policy = "permission:user.manage")]
    public IActionResult TestDynamicPolicy()
    {
        return Ok(new { message = "Dynamic policy test passed", policy = "permission:user.manage" });
    }

    [HttpGet("group-specific")]
    [RequireGroupMembership("550e8400-e29b-41d4-a716-446655440000")]
    public IActionResult TestGroupSpecificPermission()
    {
        return Ok(new { message = "Group-specific permission test passed", permission = "group.admin" });
    }

    [HttpGet("user-permissions")]
    public async Task<IActionResult> GetUserPermissions()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var permissionsResult = await _permissionService.GetUserPermissionNamesAsync(userId);
            if (!permissionsResult.IsSuccess)
            {
                return BadRequest(permissionsResult.Error);
            }

            var effectivePermissions = await _hierarchyService.GetEffectivePermissionsAsync(userId);

            return Ok(new
            {
                userId = userId,
                directPermissions = permissionsResult.Value,
                effectivePermissions = effectivePermissions,
                permissionCount = permissionsResult.Value.Count(),
                effectivePermissionCount = effectivePermissions.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions for {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("check-permission")]
    public async Task<IActionResult> CheckPermission([FromBody] CheckPermissionRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var hasPermission = await _hierarchyService.HasPermissionWithHierarchyAsync(
                userId, request.Permission, request.GroupId);

            var isWildcardMatch = _hierarchyService.IsWildcardMatch(request.WildcardPattern ?? "*", request.Permission);

            return Ok(new
            {
                userId = userId,
                permission = request.Permission,
                hasPermission = hasPermission,
                wildcardPattern = request.WildcardPattern,
                isWildcardMatch = isWildcardMatch,
                groupId = request.GroupId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", request.Permission, userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("check-multiple-permissions")]
    public async Task<IActionResult> CheckMultiplePermissions([FromBody] CheckMultiplePermissionsRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var permissionResults = await _queryOptimizer.CheckMultiplePermissionsAsync(
                userId, request.Permissions, request.GroupId);

            return Ok(new
            {
                userId = userId,
                groupId = request.GroupId,
                permissionResults = permissionResults,
                grantedCount = permissionResults.Count(p => p.Value),
                deniedCount = permissionResults.Count(p => !p.Value)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking multiple permissions for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("performance-test")]
    [AdminOnly]
    public async Task<IActionResult> PerformanceTest()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var testPermissions = new[]
            {
                "user.read", "user.write", "user.delete",
                "admin.read", "admin.write", "admin.delete",
                "group.manage", "permission.manage",
                "system.monitor", "system.configure"
            };

            var multiplePermissionsResult = await _queryOptimizer.CheckMultiplePermissionsAsync(
                userId, testPermissions);

            stopwatch.Stop();

            return Ok(new
            {
                userId = userId,
                testPermissions = testPermissions,
                results = multiplePermissionsResult,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                averageTimePerPermissionMs = (double)stopwatch.ElapsedMilliseconds / testPermissions.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance test for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

public class CheckPermissionRequest
{
    public string Permission { get; set; } = string.Empty;
    public string? WildcardPattern { get; set; }
    public Guid? GroupId { get; set; }
}

public class CheckMultiplePermissionsRequest
{
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
    public Guid? GroupId { get; set; }
}