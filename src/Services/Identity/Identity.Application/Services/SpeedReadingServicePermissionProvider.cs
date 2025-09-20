using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;

namespace Identity.Application.Services;

public class SpeedReadingServicePermissionProvider : IServicePermissionProvider
{
    public string ServiceName => "SpeedReading";

    public IEnumerable<string> GetResources()
    {
        return new[]
        {
            "Exercises",
            "ReadingTexts",
            "UserProfiles",
            "Sessions",
            "Progress",
            "Statistics"
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
            "Start",
            "Complete",
            "Track"
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
                        Description = $"Permission to {action.ToLower()} {resource.ToLower()} in the SpeedReading service",
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
            "Sessions" => action == "Create" || action == "Read" || action == "Start" || action == "Complete",
            "Progress" => action == "Read" || action == "Update" || action == "Track",
            "Statistics" => action == "Read" || action == "Track",
            "Exercises" => action != "Start" && action != "Complete" && action != "Track",
            "ReadingTexts" => action != "Start" && action != "Complete" && action != "Track",
            "UserProfiles" => action != "Start" && action != "Complete" && action != "Track",
            _ => false
        };
    }

    private PermissionType GetPermissionType(string resource, string action)
    {
        return action switch
        {
            "Delete" => PermissionType.Delete,
            "Create" or "Update" => PermissionType.Write,
            "Start" or "Complete" or "Track" => PermissionType.Execute,
            "Read" => PermissionType.Read,
            _ => PermissionType.Custom
        };
    }

    private int GetPermissionPriority(string resource, string action)
    {
        var basePriority = resource switch
        {
            "Exercises" => 100,
            "ReadingTexts" => 90,
            "UserProfiles" => 80,
            "Sessions" => 70,
            "Progress" => 60,
            "Statistics" => 50,
            _ => 0
        };

        var actionPriority = action switch
        {
            "Delete" => 20,
            "Create" => 15,
            "Update" => 10,
            "Complete" => 8,
            "Start" => 5,
            "Track" => 3,
            "Read" => 1,
            _ => 0
        };

        return basePriority + actionPriority;
    }
}