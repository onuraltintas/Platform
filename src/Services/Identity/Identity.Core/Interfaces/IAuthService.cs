using Identity.Core.DTOs;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IAuthService
{
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> LogoutAsync(string userId, string? deviceId = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> LogoutAllDevicesAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> SwitchGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    Task<Result<TokenResponse>> GenerateTokenAsync(string userId, Guid? groupId = null, string? deviceId = null, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateRefreshTokenAsync();
    Task<Result<bool>> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<string>> GetUserIdFromTokenAsync(string token);
    Task<Result<bool>> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
    Task<Result<bool>> BlacklistTokenAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
}