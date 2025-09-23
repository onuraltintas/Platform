using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

/// <summary>
/// Modern permission seeding service that uses the new Permission and RolePermission database tables
/// This replaces the old claims-based approach with a proper database-driven permission system
/// </summary>
public class ModernPermissionSeedingService
{
    private readonly IdentityDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ModernPermissionSeedingService> _logger;

    public ModernPermissionSeedingService(
        IdentityDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ModernPermissionSeedingService> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all permissions and role assignments to the database
    /// </summary>
    public async Task SeedAllPermissionsAsync()
    {
        _logger.LogInformation("Starting modern permission seeding...");

        try
        {
            // First, seed permission definitions
            await SeedPermissionDefinitionsAsync();

            // Then, seed services
            await SeedServicesAsync();

            // Finally, assign permissions to roles
            await SeedRolePermissionsAsync();

            _logger.LogInformation("Modern permission seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed permissions");
            throw;
        }
    }

    /// <summary>
    /// Seeds permission definitions to the database
    /// </summary>
    private async Task SeedPermissionDefinitionsAsync()
    {
        var permissions = GetPermissionDefinitions();

        foreach (var permissionDef in permissions)
        {
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionDef.Code);

            if (existingPermission == null)
            {
                var permission = new Permission
                {
                    Code = permissionDef.Code,
                    Name = permissionDef.Name,
                    Description = permissionDef.Description,
                    Category = permissionDef.Category,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.Permissions.Add(permission);
                _logger.LogDebug("Added permission: {Code}", permissionDef.Code);
            }
            else
            {
                // Update existing permission if needed
                existingPermission.Name = permissionDef.Name;
                existingPermission.Description = permissionDef.Description;
                existingPermission.Category = permissionDef.Category;
                existingPermission.LastModifiedAt = DateTime.UtcNow;
                existingPermission.LastModifiedBy = "System";
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} permissions", permissions.Count);
    }

    /// <summary>
    /// Seeds service definitions to the database
    /// </summary>
    private async Task SeedServicesAsync()
    {
        var services = GetServiceDefinitions();

        foreach (var serviceDef in services)
        {
            var existingService = await _context.Services
                .FirstOrDefaultAsync(s => s.Code == serviceDef.Code);

            if (existingService == null)
            {
                var service = new Service
                {
                    Code = serviceDef.Code,
                    Name = serviceDef.Name,
                    Description = serviceDef.Description,
                    Version = serviceDef.Version,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Services.Add(service);
                _logger.LogDebug("Added service: {Code}", serviceDef.Code);
            }
            else
            {
                // Update existing service if needed
                existingService.Name = serviceDef.Name;
                existingService.Description = serviceDef.Description;
                existingService.Version = serviceDef.Version;
                existingService.LastModifiedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} services", services.Count);
    }

    /// <summary>
    /// Seeds role permission assignments to the database
    /// </summary>
    private async Task SeedRolePermissionsAsync()
    {
        var rolePermissionMappings = GetRolePermissionMappings();

        foreach (var mapping in rolePermissionMappings)
        {
            var role = await _roleManager.FindByNameAsync(mapping.RoleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found, skipping permission assignment", mapping.RoleName);
                continue;
            }

            foreach (var permissionCode in mapping.PermissionCodes)
            {
                await AssignPermissionToRoleAsync(role.Id, permissionCode);
            }

            // Handle wildcard permissions
            foreach (var wildcardPattern in mapping.WildcardPatterns)
            {
                await AssignWildcardPermissionToRoleAsync(role.Id, wildcardPattern);
            }
        }

        _logger.LogInformation("Completed role permission assignments");
    }

    /// <summary>
    /// Assigns a specific permission to a role
    /// </summary>
    private async Task AssignPermissionToRoleAsync(string roleId, string permissionCode)
    {
        try
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode);

            if (permission == null)
            {
                _logger.LogWarning("Permission {Code} not found", permissionCode);
                return;
            }

            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);

            if (existingRolePermission == null)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = "System",
                    IsActive = true
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Assigned permission {Permission} to role {RoleId}", permissionCode, roleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {Permission} to role {RoleId}", permissionCode, roleId);
        }
    }

    /// <summary>
    /// Assigns a wildcard permission to a role
    /// </summary>
    private async Task AssignWildcardPermissionToRoleAsync(string roleId, string wildcardPattern)
    {
        try
        {
            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId
                    && rp.PermissionPattern == wildcardPattern
                    && rp.IsWildcard);

            if (existingRolePermission == null)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionPattern = wildcardPattern,
                    IsWildcard = true,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = "System",
                    IsActive = true
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Assigned wildcard permission {Pattern} to role {RoleId}", wildcardPattern, roleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign wildcard permission {Pattern} to role {RoleId}", wildcardPattern, roleId);
        }
    }

    /// <summary>
    /// Gets permission definitions for seeding
    /// </summary>
    private List<PermissionDefinition> GetPermissionDefinitions()
    {
        return new List<PermissionDefinition>
        {
            // Identity Service Permissions - Users
            new("Identity.Users.Read", "Read Users", "View user accounts and profiles", "User Management"),
            new("Identity.Users.Create", "Create Users", "Create new user accounts", "User Management"),
            new("Identity.Users.Update", "Update Users", "Modify existing user accounts", "User Management"),
            new("Identity.Users.Delete", "Delete Users", "Remove user accounts", "User Management"),
            new("Identity.Users.ManageRoles", "Manage User Roles", "Assign and remove roles from users", "User Management"),
            new("Identity.Users.ManagePermissions", "Manage User Permissions", "Assign direct permissions to users", "User Management"),
            new("Identity.Users.ViewAudit", "View User Audit", "View user activity audit logs", "User Management"),
            new("Identity.Users.Export", "Export Users", "Export user data", "User Management"),
            new("Identity.Users.Import", "Import Users", "Import user data", "User Management"),

            // Identity Service Permissions - Roles
            new("Identity.Roles.Read", "Read Roles", "View roles and their permissions", "Role Management"),
            new("Identity.Roles.Create", "Create Roles", "Create new roles", "Role Management"),
            new("Identity.Roles.Update", "Update Roles", "Modify existing roles", "Role Management"),
            new("Identity.Roles.Delete", "Delete Roles", "Remove roles", "Role Management"),
            new("Identity.Roles.ManagePermissions", "Manage Role Permissions", "Assign permissions to roles", "Role Management"),
            new("Identity.Roles.ViewAudit", "View Role Audit", "View role audit logs", "Role Management"),

            // Identity Service Permissions - Groups
            new("Identity.Groups.Read", "Read Groups", "View groups and their members", "Group Management"),
            new("Identity.Groups.Create", "Create Groups", "Create new groups", "Group Management"),
            new("Identity.Groups.Update", "Update Groups", "Modify existing groups", "Group Management"),
            new("Identity.Groups.Delete", "Delete Groups", "Remove groups", "Group Management"),
            new("Identity.Groups.ManageMembers", "Manage Group Members", "Add and remove group members", "Group Management"),
            new("Identity.Groups.ManageServices", "Manage Group Services", "Configure group service access", "Group Management"),

            // Identity Service Permissions - Permissions
            new("Identity.Permissions.Read", "Read Permissions", "View permission definitions", "Permission Management"),
            new("Identity.Permissions.Create", "Create Permissions", "Create new permission definitions", "Permission Management"),
            new("Identity.Permissions.Update", "Update Permissions", "Modify permission definitions", "Permission Management"),
            new("Identity.Permissions.Delete", "Delete Permissions", "Remove permission definitions", "Permission Management"),
            new("Identity.Permissions.Audit", "Audit Permissions", "View permission audit logs", "Permission Management"),

            // Identity Service Permissions - Audit
            new("Identity.Audit.Read", "Read Audit Logs", "View system audit logs", "Audit & Security"),
            new("Identity.Audit.Export", "Export Audit Data", "Export audit log data", "Audit & Security"),
            new("Identity.Audit.Delete", "Delete Audit Logs", "Remove old audit logs", "Audit & Security"),

            // System Administration
            new("System.Admin.FullAccess", "Full System Access", "Complete system administration access", "System Administration"),
            new("System.Admin.UserManagement", "System User Management", "Advanced user management capabilities", "System Administration"),
            new("System.Admin.SystemConfig", "System Configuration", "Configure system settings", "System Administration"),
            new("System.Admin.SecurityAudit", "Security Audit", "Access to security audit features", "System Administration"),

            // Speed Reading Service Permissions
            new("SpeedReading.Texts.Read", "Read Reading Texts", "View reading texts and materials", "Speed Reading"),
            new("SpeedReading.Texts.Create", "Create Reading Texts", "Add new reading texts", "Speed Reading"),
            new("SpeedReading.Texts.Update", "Update Reading Texts", "Modify reading texts", "Speed Reading"),
            new("SpeedReading.Texts.Delete", "Delete Reading Texts", "Remove reading texts", "Speed Reading"),

            new("SpeedReading.Exercises.Read", "Read Exercises", "View speed reading exercises", "Speed Reading"),
            new("SpeedReading.Exercises.Create", "Create Exercises", "Create new exercises", "Speed Reading"),
            new("SpeedReading.Exercises.Update", "Update Exercises", "Modify exercises", "Speed Reading"),
            new("SpeedReading.Exercises.Delete", "Delete Exercises", "Remove exercises", "Speed Reading"),

            new("SpeedReading.Analytics.Read", "Read Analytics", "View speed reading analytics", "Speed Reading"),
            new("SpeedReading.Analytics.Export", "Export Analytics", "Export analytics data", "Speed Reading"),
        };
    }

    /// <summary>
    /// Gets service definitions for seeding
    /// </summary>
    private List<ServiceDefinition> GetServiceDefinitions()
    {
        return new List<ServiceDefinition>
        {
            new("Identity", "Identity Service", "User authentication and authorization service", "1.0.0"),
            new("Gateway", "API Gateway", "API gateway and routing service", "1.0.0"),
            new("SpeedReading", "Speed Reading Service", "Speed reading training and analytics service", "1.0.0"),
            new("System", "System Service", "Core system administration service", "1.0.0")
        };
    }

    /// <summary>
    /// Gets role permission mappings for seeding
    /// </summary>
    private List<RolePermissionMapping> GetRolePermissionMappings()
    {
        return new List<RolePermissionMapping>
        {
            new("SuperAdmin",
                new[] { "*.*.*" }, // Wildcard for all permissions
                new string[] { }   // No specific permissions needed
            ),

            new("Admin",
                new[] { "Identity.*.*", "SpeedReading.*.*" }, // Service-level wildcards
                new[]
                {
                    "System.Admin.UserManagement",
                    "System.Admin.SecurityAudit"
                }
            ),

            new("Manager",
                new string[] { }, // No wildcards
                new[]
                {
                    "Identity.Users.Read", "Identity.Users.Update",
                    "Identity.Groups.Read", "Identity.Groups.Update", "Identity.Groups.ManageMembers",
                    "SpeedReading.Texts.Read", "SpeedReading.Exercises.Read",
                    "SpeedReading.Analytics.Read"
                }
            ),

            new("User",
                new string[] { }, // No wildcards
                new[]
                {
                    "Identity.Users.Read", // Can read own profile
                    "SpeedReading.Texts.Read", "SpeedReading.Exercises.Read",
                    "SpeedReading.Analytics.Read"
                }
            ),

            new("Student",
                new string[] { }, // No wildcards
                new[]
                {
                    "SpeedReading.Texts.Read", "SpeedReading.Exercises.Read",
                    "SpeedReading.Analytics.Read"
                }
            ),

            new("Guest",
                new string[] { }, // No wildcards
                new[]
                {
                    "SpeedReading.Texts.Read"
                }
            )
        };
    }

    /// <summary>
    /// Gets current permissions for a role from database
    /// </summary>
    public async Task<List<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return new List<string>();

        var rolePermissions = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == role.Id && rp.IsActive)
            .ToListAsync();

        var permissions = new List<string>();

        // Add specific permissions
        permissions.AddRange(rolePermissions
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code));

        // Add wildcard patterns
        permissions.AddRange(rolePermissions
            .Where(rp => rp.IsWildcard && !string.IsNullOrEmpty(rp.PermissionPattern))
            .Select(rp => rp.PermissionPattern!));

        return permissions;
    }

    // Record types for data definitions
    private record PermissionDefinition(string Code, string Name, string Description, string Category);
    private record ServiceDefinition(string Code, string Name, string Description, string Version);
    private record RolePermissionMapping(string RoleName, string[] WildcardPatterns, string[] PermissionCodes);
}