using Microsoft.AspNetCore.Authorization;

// Forward declaration - UserGroupRole is defined in Identity.Core.Entities

namespace Enterprise.Shared.Authorization.Attributes;

/// <summary>
/// Requires a specific permission for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required permission name (e.g., "users.read", "documents.write")
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Optional resource context (e.g., "users", "documents")
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Optional group ID for multi-tenant scenarios
    /// </summary>
    public string? GroupId { get; set; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));

        // Build the policy name dynamically
        var policyParts = new List<string> { "permission", permission };

        if (!string.IsNullOrEmpty(Resource))
            policyParts.Add($"resource={Resource}");

        if (!string.IsNullOrEmpty(GroupId))
            policyParts.Add($"group={GroupId}");

        Policy = string.Join(":", policyParts);
    }
}

/// <summary>
/// Requires any of the specified permissions for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required permission names
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Optional resource context
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Optional group ID for multi-tenant scenarios
    /// </summary>
    public string? GroupId { get; set; }

    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        Permissions = permissions;

        // Build the policy name dynamically
        var policyParts = new List<string> { "permissions", string.Join(",", permissions) };

        if (!string.IsNullOrEmpty(Resource))
            policyParts.Add($"resource={Resource}");

        if (!string.IsNullOrEmpty(GroupId))
            policyParts.Add($"group={GroupId}");

        Policy = string.Join(":", policyParts);
    }
}

/// <summary>
/// Requires all of the specified permissions for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAllPermissionsAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required permission names (all must be present)
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Optional resource context
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Optional group ID for multi-tenant scenarios
    /// </summary>
    public string? GroupId { get; set; }

    public RequireAllPermissionsAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        Permissions = permissions;

        // Build the policy name dynamically
        var policyParts = new List<string> { "permissions-all", string.Join(",", permissions) };

        if (!string.IsNullOrEmpty(Resource))
            policyParts.Add($"resource={Resource}");

        if (!string.IsNullOrEmpty(GroupId))
            policyParts.Add($"group={GroupId}");

        Policy = string.Join(":", policyParts);
    }
}

/// <summary>
/// Requires group membership for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireGroupMembershipAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required group ID
    /// </summary>
    public Guid GroupId { get; }

    public RequireGroupMembershipAttribute(string groupId)
    {
        if (!Guid.TryParse(groupId, out var guid))
            throw new ArgumentException("Invalid group ID format", nameof(groupId));

        GroupId = guid;
        Policy = $"group:{groupId}";
    }
}

/// <summary>
/// Requires specific group role for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireGroupRoleAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required group ID
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// Required minimum role in the group
    /// </summary>
    public int MinimumRole { get; }

    public RequireGroupRoleAttribute(string groupId, int minimumRole)
    {
        if (!Guid.TryParse(groupId, out var guid))
            throw new ArgumentException("Invalid group ID format", nameof(groupId));

        GroupId = guid;
        MinimumRole = minimumRole;
        Policy = $"group-role:{groupId}:{minimumRole}";
    }

    public RequireGroupRoleAttribute(string groupId, string minimumRole)
    {
        if (!Guid.TryParse(groupId, out var guid))
            throw new ArgumentException("Invalid group ID format", nameof(groupId));

        // Parse role name to enum value (1=Member, 2=Moderator, 3=Admin, 4=Owner)
        var roleValue = minimumRole.ToLower() switch
        {
            "member" => 1,
            "moderator" => 2,
            "admin" => 3,
            "owner" => 4,
            _ => throw new ArgumentException("Invalid role name. Valid values: Member, Moderator, Admin, Owner", nameof(minimumRole))
        };

        GroupId = guid;
        MinimumRole = roleValue;
        Policy = $"group-role:{groupId}:{MinimumRole}";
    }
}

/// <summary>
/// Requires group admin role for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireGroupAdminAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required group ID (optional - if null, requires admin in any group)
    /// </summary>
    public Guid? GroupId { get; }

    public RequireGroupAdminAttribute(string? groupId = null)
    {
        if (!string.IsNullOrEmpty(groupId))
        {
            if (!Guid.TryParse(groupId, out var guid))
                throw new ArgumentException("Invalid group ID format", nameof(groupId));
            GroupId = guid;
            Policy = $"group-admin:{groupId}";
        }
        else
        {
            GroupId = null;
            Policy = "group-admin";
        }
    }
}

/// <summary>
/// Requires permission within group context
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireGroupPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required permission name
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Required group ID (if null, checks user's default group)
    /// </summary>
    public Guid? GroupId { get; }

    public RequireGroupPermissionAttribute(string permission, string? groupId = null)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));

        if (!string.IsNullOrEmpty(groupId))
        {
            if (!Guid.TryParse(groupId, out var guid))
                throw new ArgumentException("Invalid group ID format", nameof(groupId));
            GroupId = guid;
            Policy = $"group-permission:{permission}:{groupId}";
        }
        else
        {
            GroupId = null;
            Policy = $"group-permission:{permission}";
        }
    }
}

// Note: UserGroupRole enum is defined in Identity.Core.Entities namespace

/// <summary>
/// Requires resource ownership for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireOwnershipAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Resource type name
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// Property name that contains the owner ID (default: "UserId")
    /// </summary>
    public string OwnerProperty { get; set; } = "UserId";

    public RequireOwnershipAttribute(string resourceType)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));

        var policyParts = new List<string> { "owner", resourceType };

        if (OwnerProperty != "UserId")
            policyParts.Add($"property={OwnerProperty}");

        Policy = string.Join(":", policyParts);
    }
}

/// <summary>
/// Requires time-based conditions for authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireBusinessHoursAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Start time (24-hour format, e.g., "09:00")
    /// </summary>
    public string? StartTime { get; set; }

    /// <summary>
    /// End time (24-hour format, e.g., "17:00")
    /// </summary>
    public string? EndTime { get; set; }

    /// <summary>
    /// Allowed days of week (comma-separated, e.g., "Monday,Tuesday,Wednesday,Thursday,Friday")
    /// </summary>
    public string? AllowedDays { get; set; }

    public RequireBusinessHoursAttribute()
    {
        // Default business hours
        StartTime = "09:00";
        EndTime = "17:00";
        AllowedDays = "Monday,Tuesday,Wednesday,Thursday,Friday";

        UpdatePolicy();
    }

    public RequireBusinessHoursAttribute(string startTime, string endTime, string? allowedDays = null)
    {
        StartTime = startTime;
        EndTime = endTime;
        AllowedDays = allowedDays ?? "Monday,Tuesday,Wednesday,Thursday,Friday";

        UpdatePolicy();
    }

    private void UpdatePolicy()
    {
        var policyParts = new List<string> { "time" };

        if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime))
            policyParts.Add($"{StartTime}-{EndTime}");
        else
            policyParts.Add("");

        if (!string.IsNullOrEmpty(AllowedDays))
            policyParts.Add($"days={AllowedDays}");

        Policy = string.Join(":", policyParts);
    }
}

/// <summary>
/// Combines multiple authorization requirements with AND logic
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireComplexAuthorizationAttribute : AuthorizeAttribute
{
    public RequireComplexAuthorizationAttribute(string policy)
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }
}

/// <summary>
/// Pre-defined policy attributes for common scenarios
/// </summary>
public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute() => Policy = "role:Admin";
}

public class UserManagementAttribute : RequireAllPermissionsAttribute
{
    public UserManagementAttribute() : base("users.read", "users.write") { }
}

public class ReadOnlyAttribute : RequirePermissionAttribute
{
    public ReadOnlyAttribute(string resource) : base($"{resource}.read")
    {
        Resource = resource;
    }
}