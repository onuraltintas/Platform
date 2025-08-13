using Microsoft.AspNetCore.Authorization;

namespace EgitimPlatform.Shared.Security.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class CategoryAttribute : AuthorizeAttribute
{
    public CategoryAttribute(string category) : base(policy: $"Category:{category}")
    {
        Category = category;
    }
    
    public string Category { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAnyCategoryAttribute : AuthorizeAttribute
{
    public RequireAnyCategoryAttribute(params string[] categories)
    {
        Categories = categories;
        Policy = $"AnyCategory:{string.Join(",", categories)}";
    }
    
    public string[] Categories { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAndCategoryAttribute : AuthorizeAttribute
{
    public RequireRoleAndCategoryAttribute(string role, string category)
    {
        Role = role;
        Category = category;
        Policy = $"RoleAndCategory:{role}:{category}";
    }
    
    public string Role { get; }
    public string Category { get; }
}