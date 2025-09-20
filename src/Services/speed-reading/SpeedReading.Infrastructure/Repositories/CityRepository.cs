using Microsoft.EntityFrameworkCore;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Infrastructure.Data;

namespace SpeedReading.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly SpeedReadingDbContext _context;

    public CityRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Include(c => c.Districts.Where(d => d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<City>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .Where(c => c.IsActive && c.Name.Contains(searchTerm))
            .OrderBy(c => c.Name)
            .Include(c => c.Districts.Where(d => d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .Include(c => c.Districts.Where(d => d.IsActive))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
    }

    public async Task<City?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .Include(c => c.Districts.Where(d => d.IsActive))
            .FirstOrDefaultAsync(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.IsActive, cancellationToken);
    }

    public async Task<List<City>> GetByRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .Where(c => c.Region.Equals(region, StringComparison.OrdinalIgnoreCase) && c.IsActive)
            .OrderBy(c => c.Name)
            .Include(c => c.Districts.Where(d => d.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public class DistrictRepository : IDistrictRepository
{
    private readonly SpeedReadingDbContext _context;

    public DistrictRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<List<District>> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Where(d => d.CityId == cityId && d.IsActive)
            .OrderBy(d => d.Name)
            .Include(d => d.City)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<District>> SearchByCityAsync(int cityId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Where(d => d.CityId == cityId && d.IsActive && d.Name.Contains(searchTerm))
            .OrderBy(d => d.Name)
            .Include(d => d.City)
            .ToListAsync(cancellationToken);
    }

    public async Task<District?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Include(d => d.City)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);
    }

    public async Task<District?> GetByCityAndNameAsync(int cityId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Include(d => d.City)
            .FirstOrDefaultAsync(d => d.CityId == cityId && 
                                   d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                                   d.IsActive, cancellationToken);
    }

    public async Task<bool> ValidateCityDistrictAsync(string cityName, string districtName, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Include(d => d.City)
            .AnyAsync(d => d.City.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase) &&
                          d.Name.Equals(districtName, StringComparison.OrdinalIgnoreCase) &&
                          d.IsActive && d.City.IsActive, cancellationToken);
    }
}