namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for managing API keys
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key
    /// </summary>
    /// <param name="metadata">Optional metadata for the key</param>
    /// <returns>Generated API key</returns>
    ApiKey GenerateApiKey(Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Validates an API key
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if valid</returns>
    Task<bool> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Gets API key details
    /// </summary>
    /// <param name="apiKey">The API key</param>
    /// <returns>API key details or null if not found</returns>
    Task<ApiKeyDetails?> GetApiKeyDetailsAsync(string apiKey);

    /// <summary>
    /// Revokes an API key
    /// </summary>
    /// <param name="apiKey">The API key to revoke</param>
    Task RevokeApiKeyAsync(string apiKey);

    /// <summary>
    /// Refreshes an API key
    /// </summary>
    /// <param name="oldApiKey">The old API key</param>
    /// <returns>New API key</returns>
    Task<ApiKey> RefreshApiKeyAsync(string oldApiKey);

    /// <summary>
    /// Tracks API key usage
    /// </summary>
    /// <param name="apiKey">The API key</param>
    /// <param name="endpoint">The endpoint accessed</param>
    Task TrackUsageAsync(string apiKey, string endpoint);

    /// <summary>
    /// Checks if rate limit is exceeded
    /// </summary>
    /// <param name="apiKey">The API key</param>
    /// <returns>True if rate limit exceeded</returns>
    Task<bool> IsRateLimitExceededAsync(string apiKey);
}

/// <summary>
/// Represents an API key
/// </summary>
public class ApiKey
{
    public string Key { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// API key details
/// </summary>
public class ApiKeyDetails
{
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public long UsageCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}