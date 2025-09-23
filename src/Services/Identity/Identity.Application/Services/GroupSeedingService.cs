using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class GroupSeedingService
{
    private readonly IdentityDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<GroupSeedingService> _logger;

    public GroupSeedingService(
        IdentityDbContext db,
        RoleManager<ApplicationRole> roleManager,
        ILogger<GroupSeedingService> logger)
    {
        _db = db;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedDefaultGroupsAsync()
    {
        if (await _db.Groups.AnyAsync())
        {
            _logger.LogDebug("Groups already exist. Skipping default group creation.");
            return;
        }

        var defaultGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Default Group",
            Description = "Default tenant/group for initial setup",
            Type = GroupType.Organization,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            IsActive = true
        };

        _db.Groups.Add(defaultGroup);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created default group {GroupName} ({GroupId})", defaultGroup.Name, defaultGroup.Id);
    }

    public async Task SeedGroupRolesAndPermissionsAsync()
    {
        var groups = await _db.Groups.Where(g => g.IsActive && !g.IsDeleted).ToListAsync();
        if (groups.Count == 0)
        {
            _logger.LogWarning("No groups found for seeding group roles.");
            return;
        }

        foreach (var group in groups)
        {
            // Ensure roles exist per group with unique names (Name must be unique across system)
            var adminRoleName = $"{group.Id}:GroupAdmin";
            var managerRoleName = $"{group.Id}:GroupManager";
            var memberRoleName = $"{group.Id}:GroupMember";

            await CreateGroupRoleIfNotExistsAsync(group, adminRoleName, "Group administrator role with elevated permissions");
            await CreateGroupRoleIfNotExistsAsync(group, managerRoleName, "Group manager role with managerial permissions");
            await CreateGroupRoleIfNotExistsAsync(group, memberRoleName, "Group member role with basic permissions");

            // Assign permissions to group roles based on patterns
            await AssignPermissionsToGroupRoleAsync(adminRoleName, new[]
            {
                "Identity.Users.*", "Identity.Roles.Read", "Identity.Roles.Update", "Identity.Roles.Assign", "Identity.Roles.Revoke",
                "Identity.Groups.*", "Identity.Permissions.Read", "User.*.*", "SpeedReading.Exercises.*", "SpeedReading.ReadingTexts.*"
            });

            await AssignPermissionsToGroupRoleAsync(managerRoleName, new[]
            {
                "Identity.Users.Read", "Identity.Users.Update", "Identity.Groups.Read", "Identity.Groups.Update",
                "User.Profiles.*", "User.Preferences.*", "SpeedReading.Exercises.Read", "SpeedReading.Statistics.Read"
            });

            await AssignPermissionsToGroupRoleAsync(memberRoleName, new[]
            {
                "Identity.Auth.*", "User.Profiles.Read", "User.Profiles.Update", "User.Preferences.*",
                "SpeedReading.Sessions.*", "SpeedReading.Progress.*", "SpeedReading.Statistics.Read"
            });
        }
    }

    private async Task CreateGroupRoleIfNotExistsAsync(Group group, string roleName, string description)
    {
        var existing = await _roleManager.FindByNameAsync(roleName);
        if (existing != null)
        {
            _logger.LogDebug("Role {RoleName} already exists for group {GroupId}", roleName, group.Id);
            return;
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = description,
            IsSystemRole = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            GroupId = group.Id
        };

        var result = await _roleManager.CreateAsync(role);
        if (result.Succeeded)
        {
            _logger.LogInformation("Created role {RoleName} for group {GroupId}", roleName, group.Id);
        }
        else
        {
            _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task AssignPermissionsToGroupRoleAsync(string roleName, string[] patterns)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return;

        var existingPermissionIds = await _db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var allPermissions = await _db.Permissions.ToListAsync();
        var matched = allPermissions.Where(p => patterns.Any(pattern => MatchesPattern(p.Name, pattern)))
                                    .ToList();

        var newOnes = matched.Where(p => !existingPermissionIds.Contains(p.Id))
                             .Select(p => new RolePermission
                             {
                                 RoleId = role.Id,
                                 PermissionId = p.Id,
                                 GrantedAt = DateTime.UtcNow,
                                 GrantedBy = "group-seed"
                             })
                             .ToList();

        if (newOnes.Count > 0)
        {
            await _db.RolePermissions.AddRangeAsync(newOnes);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Assigned {Count} permissions to role {RoleName}", newOnes.Count, roleName);
        }
    }

    private static bool MatchesPattern(string permissionName, string pattern)
    {
        if (pattern == "*") return true;
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return permissionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        return permissionName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}

