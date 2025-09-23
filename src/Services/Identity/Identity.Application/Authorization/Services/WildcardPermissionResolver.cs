using Microsoft.Extensions.Logging;

namespace Identity.Application.Authorization.Services;

/// <summary>
/// Service for resolving wildcard permissions
/// Supports patterns like:
/// - "*.*.*" matches everything (SuperAdmin)
/// - "Identity.*.*" matches all Identity service permissions
/// - "*.Users.*" matches Users resource in all services
/// - "*.*.Read" matches all Read actions
/// </summary>
public class WildcardPermissionResolver : IWildcardPermissionResolver
{
    private readonly ILogger<WildcardPermissionResolver> _logger;

    public WildcardPermissionResolver(ILogger<WildcardPermissionResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if a user permission (which might be a wildcard) matches a required permission
    /// </summary>
    public bool HasPermission(string userPermission, string requiredPermission)
    {
        // Normalize permissions to lowercase for case-insensitive comparison
        userPermission = userPermission?.ToLower() ?? string.Empty;
        requiredPermission = requiredPermission?.ToLower() ?? string.Empty;

        // SuperAdmin bypass - has all permissions
        if (userPermission == "*.*.*")
        {
            _logger.LogDebug("SuperAdmin wildcard (*.*.*) matched for {RequiredPermission}", requiredPermission);
            return true;
        }

        // Direct match
        if (userPermission == requiredPermission)
        {
            _logger.LogDebug("Direct permission match: {Permission}", requiredPermission);
            return true;
        }

        // Wildcard matching
        if (userPermission.Contains("*"))
        {
            return MatchWildcard(userPermission, requiredPermission);
        }

        return false;
    }

    /// <summary>
    /// Check if any of the user's permissions match the required permission
    /// </summary>
    public bool HasAnyPermission(IEnumerable<string> userPermissions, string requiredPermission)
    {
        return userPermissions?.Any(p => HasPermission(p, requiredPermission)) ?? false;
    }

    /// <summary>
    /// Check if user has all of the required permissions
    /// </summary>
    public bool HasAllPermissions(IEnumerable<string> userPermissions, IEnumerable<string> requiredPermissions)
    {
        var userPermList = userPermissions?.ToList() ?? new List<string>();
        return requiredPermissions?.All(rp => HasAnyPermission(userPermList, rp)) ?? true;
    }

    /// <summary>
    /// Expand wildcard patterns to actual permissions
    /// </summary>
    public IEnumerable<string> ExpandWildcards(string pattern, IEnumerable<string> allPermissions)
    {
        if (!pattern.Contains("*"))
        {
            // Not a wildcard, return as-is if it exists
            return allPermissions.Contains(pattern) ? new[] { pattern } : Array.Empty<string>();
        }

        // Convert wildcard pattern to regex
        var regexPattern = WildcardToRegex(pattern);
        var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return allPermissions.Where(p => regex.IsMatch(p));
    }

    private bool MatchWildcard(string wildcardPattern, string permission)
    {
        // Split both patterns into parts (Service.Resource.Action)
        var patternParts = wildcardPattern.Split('.');
        var permissionParts = permission.Split('.');

        // If different number of parts, they can't match (unless using advanced wildcards)
        if (patternParts.Length != permissionParts.Length)
        {
            // Special case: "Identity.*" could match "Identity.Users.Read"
            if (patternParts.Length == 2 && patternParts[1] == "*")
            {
                return permissionParts[0].Equals(patternParts[0], StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        // Compare each part
        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "*")
            {
                // Wildcard matches anything
                continue;
            }

            if (!patternParts[i].Equals(permissionParts[i], StringComparison.OrdinalIgnoreCase))
            {
                // Parts don't match
                return false;
            }
        }

        _logger.LogDebug("Wildcard pattern {Pattern} matched permission {Permission}", wildcardPattern, permission);
        return true;
    }

    private string WildcardToRegex(string pattern)
    {
        // Escape special regex characters except *
        var escaped = System.Text.RegularExpressions.Regex.Escape(pattern);
        // Replace escaped \* with .*
        escaped = escaped.Replace("\\*", ".*");
        // Ensure it matches the entire string
        return $"^{escaped}$";
    }
}

/// <summary>
/// Interface for wildcard permission resolution
/// </summary>
public interface IWildcardPermissionResolver
{
    bool HasPermission(string userPermission, string requiredPermission);
    bool HasAnyPermission(IEnumerable<string> userPermissions, string requiredPermission);
    bool HasAllPermissions(IEnumerable<string> userPermissions, IEnumerable<string> requiredPermissions);
    IEnumerable<string> ExpandWildcards(string pattern, IEnumerable<string> allPermissions);
}