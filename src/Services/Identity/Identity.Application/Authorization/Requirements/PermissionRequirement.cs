using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Authorization.Requirements;

/// <summary>
/// Resource-based permission requirement
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public string? Resource { get; }
    public Guid? GroupId { get; }
    public bool RequireAll { get; }

    public PermissionRequirement(
        string permission,
        string? resource = null,
        Guid? groupId = null,
        bool requireAll = false)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        Resource = resource;
        GroupId = groupId;
        RequireAll = requireAll;
    }
}

/// <summary>
/// Multiple permissions requirement - can require ANY or ALL permissions
/// </summary>
public class MultiplePermissionsRequirement : IAuthorizationRequirement
{
    public IEnumerable<string> Permissions { get; }
    public string? Resource { get; }
    public Guid? GroupId { get; }
    public bool RequireAll { get; }

    public MultiplePermissionsRequirement(
        IEnumerable<string> permissions,
        string? resource = null,
        Guid? groupId = null,
        bool requireAll = true)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        Resource = resource;
        GroupId = groupId;
        RequireAll = requireAll;
    }
}

/// <summary>
/// Resource ownership requirement - user must own the resource
/// </summary>
public class ResourceOwnerRequirement : IAuthorizationRequirement
{
    public string ResourceType { get; }
    public string OwnerIdProperty { get; }

    public ResourceOwnerRequirement(string resourceType, string ownerIdProperty = "UserId")
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        OwnerIdProperty = ownerIdProperty ?? throw new ArgumentNullException(nameof(ownerIdProperty));
    }
}

/// <summary>
/// Group membership requirement - user must be member of specified group
/// </summary>
public class GroupMemberRequirement : IAuthorizationRequirement
{
    public Guid GroupId { get; }
    public bool RequireActiveStatus { get; }

    public GroupMemberRequirement(Guid groupId, bool requireActiveStatus = true)
    {
        GroupId = groupId;
        RequireActiveStatus = requireActiveStatus;
    }
}

/// <summary>
/// Time-based requirement - permission only valid during specific time periods
/// </summary>
public class TimeBasedRequirement : IAuthorizationRequirement
{
    public TimeSpan? StartTime { get; }
    public TimeSpan? EndTime { get; }
    public DayOfWeek[]? AllowedDays { get; }

    public TimeBasedRequirement(
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        params DayOfWeek[] allowedDays)
    {
        StartTime = startTime;
        EndTime = endTime;
        AllowedDays = allowedDays?.Length > 0 ? allowedDays : null;
    }
}

/// <summary>
/// Conditional requirement - permission based on dynamic conditions
/// </summary>
public class ConditionalRequirement : IAuthorizationRequirement
{
    public string ConditionExpression { get; }
    public Dictionary<string, object> Parameters { get; }

    public ConditionalRequirement(string conditionExpression, Dictionary<string, object>? parameters = null)
    {
        ConditionExpression = conditionExpression ?? throw new ArgumentNullException(nameof(conditionExpression));
        Parameters = parameters ?? new Dictionary<string, object>();
    }
}