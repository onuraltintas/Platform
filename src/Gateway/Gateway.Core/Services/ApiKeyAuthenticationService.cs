using Gateway.Core.Interfaces;
using Gateway.Core.Models;
using Gateway.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Common.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Gateway.Core.Services;

public class ApiKeyAuthenticationService : IApiKeyAuthenticationService
{
    private readonly ILogger<ApiKeyAuthenticationService> _logger;
    private readonly GatewayOptions _options;
    private readonly Dictionary<string, ApiKeyInfo> _apiKeys;

    public ApiKeyAuthenticationService(
        ILogger<ApiKeyAuthenticationService> logger,
        IOptions<GatewayOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _apiKeys = InitializeApiKeys();
    }

    public async Task<Result<GatewayUserInfo>> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Empty API key provided for validation");
                return Result<GatewayUserInfo>.Failure("API key is required", OperationStatus.ValidationFailed);
            }

            var hashedKey = HashApiKey(apiKey);
            
            if (!_apiKeys.ContainsKey(hashedKey))
            {
                _logger.LogWarning("Invalid API key provided: {KeyPrefix}", GetKeyPrefix(apiKey));
                return Result<GatewayUserInfo>.Failure("Invalid API key", OperationStatus.Unauthorized);
            }

            var keyInfo = _apiKeys[hashedKey];
            
            // Check if key is active
            if (!keyInfo.IsActive)
            {
                _logger.LogWarning("Inactive API key used: {KeyName}", keyInfo.Name);
                return Result<GatewayUserInfo>.Failure("API key is inactive", OperationStatus.Unauthorized);
            }

            // Check expiry
            if (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key used: {KeyName}", keyInfo.Name);
                return Result<GatewayUserInfo>.Failure("API key has expired", OperationStatus.Unauthorized);
            }

            // Check rate limits
            if (await IsRateLimited(keyInfo, cancellationToken))
            {
                _logger.LogWarning("API key rate limit exceeded: {KeyName}", keyInfo.Name);
                return Result<GatewayUserInfo>.Failure("Rate limit exceeded", OperationStatus.RateLimitExceeded);
            }

            // Update last used time
            keyInfo.LastUsedAt = DateTime.UtcNow;
            keyInfo.UsageCount++;

            var userInfo = CreateUserInfoFromApiKey(keyInfo);
            
            _logger.LogDebug("API key validated successfully: {KeyName}", keyInfo.Name);
            return Result<GatewayUserInfo>.Success(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return Result<GatewayUserInfo>.Failure("API key validation failed", OperationStatus.Failed);
        }
    }

    public async Task<Result<ApiKeyInfo>> CreateApiKeyAsync(string name, List<string> permissions, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<ApiKeyInfo>.Failure("API key name is required", OperationStatus.ValidationFailed);
            }

            var apiKey = GenerateApiKey();
            var hashedKey = HashApiKey(apiKey);
            
            var keyInfo = new ApiKeyInfo
            {
                Id = Guid.NewGuid(),
                Name = name,
                HashedKey = hashedKey,
                PlainKey = apiKey, // Only returned during creation
                Permissions = permissions ?? new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                UsageCount = 0,
                RateLimitPerMinute = 100, // Default rate limit
                RateLimitPerHour = 1000
            };

            _apiKeys[hashedKey] = keyInfo;
            
            _logger.LogInformation("API key created: {KeyName}", name);
            return Result<ApiKeyInfo>.Success(keyInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return Result<ApiKeyInfo>.Failure("API key creation failed", OperationStatus.Failed);
        }
    }

    public async Task<Result> RevokeApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashedKey = HashApiKey(apiKey);
            
            if (!_apiKeys.ContainsKey(hashedKey))
            {
                return Result.Failure("API key not found", OperationStatus.NotFound);
            }

            _apiKeys[hashedKey].IsActive = false;
            _apiKeys[hashedKey].RevokedAt = DateTime.UtcNow;
            
            _logger.LogInformation("API key revoked: {KeyName}", _apiKeys[hashedKey].Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key");
            return Result.Failure("API key revocation failed", OperationStatus.Failed);
        }
    }

    public async Task<Result<IEnumerable<ApiKeyInfo>>> GetApiKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = _apiKeys.Values
                .Select(k => new ApiKeyInfo
                {
                    Id = k.Id,
                    Name = k.Name,
                    PlainKey = null, // Never return plain key in list operations
                    HashedKey = k.HashedKey,
                    Permissions = k.Permissions,
                    IsActive = k.IsActive,
                    CreatedAt = k.CreatedAt,
                    ExpiresAt = k.ExpiresAt,
                    LastUsedAt = k.LastUsedAt,
                    UsageCount = k.UsageCount,
                    RateLimitPerMinute = k.RateLimitPerMinute,
                    RateLimitPerHour = k.RateLimitPerHour,
                    RevokedAt = k.RevokedAt
                })
                .ToList();

            return Result<IEnumerable<ApiKeyInfo>>.Success(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return Result<IEnumerable<ApiKeyInfo>>.Failure("Failed to retrieve API keys", OperationStatus.Failed);
        }
    }

    private Dictionary<string, ApiKeyInfo> InitializeApiKeys()
    {
        // Initialize with some default API keys for development
        var keys = new Dictionary<string, ApiKeyInfo>();
        
        if (_options.Security.EnableApiKeyAuthentication)
        {
            // Create a default admin API key
            var defaultKey = "gw_dev_admin_key_12345678901234567890";
            var hashedDefaultKey = HashApiKey(defaultKey);
            
            keys[hashedDefaultKey] = new ApiKeyInfo
            {
                Id = Guid.NewGuid(),
                Name = "Default Admin Key",
                HashedKey = hashedDefaultKey,
                Permissions = new List<string> { "admin", "read", "write", "delete" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = null, // Never expires
                RateLimitPerMinute = 1000,
                RateLimitPerHour = 10000
            };

            _logger.LogInformation("Initialized default API keys for development");
        }

        return keys;
    }

    private static string GenerateApiKey()
    {
        const string prefix = "gw_";
        const int keyLength = 32;
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[keyLength];
        rng.GetBytes(bytes);
        
        return prefix + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..keyLength];
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string GetKeyPrefix(string apiKey)
    {
        return apiKey.Length > 8 ? apiKey[..8] + "..." : apiKey;
    }

    private async Task<bool> IsRateLimited(ApiKeyInfo keyInfo, CancellationToken cancellationToken)
    {
        // TODO: Implement proper rate limiting with distributed cache
        // For now, this is a placeholder that always returns false
        await Task.Delay(1, cancellationToken);
        return false;
    }

    private static GatewayUserInfo CreateUserInfoFromApiKey(ApiKeyInfo keyInfo)
    {
        return new GatewayUserInfo
        {
            UserId = keyInfo.Id.ToString(),
            Email = $"apikey+{keyInfo.Name.ToLower().Replace(" ", "")}@gateway.local",
            FirstName = "API",
            LastName = "Key",
            Roles = new List<string> { "api-client" },
            Permissions = keyInfo.Permissions,
            ExpiresAt = keyInfo.ExpiresAt ?? DateTime.UtcNow.AddYears(10)
        };
    }
}