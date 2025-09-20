using Gateway.Core.Models;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Common.Enums;

namespace Gateway.Core.Interfaces;

/// <summary>
/// Gateway service for managing proxy operations
/// </summary>
public interface IGatewayService
{
    /// <summary>
    /// Validates if the route is allowed for the user
    /// </summary>
    Task<Result<bool>> ValidateRouteAccessAsync(string route, string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs gateway request information
    /// </summary>
    Task LogRequestAsync(GatewayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs gateway response information
    /// </summary>
    Task LogResponseAsync(GatewayResponse response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks rate limit for the given key
    /// </summary>
    Task<Result<RateLimitInfo>> CheckRateLimitAsync(string key, string endpoint, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for health checks of downstream services
/// </summary>
public interface IServiceHealthService
{
    /// <summary>
    /// Checks health of a specific service
    /// </summary>
    Task<ServiceHealthResult> CheckServiceHealthAsync(string serviceName, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets health status of all configured services
    /// </summary>
    Task<Result<IEnumerable<ServiceHealthResult>>> GetAllServiceHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Authentication service for gateway
/// </summary>
public interface IGatewayAuthenticationService
{
    /// <summary>
    /// Validates JWT token and extracts user information
    /// </summary>
    Task<Result<GatewayUserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has permission for the requested resource
    /// </summary>
    Task<Result<bool>> CheckPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service discovery for managing service endpoints
/// </summary>
public interface IServiceDiscoveryService
{
    /// <summary>
    /// Discovers all available services
    /// </summary>
    Task<Result<IEnumerable<ServiceEndpoint>>> DiscoverServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific service endpoint with load balancing
    /// </summary>
    Task<Result<ServiceEndpoint>> GetServiceEndpointAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new service endpoint
    /// </summary>
    Task<Result> RegisterServiceAsync(ServiceEndpoint serviceEndpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters a service endpoint
    /// </summary>
    Task<Result> DeregisterServiceAsync(string serviceName, string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered services
    /// </summary>
    Task<Result<IEnumerable<ServiceEndpoint>>> GetAllServicesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// API Key authentication service
/// </summary>
public interface IApiKeyAuthenticationService
{
    /// <summary>
    /// Validates an API key and returns user information
    /// </summary>
    Task<Result<GatewayUserInfo>> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key
    /// </summary>
    Task<Result<ApiKeyInfo>> CreateApiKeyAsync(string name, List<string> permissions, DateTime? expiresAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task<Result> RevokeApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API keys (without plain key values)
    /// </summary>
    Task<Result<IEnumerable<ApiKeyInfo>>> GetApiKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Gateway user information
/// </summary>
public class GatewayUserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public DateTime ExpiresAt { get; set; }
}