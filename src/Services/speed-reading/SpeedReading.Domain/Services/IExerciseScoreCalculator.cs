using SpeedReading.Domain.Entities;

namespace SpeedReading.Domain.Services;

public interface IExerciseScoreCalculator
{
    Task<ExerciseScoreResult> CalculateScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions);
    Task<int> CalculateQuestionScoreAsync(Question question, string userAnswer);
    Task<bool> ValidateAnswerAsync(Question question, string userAnswer);
    Task<ExerciseScoreResult> RecalculateScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions);
}

public class ExerciseScoreResult
{
    public int TotalScore { get; set; }
    public int MaxPossibleScore { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public Dictionary<Guid, QuestionScoreResult> QuestionScores { get; set; } = new();
    public TimeSpan CalculationTime { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class QuestionScoreResult
{
    public Guid QuestionId { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public int MaxPoints { get; set; }
    public string? Feedback { get; set; }
    public double PartialScorePercentage { get; set; }
}