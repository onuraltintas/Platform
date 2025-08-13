using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Security.Models;

namespace EgitimPlatform.Shared.Security.Authorization;

public class CategoryAuthorizationHandler : AuthorizationHandler<CategoryRequirement>
{
    private readonly ILogger<CategoryAuthorizationHandler> _logger;
    
    public CategoryAuthorizationHandler(ILogger<CategoryAuthorizationHandler> logger)
    {
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CategoryRequirement requirement)
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
            
            if (user.HasCategory(requirement.Category))
            {
                _logger.LogDebug("Category {Category} granted to user {UserId}", requirement.Category, user.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Category {Category} denied for user {UserId}", requirement.Category, user.Id);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during category authorization for requirement {Category}", requirement.Category);
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}

public class CategoryRequirement : IAuthorizationRequirement
{
    public CategoryRequirement(string category)
    {
        Category = category;
    }
    
    public string Category { get; }
}

public class AnyCategoryAuthorizationHandler : AuthorizationHandler<AnyCategoryRequirement>
{
    private readonly ILogger<AnyCategoryAuthorizationHandler> _logger;
    
    public AnyCategoryAuthorizationHandler(ILogger<AnyCategoryAuthorizationHandler> logger)
    {
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyCategoryRequirement requirement)
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
            
            if (user.HasAnyCategory(requirement.Categories))
            {
                _logger.LogDebug("One of categories {Categories} granted to user {UserId}", string.Join(", ", requirement.Categories), user.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("None of categories {Categories} granted for user {UserId}", string.Join(", ", requirement.Categories), user.Id);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during any category authorization for requirements {Categories}", string.Join(", ", requirement.Categories));
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}

public class AnyCategoryRequirement : IAuthorizationRequirement
{
    public AnyCategoryRequirement(params string[] categories)
    {
        Categories = categories;
    }
    
    public string[] Categories { get; }
}

public class RoleAndCategoryAuthorizationHandler : AuthorizationHandler<RoleAndCategoryRequirement>
{
    private readonly ILogger<RoleAndCategoryAuthorizationHandler> _logger;
    
    public RoleAndCategoryAuthorizationHandler(ILogger<RoleAndCategoryAuthorizationHandler> logger)
    {
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleAndCategoryRequirement requirement)
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
            
            if (user.HasRoleAndCategory(requirement.Role, requirement.Category))
            {
                _logger.LogDebug("Role {Role} and Category {Category} granted to user {UserId}", requirement.Role, requirement.Category, user.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Role {Role} and Category {Category} denied for user {UserId}", requirement.Role, requirement.Category, user.Id);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during role and category authorization for role {Role} and category {Category}", requirement.Role, requirement.Category);
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}

public class RoleAndCategoryRequirement : IAuthorizationRequirement
{
    public RoleAndCategoryRequirement(string role, string category)
    {
        Role = role;
        Category = category;
    }
    
    public string Role { get; }
    public string Category { get; }
}