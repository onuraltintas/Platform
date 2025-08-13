using Microsoft.AspNetCore.Authorization;

namespace EgitimPlatform.Shared.Security.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permission) : base(policy: permission)
    {
        Permission = permission;
    }
    
    public string Permission { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        Permissions = permissions;
        Policy = string.Join(",", permissions);
    }
    
    public string[] Permissions { get; }
}