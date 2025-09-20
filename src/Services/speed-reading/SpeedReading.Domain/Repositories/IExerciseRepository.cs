using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Repositories;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Exercise?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Exercise?> GetByIdWithAttemptsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Exercise?> GetByIdFullAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Exercise>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetActiveExercisesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByStatusAsync(ExerciseStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByTypeAsync(ExerciseType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByEducationLevelAsync(EducationCategory educationLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByDifficultyAsync(TextDifficulty difficulty, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByReadingTextAsync(Guid readingTextId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Exercise>> SearchAsync(ExerciseSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetRecommendedForUserAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetPopularExercisesAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetRecentExercisesAsync(int count = 10, CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(ExerciseStatus status, CancellationToken cancellationToken = default);
    Task<int> CountByTypeAsync(ExerciseType type, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default);
    Task UpdateAsync(Exercise exercise, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Exercise exercise, CancellationToken cancellationToken = default);
}

public class ExerciseSearchCriteria
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ExerciseType? Type { get; set; }
    public ExerciseStatus? Status { get; set; }
    public EducationCategory? EducationLevel { get; set; }
    public TextDifficulty? Difficulty { get; set; }
    public Guid? ReadingTextId { get; set; }
    public Guid? CreatorId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
    public string[]? Tags { get; set; }
    
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string? OrderBy { get; set; }
    public bool OrderDescending { get; set; } = true;
}