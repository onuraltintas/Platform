using EgitimPlatform.Shared.Security.Models;
using System.Security.Claims;

namespace EgitimPlatform.Shared.Security.Services;

public interface ITokenService
{
    Task<TokenResult> GenerateTokenAsync(SecurityUser user, string? deviceId = null, string? ipAddress = null);
    Task<TokenResult?> RefreshTokenAsync(string refreshToken, string? deviceId = null, string? ipAddress = null);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<bool> RevokeAllUserTokensAsync(string userId);
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> IsTokenValidAsync(string token);
    string GenerateSecureToken();
}