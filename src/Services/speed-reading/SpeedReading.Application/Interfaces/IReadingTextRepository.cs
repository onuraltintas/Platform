using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Interfaces;

public interface IReadingTextRepository
{
    // Basic CRUD
    Task<ReadingText?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReadingText> AddAsync(ReadingText text, CancellationToken cancellationToken = default);
    Task<ReadingText> UpdateAsync(ReadingText text, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Query methods
    Task<List<ReadingText>> GetActiveTextsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetPublishedTextsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetByCategoryAsync(TextCategory category, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetByDifficultyAsync(TextDifficulty difficulty, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetByEducationLevelAsync(EducationCategory educationLevel, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetByGradeLevelAsync(int gradeLevel, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    
    // Search and filter
    Task<List<ReadingText>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetFilteredAsync(TextFilterCriteria criteria, CancellationToken cancellationToken = default);
    
    // Popular and recommended
    Task<List<ReadingText>> GetMostPopularAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetRecentlyAddedAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<List<ReadingText>> GetRecommendedForUserAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByCategoryAsync(TextCategory category, CancellationToken cancellationToken = default);
    Task<Dictionary<TextCategory, int>> GetCategoryDistributionAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<TextDifficulty, int>> GetDifficultyDistributionAsync(CancellationToken cancellationToken = default);
    
    // Batch operations
    Task<List<ReadingText>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> BulkUpdateStatusAsync(IEnumerable<Guid> ids, TextStatus status, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class TextFilterCriteria
{
    public string? SearchTerm { get; set; }
    public TextCategory? Category { get; set; }
    public TextDifficulty? Difficulty { get; set; }
    public EducationCategory? EducationLevel { get; set; }
    public int? MinGradeLevel { get; set; }
    public int? MaxGradeLevel { get; set; }
    public TextStatus? Status { get; set; }
    public TextSource? Source { get; set; }
    public int? MinWordCount { get; set; }
    public int? MaxWordCount { get; set; }
    public double? MinReadabilityScore { get; set; }
    public double? MaxReadabilityScore { get; set; }
    public string? Author { get; set; }
    public string[]? Tags { get; set; }
    public DateTime? PublishedAfter { get; set; }
    public DateTime? PublishedBefore { get; set; }
    public bool? IsActive { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string OrderBy { get; set; } = "CreatedAt";
    public bool OrderDescending { get; set; } = true;
}