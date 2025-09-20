using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Repositories;

public interface IExerciseAttemptRepository
{
    Task<ExerciseAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExerciseAttempt?> GetByIdWithAnswersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExerciseAttempt?> GetByIdFullAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<ExerciseAttempt>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttempt>> GetByExerciseIdAsync(Guid exerciseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttempt>> GetByUserAndExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttempt>> GetByStatusAsync(AttemptStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttempt>> GetInProgressAttemptsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttempt>> GetExpiredAttemptsAsync(CancellationToken cancellationToken = default);
    
    Task<ExerciseAttempt?> GetActiveAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    Task<ExerciseAttempt?> GetLastAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    Task<ExerciseAttempt?> GetBestAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<ExerciseAttempt>> SearchAsync(AttemptSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttemptStatistics>> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseAttemptStatistics>> GetExerciseStatisticsAsync(Guid exerciseId, CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByExerciseAsync(Guid exerciseId, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(AttemptStatus status, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAttemptAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    
    Task AddAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(ExerciseAttempt attempt, CancellationToken cancellationToken = default);
    
    Task<int> TimeOutExpiredAttemptsAsync(CancellationToken cancellationToken = default);
}

public class AttemptSearchCriteria
{
    public Guid? UserId { get; set; }
    public Guid? ExerciseId { get; set; }
    public AttemptStatus? Status { get; set; }
    public DateTime? StartedAfter { get; set; }
    public DateTime? StartedBefore { get; set; }
    public DateTime? CompletedAfter { get; set; }
    public DateTime? CompletedBefore { get; set; }
    public int? MinScore { get; set; }
    public int? MaxScore { get; set; }
    public bool? IsPassed { get; set; }
    public ExerciseType? ExerciseType { get; set; }
    public EducationCategory? EducationLevel { get; set; }
    
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string? OrderBy { get; set; }
    public bool OrderDescending { get; set; } = true;
}

public class ExerciseAttemptStatistics
{
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public string ExerciseTitle { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public int PassedAttempts { get; set; }
    
    public int? BestScore { get; set; }
    public int? LatestScore { get; set; }
    public double? AverageScore { get; set; }
    
    public TimeSpan? BestTime { get; set; }
    public TimeSpan? LatestTime { get; set; }
    public TimeSpan? AverageTime { get; set; }
    
    public DateTime? FirstAttemptDate { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    
    public bool IsPassed { get; set; }
    public double ImprovementRate { get; set; }
}