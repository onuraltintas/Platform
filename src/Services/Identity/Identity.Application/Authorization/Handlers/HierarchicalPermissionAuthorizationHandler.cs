using Identity.Application.Authorization.Requirements;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Identity.Application.Authorization.Handlers;

/// <summary>
/// Advanced permission authorization handler with hierarchy and wildcard support
/// </summary>
public class HierarchicalPermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionHierarchyService _hierarchyService;
    private readonly IPermissionQueryOptimizer _queryOptimizer;
    private readonly ILogger<HierarchicalPermissionAuthorizationHandler> _logger;

    public HierarchicalPermissionAuthorizationHandler(
        IPermissionHierarchyService hierarchyService,
        IPermissionQueryOptimizer queryOptimizer,
        ILogger<HierarchicalPermissionAuthorizationHandler> logger)
    {
        _hierarchyService = hierarchyService;
        _queryOptimizer = queryOptimizer;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Hierarchical permission check failed: User ID not found in claims");
                context.Fail();
                return;
            }

            // Get group ID from claims or requirement
            var groupIdClaim = context.User.FindFirst("GroupId")?.Value;
            var groupId = requirement.GroupId;

            if (string.IsNullOrEmpty(groupIdClaim) == false && Guid.TryParse(groupIdClaim, out var claimGroupId))
            {
                groupId ??= claimGroupId;
            }

            // Use hierarchical permission check
            var hasPermission = await _hierarchyService.HasPermissionWithHierarchyAsync(
                userId,
                requirement.Permission,
                groupId);

            if (hasPermission)
            {
                _logger.LogDebug(
                    "Hierarchical permission granted for user {UserId}, permission {Permission}",
                    userId, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Hierarchical permission denied for user {UserId}, permission {Permission}",
                    userId, requirement.Permission);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in hierarchical permission authorization for user {UserId}, permission {Permission}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                requirement.Permission);
            context.Fail();
        }
    }
}

/// <summary>
/// Optimized multiple permissions handler with batch operations
/// </summary>
public class OptimizedMultiplePermissionsAuthorizationHandler : AuthorizationHandler<MultiplePermissionsRequirement>
{
    private readonly IPermissionQueryOptimizer _queryOptimizer;
    private readonly IPermissionHierarchyService _hierarchyService;
    private readonly ILogger<OptimizedMultiplePermissionsAuthorizationHandler> _logger;

    public OptimizedMultiplePermissionsAuthorizationHandler(
        IPermissionQueryOptimizer queryOptimizer,
        IPermissionHierarchyService hierarchyService,
        ILogger<OptimizedMultiplePermissionsAuthorizationHandler> logger)
    {
        _queryOptimizer = queryOptimizer;
        _hierarchyService = hierarchyService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MultiplePermissionsRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Optimized multiple permissions check failed: User ID not found in claims");
                context.Fail();
                return;
            }

            // Get group ID from claims or requirement
            var groupIdClaim = context.User.FindFirst("GroupId")?.Value;
            var groupId = requirement.GroupId;

            if (string.IsNullOrEmpty(groupIdClaim) == false && Guid.TryParse(groupIdClaim, out var claimGroupId))
            {
                groupId ??= claimGroupId;
            }

            // Use optimized batch check
            var checkResults = await _queryOptimizer.CheckMultiplePermissionsAsync(
                userId,
                requirement.Permissions,
                groupId);

            // If batch check doesn't cover all permissions, fall back to hierarchy service
            var uncheckedPermissions = requirement.Permissions
                .Where(p => !checkResults.ContainsKey(p))
                .ToList();

            if (uncheckedPermissions.Any())
            {
                _logger.LogDebug(
                    "Falling back to hierarchy service for {Count} unchecked permissions",
                    uncheckedPermissions.Count);

                foreach (var permission in uncheckedPermissions)
                {
                    var hasPermission = await _hierarchyService.HasPermissionWithHierarchyAsync(
                        userId, permission, groupId);
                    checkResults[permission] = hasPermission;
                }
            }

            bool authorized;
            if (requirement.RequireAll)
            {
                authorized = checkResults.Values.All(result => result);
                _logger.LogDebug(
                    "Optimized multiple permissions check (ALL required) for user {UserId}: {Granted}/{Total}",
                    userId, checkResults.Count(r => r.Value), checkResults.Count);
            }
            else
            {
                authorized = checkResults.Values.Any(result => result);
                _logger.LogDebug(
                    "Optimized multiple permissions check (ANY required) for user {UserId}: {Granted}/{Total}",
                    userId, checkResults.Count(r => r.Value), checkResults.Count);
            }

            if (authorized)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in optimized multiple permissions authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
        }
    }
}

/// <summary>
/// Conditional permission handler with expression evaluation
/// </summary>
public class ConditionalPermissionAuthorizationHandler : AuthorizationHandler<ConditionalRequirement>
{
    private readonly IPermissionHierarchyService _hierarchyService;
    private readonly ILogger<ConditionalPermissionAuthorizationHandler> _logger;

    public ConditionalPermissionAuthorizationHandler(
        IPermissionHierarchyService hierarchyService,
        ILogger<ConditionalPermissionAuthorizationHandler> logger)
    {
        _hierarchyService = hierarchyService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConditionalRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Conditional permission check failed: User ID not found in claims");
                context.Fail();
                return;
            }

            // Simple expression evaluation - in production, you might use a proper expression engine
            var isConditionMet = await EvaluateConditionAsync(
                requirement.ConditionExpression,
                requirement.Parameters,
                context,
                userId);

            if (isConditionMet)
            {
                _logger.LogDebug(
                    "Conditional permission granted for user {UserId}, condition: {Condition}",
                    userId, requirement.ConditionExpression);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Conditional permission denied for user {UserId}, condition: {Condition}",
                    userId, requirement.ConditionExpression);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in conditional permission authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
        }
    }

    private async Task<bool> EvaluateConditionAsync(
        string conditionExpression,
        Dictionary<string, object> parameters,
        AuthorizationHandlerContext context,
        string userId)
    {
        try
        {
            // Simple condition evaluation examples:
            // "hasPermission:users.read" - Check if user has specific permission
            // "timeRange:09:00-17:00" - Check if current time is within range
            // "userProperty:Department=IT" - Check user property
            // "custom:MyCustomCondition" - Custom condition logic

            if (conditionExpression.StartsWith("hasPermission:"))
            {
                var permission = conditionExpression.Substring("hasPermission:".Length);
                return await _hierarchyService.HasPermissionWithHierarchyAsync(userId, permission);
            }

            if (conditionExpression.StartsWith("timeRange:"))
            {
                var timeRange = conditionExpression.Substring("timeRange:".Length);
                return IsWithinTimeRange(timeRange);
            }

            if (conditionExpression.StartsWith("userProperty:"))
            {
                var propertyCheck = conditionExpression.Substring("userProperty:".Length);
                return CheckUserProperty(context, propertyCheck);
            }

            if (conditionExpression.StartsWith("custom:"))
            {
                var customCondition = conditionExpression.Substring("custom:".Length);
                return await EvaluateCustomConditionAsync(customCondition, parameters, context, userId);
            }

            _logger.LogWarning("Unknown condition expression format: {Expression}", conditionExpression);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition: {Expression}", conditionExpression);
            return false;
        }
    }

    private bool IsWithinTimeRange(string timeRange)
    {
        try
        {
            var parts = timeRange.Split('-');
            if (parts.Length != 2) return false;

            if (TimeSpan.TryParse(parts[0], out var startTime) &&
                TimeSpan.TryParse(parts[1], out var endTime))
            {
                var now = DateTime.Now.TimeOfDay;
                return now >= startTime && now <= endTime;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckUserProperty(AuthorizationHandlerContext context, string propertyCheck)
    {
        try
        {
            var parts = propertyCheck.Split('=');
            if (parts.Length != 2) return false;

            var claimType = parts[0];
            var expectedValue = parts[1];

            var claimValue = context.User.FindFirst(claimType)?.Value;
            return string.Equals(claimValue, expectedValue, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EvaluateCustomConditionAsync(
        string customCondition,
        Dictionary<string, object> parameters,
        AuthorizationHandlerContext context,
        string userId)
    {
        // Implement custom condition logic here
        // This could integrate with business rule engines, external services, etc.

        await Task.Delay(1); // Placeholder for async operation

        return customCondition switch
        {
            "IsBusinessHours" => IsWithinTimeRange("09:00-17:00"),
            "IsWeekday" => DateTime.Now.DayOfWeek >= DayOfWeek.Monday && DateTime.Now.DayOfWeek <= DayOfWeek.Friday,
            _ => false
        };
    }
}