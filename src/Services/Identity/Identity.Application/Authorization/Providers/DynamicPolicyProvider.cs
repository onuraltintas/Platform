using Identity.Application.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Authorization.Providers;

/// <summary>
/// Dynamic policy provider that creates policies on demand
/// Supports various policy formats:
/// - "permission:resource.action" -> Single permission requirement
/// - "permissions:resource.read,resource.write" -> Multiple permissions (ANY)
/// - "permissions-all:resource.read,resource.write" -> Multiple permissions (ALL)
/// - "group:guid" -> Group membership requirement
/// - "owner:resourceType" -> Resource ownership requirement
/// - "time:weekdays:09:00-17:00" -> Time-based requirement
/// </summary>
public class DynamicPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _defaultProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DynamicPolicyProvider> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public DynamicPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IMemoryCache cache,
        ILogger<DynamicPolicyProvider> logger)
    {
        _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
        _cache = cache;
        _logger = logger;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _defaultProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _defaultProvider.GetFallbackPolicyAsync();
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check cache first
        var cacheKey = $"policy:{policyName}";
        if (_cache.TryGetValue<AuthorizationPolicy>(cacheKey, out var cachedPolicy))
        {
            _logger.LogDebug("Policy {PolicyName} retrieved from cache", policyName);
            return cachedPolicy;
        }

        // Try default provider first
        var defaultPolicy = await _defaultProvider.GetPolicyAsync(policyName);
        if (defaultPolicy != null)
        {
            return defaultPolicy;
        }

        // Parse dynamic policy
        var dynamicPolicy = ParseDynamicPolicy(policyName);
        if (dynamicPolicy != null)
        {
            _cache.Set(cacheKey, dynamicPolicy, _cacheExpiration);
            _logger.LogInformation("Created dynamic policy: {PolicyName}", policyName);
        }

        return dynamicPolicy;
    }

    private AuthorizationPolicy? ParseDynamicPolicy(string policyName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(policyName))
                return null;

            var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var policyType = parts[0].ToLowerInvariant();
            var policyValue = parts[1];

            var policyBuilder = new AuthorizationPolicyBuilder();

            switch (policyType)
            {
                case "permission":
                    return CreatePermissionPolicy(policyBuilder, policyValue, parts.Skip(2).ToArray());

                case "permissions":
                    return CreateMultiplePermissionsPolicy(policyBuilder, policyValue, requireAll: false, parts.Skip(2).ToArray());

                case "permissions-all":
                    return CreateMultiplePermissionsPolicy(policyBuilder, policyValue, requireAll: true, parts.Skip(2).ToArray());

                case "group":
                    return CreateGroupPolicy(policyBuilder, policyValue);

                case "group-permission":
                    return CreateGroupPermissionPolicy(policyBuilder, policyValue, parts.Skip(2).ToArray());

                case "owner":
                    return CreateOwnerPolicy(policyBuilder, policyValue, parts.Skip(2).ToArray());

                case "time":
                    return CreateTimeBasedPolicy(policyBuilder, policyValue, parts.Skip(2).ToArray());

                case "role":
                    return CreateRolePolicy(policyBuilder, policyValue);

                default:
                    _logger.LogWarning("Unknown dynamic policy type: {PolicyType}", policyType);
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing dynamic policy: {PolicyName}", policyName);
            return null;
        }
    }

    private AuthorizationPolicy CreatePermissionPolicy(
        AuthorizationPolicyBuilder builder,
        string permission,
        string[] additionalParams)
    {
        string? resource = null;
        Guid? groupId = null;

        // Parse additional parameters
        foreach (var param in additionalParams)
        {
            if (param.StartsWith("resource="))
            {
                resource = param.Substring(9);
            }
            else if (param.StartsWith("group=") && Guid.TryParse(param.Substring(6), out var gId))
            {
                groupId = gId;
            }
        }

        var requirement = new PermissionRequirement(permission, resource, groupId);
        return builder.AddRequirements(requirement).Build();
    }

    private AuthorizationPolicy CreateMultiplePermissionsPolicy(
        AuthorizationPolicyBuilder builder,
        string permissions,
        bool requireAll,
        string[] additionalParams)
    {
        var permissionList = permissions.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (permissionList.Length == 0)
            throw new ArgumentException("No permissions specified", nameof(permissions));

        string? resource = null;
        Guid? groupId = null;

        // Parse additional parameters
        foreach (var param in additionalParams)
        {
            if (param.StartsWith("resource="))
            {
                resource = param.Substring(9);
            }
            else if (param.StartsWith("group=") && Guid.TryParse(param.Substring(6), out var gId))
            {
                groupId = gId;
            }
        }

        var requirement = new MultiplePermissionsRequirement(permissionList, resource, groupId, requireAll);
        return builder.AddRequirements(requirement).Build();
    }

    private AuthorizationPolicy CreateGroupPolicy(AuthorizationPolicyBuilder builder, string groupIdStr)
    {
        if (!Guid.TryParse(groupIdStr, out var groupId))
            throw new ArgumentException($"Invalid group ID: {groupIdStr}", nameof(groupIdStr));

        var requirement = new GroupMemberRequirement(groupId);
        return builder.AddRequirements(requirement).Build();
    }

    private AuthorizationPolicy CreateOwnerPolicy(
        AuthorizationPolicyBuilder builder,
        string resourceType,
        string[] additionalParams)
    {
        var ownerProperty = "UserId";

        // Parse additional parameters
        foreach (var param in additionalParams)
        {
            if (param.StartsWith("property="))
            {
                ownerProperty = param.Substring(9);
            }
        }

        var requirement = new ResourceOwnerRequirement(resourceType, ownerProperty);
        return builder.AddRequirements(requirement).Build();
    }

    private AuthorizationPolicy CreateTimeBasedPolicy(
        AuthorizationPolicyBuilder builder,
        string timeSpec,
        string[] additionalParams)
    {
        TimeSpan? startTime = null;
        TimeSpan? endTime = null;
        DayOfWeek[]? allowedDays = null;

        // Parse time specification
        if (timeSpec.Contains('-'))
        {
            var timeParts = timeSpec.Split('-');
            if (timeParts.Length == 2)
            {
                if (TimeSpan.TryParse(timeParts[0], out var start))
                    startTime = start;
                if (TimeSpan.TryParse(timeParts[1], out var end))
                    endTime = end;
            }
        }

        // Parse additional parameters
        foreach (var param in additionalParams)
        {
            if (param.StartsWith("days="))
            {
                var dayNames = param.Substring(5).Split(',', StringSplitOptions.RemoveEmptyEntries);
                var days = new List<DayOfWeek>();

                foreach (var dayName in dayNames)
                {
                    if (Enum.TryParse<DayOfWeek>(dayName, true, out var day))
                    {
                        days.Add(day);
                    }
                }

                if (days.Count > 0)
                    allowedDays = days.ToArray();
            }
        }

        var requirement = new TimeBasedRequirement(startTime, endTime, allowedDays ?? Array.Empty<DayOfWeek>());
        return builder.AddRequirements(requirement).Build();
    }

    private AuthorizationPolicy CreateRolePolicy(AuthorizationPolicyBuilder builder, string roles)
    {
        var roleList = roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return builder.RequireRole(roleList).Build();
    }

    private AuthorizationPolicy CreateGroupPermissionPolicy(
        AuthorizationPolicyBuilder builder,
        string permission,
        string[] additionalParams)
    {
        // Group-permission policy is similar to regular permission policy
        // but it considers group context from the user's claims
        Guid? groupId = null;
        string? resource = null;

        // Parse additional parameters (group ID might be provided)
        foreach (var param in additionalParams)
        {
            if (Guid.TryParse(param, out var gId))
            {
                groupId = gId;
            }
            else if (param.StartsWith("resource="))
            {
                resource = param.Substring(9);
            }
        }

        // Create a permission requirement with group context
        // The group context will be taken from user's GroupId claim if not explicitly provided
        var requirement = new PermissionRequirement(permission, resource, groupId);
        return builder.AddRequirements(requirement).Build();
    }
}

/// <summary>
/// Policy constants for commonly used policies
/// </summary>
public static class PolicyConstants
{
    // Permission-based policies
    public const string ReadUsers = "permission:users.read";
    public const string WriteUsers = "permission:users.write";
    public const string DeleteUsers = "permission:users.delete";
    public const string ManageUsers = "permissions-all:users.read,users.write,users.delete";

    public const string ReadRoles = "permission:roles.read";
    public const string WriteRoles = "permission:roles.write";
    public const string ManageRoles = "permissions-all:roles.read,roles.write,roles.delete";

    public const string ReadPermissions = "permission:permissions.read";
    public const string WritePermissions = "permission:permissions.write";
    public const string ManagePermissions = "permissions-all:permissions.read,permissions.write,permissions.delete";

    // Role-based policies
    public const string AdminOnly = "role:Admin";
    public const string AdminOrManager = "role:Admin,Manager";

    // Time-based policies
    public const string BusinessHours = "time:09:00-17:00:days=Monday,Tuesday,Wednesday,Thursday,Friday";
    public const string Weekends = "time::days=Saturday,Sunday";

    // Resource ownership
    public const string ProfileOwner = "owner:Profile";
    public const string DocumentOwner = "owner:Document";
}