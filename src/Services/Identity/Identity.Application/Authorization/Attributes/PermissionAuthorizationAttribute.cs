using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Authorization.Attributes;

/// <summary>
/// Modern permission-based authorization attribute
/// Uses consistent permission format and integrates with the DynamicPolicyProvider
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        Policy = $"permission:{permission}";
    }
}

/// <summary>
/// Requires any of the specified permissions (OR logic)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public string[] Permissions { get; }

    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        Permissions = permissions;
        Policy = $"permissions:{string.Join(",", permissions)}";
    }
}

/// <summary>
/// Requires all of the specified permissions (AND logic)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAllPermissionsAttribute : AuthorizeAttribute
{
    public string[] Permissions { get; }

    public RequireAllPermissionsAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        Permissions = permissions;
        Policy = $"permissions-all:{string.Join(",", permissions)}";
    }
}

/// <summary>
/// SuperAdmin-only access
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SuperAdminOnlyAttribute : AuthorizeAttribute
{
    public SuperAdminOnlyAttribute()
    {
        Policy = "role:SuperAdmin";
    }
}

/// <summary>
/// Admin-level access (Admin or SuperAdmin)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute()
    {
        Policy = "role:SuperAdmin,Admin";
    }
}

/// <summary>
/// Public endpoint - no authentication required
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicEndpointAttribute : Attribute
{
    // This is just a marker attribute - will be handled by skipping authorization
}

/// <summary>
/// Self-service only - users can only access their own resources
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SelfServiceOnlyAttribute : AuthorizeAttribute
{
    public string OwnerProperty { get; set; } = "UserId";

    public SelfServiceOnlyAttribute()
    {
        Policy = $"owner:User:property={OwnerProperty}";
    }
}

/// <summary>
/// Development/Debug only - only active in Development environment
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DevelopmentOnlyAttribute : AuthorizeAttribute
{
    public DevelopmentOnlyAttribute()
    {
        Policy = "environment:Development";
    }
}