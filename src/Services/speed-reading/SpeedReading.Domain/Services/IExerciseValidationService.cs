using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Services;

public interface IExerciseValidationService
{
    Task<ExerciseValidationResult> ValidateExerciseAsync(Exercise exercise);
    Task<QuestionValidationResult> ValidateQuestionAsync(Question question);
    Task<AttemptValidationResult> ValidateAttemptAsync(ExerciseAttempt attempt, Guid userId);
    Task<bool> CanUserAttemptExerciseAsync(Exercise exercise, Guid userId);
    Task<bool> IsExerciseAppropriateForUserAsync(Exercise exercise, UserReadingProfile profile);
}

public class ExerciseValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ExerciseQualityScore QualityScore { get; set; } = new();
}

public class QuestionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public QuestionQualityMetrics QualityMetrics { get; set; } = new();
}

public class AttemptValidationResult
{
    public bool CanAttempt { get; set; }
    public string? ReasonIfCannot { get; set; }
    public int RemainingAttempts { get; set; }
    public TimeSpan? CooldownPeriod { get; set; }
    public List<string> Prerequisites { get; set; } = new();
}

public class ExerciseQualityScore
{
    public double OverallScore { get; set; } // 0-100
    public double ContentQuality { get; set; }
    public double QuestionQuality { get; set; }
    public double DifficultyConsistency { get; set; }
    public double LevelAppropriateness { get; set; }
    public Dictionary<string, double> DetailedMetrics { get; set; } = new();
}

public class QuestionQualityMetrics
{
    public double ClarityScore { get; set; }
    public double DifficultyScore { get; set; }
    public double AnswerValidityScore { get; set; }
    public bool HasCorrectAnswer { get; set; }
    public bool HasDistractors { get; set; }
    public int OptionCount { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}