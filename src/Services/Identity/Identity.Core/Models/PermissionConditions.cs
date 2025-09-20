using System.Text.Json.Serialization;

namespace Identity.Core.Models;

/// <summary>
/// Base class for permission conditions
/// </summary>
public abstract class PermissionCondition
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    public abstract Task<bool> EvaluateAsync(PermissionConditionContext context);
}

/// <summary>
/// Context for evaluating permission conditions
/// </summary>
public class PermissionConditionContext
{
    public string UserId { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string? Resource { get; set; }
    public string? IpAddress { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> CustomData { get; set; } = new();
}

/// <summary>
/// Time-based permission condition
/// </summary>
public class TimeCondition : PermissionCondition
{
    public override string Type => "time";

    [JsonPropertyName("workHours")]
    public string? WorkHours { get; set; } // e.g., "09:00-18:00"

    [JsonPropertyName("workDays")]
    public List<string>? WorkDays { get; set; } // e.g., ["Mon", "Tue", "Wed", "Thu", "Fri"]

    [JsonPropertyName("validFrom")]
    public DateTime? ValidFrom { get; set; }

    [JsonPropertyName("validUntil")]
    public DateTime? ValidUntil { get; set; }

    public override Task<bool> EvaluateAsync(PermissionConditionContext context)
    {
        var now = context.RequestTime;

        // Check validity period
        if (ValidFrom.HasValue && now < ValidFrom.Value)
            return Task.FromResult(false);

        if (ValidUntil.HasValue && now > ValidUntil.Value)
            return Task.FromResult(false);

        // Check work days
        if (WorkDays != null && WorkDays.Any())
        {
            var currentDay = now.DayOfWeek.ToString().Substring(0, 3);
            if (!WorkDays.Contains(currentDay))
                return Task.FromResult(false);
        }

        // Check work hours
        if (!string.IsNullOrEmpty(WorkHours))
        {
            var parts = WorkHours.Split('-');
            if (parts.Length == 2)
            {
                if (TimeOnly.TryParse(parts[0], out var startTime) &&
                    TimeOnly.TryParse(parts[1], out var endTime))
                {
                    var currentTime = TimeOnly.FromDateTime(now);
                    if (currentTime < startTime || currentTime > endTime)
                        return Task.FromResult(false);
                }
            }
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// Location-based permission condition
/// </summary>
public class LocationCondition : PermissionCondition
{
    public override string Type => "location";

    [JsonPropertyName("allowedIPs")]
    public List<string>? AllowedIPs { get; set; }

    [JsonPropertyName("blockedIPs")]
    public List<string>? BlockedIPs { get; set; }

    [JsonPropertyName("allowedCountries")]
    public List<string>? AllowedCountries { get; set; }

    public override Task<bool> EvaluateAsync(PermissionConditionContext context)
    {
        if (string.IsNullOrEmpty(context.IpAddress))
            return Task.FromResult(true); // No IP to check

        // Check blocked IPs first
        if (BlockedIPs != null && BlockedIPs.Contains(context.IpAddress))
            return Task.FromResult(false);

        // Check allowed IPs
        if (AllowedIPs != null && AllowedIPs.Any())
        {
            if (!AllowedIPs.Contains(context.IpAddress) &&
                !AllowedIPs.Any(ip => IsIpInRange(context.IpAddress, ip)))
                return Task.FromResult(false);
        }

        // Country check would require IP geolocation service
        // For now, return true if no specific IP restrictions failed
        return Task.FromResult(true);
    }

    private bool IsIpInRange(string ipAddress, string range)
    {
        // Simple CIDR range check implementation
        // In production, use a proper IP range library
        if (!range.Contains('/'))
            return false;

        // Simplified check - in production use proper IP range validation
        var parts = range.Split('/');
        return ipAddress.StartsWith(parts[0].Substring(0, parts[0].LastIndexOf('.')));
    }
}

/// <summary>
/// Resource-based permission condition
/// </summary>
public class ResourceCondition : PermissionCondition
{
    public override string Type => "resource";

    [JsonPropertyName("ownDataOnly")]
    public bool OwnDataOnly { get; set; }

    [JsonPropertyName("teamOnly")]
    public bool TeamOnly { get; set; }

    [JsonPropertyName("maxAmount")]
    public decimal? MaxAmount { get; set; }

    [JsonPropertyName("allowedResources")]
    public List<string>? AllowedResources { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    public override Task<bool> EvaluateAsync(PermissionConditionContext context)
    {
        // Check if resource is in allowed list
        if (AllowedResources != null && AllowedResources.Any())
        {
            if (string.IsNullOrEmpty(context.Resource) ||
                !AllowedResources.Contains(context.Resource))
                return Task.FromResult(false);
        }

        // Check amount limit if provided in custom data
        if (MaxAmount.HasValue && context.CustomData.ContainsKey("amount"))
        {
            if (context.CustomData["amount"] is decimal amount && amount > MaxAmount.Value)
                return Task.FromResult(false);
        }

        // Check department if provided
        if (!string.IsNullOrEmpty(Department) && context.CustomData.ContainsKey("department"))
        {
            if (context.CustomData["department"]?.ToString() != Department)
                return Task.FromResult(false);
        }

        // OwnDataOnly and TeamOnly checks would require additional context
        // These would be implemented based on specific business logic
        return Task.FromResult(true);
    }
}

/// <summary>
/// Composite condition that combines multiple conditions
/// </summary>
public class CompositeCondition : PermissionCondition
{
    public override string Type => "composite";

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "AND"; // AND or OR

    [JsonPropertyName("conditions")]
    public List<PermissionCondition> Conditions { get; set; } = new();

    public override async Task<bool> EvaluateAsync(PermissionConditionContext context)
    {
        if (!Conditions.Any())
            return true;

        if (Operator.ToUpper() == "OR")
        {
            foreach (var condition in Conditions)
            {
                if (await condition.EvaluateAsync(context))
                    return true;
            }
            return false;
        }
        else // AND
        {
            foreach (var condition in Conditions)
            {
                if (!await condition.EvaluateAsync(context))
                    return false;
            }
            return true;
        }
    }
}