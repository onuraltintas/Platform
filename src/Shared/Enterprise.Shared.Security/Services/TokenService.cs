using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly SecuritySettings _settings;
    private readonly IMemoryCache _cache;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public TokenService(
        ILogger<TokenService> logger,
        IOptions<SecuritySettings> settings,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _tokenHandler = new JwtSecurityTokenHandler();

        // Setup signing credentials
        var key = Encoding.UTF8.GetBytes(_settings.JwtSecretKey ?? throw new InvalidOperationException("JWT secret key is not configured"));
        var securityKey = new SymmetricSecurityKey(key);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Setup validation parameters
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.JwtIssuer,
            ValidAudience = _settings.JwtAudience,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.FromMinutes(_settings.JwtClockSkewMinutes ?? 5)
        };

        _logger.LogDebug("Token service initialized");
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims, int? expirationMinutes = null)
    {
        if (claims == null)
            throw new ArgumentNullException(nameof(claims));

        try
        {
            var expiration = expirationMinutes ?? _settings.JwtAccessTokenExpirationMinutes;
            // Ensure minimum 1 minute expiration to avoid JWT validation issues
            if (expiration <= 0) expiration = 1;
            var expires = DateTime.UtcNow.AddMinutes(expiration);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = _settings.JwtIssuer,
                Audience = _settings.JwtAudience,
                SigningCredentials = _signingCredentials,
                NotBefore = DateTime.UtcNow
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogDebug("Access token generated successfully");
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token");
            throw new SecurityException("Token generation failed", ex);
        }
    }

    public string GenerateRefreshToken()
    {
        try
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var refreshToken = Convert.ToBase64String(randomBytes);

            _logger.LogDebug("Refresh token generated successfully");
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            throw new SecurityException("Refresh token generation failed", ex);
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Invalid token algorithm");
                return null;
            }

            _logger.LogDebug("Token validated successfully");
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    public IEnumerable<Claim> GetClaimsFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading claims from token");
            return Enumerable.Empty<Claim>();
        }
    }

    public DateTime? GetTokenExpiration(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token expiration");
            return null;
        }
    }

    public async Task<string?> RefreshAccessTokenAsync(string accessToken, string refreshToken)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

        try
        {
            // Check if refresh token is valid (not revoked)
            var isRevoked = await IsTokenRevokedAsync(refreshToken);
            if (isRevoked)
            {
                _logger.LogWarning("Refresh token is revoked");
                return null;
            }

            // Get claims from expired access token
            var validationParams = _validationParameters.Clone();
            validationParams.ValidateLifetime = false; // Don't validate expiration

            var principal = _tokenHandler.ValidateToken(accessToken, validationParams, out _);
            if (principal == null)
            {
                _logger.LogWarning("Invalid access token for refresh");
                return null;
            }

            // Generate new access token with same claims
            var newToken = GenerateAccessToken(principal.Claims);

            _logger.LogInformation("Access token refreshed successfully");
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            return null;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

        try
        {
            var cacheKey = $"revoked_token_{refreshToken}";
            var expirationDays = _settings.RefreshTokenExpirationDays;

            // Store in cache with expiration
            _cache.Set(cacheKey, true, TimeSpan.FromDays(expirationDays));

            _logger.LogInformation("Refresh token revoked");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        try
        {
            var cacheKey = $"revoked_token_{token}";
            var isRevoked = _cache.TryGetValue(cacheKey, out bool _);

            await Task.CompletedTask;
            return isRevoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is revoked");
            return false;
        }
    }
}