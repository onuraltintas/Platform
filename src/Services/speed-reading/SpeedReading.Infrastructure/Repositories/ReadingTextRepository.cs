using Microsoft.EntityFrameworkCore;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Infrastructure.Data;
using System.Linq.Expressions;

namespace SpeedReading.Infrastructure.Repositories;

public class ReadingTextRepository : IReadingTextRepository
{
    private readonly SpeedReadingDbContext _context;

    public ReadingTextRepository(SpeedReadingDbContext context)
    {
        _context = context;
    }

    public async Task<ReadingText?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);
    }

    public async Task<ReadingText> AddAsync(ReadingText text, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<ReadingText>().AddAsync(text, cancellationToken);
        return entry.Entity;
    }

    public async Task<ReadingText> UpdateAsync(ReadingText text, CancellationToken cancellationToken = default)
    {
        _context.Set<ReadingText>().Update(text);
        return await Task.FromResult(text);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var text = await GetByIdAsync(id, cancellationToken);
        if (text != null)
        {
            text.Archive();
            _context.Set<ReadingText>().Update(text);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .AnyAsync(t => t.Id == id && t.IsActive, cancellationToken);
    }

    public async Task<List<ReadingText>> GetActiveTextsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetPublishedTextsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PublishedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetByCategoryAsync(TextCategory category, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.Category == category && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PopularityScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetByDifficultyAsync(TextDifficulty difficulty, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.Difficulty == difficulty && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PopularityScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetByEducationLevelAsync(EducationCategory educationLevel, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.TargetEducationLevel == educationLevel && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PopularityScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetByGradeLevelAsync(int gradeLevel, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && 
                       t.Status == TextStatus.Published &&
                       t.MinGradeLevel <= gradeLevel && 
                       (t.MaxGradeLevel == null || t.MaxGradeLevel >= gradeLevel))
            .OrderByDescending(t => t.PopularityScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<ReadingText>();

        searchTerm = searchTerm.ToLowerInvariant();

        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && 
                       t.Status == TextStatus.Published &&
                       (t.Title.ToLower().Contains(searchTerm) ||
                        t.Content.ToLower().Contains(searchTerm) ||
                        (t.Summary != null && t.Summary.ToLower().Contains(searchTerm)) ||
                        t.Metadata.Author.ToLower().Contains(searchTerm)))
            .OrderByDescending(t => t.PopularityScore)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetFilteredAsync(TextFilterCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ReadingText>().AsQueryable();

        // Apply filters
        if (criteria.IsActive.HasValue)
            query = query.Where(t => t.IsActive == criteria.IsActive.Value);
        else
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLowerInvariant();
            query = query.Where(t => t.Title.ToLower().Contains(term) ||
                                    t.Content.ToLower().Contains(term) ||
                                    (t.Summary != null && t.Summary.ToLower().Contains(term)));
        }

        if (criteria.Category.HasValue)
            query = query.Where(t => t.Category == criteria.Category.Value);

        if (criteria.Difficulty.HasValue)
            query = query.Where(t => t.Difficulty == criteria.Difficulty.Value);

        if (criteria.EducationLevel.HasValue)
            query = query.Where(t => t.TargetEducationLevel == criteria.EducationLevel.Value);

        if (criteria.Status.HasValue)
            query = query.Where(t => t.Status == criteria.Status.Value);

        if (criteria.Source.HasValue)
            query = query.Where(t => t.Source == criteria.Source.Value);

        if (criteria.MinGradeLevel.HasValue)
            query = query.Where(t => t.MinGradeLevel >= criteria.MinGradeLevel.Value);

        if (criteria.MaxGradeLevel.HasValue)
            query = query.Where(t => t.MaxGradeLevel <= criteria.MaxGradeLevel.Value);

        if (criteria.MinWordCount.HasValue)
            query = query.Where(t => t.Statistics.WordCount >= criteria.MinWordCount.Value);

        if (criteria.MaxWordCount.HasValue)
            query = query.Where(t => t.Statistics.WordCount <= criteria.MaxWordCount.Value);

        if (!string.IsNullOrWhiteSpace(criteria.Author))
            query = query.Where(t => t.Metadata.Author.Contains(criteria.Author));

        if (criteria.PublishedAfter.HasValue)
            query = query.Where(t => t.PublishedAt >= criteria.PublishedAfter.Value);

        if (criteria.PublishedBefore.HasValue)
            query = query.Where(t => t.PublishedAt <= criteria.PublishedBefore.Value);

        // Apply ordering
        query = ApplyOrdering(query, criteria.OrderBy, criteria.OrderDescending);

        // Apply pagination
        return await query
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetMostPopularAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PopularityScore)
            .ThenByDescending(t => t.ReadCount)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetRecentlyAddedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && t.Status == TextStatus.Published)
            .OrderByDescending(t => t.PublishedAt ?? t.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingText>> GetRecommendedForUserAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        // Get user profile to determine preferences
        var userProfile = await _context.UserReadingProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (userProfile == null)
        {
            // Return popular texts if no profile
            return await GetMostPopularAsync(count, cancellationToken);
        }

        // Get texts matching user's education level and preferences
        var educationCategory = userProfile.Demographics.GetEducationCategory();
        var gradeLevel = userProfile.Demographics.GradeLevel ?? 10;

        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive && 
                       t.Status == TextStatus.Published &&
                       t.TargetEducationLevel == educationCategory &&
                       t.MinGradeLevel <= gradeLevel &&
                       (t.MaxGradeLevel == null || t.MaxGradeLevel >= gradeLevel))
            .OrderByDescending(t => t.PopularityScore)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .CountAsync(t => t.IsActive, cancellationToken);
    }

    public async Task<int> GetCountByCategoryAsync(TextCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .CountAsync(t => t.IsActive && t.Category == category, cancellationToken);
    }

    public async Task<Dictionary<TextCategory, int>> GetCategoryDistributionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<TextDifficulty, int>> GetDifficultyDistributionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => t.IsActive)
            .GroupBy(t => t.Difficulty)
            .Select(g => new { Difficulty = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Difficulty, x => x.Count, cancellationToken);
    }

    public async Task<List<ReadingText>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingText>()
            .Where(t => ids.Contains(t.Id) && t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(IEnumerable<Guid> ids, TextStatus status, CancellationToken cancellationToken = default)
    {
        var texts = await GetByIdsAsync(ids, cancellationToken);
        
        foreach (var text in texts)
        {
            if (status == TextStatus.Published)
                text.Publish();
            else if (status == TextStatus.Archived)
                text.Archive();
            // For other statuses, we'd need to add methods to the entity
        }

        return texts.Count;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ReadingText> ApplyOrdering(IQueryable<ReadingText> query, string orderBy, bool descending)
    {
        Expression<Func<ReadingText, object>> orderExpression = orderBy.ToLowerInvariant() switch
        {
            "title" => t => t.Title,
            "category" => t => t.Category,
            "difficulty" => t => t.Difficulty,
            "popularity" => t => t.PopularityScore,
            "readcount" => t => t.ReadCount,
            "wordcount" => t => t.Statistics.WordCount,
            "publishedat" => t => t.PublishedAt ?? t.CreatedAt,
            _ => t => t.CreatedAt
        };

        return descending 
            ? query.OrderByDescending(orderExpression) 
            : query.OrderBy(orderExpression);
    }
}