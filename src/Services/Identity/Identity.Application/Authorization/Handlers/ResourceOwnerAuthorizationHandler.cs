using Identity.Application.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Claims;

namespace Identity.Application.Authorization.Handlers;

/// <summary>
/// Handles resource ownership authorization
/// </summary>
public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ResourceOwnerAuthorizationHandler> _logger;

    public ResourceOwnerAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ResourceOwnerAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Resource owner check failed: User ID not found in claims");
                context.Fail();
                return Task.CompletedTask;
            }

            // Get resource from context
            var resource = context.Resource;
            if (resource == null)
            {
                _logger.LogWarning("Resource owner check failed: Resource not provided in context");
                context.Fail();
                return Task.CompletedTask;
            }

            // Try to get owner ID from the resource using reflection
            var resourceType = resource.GetType();
            var ownerProperty = resourceType.GetProperty(requirement.OwnerIdProperty);

            if (ownerProperty == null)
            {
                _logger.LogWarning(
                    "Resource owner check failed: Property {PropertyName} not found on resource type {ResourceType}",
                    requirement.OwnerIdProperty, resourceType.Name);
                context.Fail();
                return Task.CompletedTask;
            }

            var ownerId = ownerProperty.GetValue(resource)?.ToString();
            if (string.IsNullOrEmpty(ownerId))
            {
                _logger.LogWarning(
                    "Resource owner check failed: Owner ID is null or empty for resource type {ResourceType}",
                    resourceType.Name);
                context.Fail();
                return Task.CompletedTask;
            }

            if (userId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Resource ownership confirmed for user {UserId} on resource type {ResourceType}",
                    userId, resourceType.Name);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Resource ownership denied for user {UserId} on resource owned by {OwnerId}",
                    userId, ownerId);
                context.Fail();
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in resource owner authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Handles group membership authorization
/// </summary>
public class GroupMemberAuthorizationHandler : AuthorizationHandler<GroupMemberRequirement>
{
    private readonly ILogger<GroupMemberAuthorizationHandler> _logger;

    public GroupMemberAuthorizationHandler(ILogger<GroupMemberAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupMemberRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Group membership check failed: User ID not found in claims");
                context.Fail();
                return Task.CompletedTask;
            }

            // Get user's groups from claims
            var groupClaims = context.User.FindAll("GroupId");
            var userGroupIds = groupClaims
                .Select(claim => claim.Value)
                .Where(value => Guid.TryParse(value, out _))
                .Select(value => Guid.Parse(value))
                .ToList();

            if (userGroupIds.Contains(requirement.GroupId))
            {
                _logger.LogDebug(
                    "Group membership confirmed for user {UserId} in group {GroupId}",
                    userId, requirement.GroupId);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Group membership denied for user {UserId} in group {GroupId}",
                    userId, requirement.GroupId);
                context.Fail();
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in group membership authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Handles time-based authorization
/// </summary>
public class TimeBasedAuthorizationHandler : AuthorizationHandler<TimeBasedRequirement>
{
    private readonly ILogger<TimeBasedAuthorizationHandler> _logger;

    public TimeBasedAuthorizationHandler(ILogger<TimeBasedAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TimeBasedRequirement requirement)
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentTime = now.TimeOfDay;
            var currentDay = now.DayOfWeek;

            // Check day of week if specified
            if (requirement.AllowedDays != null && requirement.AllowedDays.Length > 0)
            {
                if (!requirement.AllowedDays.Contains(currentDay))
                {
                    _logger.LogInformation(
                        "Time-based access denied: Current day {CurrentDay} not in allowed days [{AllowedDays}]",
                        currentDay, string.Join(", ", requirement.AllowedDays));
                    context.Fail();
                    return Task.CompletedTask;
                }
            }

            // Check time range if specified
            if (requirement.StartTime.HasValue && requirement.EndTime.HasValue)
            {
                var startTime = requirement.StartTime.Value;
                var endTime = requirement.EndTime.Value;

                // Handle overnight time ranges (e.g., 22:00 - 06:00)
                bool isInTimeRange;
                if (startTime > endTime)
                {
                    isInTimeRange = currentTime >= startTime || currentTime <= endTime;
                }
                else
                {
                    isInTimeRange = currentTime >= startTime && currentTime <= endTime;
                }

                if (!isInTimeRange)
                {
                    _logger.LogInformation(
                        "Time-based access denied: Current time {CurrentTime} not in allowed range {StartTime}-{EndTime}",
                        currentTime, startTime, endTime);
                    context.Fail();
                    return Task.CompletedTask;
                }
            }

            _logger.LogDebug("Time-based access granted for current time {CurrentTime} on day {CurrentDay}",
                currentTime, currentDay);
            context.Succeed(requirement);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in time-based authorization");
            context.Fail();
            return Task.CompletedTask;
        }
    }
}