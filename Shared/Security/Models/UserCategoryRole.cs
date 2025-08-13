namespace EgitimPlatform.Shared.Security.Models;

public class UserCategoryRole
{
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }
}

public class CategoryRoleAssignment
{
    public string Category { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public static class CategoryRoleExtensions
{
    public static List<UserCategoryRole> GetUserCategoryRoles(this SecurityUser user, List<UserCategoryRole> allAssignments)
    {
        return allAssignments.Where(assignment => 
            assignment.UserId == user.Id && 
            assignment.IsActive &&
            (assignment.ExpiresAt == null || assignment.ExpiresAt > DateTime.UtcNow))
            .ToList();
    }
    
    public static bool HasCategoryRole(this SecurityUser user, string category, string role, List<UserCategoryRole> assignments)
    {
        return assignments.Any(assignment =>
            assignment.UserId == user.Id &&
            assignment.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
            assignment.Role.Equals(role, StringComparison.OrdinalIgnoreCase) &&
            assignment.IsActive &&
            (assignment.ExpiresAt == null || assignment.ExpiresAt > DateTime.UtcNow));
    }
    
    public static List<string> GetCategoriesForRole(this SecurityUser user, string role, List<UserCategoryRole> assignments)
    {
        return assignments.Where(assignment =>
            assignment.UserId == user.Id &&
            assignment.Role.Equals(role, StringComparison.OrdinalIgnoreCase) &&
            assignment.IsActive &&
            (assignment.ExpiresAt == null || assignment.ExpiresAt > DateTime.UtcNow))
            .Select(assignment => assignment.Category)
            .Distinct()
            .ToList();
    }
    
    public static List<string> GetRolesForCategory(this SecurityUser user, string category, List<UserCategoryRole> assignments)
    {
        return assignments.Where(assignment =>
            assignment.UserId == user.Id &&
            assignment.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
            assignment.IsActive &&
            (assignment.ExpiresAt == null || assignment.ExpiresAt > DateTime.UtcNow))
            .Select(assignment => assignment.Role)
            .Distinct()
            .ToList();
    }
}