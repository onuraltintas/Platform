using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Identity.Application.Services;

public class RoleSeedingService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RoleSeedingService> _logger;

    public RoleSeedingService(
        RoleManager<ApplicationRole> roleManager,
        IPermissionService permissionService,
        ILogger<RoleSeedingService> logger)
    {
        _roleManager = roleManager;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task SeedStandardRolesAsync()
    {
        var standardRoles = GetStandardRoles();

        foreach (var roleInfo in standardRoles)
        {
            await CreateRoleIfNotExistsAsync(roleInfo.Name, roleInfo.Description, roleInfo.IsSystemRole);
        }

        await AssignPermissionsToRolesAsync();
    }

    private List<(string Name, string Description, bool IsSystemRole)> GetStandardRoles()
    {
        return new List<(string, string, bool)>
        {
            ("SuperAdmin", "System super administrator with full access to all services and system settings", true),
            ("Admin", "Service administrator with broad access to manage users, roles, and most resources", true),
            ("Manager", "Department manager with access to team management and departmental resources", false),
            ("User", "Standard user with access to basic application features", false),
            ("Student", "Student user with access to learning resources and educational content", false),
            ("Guest", "Guest user with limited read-only access to public resources", false)
        };
    }

    private async Task CreateRoleIfNotExistsAsync(string roleName, string description, bool isSystemRole)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = description,
                IsSystemRole = isSystemRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created role: {RoleName}", roleName);
            }
            else
            {
                _logger.LogError("Failed to create role {RoleName}: {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogDebug("Role {RoleName} already exists", roleName);
        }
    }

    private async Task AssignPermissionsToRolesAsync()
    {
        var rolePermissionMappings = GetRolePermissionMappings();

        foreach (var mapping in rolePermissionMappings)
        {
            var role = await _roleManager.FindByNameAsync(mapping.RoleName);
            if (role != null)
            {
                foreach (var permissionPattern in mapping.PermissionPatterns)
                {
                    try
                    {
                        // For SuperAdmin, add wildcard permission as a claim
                        if (mapping.RoleName == "SuperAdmin" && permissionPattern == "Identity.*.*")
                        {
                            // Add wildcard permission claim to SuperAdmin role
                            var claim = new Claim("permission", "*.*.*");
                            var existingClaim = await _roleManager.GetClaimsAsync(role);

                            if (!existingClaim.Any(c => c.Type == "permission" && c.Value == "*.*.*"))
                            {
                                var result = await _roleManager.AddClaimAsync(role, claim);
                                if (result.Succeeded)
                                {
                                    _logger.LogInformation("Assigned wildcard permission (*.*.*) to SuperAdmin role");
                                }
                                else
                                {
                                    _logger.LogError("Failed to assign wildcard permission to SuperAdmin: {Errors}",
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
                                }
                            }
                        }
                        else
                        {
                            // For other roles, add specific permission patterns as claims
                            var claim = new Claim("permission", permissionPattern);
                            var existingClaims = await _roleManager.GetClaimsAsync(role);

                            if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permissionPattern))
                            {
                                var result = await _roleManager.AddClaimAsync(role, claim);
                                if (result.Succeeded)
                                {
                                    _logger.LogInformation("Assigned permission {Permission} to role {Role}",
                                        permissionPattern, mapping.RoleName);
                                }
                                else
                                {
                                    _logger.LogError("Failed to assign permission {Permission} to role {Role}: {Errors}",
                                        permissionPattern, mapping.RoleName,
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to assign permission {Permission} to role {Role}",
                            permissionPattern, mapping.RoleName);
                    }
                }
            }
        }
    }

    private List<RolePermissionMapping> GetRolePermissionMappings()
    {
        return new List<RolePermissionMapping>
        {
            new("SuperAdmin", new[]
            {
                "Identity.*.*",      // Full access to Identity service
                "User.*.*",          // Full access to User service
                "SpeedReading.*.*"   // Full access to SpeedReading service
            }),

            new("Admin", new[]
            {
                "Identity.Users.*",
                "Identity.Roles.*",
                "Identity.Groups.*",
                "Identity.Permissions.*",
                "User.*.*",
                "SpeedReading.Exercises.*",
                "SpeedReading.ReadingTexts.*",
                "SpeedReading.UserProfiles.Read", "SpeedReading.UserProfiles.Update"
            }),

            new("Manager", new[]
            {
                "Identity.Users.Read", "Identity.Users.Update",
                "Identity.Groups.Read", "Identity.Groups.Update",
                "User.Profiles.*",
                "User.Preferences.*",
                "SpeedReading.Exercises.Read",
                "SpeedReading.ReadingTexts.Read",
                "SpeedReading.UserProfiles.Read",
                "SpeedReading.Statistics.Read"
            }),

            new("User", new[]
            {
                "Identity.Auth.Login", "Identity.Auth.Logout",
                "User.Profiles.Read", "User.Profiles.Update",
                "User.Preferences.*",
                "User.EmailVerifications.Create", "User.EmailVerifications.Verify",
                "User.PasswordResets.Create",
                "SpeedReading.Sessions.*",
                "SpeedReading.Progress.*",
                "SpeedReading.Statistics.Read"
            }),

            new("Student", new[]
            {
                "Identity.Auth.Login", "Identity.Auth.Logout",
                "User.Profiles.Read", "User.Profiles.Update",
                "User.Preferences.Read", "User.Preferences.Update",
                "SpeedReading.Exercises.Read",
                "SpeedReading.ReadingTexts.Read",
                "SpeedReading.Sessions.*",
                "SpeedReading.Progress.*",
                "SpeedReading.Statistics.Read"
            }),

            new("Guest", new[]
            {
                "Identity.Auth.Login",
                "SpeedReading.Exercises.Read",
                "SpeedReading.ReadingTexts.Read"
            })
        };
    }

    /// <summary>
    /// Seed role hierarchy relationships
    /// </summary>
    public async Task SeedRoleHierarchyAsync()
    {
        var hierarchyMappings = GetRoleHierarchyMappings();

        foreach (var mapping in hierarchyMappings)
        {
            var role = await _roleManager.FindByNameAsync(mapping.RoleName);
            if (role != null)
            {
                // Set parent role if specified
                if (!string.IsNullOrEmpty(mapping.ParentRoleName))
                {
                    var parentRole = await _roleManager.FindByNameAsync(mapping.ParentRoleName);
                    if (parentRole != null)
                    {
                        role.ParentRoleId = parentRole.Id;
                    }
                }

                // Set hierarchy properties
                role.HierarchyLevel = mapping.HierarchyLevel;
                role.HierarchyPath = mapping.HierarchyPath;
                role.InheritPermissions = mapping.InheritPermissions;
                role.Priority = mapping.Priority;

                var result = await _roleManager.UpdateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Updated role hierarchy for: {RoleName}", mapping.RoleName);
                }
                else
                {
                    _logger.LogError("Failed to update role hierarchy for {RoleName}: {Errors}",
                        mapping.RoleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private List<RoleHierarchyMapping> GetRoleHierarchyMappings()
    {
        return new List<RoleHierarchyMapping>
        {
            new("SuperAdmin", null, 0, "/SuperAdmin", true, 1000),
            new("Admin", "SuperAdmin", 1, "/SuperAdmin/Admin", true, 900),
            new("Manager", "Admin", 2, "/SuperAdmin/Admin/Manager", true, 800),
            new("Moderator", "Manager", 3, "/SuperAdmin/Admin/Manager/Moderator", true, 700),
            new("User", "Moderator", 4, "/SuperAdmin/Admin/Manager/Moderator/User", true, 600),
            new("Student", "User", 5, "/SuperAdmin/Admin/Manager/Moderator/User/Student", true, 500),
            new("Guest", "Student", 6, "/SuperAdmin/Admin/Manager/Moderator/User/Student/Guest", true, 100)
        };
    }

    private record RolePermissionMapping(string RoleName, string[] PermissionPatterns);
    private record RoleHierarchyMapping(
        string RoleName,
        string? ParentRoleName,
        int HierarchyLevel,
        string HierarchyPath,
        bool InheritPermissions,
        int Priority);
}