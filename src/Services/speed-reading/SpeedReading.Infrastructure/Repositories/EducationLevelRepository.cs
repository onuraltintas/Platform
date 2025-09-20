using Microsoft.EntityFrameworkCore;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Infrastructure.Data;

namespace SpeedReading.Infrastructure.Repositories;

public class EducationLevelRepository : IEducationLevelRepository
{
    private readonly SpeedReadingDbContext _context;

    public EducationLevelRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<List<EducationLevel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EducationLevels
            .Where(e => e.IsActive)
            .OrderBy(e => e.GradeNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EducationLevel>> GetByCategoryAsync(int category, CancellationToken cancellationToken = default)
    {
        return await _context.EducationLevels
            .Where(e => e.IsActive && (int)e.Category == category)
            .OrderBy(e => e.GradeNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<EducationLevel?> GetByGradeLevelAsync(int gradeLevel, CancellationToken cancellationToken = default)
    {
        return await _context.EducationLevels
            .FirstOrDefaultAsync(e => e.GradeNumber == gradeLevel && e.IsActive, cancellationToken);
    }

    public async Task<EducationLevel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.EducationLevels
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);
    }
}