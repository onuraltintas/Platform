namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for hashing and verifying data
/// </summary>
public sealed class HashingService : IHashingService
{
    private readonly ILogger<HashingService> _logger;
    private readonly SecuritySettings _settings;

    public HashingService(
        ILogger<HashingService> logger,
        IOptions<SecuritySettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _logger.LogDebug("Hashing service initialized");
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        try
        {
            // Use BCrypt for password hashing
            var workFactor = _settings.BCryptWorkFactor ?? 12;
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor);

            _logger.LogDebug("Password hashed successfully");
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            throw new SecurityException("Password hashing failed", ex);
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));

        try
        {
            var isValid = BCrypt.Net.BCrypt.Verify(password, hash);

            if (!isValid)
            {
                _logger.LogWarning("Password verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    public string ComputeSha256(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        try
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing SHA256 hash");
            throw new SecurityException("SHA256 hashing failed", ex);
        }
    }

    public string ComputeSha512(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        try
        {
            using var sha512 = SHA512.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha512.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing SHA512 hash");
            throw new SecurityException("SHA512 hashing failed", ex);
        }
    }

    public string ComputeHmacSha256(string input, string key)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hash = hmac.ComputeHash(inputBytes);
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing HMAC-SHA256 hash");
            throw new SecurityException("HMAC-SHA256 hashing failed", ex);
        }
    }

    public string GenerateSalt(int size = 32)
    {
        if (size <= 0)
            throw new ArgumentException("Salt size must be greater than 0", nameof(size));

        try
        {
            var salt = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating salt");
            throw new SecurityException("Salt generation failed", ex);
        }
    }
}