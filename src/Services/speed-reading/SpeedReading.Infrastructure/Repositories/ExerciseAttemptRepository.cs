using Microsoft.EntityFrameworkCore;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Repositories;
using SpeedReading.Infrastructure.Data;

namespace SpeedReading.Infrastructure.Repositories;

public class ExerciseAttemptRepository : IExerciseAttemptRepository
{
    private readonly SpeedReadingDbContext _context;

    public ExerciseAttemptRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<ExerciseAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<ExerciseAttempt?> GetByIdWithAnswersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Include(a => a.Answers)
                .ThenInclude(qa => qa.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<ExerciseAttempt?> GetByIdFullAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Include(a => a.Exercise)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Exercise)
                .ThenInclude(e => e.ReadingText)
            .Include(a => a.Answers)
                .ThenInclude(qa => qa.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.UserId == userId)
            .Include(a => a.Exercise)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetByExerciseIdAsync(Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.ExerciseId == exerciseId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetByUserAndExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.UserId == userId && a.ExerciseId == exerciseId)
            .Include(a => a.Exercise)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetByStatusAsync(AttemptStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.Status == status)
            .Include(a => a.Exercise)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetInProgressAttemptsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.Status == AttemptStatus.InProgress)
            .Include(a => a.Exercise)
            .OrderBy(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> GetExpiredAttemptsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.ExerciseAttempts
            .Where(a => a.Status == AttemptStatus.InProgress && 
                       a.ExpiresAt.HasValue && 
                       a.ExpiresAt.Value < now)
            .Include(a => a.Exercise)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExerciseAttempt?> GetActiveAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.UserId == userId && 
                       a.ExerciseId == exerciseId && 
                       a.Status == AttemptStatus.InProgress)
            .Include(a => a.Exercise)
            .Include(a => a.Answers)
                .ThenInclude(qa => qa.Question)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExerciseAttempt?> GetLastAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.UserId == userId && a.ExerciseId == exerciseId)
            .Include(a => a.Exercise)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExerciseAttempt?> GetBestAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts
            .Where(a => a.UserId == userId && 
                       a.ExerciseId == exerciseId && 
                       a.Status == AttemptStatus.Completed)
            .Include(a => a.Exercise)
            .OrderByDescending(a => a.TotalScore)
            .ThenByDescending(a => a.ScorePercentage)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttempt>> SearchAsync(AttemptSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.ExerciseAttempts.AsQueryable();

        // Apply filters
        if (criteria.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == criteria.UserId.Value);
        }

        if (criteria.ExerciseId.HasValue)
        {
            query = query.Where(a => a.ExerciseId == criteria.ExerciseId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(a => a.Status == criteria.Status.Value);
        }

        if (criteria.StartedAfter.HasValue)
        {
            query = query.Where(a => a.StartedAt >= criteria.StartedAfter.Value);
        }

        if (criteria.StartedBefore.HasValue)
        {
            query = query.Where(a => a.StartedAt <= criteria.StartedBefore.Value);
        }

        if (criteria.CompletedAfter.HasValue)
        {
            query = query.Where(a => a.CompletedAt.HasValue && a.CompletedAt.Value >= criteria.CompletedAfter.Value);
        }

        if (criteria.CompletedBefore.HasValue)
        {
            query = query.Where(a => a.CompletedAt.HasValue && a.CompletedAt.Value <= criteria.CompletedBefore.Value);
        }

        if (criteria.MinScore.HasValue)
        {
            query = query.Where(a => a.TotalScore >= criteria.MinScore.Value);
        }

        if (criteria.MaxScore.HasValue)
        {
            query = query.Where(a => a.TotalScore <= criteria.MaxScore.Value);
        }

        if (criteria.IsPassed.HasValue)
        {
            query = query.Where(a => a.IsPassed == criteria.IsPassed.Value);
        }

        if (criteria.ExerciseType.HasValue)
        {
            query = query.Where(a => a.Exercise != null && a.Exercise.Type == criteria.ExerciseType.Value);
        }

        if (criteria.EducationLevel.HasValue)
        {
            query = query.Where(a => a.Exercise != null && a.Exercise.TargetEducationLevel == criteria.EducationLevel.Value);
        }

        // Include related data
        query = query.Include(a => a.Exercise)
                    .Include(a => a.Answers);

        // Apply ordering
        query = !string.IsNullOrEmpty(criteria.OrderBy) ? criteria.OrderBy.ToLower() switch
        {
            "startedat" => criteria.OrderDescending ? query.OrderByDescending(a => a.StartedAt) : query.OrderBy(a => a.StartedAt),
            "completedat" => criteria.OrderDescending ? query.OrderByDescending(a => a.CompletedAt) : query.OrderBy(a => a.CompletedAt),
            "score" => criteria.OrderDescending ? query.OrderByDescending(a => a.TotalScore) : query.OrderBy(a => a.TotalScore),
            "percentage" => criteria.OrderDescending ? query.OrderByDescending(a => a.ScorePercentage) : query.OrderBy(a => a.ScorePercentage),
            "timespent" => criteria.OrderDescending ? query.OrderByDescending(a => a.TimeSpent) : query.OrderBy(a => a.TimeSpent),
            _ => criteria.OrderDescending ? query.OrderByDescending(a => a.StartedAt) : query.OrderBy(a => a.StartedAt)
        } : query.OrderByDescending(a => a.StartedAt);

        // Apply pagination
        query = query.Skip(criteria.Skip).Take(Math.Min(criteria.Take, 1000)); // Max 1000 results

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseAttemptStatistics>> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var statistics = await _context.ExerciseAttempts
            .Where(a => a.UserId == userId)
            .Include(a => a.Exercise)
            .GroupBy(a => new { a.ExerciseId, a.Exercise!.Title, a.Exercise.Type })
            .Select(g => new ExerciseAttemptStatistics
            {
                UserId = userId,
                ExerciseId = g.Key.ExerciseId,
                ExerciseTitle = g.Key.Title,
                ExerciseType = g.Key.Type,
                TotalAttempts = g.Count(),
                CompletedAttempts = g.Count(a => a.Status == AttemptStatus.Completed),
                PassedAttempts = g.Count(a => a.IsPassed),
                BestScore = g.Where(a => a.Status == AttemptStatus.Completed).Max(a => (int?)a.TotalScore),
                LatestScore = g.OrderByDescending(a => a.StartedAt).Where(a => a.Status == AttemptStatus.Completed).Select(a => (int?)a.TotalScore).FirstOrDefault(),
                AverageScore = g.Where(a => a.Status == AttemptStatus.Completed).Average(a => (double?)a.ScorePercentage),
                BestTime = g.Where(a => a.TimeSpent.HasValue).Min(a => a.TimeSpent),
                LatestTime = g.OrderByDescending(a => a.StartedAt).Where(a => a.TimeSpent.HasValue).Select(a => a.TimeSpent).FirstOrDefault(),
                AverageTime = TimeSpan.FromTicks((long)(g.Where(a => a.TimeSpent.HasValue).Average(a => (double?)a.TimeSpent!.Value.Ticks) ?? 0L)),
                FirstAttemptDate = g.Min(a => (DateTime?)a.StartedAt),
                LastAttemptDate = g.Max(a => (DateTime?)a.StartedAt),
                IsPassed = g.Any(a => a.IsPassed)
            })
            .ToListAsync(cancellationToken);

        // Calculate improvement rate for each exercise
        foreach (var stat in statistics)
        {
            var attempts = await _context.ExerciseAttempts
                .Where(a => a.UserId == userId && a.ExerciseId == stat.ExerciseId && a.Status == AttemptStatus.Completed)
                .OrderBy(a => a.StartedAt)
                .Select(a => a.ScorePercentage)
                .ToListAsync(cancellationToken);

            if (attempts.Count >= 2)
            {
                var firstHalf = attempts.Take(attempts.Count / 2).Average();
                var secondHalf = attempts.Skip(attempts.Count / 2).Average();
                stat.ImprovementRate = secondHalf - firstHalf;
            }
        }

        return statistics;
    }

    public async Task<IReadOnlyList<ExerciseAttemptStatistics>> GetExerciseStatisticsAsync(Guid exerciseId, CancellationToken cancellationToken = default)
    {
        var statistics = await _context.ExerciseAttempts
            .Where(a => a.ExerciseId == exerciseId)
            .Include(a => a.Exercise)
            .GroupBy(a => a.UserId)
            .Select(g => new ExerciseAttemptStatistics
            {
                UserId = g.Key,
                ExerciseId = exerciseId,
                ExerciseTitle = g.First().Exercise!.Title,
                ExerciseType = g.First().Exercise.Type,
                TotalAttempts = g.Count(),
                CompletedAttempts = g.Count(a => a.Status == AttemptStatus.Completed),
                PassedAttempts = g.Count(a => a.IsPassed),
                BestScore = g.Where(a => a.Status == AttemptStatus.Completed).Max(a => (int?)a.TotalScore),
                LatestScore = g.OrderByDescending(a => a.StartedAt).Where(a => a.Status == AttemptStatus.Completed).Select(a => (int?)a.TotalScore).FirstOrDefault(),
                AverageScore = g.Where(a => a.Status == AttemptStatus.Completed).Average(a => (double?)a.ScorePercentage),
                FirstAttemptDate = g.Min(a => (DateTime?)a.StartedAt),
                LastAttemptDate = g.Max(a => (DateTime?)a.StartedAt),
                IsPassed = g.Any(a => a.IsPassed)
            })
            .ToListAsync(cancellationToken);

        return statistics;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.CountAsync(cancellationToken);
    }

    public async Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.CountAsync(a => a.UserId == userId, cancellationToken);
    }

    public async Task<int> CountByExerciseAsync(Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.CountAsync(a => a.ExerciseId == exerciseId, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(AttemptStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.CountAsync(a => a.Status == status, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> HasActiveAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseAttempts.AnyAsync(
            a => a.UserId == userId && a.ExerciseId == exerciseId && a.Status == AttemptStatus.InProgress,
            cancellationToken);
    }

    public async Task AddAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.ExerciseAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.ExerciseAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attempt = await _context.ExerciseAttempts.FindAsync(new object[] { id }, cancellationToken);
        if (attempt != null)
        {
            _context.ExerciseAttempts.Remove(attempt);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.ExerciseAttempts.Remove(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> TimeOutExpiredAttemptsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredAttempts = await _context.ExerciseAttempts
            .Where(a => a.Status == AttemptStatus.InProgress && 
                       a.ExpiresAt.HasValue && 
                       a.ExpiresAt.Value < now)
            .ToListAsync(cancellationToken);

        foreach (var attempt in expiredAttempts)
        {
            attempt.TimeOut();
        }

        if (expiredAttempts.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return expiredAttempts.Count;
    }
}