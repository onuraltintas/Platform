using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Services;

public interface IComprehensionTestService
{
    Task<Exercise> CreateComprehensionTestAsync(Guid readingTextId, ComprehensionTestOptions options, Guid createdBy);
    Task<ExerciseAttempt> StartTestAttemptAsync(Guid exerciseId, Guid userId);
    Task<ExerciseAttempt> SubmitAnswerAsync(Guid attemptId, Guid questionId, string answer);
    Task<ExerciseAttempt> CompleteTestAttemptAsync(Guid attemptId);
    Task<ExerciseScoreResult> CalculateTestScoreAsync(Guid attemptId);
    
    Task<IReadOnlyList<Exercise>> GetAvailableTestsAsync(Guid userId, EducationCategory? educationLevel = null, TextDifficulty? difficulty = null);
    Task<IReadOnlyList<ExerciseAttempt>> GetUserTestHistoryAsync(Guid userId, int count = 10);
    Task<UserTestPerformance> GetUserPerformanceAnalyticsAsync(Guid userId);
    
    Task<bool> ValidateTestReadinessAsync(Guid exerciseId);
    Task<Exercise> UpdateTestAsync(Guid exerciseId, ComprehensionTestOptions options);
    Task<bool> ArchiveTestAsync(Guid exerciseId);
}

public class UserTestPerformance
{
    public Guid UserId { get; set; }
    public int TotalTestsTaken { get; set; }
    public int TestsPassed { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }
    public double AverageCompletionTime { get; set; }
    
    public Dictionary<ExerciseType, TestTypePerformance> PerformanceByType { get; set; } = new();
    public Dictionary<EducationCategory, TestLevelPerformance> PerformanceByLevel { get; set; } = new();
    public Dictionary<TextDifficulty, TestDifficultyPerformance> PerformanceByDifficulty { get; set; } = new();
    
    public IReadOnlyList<string> StrengthAreas { get; set; } = new List<string>();
    public IReadOnlyList<string> ImprovementAreas { get; set; } = new List<string>();
    public IReadOnlyList<string> Recommendations { get; set; } = new List<string>();
    
    public DateTime AnalysisDate { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
}

public class TestTypePerformance
{
    public ExerciseType Type { get; set; }
    public int TestsTaken { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double AverageTime { get; set; }
    public double ImprovementTrend { get; set; }
}

public class TestLevelPerformance
{
    public EducationCategory Level { get; set; }
    public int TestsTaken { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public bool IsReadyForNextLevel { get; set; }
}

public class TestDifficultyPerformance
{
    public TextDifficulty Difficulty { get; set; }
    public int TestsTaken { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double ComfortLevel { get; set; }
}