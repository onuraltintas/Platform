using Identity.Application.Authorization.Requirements;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Identity.Application.Authorization.Handlers;

/// <summary>
/// Handles permission-based authorization using PermissionService
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
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
                _logger.LogWarning("Permission check failed: User ID not found in claims");
                context.Fail();
                return;
            }

            // Get group ID from claims if available
            var groupIdClaim = context.User.FindFirst("GroupId")?.Value;
            var groupId = requirement.GroupId;

            if (string.IsNullOrEmpty(groupIdClaim) == false && Guid.TryParse(groupIdClaim, out var claimGroupId))
            {
                groupId ??= claimGroupId;
            }

            var hasPermissionResult = await _permissionService.HasPermissionAsync(
                userId,
                requirement.Permission,
                groupId,
                requirement.Resource);

            if (!hasPermissionResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Permission service error for user {UserId}, permission {Permission}: {Error}",
                    userId, requirement.Permission, hasPermissionResult.Error);
                context.Fail();
                return;
            }

            if (hasPermissionResult.Value)
            {
                _logger.LogDebug(
                    "Permission granted for user {UserId}, permission {Permission}",
                    userId, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Permission denied for user {UserId}, permission {Permission}",
                    userId, requirement.Permission);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in permission authorization for user {UserId}, permission {Permission}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                requirement.Permission);
            context.Fail();
        }
    }
}

/// <summary>
/// Handles multiple permissions authorization
/// </summary>
public class MultiplePermissionsAuthorizationHandler : AuthorizationHandler<MultiplePermissionsRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<MultiplePermissionsAuthorizationHandler> _logger;

    public MultiplePermissionsAuthorizationHandler(
        IPermissionService permissionService,
        ILogger<MultiplePermissionsAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
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
                _logger.LogWarning("Multiple permissions check failed: User ID not found in claims");
                context.Fail();
                return;
            }

            // Get group ID from claims if available
            var groupIdClaim = context.User.FindFirst("GroupId")?.Value;
            var groupId = requirement.GroupId;

            if (string.IsNullOrEmpty(groupIdClaim) == false && Guid.TryParse(groupIdClaim, out var claimGroupId))
            {
                groupId ??= claimGroupId;
            }

            var checkResults = new List<bool>();

            foreach (var permission in requirement.Permissions)
            {
                var hasPermissionResult = await _permissionService.HasPermissionAsync(
                    userId,
                    permission,
                    groupId,
                    requirement.Resource);

                if (hasPermissionResult.IsSuccess)
                {
                    checkResults.Add(hasPermissionResult.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "Permission service error for user {UserId}, permission {Permission}: {Error}",
                        userId, permission, hasPermissionResult.Error);
                    checkResults.Add(false);
                }
            }

            bool authorized;
            if (requirement.RequireAll)
            {
                authorized = checkResults.All(result => result);
                _logger.LogDebug(
                    "Multiple permissions check (ALL required) for user {UserId}: {Granted}/{Total}",
                    userId, checkResults.Count(r => r), checkResults.Count);
            }
            else
            {
                authorized = checkResults.Any(result => result);
                _logger.LogDebug(
                    "Multiple permissions check (ANY required) for user {UserId}: {Granted}/{Total}",
                    userId, checkResults.Count(r => r), checkResults.Count);
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
                "Unexpected error in multiple permissions authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
        }
    }
}