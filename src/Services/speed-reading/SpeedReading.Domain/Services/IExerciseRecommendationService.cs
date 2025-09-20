using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Services;

public interface IExerciseRecommendationService
{
    Task<IReadOnlyList<Exercise>> GetRecommendedExercisesAsync(UserReadingProfile profile, int count = 5);
    Task<IReadOnlyList<Exercise>> GetExercisesByDifficultyAsync(EducationCategory educationLevel, TextDifficulty difficulty, int count = 10);
    Task<IReadOnlyList<Exercise>> GetProgressionExercisesAsync(UserReadingProfile profile, int count = 3);
    Task<Exercise?> GetNextChallengeExerciseAsync(UserReadingProfile profile);
    Task<IReadOnlyList<Exercise>> GetReviewExercisesAsync(Guid userId, int count = 5);
    Task<ExerciseRecommendationResult> GetPersonalizedRecommendationsAsync(UserReadingProfile profile);
}

public class ExerciseRecommendationResult
{
    public IReadOnlyList<Exercise> RecommendedExercises { get; set; } = new List<Exercise>();
    public IReadOnlyList<Exercise> ProgressionExercises { get; set; } = new List<Exercise>();
    public Exercise? ChallengeExercise { get; set; }
    public IReadOnlyList<Exercise> ReviewExercises { get; set; } = new List<Exercise>();
    
    public RecommendationReason PrimaryReason { get; set; }
    public string RecommendationMessage { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    
    public DateTime GeneratedAt { get; set; }
    public TimeSpan GenerationTime { get; set; }
}

public enum RecommendationReason
{
    LevelAppropriate,
    SkillImprovement,
    WeaknessTargeted,
    ProgressionPath,
    Challenge,
    Review,
    Popular,
    Recent
}