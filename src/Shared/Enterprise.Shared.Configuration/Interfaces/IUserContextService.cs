namespace Enterprise.Shared.Configuration.Interfaces;

/// <summary>
/// Service for providing user context information
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user ID
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    string? GetCurrentUserId();

    /// <summary>
    /// Gets the current user's roles
    /// </summary>
    /// <returns>List of user roles</returns>
    IEnumerable<string> GetCurrentUserRoles();

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    bool HasRole(string role);

    /// <summary>
    /// Gets user claims
    /// </summary>
    /// <returns>Dictionary of user claims</returns>
    Dictionary<string, string> GetUserClaims();

    /// <summary>
    /// Gets a specific user claim value
    /// </summary>
    /// <param name="claimType">Claim type</param>
    /// <returns>Claim value or null if not found</returns>
    string? GetClaimValue(string claimType);

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    /// <returns>True if user is authenticated</returns>
    bool IsAuthenticated();
}