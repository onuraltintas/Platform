using Identity.Core.Entities;

namespace Identity.Core.DTOs;

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType Type { get; set; }
    public string TypeDisplay => Type.ToString();
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    
    // Settings
    public int MaxUsers { get; set; }
    public int CurrentUserCount { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public string? SubscriptionPlan { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // User's role in this group
    public string? UserRole { get; set; }
    public DateTime? UserJoinedAt { get; set; }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType Type { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int MaxUsers { get; set; } = 100;
    public string? SubscriptionPlan { get; set; }
}

public class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int MaxUsers { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? LogoUrl { get; set; }
}

public class GroupInvitationRequest
{
    public Guid GroupId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class GroupStatisticsDto
{
    public int TotalGroups { get; set; }
    public int SystemGroups { get; set; }
    public int CustomGroups { get; set; }
    public int TotalMembers { get; set; }
    public double AverageMembersPerGroup { get; set; }
    public GroupDto? LargestGroup { get; set; }
    public int EmptyGroups { get; set; }
}