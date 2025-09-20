namespace Identity.Core.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? About { get; set; }
    
    // Address Information
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    
    // Group Information
    public Guid? DefaultGroupId { get; set; }
    public GroupDto? DefaultGroup { get; set; }
    public IEnumerable<UserGroupDto> Groups { get; set; } = new List<UserGroupDto>();
    
    // Security
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // GDPR
    public bool MarketingEmailsConsent { get; set; }
    public bool DataProcessingConsent { get; set; }
    public DateTime? ConsentGivenAt { get; set; }
}

public class UserGroupDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public GroupDto Group { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DefaultGroupId { get; set; }
    public List<string>? RoleIds { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

public class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? About { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public List<string>? Roles { get; set; }
}