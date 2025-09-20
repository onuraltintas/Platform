namespace Identity.Core.DTOs;

public class GetUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? RoleId { get; set; }
    public string? GroupId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailConfirmed { get; set; }
}

public class PagedUsersResponse
{
    public UserSummaryDto[] Users { get; set; } = Array.Empty<UserSummaryDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public string? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<UserGroupInfo> Groups { get; set; } = new();
    public UserGroupInfo? DefaultGroup { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class UserGroupInfo
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsDefault { get; set; }
}

public class GetRolesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}


public class GetPermissionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? Search { get; set; }
    public string? Group { get; set; }
    public bool? IsActive { get; set; }
}