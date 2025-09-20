using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Services;

public interface IComprehensionTestGenerator
{
    Task<Exercise> GenerateComprehensionTestAsync(ReadingText text, ComprehensionTestOptions options);
    Task<IReadOnlyList<Question>> GenerateQuestionsFromTextAsync(ReadingText text, int questionCount, EducationCategory targetLevel);
    Task<Question> GenerateMainIdeaQuestionAsync(ReadingText text, EducationCategory targetLevel);
    Task<Question> GenerateDetailQuestionAsync(ReadingText text, EducationCategory targetLevel);
    Task<Question> GenerateInferenceQuestionAsync(ReadingText text, EducationCategory targetLevel);
    Task<Question> GenerateVocabularyQuestionAsync(ReadingText text, EducationCategory targetLevel);
    Task<Question> GenerateSummaryQuestionAsync(ReadingText text, EducationCategory targetLevel);
}

public class ComprehensionTestOptions
{
    public int TotalQuestions { get; set; } = 5;
    public int MainIdeaQuestions { get; set; } = 1;
    public int DetailQuestions { get; set; } = 2;
    public int InferenceQuestions { get; set; } = 1;
    public int VocabularyQuestions { get; set; } = 1;
    public int SummaryQuestions { get; set; } = 0;
    
    public int TimeLimit { get; set; } = 30; // minutes
    public int PassingScore { get; set; } = 60;
    public bool IsRandomized { get; set; } = true;
    public bool AllowRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    
    public EducationCategory TargetEducationLevel { get; set; }
    public TextDifficulty DifficultyLevel { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = "Metni okuyarak aşağıdaki soruları cevaplayınız.";
}