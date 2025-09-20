using System.IdentityModel.Tokens.Jwt;

namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token
    /// </summary>
    /// <param name="claims">The claims to include in the token</param>
    /// <param name="expirationMinutes">Token expiration in minutes (optional)</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(IEnumerable<Claim> claims, int? expirationMinutes = null);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>Principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets claims from a token without validation
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Claims from the token</returns>
    IEnumerable<Claim> GetClaimsFromToken(string token);

    /// <summary>
    /// Gets the token expiration time
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Expiration time or null if invalid</returns>
    DateTime? GetTokenExpiration(string token);

    /// <summary>
    /// Refreshes an access token
    /// </summary>
    /// <param name="accessToken">The current access token</param>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>New access token or null if refresh fails</returns>
    Task<string?> RefreshAccessTokenAsync(string accessToken, string refreshToken);

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    Task RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Checks if a token is revoked
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True if revoked</returns>
    Task<bool> IsTokenRevokedAsync(string token);
}