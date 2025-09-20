namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for managing API keys
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private readonly ILogger<ApiKeyService> _logger;
    private readonly IHashingService _hashingService;
    private readonly IMemoryCache _cache;
    private readonly SecuritySettings _settings;

    // In-memory storage for demo (should use database in production)
    private readonly Dictionary<string, ApiKeyDetails> _apiKeys = new();
    private readonly Dictionary<string, DateTime> _revokedKeys = new();
    private readonly Dictionary<string, List<DateTime>> _usageTracking = new();

    public ApiKeyService(
        ILogger<ApiKeyService> logger,
        IHashingService hashingService,
        IMemoryCache cache,
        IOptions<SecuritySettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hashingService = hashingService ?? throw new ArgumentNullException(nameof(hashingService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _logger.LogDebug("API key service initialized");
    }

    public ApiKey GenerateApiKey(Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Generate a secure random API key
            var keyBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(keyBytes);

            var apiKeyString = Convert.ToBase64String(keyBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            var apiKey = new ApiKey
            {
                Key = $"sk_{apiKeyString}",
                HashedKey = _hashingService.ComputeSha256(apiKeyString),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = _settings.ApiKeyExpirationDays.HasValue
                    ? DateTime.UtcNow.AddDays(_settings.ApiKeyExpirationDays.Value)
                    : null,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Store in memory (should use database in production)
            var details = new ApiKeyDetails
            {
                Key = apiKey.HashedKey,
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt,
                IsActive = true,
                UsageCount = 0,
                Metadata = apiKey.Metadata,
                Permissions = new List<string>()
            };

            _apiKeys[apiKey.HashedKey] = details;

            _logger.LogInformation("API key generated successfully");
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key");
            throw new SecurityException("API key generation failed", ex);
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return false;

        try
        {
            // Check cache first
            var cacheKey = $"api_key_valid_{apiKey}";
            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }

            // Remove prefix if present
            var keyToHash = apiKey.StartsWith("sk_") ? apiKey[3..] : apiKey;
            var hashedKey = _hashingService.ComputeSha256(keyToHash);

            // Check if key exists and is valid
            if (!_apiKeys.TryGetValue(hashedKey, out var details))
            {
                _logger.LogWarning("Invalid API key attempted");
                return false;
            }

            // Check if revoked
            if (_revokedKeys.ContainsKey(hashedKey))
            {
                _logger.LogWarning("Revoked API key attempted");
                return false;
            }

            // Check if expired
            if (details.ExpiresAt.HasValue && details.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key attempted");
                return false;
            }

            // Check if active
            if (!details.IsActive)
            {
                _logger.LogWarning("Inactive API key attempted");
                return false;
            }

            // Cache the result
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return false;
        }
    }

    public async Task<ApiKeyDetails?> GetApiKeyDetailsAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        try
        {
            var keyToHash = apiKey.StartsWith("sk_") ? apiKey[3..] : apiKey;
            var hashedKey = _hashingService.ComputeSha256(keyToHash);

            if (_apiKeys.TryGetValue(hashedKey, out var details))
            {
                await Task.CompletedTask;
                return details;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key details");
            return null;
        }
    }

    public async Task RevokeApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        try
        {
            var keyToHash = apiKey.StartsWith("sk_") ? apiKey[3..] : apiKey;
            var hashedKey = _hashingService.ComputeSha256(keyToHash);

            _revokedKeys[hashedKey] = DateTime.UtcNow;

            // Clear cache
            var cacheKey = $"api_key_valid_{apiKey}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("API key revoked");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key");
            throw;
        }
    }

    public async Task<ApiKey> RefreshApiKeyAsync(string oldApiKey)
    {
        if (string.IsNullOrEmpty(oldApiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(oldApiKey));

        try
        {
            // Get old key details
            var details = await GetApiKeyDetailsAsync(oldApiKey);
            if (details == null)
            {
                throw new SecurityException("Invalid API key");
            }

            // Revoke old key
            await RevokeApiKeyAsync(oldApiKey);

            // Generate new key with same metadata
            var newApiKey = GenerateApiKey(details.Metadata);

            // Copy permissions
            if (_apiKeys.TryGetValue(newApiKey.HashedKey, out var newDetails))
            {
                newDetails.Permissions = details.Permissions;
            }

            _logger.LogInformation("API key refreshed successfully");
            return newApiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing API key");
            throw;
        }
    }

    public async Task TrackUsageAsync(string apiKey, string endpoint)
    {
        if (string.IsNullOrEmpty(apiKey))
            return;

        try
        {
            var keyToHash = apiKey.StartsWith("sk_") ? apiKey[3..] : apiKey;
            var hashedKey = _hashingService.ComputeSha256(keyToHash);

            // Update usage count
            if (_apiKeys.TryGetValue(hashedKey, out var details))
            {
                details.UsageCount++;
                details.LastUsedAt = DateTime.UtcNow;
            }

            // Track usage for rate limiting
            if (!_usageTracking.ContainsKey(hashedKey))
            {
                _usageTracking[hashedKey] = new List<DateTime>();
            }

            _usageTracking[hashedKey].Add(DateTime.UtcNow);

            // Clean old entries (keep only last hour)
            var cutoff = DateTime.UtcNow.AddHours(-1);
            _usageTracking[hashedKey] = _usageTracking[hashedKey]
                .Where(dt => dt > cutoff)
                .ToList();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking API key usage");
        }
    }

    public async Task<bool> IsRateLimitExceededAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return true;

        try
        {
            var keyToHash = apiKey.StartsWith("sk_") ? apiKey[3..] : apiKey;
            var hashedKey = _hashingService.ComputeSha256(keyToHash);

            if (!_usageTracking.TryGetValue(hashedKey, out var usages))
            {
                return false;
            }

            // Check rate limit (default: 1000 requests per hour)
            var limit = _settings.ApiKeyRateLimit ?? 1000;
            var windowStart = DateTime.UtcNow.AddHours(-1);
            var recentUsages = usages.Count(dt => dt > windowStart);

            var exceeded = recentUsages >= limit;

            if (exceeded)
            {
                _logger.LogWarning("API key rate limit exceeded");
            }

            await Task.CompletedTask;
            return exceeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit");
            return false;
        }
    }
}