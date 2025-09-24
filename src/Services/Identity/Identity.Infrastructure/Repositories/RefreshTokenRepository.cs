using System.Security.Cryptography;
using System.Text;
using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        // Store only a hash of the token
        token.Token = Hash(token.Token);
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByHashedTokenAsync(string hashedToken, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);
    }

    public async Task MarkUsedAsync(string hashedToken, string? replacedByHashedToken = null, CancellationToken cancellationToken = default)
    {
        var entity = await GetByHashedTokenAsync(hashedToken, cancellationToken);
        if (entity == null) return;
        entity.IsUsed = true;
        entity.ReplacedByToken = replacedByHashedToken;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(string hashedToken, string reason, CancellationToken cancellationToken = default)
    {
        var entity = await GetByHashedTokenAsync(hashedToken, cancellationToken);
        if (entity == null) return;
        entity.IsRevoked = true;
        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokedReason = reason;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllByUserAsync(string userId, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed);
        if (!string.IsNullOrEmpty(deviceId))
        {
            query = query.Where(rt => rt.DeviceId == deviceId);
        }

        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(rt => rt.IsRevoked, true)
            .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
            .SetProperty(rt => rt.RevokedReason, "User logout"), cancellationToken);
    }

    private static string Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

