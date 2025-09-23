using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Authorization.Handlers;

/// <summary>
/// Authorization handler that automatically approves all requests for SuperAdmin users
/// This provides a global bypass for SuperAdmin role, ensuring they have access to everything
/// </summary>
public class SuperAdminBypassHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private readonly ILogger<SuperAdminBypassHandler> _logger;

    public SuperAdminBypassHandler(ILogger<SuperAdminBypassHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        // Check if user has SuperAdmin role
        if (context.User.IsInRole("SuperAdmin"))
        {
            _logger.LogDebug("SuperAdmin bypass activated for requirement: {Requirement}",
                requirement.GetType().Name);

            // SuperAdmin bypasses all authorization requirements
            context.Succeed(requirement);
        }

        // If not SuperAdmin, let other handlers process the requirement
        return Task.CompletedTask;
    }
}