using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

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
                        // This would need to be implemented in the permission service
                        // await _permissionService.AssignPermissionsToRoleAsync(role.Id, permissionPattern);
                        _logger.LogDebug("Would assign permission pattern {Pattern} to role {Role}",
                            permissionPattern, mapping.RoleName);
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
                "Identity.Roles.Read", "Identity.Roles.Update", "Identity.Roles.Assign", "Identity.Roles.Revoke",
                "Identity.Groups.*",
                "Identity.Permissions.Read",
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

    private record RolePermissionMapping(string RoleName, string[] PermissionPatterns);
}