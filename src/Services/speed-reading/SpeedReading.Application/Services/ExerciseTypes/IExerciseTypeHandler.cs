using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services.ExerciseTypes;

public interface IExerciseTypeHandler
{
    ExerciseType SupportedType { get; }
    Task<Exercise> CreateExerciseAsync(ExerciseCreationRequest request);
    Task<IReadOnlyList<Question>> GenerateQuestionsAsync(ExerciseCreationRequest request);
    Task<bool> ValidateExerciseAsync(Exercise exercise);
    Task<string> GenerateInstructionsAsync(EducationCategory level);
}

public class ExerciseCreationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReadingText ReadingText { get; set; } = null!;
    public EducationCategory TargetEducationLevel { get; set; }
    public TextDifficulty DifficultyLevel { get; set; }
    public int QuestionCount { get; set; } = 5;
    public int TimeLimit { get; set; } = 30;
    public int PassingScore { get; set; } = 60;
    public bool IsRandomized { get; set; } = true;
    public bool AllowRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public Guid CreatedBy { get; set; }
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}