using Enterprise.Shared.Configuration.Interfaces;

namespace Enterprise.Shared.Configuration.Services;

/// <summary>
/// Default implementation of user context service for scenarios where no HTTP context is available
/// </summary>
public sealed class DefaultUserContextService : IUserContextService
{
    private readonly ILogger<DefaultUserContextService> _logger;

    public DefaultUserContextService(ILogger<DefaultUserContextService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string? GetCurrentUserId()
    {
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetCurrentUserRoles()
    {
        return Enumerable.Empty<string>();
    }

    /// <inheritdoc/>
    public bool HasRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        
        return false;
    }

    /// <inheritdoc/>
    public Dictionary<string, string> GetUserClaims()
    {
        return new Dictionary<string, string>();
    }

    /// <inheritdoc/>
    public string? GetClaimValue(string claimType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);
        
        return null;
    }

    /// <inheritdoc/>
    public bool IsAuthenticated()
    {
        return false;
    }
}