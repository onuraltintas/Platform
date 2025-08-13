using Microsoft.Extensions.Logging;
using EgitimPlatform.Shared.Security.Models;

namespace EgitimPlatform.Shared.Security.Services;

public class CategoryRoleService : ICategoryRoleService
{
    private readonly ILogger<CategoryRoleService> _logger;
    private readonly List<UserCategoryRole> _categoryRoles; // In production, use database
    
    public CategoryRoleService(ILogger<CategoryRoleService> logger)
    {
        _logger = logger;
        _categoryRoles = new List<UserCategoryRole>();
    }
    
    public async Task<bool> AssignCategoryRoleAsync(string userId, string category, string role, DateTime? expiresAt = null, string? assignedBy = null, string? notes = null)
    {
        try
        {
            // Check if assignment already exists
            var existingAssignment = _categoryRoles.FirstOrDefault(cr =>
                cr.UserId == userId &&
                cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            if (existingAssignment != null)
            {
                // Update existing assignment
                existingAssignment.IsActive = true;
                existingAssignment.ExpiresAt = expiresAt;
                existingAssignment.AssignedBy = assignedBy;
                existingAssignment.Notes = notes;
                existingAssignment.AssignedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Updated category role assignment: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            }
            else
            {
                // Create new assignment
                var newAssignment = new UserCategoryRole
                {
                    UserId = userId,
                    Category = category,
                    Role = role,
                    ExpiresAt = expiresAt,
                    AssignedBy = assignedBy,
                    Notes = notes,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                };
                
                _categoryRoles.Add(newAssignment);
                _logger.LogInformation("Assigned category role: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning category role: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            return false;
        }
    }
    
    public async Task<bool> RemoveCategoryRoleAsync(string userId, string category, string role)
    {
        try
        {
            var assignment = _categoryRoles.FirstOrDefault(cr =>
                cr.UserId == userId &&
                cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            if (assignment != null)
            {
                assignment.IsActive = false;
                _logger.LogInformation("Removed category role: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing category role: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            return false;
        }
    }
    
    public async Task<bool> RemoveAllCategoryRolesAsync(string userId, string? category = null)
    {
        try
        {
            var assignmentsToRemove = _categoryRoles.Where(cr => 
                cr.UserId == userId && 
                (category == null || cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));
            
            foreach (var assignment in assignmentsToRemove)
            {
                assignment.IsActive = false;
            }
            
            _logger.LogInformation("Removed all category roles for user {UserId}, category filter: {Category}", userId, category ?? "all");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing all category roles for user {UserId}", userId);
            return false;
        }
    }
    
    public async Task<List<UserCategoryRole>> GetUserCategoryRolesAsync(string userId)
    {
        return _categoryRoles.Where(cr => 
            cr.UserId == userId && 
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .ToList();
    }
    
    public async Task<List<UserCategoryRole>> GetCategoryUsersAsync(string category)
    {
        return _categoryRoles.Where(cr => 
            cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && 
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .ToList();
    }
    
    public async Task<List<UserCategoryRole>> GetRoleUsersAsync(string role)
    {
        return _categoryRoles.Where(cr => 
            cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase) && 
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .ToList();
    }
    
    public async Task<bool> HasCategoryRoleAsync(string userId, string category, string role)
    {
        return _categoryRoles.Any(cr =>
            cr.UserId == userId &&
            cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
            cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase) &&
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow));
    }
    
    public async Task<List<string>> GetUserCategoriesAsync(string userId)
    {
        return _categoryRoles.Where(cr =>
            cr.UserId == userId &&
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .Select(cr => cr.Category)
            .Distinct()
            .ToList();
    }
    
    public async Task<List<string>> GetUserRolesInCategoryAsync(string userId, string category)
    {
        return _categoryRoles.Where(cr =>
            cr.UserId == userId &&
            cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .Select(cr => cr.Role)
            .Distinct()
            .ToList();
    }
    
    public async Task<List<string>> GetUserCategoriesForRoleAsync(string userId, string role)
    {
        return _categoryRoles.Where(cr =>
            cr.UserId == userId &&
            cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase) &&
            cr.IsActive &&
            (cr.ExpiresAt == null || cr.ExpiresAt > DateTime.UtcNow))
            .Select(cr => cr.Category)
            .Distinct()
            .ToList();
    }
    
    public async Task<bool> UpdateCategoryRoleExpirationAsync(string userId, string category, string role, DateTime? expiresAt)
    {
        try
        {
            var assignment = _categoryRoles.FirstOrDefault(cr =>
                cr.UserId == userId &&
                cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            if (assignment != null)
            {
                assignment.ExpiresAt = expiresAt;
                _logger.LogInformation("Updated category role expiration: User {UserId}, Category {Category}, Role {Role}, ExpiresAt {ExpiresAt}", 
                    userId, category, role, expiresAt);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category role expiration: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            return false;
        }
    }
    
    public async Task<bool> ActivateDeactivateCategoryRoleAsync(string userId, string category, string role, bool isActive)
    {
        try
        {
            var assignment = _categoryRoles.FirstOrDefault(cr =>
                cr.UserId == userId &&
                cr.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            if (assignment != null)
            {
                assignment.IsActive = isActive;
                _logger.LogInformation("Updated category role status: User {UserId}, Category {Category}, Role {Role}, IsActive {IsActive}", 
                    userId, category, role, isActive);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category role status: User {UserId}, Category {Category}, Role {Role}", userId, category, role);
            return false;
        }
    }
    
    public async Task<List<UserCategoryRole>> GetExpiringCategoryRolesAsync(DateTime beforeDate)
    {
        return _categoryRoles.Where(cr =>
            cr.IsActive &&
            cr.ExpiresAt.HasValue &&
            cr.ExpiresAt.Value <= beforeDate)
            .ToList();
    }
    
    public async Task<int> CleanupExpiredCategoryRolesAsync()
    {
        var expiredRoles = _categoryRoles.Where(cr =>
            cr.IsActive &&
            cr.ExpiresAt.HasValue &&
            cr.ExpiresAt.Value <= DateTime.UtcNow)
            .ToList();
        
        foreach (var role in expiredRoles)
        {
            role.IsActive = false;
        }
        
        _logger.LogInformation("Cleaned up {Count} expired category roles", expiredRoles.Count);
        return expiredRoles.Count;
    }
}