using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;

namespace Identity.Application.Services;

public class UserServicePermissionProvider : IServicePermissionProvider
{
    public string ServiceName => "User";

    public IEnumerable<string> GetResources()
    {
        return new[]
        {
            "Profiles",
            "Preferences",
            "EmailVerifications",
            "PasswordResets",
            "TwoFactorAuth"
        };
    }

    public IEnumerable<string> GetActions()
    {
        return new[]
        {
            "Create",
            "Read",
            "Update",
            "Delete",
            "Verify",
            "Reset",
            "Enable",
            "Disable"
        };
    }

    public IEnumerable<PermissionDto> GetPermissions()
    {
        var permissions = new List<PermissionDto>();
        var resources = GetResources();
        var actions = GetActions();

        foreach (var resource in resources)
        {
            foreach (var action in actions)
            {
                if (IsValidCombination(resource, action))
                {
                    permissions.Add(new PermissionDto
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{ServiceName}.{resource}.{action}",
                        DisplayName = $"{action} {resource}",
                        Description = $"Permission to {action.ToLower()} {resource.ToLower()} in the User service",
                        Resource = resource,
                        Action = action,
                        ServiceName = ServiceName,
                        Type = GetPermissionType(resource, action),
                        Priority = GetPermissionPriority(resource, action),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        return permissions;
    }

    private bool IsValidCombination(string resource, string action)
    {
        return resource switch
        {
            "EmailVerifications" => action == "Create" || action == "Read" || action == "Verify",
            "PasswordResets" => action == "Create" || action == "Read" || action == "Reset",
            "TwoFactorAuth" => action == "Read" || action == "Enable" || action == "Disable",
            "Profiles" => action != "Verify" && action != "Reset" && action != "Enable" && action != "Disable",
            "Preferences" => action != "Verify" && action != "Reset" && action != "Enable" && action != "Disable",
            _ => false
        };
    }

    private PermissionType GetPermissionType(string resource, string action)
    {
        return action switch
        {
            "Delete" => PermissionType.Delete,
            "Reset" or "Disable" => PermissionType.Admin,
            "Create" or "Update" or "Enable" => PermissionType.Write,
            "Read" or "Verify" => PermissionType.Read,
            _ => PermissionType.Custom
        };
    }

    private int GetPermissionPriority(string resource, string action)
    {
        var basePriority = resource switch
        {
            "TwoFactorAuth" => 100,
            "PasswordResets" => 90,
            "EmailVerifications" => 80,
            "Profiles" => 70,
            "Preferences" => 60,
            _ => 0
        };

        var actionPriority = action switch
        {
            "Delete" => 20,
            "Reset" or "Disable" => 15,
            "Create" or "Enable" => 10,
            "Update" => 8,
            "Verify" => 5,
            "Read" => 3,
            _ => 0
        };

        return basePriority + actionPriority;
    }
}