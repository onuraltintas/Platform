using Microsoft.EntityFrameworkCore;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Repositories;
using SpeedReading.Infrastructure.Data;

namespace SpeedReading.Infrastructure.Repositories;

public class ExerciseRepository : IExerciseRepository
{
    private readonly SpeedReadingDbContext _context;

    public ExerciseRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Exercise?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Exercise?> GetByIdWithAttemptsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Include(e => e.Attempts)
                .ThenInclude(a => a.Answers)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Exercise?> GetByIdFullAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .Include(e => e.Questions)
                .ThenInclude(q => q.Answers)
            .Include(e => e.Attempts)
                .ThenInclude(a => a.Answers)
            .Include(e => e.ReadingText)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetActiveExercisesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active && e.IsActive)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByStatusAsync(ExerciseStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByTypeAsync(ExerciseType type, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.Type == type && e.IsActive)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByEducationLevelAsync(EducationCategory educationLevel, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.TargetEducationLevel == educationLevel && e.IsActive)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByDifficultyAsync(TextDifficulty difficulty, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.DifficultyLevel == difficulty && e.IsActive)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByReadingTextAsync(Guid readingTextId, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.ReadingTextId == readingTextId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.CreatedBy == creatorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> SearchAsync(ExerciseSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.Exercises.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(criteria.Title))
        {
            query = query.Where(e => e.Title.Contains(criteria.Title));
        }

        if (!string.IsNullOrEmpty(criteria.Description))
        {
            query = query.Where(e => e.Description.Contains(criteria.Description));
        }

        if (criteria.Type.HasValue)
        {
            query = query.Where(e => e.Type == criteria.Type.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(e => e.Status == criteria.Status.Value);
        }

        if (criteria.EducationLevel.HasValue)
        {
            query = query.Where(e => e.TargetEducationLevel == criteria.EducationLevel.Value);
        }

        if (criteria.Difficulty.HasValue)
        {
            query = query.Where(e => e.DifficultyLevel == criteria.Difficulty.Value);
        }

        if (criteria.ReadingTextId.HasValue)
        {
            query = query.Where(e => e.ReadingTextId == criteria.ReadingTextId.Value);
        }

        if (criteria.CreatorId.HasValue)
        {
            query = query.Where(e => e.CreatedBy == criteria.CreatorId.Value);
        }

        if (criteria.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == criteria.IsActive.Value);
        }

        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= criteria.CreatedBefore.Value);
        }

        if (criteria.UpdatedAfter.HasValue)
        {
            query = query.Where(e => e.UpdatedAt >= criteria.UpdatedAfter.Value);
        }

        if (criteria.UpdatedBefore.HasValue)
        {
            query = query.Where(e => e.UpdatedAt <= criteria.UpdatedBefore.Value);
        }

        if (criteria.Tags != null && criteria.Tags.Any())
        {
            foreach (var tag in criteria.Tags)
            {
                query = query.Where(e => e.Tags.Contains(tag));
            }
        }

        // Apply ordering
        query = !string.IsNullOrEmpty(criteria.OrderBy) ? criteria.OrderBy.ToLower() switch
        {
            "title" => criteria.OrderDescending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
            "createdat" => criteria.OrderDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            "updatedat" => criteria.OrderDescending ? query.OrderByDescending(e => e.UpdatedAt) : query.OrderBy(e => e.UpdatedAt),
            "type" => criteria.OrderDescending ? query.OrderByDescending(e => e.Type) : query.OrderBy(e => e.Type),
            "difficulty" => criteria.OrderDescending ? query.OrderByDescending(e => e.DifficultyLevel) : query.OrderBy(e => e.DifficultyLevel),
            _ => criteria.OrderDescending ? query.OrderByDescending(e => e.UpdatedAt) : query.OrderBy(e => e.UpdatedAt)
        } : query.OrderByDescending(e => e.UpdatedAt);

        // Apply pagination
        query = query.Skip(criteria.Skip).Take(Math.Min(criteria.Take, 100)); // Max 100 results

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetRecommendedForUserAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        // This would typically include complex recommendation logic
        // For now, return active exercises ordered by popularity/recent activity
        return await _context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active && e.IsActive)
            .Include(e => e.Attempts)
            .OrderByDescending(e => e.Attempts.Count) // Popular exercises
            .ThenByDescending(e => e.UpdatedAt) // Recent exercises
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetPopularExercisesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active && e.IsActive)
            .Include(e => e.Attempts)
            .OrderByDescending(e => e.Attempts.Count)
            .ThenByDescending(e => e.UpdatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetRecentExercisesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active && e.IsActive)
            .OrderByDescending(e => e.PublishedAt ?? e.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Exercises.CountAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(ExerciseStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises.CountAsync(e => e.Status == status, cancellationToken);
    }

    public async Task<int> CountByTypeAsync(ExerciseType type, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises.CountAsync(e => e.Type == type, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exercises.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        _context.Exercises.Update(exercise);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { id }, cancellationToken);
        if (exercise != null)
        {
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        _context.Exercises.Remove(exercise);
        await _context.SaveChangesAsync(cancellationToken);
    }
}