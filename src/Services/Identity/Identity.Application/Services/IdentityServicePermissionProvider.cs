using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;

namespace Identity.Application.Services;

public class IdentityServicePermissionProvider : IServicePermissionProvider
{
    public string ServiceName => "Identity";

    public IEnumerable<string> GetResources()
    {
        return new[]
        {
            "Users",
            "Roles",
            "Permissions",
            "Groups",
            "Services",
            "Auth"
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
            "Assign",
            "Revoke",
            "Login",
            "Logout"
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
                // Skip invalid combinations
                if (IsValidCombination(resource, action))
                {
                    permissions.Add(new PermissionDto
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{ServiceName}.{resource}.{action}",
                        DisplayName = $"{action} {resource}",
                        Description = $"Permission to {action.ToLower()} {resource.ToLower()} in the Identity service",
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
        // Auth resource only supports Login/Logout
        if (resource == "Auth")
        {
            return action == "Login" || action == "Logout";
        }

        // All other resources support all CRUD operations plus Assign/Revoke
        return action != "Login" && action != "Logout";
    }

    private PermissionType GetPermissionType(string resource, string action)
    {
        return action switch
        {
            "Create" or "Delete" => PermissionType.Delete,
            "Update" or "Assign" or "Revoke" => PermissionType.Write,
            "Read" => PermissionType.Read,
            "Login" or "Logout" => PermissionType.Execute,
            _ => PermissionType.Custom
        };
    }

    private int GetPermissionPriority(string resource, string action)
    {
        var basePriority = resource switch
        {
            "Services" => 100,
            "Permissions" => 90,
            "Roles" => 80,
            "Groups" => 70,
            "Users" => 60,
            "Auth" => 50,
            _ => 0
        };

        var actionPriority = action switch
        {
            "Delete" => 20,
            "Create" => 15,
            "Update" => 10,
            "Assign" or "Revoke" => 8,
            "Read" => 5,
            "Login" or "Logout" => 1,
            _ => 0
        };

        return basePriority + actionPriority;
    }
}