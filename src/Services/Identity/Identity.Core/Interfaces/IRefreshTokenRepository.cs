using Identity.Core.Entities;

namespace Identity.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashedTokenAsync(string hashedToken, CancellationToken cancellationToken = default);
    Task MarkUsedAsync(string hashedToken, string? replacedByHashedToken = null, CancellationToken cancellationToken = default);
    Task RevokeAsync(string hashedToken, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllByUserAsync(string userId, string? deviceId = null, CancellationToken cancellationToken = default);
}

