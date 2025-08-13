namespace EgitimPlatform.Services.IdentityService.Models.DTOs;

public class GetUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? RoleId { get; set; }
    public string? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailConfirmed { get; set; }
}

public class PagedUsersResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PermissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }
}

public class UserCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }
}

public class RefreshTokenDto
{
    public string Id { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActive { get; set; }
}

// Request DTOs
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> PermissionIds { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<string> PermissionIds { get; set; } = new();
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool IsActive { get; set; }
}

public class AssignRoleToUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateUserRoleRequest
{
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class AssignCategoryToUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateUserCategoryRequest
{
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public List<string> CategoryIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; } = false;
}

public class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public List<string> CategoryIds { get; set; } = new();
}