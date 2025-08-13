using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Security.Models;

namespace EgitimPlatform.Shared.Security.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;
    
    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Access denied: User is not authenticated");
                context.Fail();
                return Task.CompletedTask;
            }

            var user = SecurityUser.FromClaims(context.User);
            
            // Check if user data is valid
            if (string.IsNullOrEmpty(user.Id))
            {
                _logger.LogWarning("Access denied: Invalid user claims - missing user ID");
                context.Fail();
                return Task.CompletedTask;
            }
            
            if (!user.IsActive)
            {
                _logger.LogWarning("Access denied for inactive user {UserId}", user.Id);
                context.Fail();
                return Task.CompletedTask;
            }
            
            if (user.HasPermission(requirement.Permission))
            {
                _logger.LogDebug("Permission {Permission} granted to user {UserId}", requirement.Permission, user.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Permission {Permission} denied for user {UserId}", requirement.Permission, user.Id);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during permission authorization for requirement {Permission}", requirement.Permission);
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
    
    public string Permission { get; }
}

public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly ILogger<AnyPermissionAuthorizationHandler> _logger;
    
    public AnyPermissionAuthorizationHandler(ILogger<AnyPermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPermissionRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Access denied: User is not authenticated");
                context.Fail();
                return Task.CompletedTask;
            }

            var user = SecurityUser.FromClaims(context.User);
            
            // Check if user data is valid
            if (string.IsNullOrEmpty(user.Id))
            {
                _logger.LogWarning("Access denied: Invalid user claims - missing user ID");
                context.Fail();
                return Task.CompletedTask;
            }
            
            if (!user.IsActive)
            {
                _logger.LogWarning("Access denied for inactive user {UserId}", user.Id);
                context.Fail();
                return Task.CompletedTask;
            }
            
            if (user.HasAnyPermission(requirement.Permissions))
            {
                _logger.LogDebug("One of permissions {Permissions} granted to user {UserId}", string.Join(", ", requirement.Permissions), user.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("None of permissions {Permissions} granted for user {UserId}", string.Join(", ", requirement.Permissions), user.Id);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during any permission authorization for requirements {Permissions}", string.Join(", ", requirement.Permissions));
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}

public class AnyPermissionRequirement : IAuthorizationRequirement
{
    public AnyPermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
    
    public string[] Permissions { get; }
}