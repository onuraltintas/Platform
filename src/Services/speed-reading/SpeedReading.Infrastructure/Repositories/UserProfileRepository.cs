using Microsoft.EntityFrameworkCore;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Infrastructure.Data;

namespace SpeedReading.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly SpeedReadingDbContext _context;

    public UserProfileRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<UserReadingProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserReadingProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);
    }

    public async Task<UserReadingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserReadingProfiles
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
    }

    public async Task<List<UserReadingProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserReadingProfiles
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserReadingProfile> AddAsync(UserReadingProfile profile, CancellationToken cancellationToken = default)
    {
        var entry = await _context.UserReadingProfiles.AddAsync(profile, cancellationToken);
        return entry.Entity;
    }

    public async Task<UserReadingProfile> UpdateAsync(UserReadingProfile profile, CancellationToken cancellationToken = default)
    {
        _context.UserReadingProfiles.Update(profile);
        return await Task.FromResult(profile);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserReadingProfiles
            .AnyAsync(p => p.UserId == userId && p.IsActive, cancellationToken);
    }

    public async Task<int> GetTotalUserCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserReadingProfiles
            .CountAsync(p => p.IsActive, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}