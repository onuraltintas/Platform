using SpeedReading.Application.Interfaces;
using SpeedReading.Application.Services.ExerciseTypes;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Services;

namespace SpeedReading.Application.Services;

public class ExerciseGenerationService
{
    private readonly Dictionary<ExerciseType, IExerciseTypeHandler> _exerciseHandlers;
    private readonly TurkishTextAnalyzer _textAnalyzer;
    private readonly IExerciseValidationService _validationService;

    public ExerciseGenerationService(
        IEnumerable<IExerciseTypeHandler> exerciseHandlers,
        TurkishTextAnalyzer textAnalyzer,
        IExerciseValidationService validationService)
    {
        _exerciseHandlers = exerciseHandlers.ToDictionary(h => h.SupportedType, h => h);
        _textAnalyzer = textAnalyzer;
        _validationService = validationService;
    }

    public async Task<Exercise> GenerateExerciseAsync(ExerciseGenerationRequest request)
    {
        // Validate input
        await ValidateGenerationRequestAsync(request);

        // Get appropriate handler
        if (!_exerciseHandlers.TryGetValue(request.ExerciseType, out var handler))
        {
            throw new NotSupportedException($"Exercise type {request.ExerciseType} is not supported.");
        }

        // Analyze the text for better question generation
        var textAnalysis = await AnalyzeTextForExerciseAsync(request.ReadingText);

        // Create exercise creation request
        var creationRequest = new ExerciseCreationRequest
        {
            Title = request.Title,
            Description = request.Description,
            ReadingText = request.ReadingText,
            TargetEducationLevel = request.TargetEducationLevel,
            DifficultyLevel = request.DifficultyLevel ?? DetermineDifficultyFromText(textAnalysis),
            QuestionCount = request.QuestionCount,
            TimeLimit = request.TimeLimit ?? CalculateRecommendedTimeLimit(request.ReadingText, request.ExerciseType),
            PassingScore = request.PassingScore,
            IsRandomized = request.IsRandomized,
            AllowRetry = request.AllowRetry,
            MaxRetries = request.MaxRetries,
            CreatedBy = request.CreatedBy,
            AdditionalOptions = request.AdditionalOptions
        };

        // Generate exercise
        var exercise = await handler.CreateExerciseAsync(creationRequest);

        // Validate generated exercise
        var validationResult = await _validationService.ValidateExerciseAsync(exercise);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Generated exercise validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        return exercise;
    }

    public async Task<IReadOnlyList<Exercise>> GenerateExerciseSetAsync(ExerciseSetGenerationRequest request)
    {
        var exercises = new List<Exercise>();

        foreach (var exerciseRequest in request.ExerciseRequests)
        {
            exerciseRequest.ReadingText = request.ReadingText;
            exerciseRequest.CreatedBy = request.CreatedBy;
            
            var exercise = await GenerateExerciseAsync(exerciseRequest);
            exercises.Add(exercise);
        }

        return exercises;
    }

    public async Task<Exercise> GenerateAdaptiveExerciseAsync(AdaptiveExerciseRequest request)
    {
        // Analyze user's previous performance
        var userAnalysis = await AnalyzeUserPerformanceAsync(request.UserId, request.ReadingText);

        // Determine optimal exercise type and difficulty
        var recommendedType = DetermineOptimalExerciseType(userAnalysis);
        var recommendedDifficulty = DetermineOptimalDifficulty(userAnalysis, request.ReadingText);

        // Create adaptive exercise request
        var exerciseRequest = new ExerciseGenerationRequest
        {
            Title = GenerateAdaptiveTitle(recommendedType, recommendedDifficulty),
            Description = GenerateAdaptiveDescription(recommendedType, userAnalysis),
            ReadingText = request.ReadingText,
            ExerciseType = recommendedType,
            TargetEducationLevel = request.TargetEducationLevel,
            DifficultyLevel = recommendedDifficulty,
            QuestionCount = DetermineOptimalQuestionCount(userAnalysis),
            TimeLimit = CalculateAdaptiveTimeLimit(userAnalysis, request.ReadingText),
            PassingScore = CalculateAdaptivePassingScore(userAnalysis),
            IsRandomized = true,
            AllowRetry = true,
            MaxRetries = 3,
            CreatedBy = request.CreatedBy,
            AdditionalOptions = CreateAdaptiveOptions(userAnalysis)
        };

        return await GenerateExerciseAsync(exerciseRequest);
    }

    public async Task<ExerciseTemplate> CreateExerciseTemplateAsync(TemplateCreationRequest request)
    {
        var template = new ExerciseTemplate
        {
            Name = request.Name,
            Description = request.Description,
            ExerciseType = request.ExerciseType,
            TargetEducationLevel = request.TargetEducationLevel,
            DefaultQuestionCount = request.DefaultQuestionCount,
            DefaultTimeLimit = request.DefaultTimeLimit,
            DefaultPassingScore = request.DefaultPassingScore,
            QuestionTemplates = await CreateQuestionTemplatesAsync(request),
            Settings = request.Settings,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        return template;
    }

    public async Task<Exercise> GenerateFromTemplateAsync(TemplateGenerationRequest request)
    {
        var template = request.Template;
        
        var exerciseRequest = new ExerciseGenerationRequest
        {
            Title = string.Format(template.TitleTemplate ?? "{0} - {1}", request.ReadingText.Title, template.Name),
            Description = template.Description,
            ReadingText = request.ReadingText,
            ExerciseType = template.ExerciseType,
            TargetEducationLevel = template.TargetEducationLevel,
            DifficultyLevel = request.DifficultyLevel,
            QuestionCount = request.QuestionCount ?? template.DefaultQuestionCount,
            TimeLimit = request.TimeLimit ?? template.DefaultTimeLimit,
            PassingScore = request.PassingScore ?? template.DefaultPassingScore,
            IsRandomized = request.IsRandomized ?? true,
            AllowRetry = request.AllowRetry ?? true,
            MaxRetries = request.MaxRetries ?? 3,
            CreatedBy = request.CreatedBy,
            AdditionalOptions = MergeOptions(template.Settings, request.AdditionalOptions)
        };

        var exercise = await GenerateExerciseAsync(exerciseRequest);
        
        // Apply template-specific question generation
        await ApplyQuestionTemplatesAsync(exercise, template.QuestionTemplates, request.ReadingText);

        return exercise;
    }

    private async Task ValidateGenerationRequestAsync(ExerciseGenerationRequest request)
    {
        if (request.ReadingText == null)
            throw new ArgumentNullException(nameof(request.ReadingText));

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required", nameof(request.Title));

        if (request.QuestionCount <= 0 || request.QuestionCount > 20)
            throw new ArgumentException("Question count must be between 1 and 20", nameof(request.QuestionCount));

        if (request.ReadingText.Statistics.WordCount < 50)
            throw new ArgumentException("Reading text is too short for exercise generation");
    }

    private async Task<SimpleTextAnalysisResult> AnalyzeTextForExerciseAsync(ReadingText text)
    {
        var analysis = new SimpleTextAnalysisResult
        {
            WordCount = text.Statistics.WordCount,
            SentenceCount = text.Statistics.SentenceCount,
            ParagraphCount = text.Statistics.ParagraphCount,
            ReadabilityScore = 75.0, // Default readability score
            KeyWords = await _textAnalyzer.ExtractKeywordsAsync(text.Content, 10),
            Sentences = _textAnalyzer.TokenizeSentences(text.Content).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
            ImportantSentences = _textAnalyzer.TokenizeSentences(text.Content).Take(3).ToArray(),
            TopicAnalysis = "General topic" // Simplified topic analysis
        };

        return analysis;
    }

    private TextDifficulty DetermineDifficultyFromText(SimpleTextAnalysisResult analysis)
    {
        return analysis.ReadabilityScore switch
        {
            >= 80 => TextDifficulty.Easy,
            >= 60 => TextDifficulty.Medium,
            >= 40 => TextDifficulty.Hard,
            _ => TextDifficulty.VeryHard
        };
    }

    private int CalculateRecommendedTimeLimit(ReadingText text, ExerciseType exerciseType)
    {
        var baseReadingTime = (int)Math.Ceiling(text.Statistics.WordCount / 200.0); // 200 WPM baseline
        
        var exerciseMultiplier = exerciseType switch
        {
            ExerciseType.SpeedReading => 0.7, // Faster for speed reading
            ExerciseType.ReadingComprehension => 1.2, // More time for comprehension
            ExerciseType.VocabularyTest => 1.0,
            ExerciseType.CriticalThinking => 1.5, // More time for analysis
            _ => 1.0
        };

        var questionTime = 2; // 2 minutes per question average
        return Math.Max(5, (int)(baseReadingTime * exerciseMultiplier) + questionTime);
    }

    private async Task<UserPerformanceAnalysis> AnalyzeUserPerformanceAsync(Guid userId, ReadingText text)
    {
        // This would typically fetch from database
        return new UserPerformanceAnalysis
        {
            UserId = userId,
            AverageScore = 75.0,
            PreferredExerciseTypes = new[] { ExerciseType.ReadingComprehension },
            WeakAreas = new[] { "inference", "vocabulary" },
            StrongAreas = new[] { "main_idea", "detail" },
            AverageCompletionTime = TimeSpan.FromMinutes(15),
            RecommendedDifficulty = TextDifficulty.Medium
        };
    }

    private ExerciseType DetermineOptimalExerciseType(UserPerformanceAnalysis analysis)
    {
        // Logic to determine best exercise type based on user performance
        if (analysis.WeakAreas.Contains("vocabulary"))
            return ExerciseType.VocabularyTest;
        
        if (analysis.WeakAreas.Contains("inference"))
            return ExerciseType.CriticalThinking;

        return ExerciseType.ReadingComprehension;
    }

    private TextDifficulty DetermineOptimalDifficulty(UserPerformanceAnalysis analysis, ReadingText text)
    {
        var textDifficulty = text.Difficulty;
        var userLevel = analysis.RecommendedDifficulty;

        // Adaptive difficulty adjustment
        if (analysis.AverageScore > 85)
        {
            return (TextDifficulty)Math.Min((int)textDifficulty + 1, (int)TextDifficulty.VeryHard);
        }
        else if (analysis.AverageScore < 60)
        {
            return (TextDifficulty)Math.Max((int)textDifficulty - 1, (int)TextDifficulty.Easy);
        }

        return textDifficulty;
    }

    private string GenerateAdaptiveTitle(ExerciseType type, TextDifficulty difficulty)
    {
        var difficultyText = difficulty switch
        {
            TextDifficulty.Easy => "Temel",
            TextDifficulty.Medium => "Orta",
            TextDifficulty.Hard => "İleri",
            TextDifficulty.VeryHard => "Uzman",
            _ => "Standart"
        };

        var typeText = type switch
        {
            ExerciseType.ReadingComprehension => "Okuduğunu Anlama",
            ExerciseType.VocabularyTest => "Kelime Bilgisi",
            ExerciseType.SpeedReading => "Hızlı Okuma",
            ExerciseType.CriticalThinking => "Eleştirel Düşünme",
            _ => "Okuma Egzersizi"
        };

        return $"{difficultyText} Seviye {typeText} Egzersizi";
    }

    private string GenerateAdaptiveDescription(ExerciseType type, UserPerformanceAnalysis analysis)
    {
        var baseDescription = type switch
        {
            ExerciseType.ReadingComprehension => "Bu egzersizde metni anlama becerinizi geliştireceksiniz.",
            ExerciseType.VocabularyTest => "Kelime hazinenizi genişletmek için tasarlanmış egzersiz.",
            ExerciseType.SpeedReading => "Okuma hızınızı artırmaya odaklanan egzersiz.",
            ExerciseType.CriticalThinking => "Eleştirel düşünme becerilerinizi geliştiren egzersiz.",
            _ => "Okuma becerilerinizi geliştiren egzersiz."
        };

        if (analysis.WeakAreas.Any())
        {
            baseDescription += $" Özellikle {string.Join(", ", analysis.WeakAreas)} alanlarında gelişiminizi destekleyecektir.";
        }

        return baseDescription;
    }

    private int DetermineOptimalQuestionCount(UserPerformanceAnalysis analysis)
    {
        // Adaptive question count based on user performance
        return analysis.AverageScore switch
        {
            >= 85 => 7, // More questions for high performers
            >= 70 => 5, // Standard count
            _ => 3 // Fewer questions for struggling users
        };
    }

    private int? CalculateAdaptiveTimeLimit(UserPerformanceAnalysis analysis, ReadingText text)
    {
        var baseTime = CalculateRecommendedTimeLimit(text, ExerciseType.ReadingComprehension);
        
        // Adjust based on user's average completion time
        var adjustment = analysis.AverageCompletionTime.TotalMinutes / 15.0; // 15 min baseline
        
        return Math.Max(5, (int)(baseTime * adjustment));
    }

    private int CalculateAdaptivePassingScore(UserPerformanceAnalysis analysis)
    {
        // Adaptive passing score
        return analysis.AverageScore switch
        {
            >= 85 => 70, // Higher standard for high performers
            >= 70 => 60, // Standard passing score
            _ => 50 // Lower threshold for struggling users
        };
    }

    private Dictionary<string, object> CreateAdaptiveOptions(UserPerformanceAnalysis analysis)
    {
        return new Dictionary<string, object>
        {
            ["focus_areas"] = analysis.WeakAreas,
            ["adaptive_mode"] = true,
            ["personalized"] = true,
            ["performance_level"] = analysis.AverageScore
        };
    }

    private async Task<List<QuestionTemplate>> CreateQuestionTemplatesAsync(TemplateCreationRequest request)
    {
        // Create question templates based on exercise type
        return request.ExerciseType switch
        {
            ExerciseType.ReadingComprehension => CreateComprehensionQuestionTemplates(),
            ExerciseType.VocabularyTest => CreateVocabularyQuestionTemplates(),
            ExerciseType.SpeedReading => CreateSpeedReadingQuestionTemplates(),
            _ => new List<QuestionTemplate>()
        };
    }

    private List<QuestionTemplate> CreateComprehensionQuestionTemplates()
    {
        return new List<QuestionTemplate>
        {
            new() { Type = QuestionType.MultipleChoice, Category = "main_idea", Points = 25, Template = "Bu metnin ana fikri nedir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "detail", Points = 20, Template = "Metne göre aşağıdakilerden hangisi doğrudur?" },
            new() { Type = QuestionType.MultipleChoice, Category = "inference", Points = 30, Template = "Metinden çıkarılabilecek sonuç nedir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "vocabulary", Points = 25, Template = "'{word}' kelimesinin anlamı nedir?" }
        };
    }

    private List<QuestionTemplate> CreateVocabularyQuestionTemplates()
    {
        return new List<QuestionTemplate>
        {
            new() { Type = QuestionType.MultipleChoice, Category = "word_meaning", Points = 20, Template = "'{word}' kelimesinin anlamı nedir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "synonym", Points = 20, Template = "'{word}' kelimesinin eş anlamlısı hangisidir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "usage", Points = 20, Template = "'{word}' kelimesi hangi cümlede doğru kullanılmıştır?" }
        };
    }

    private List<QuestionTemplate> CreateSpeedReadingQuestionTemplates()
    {
        return new List<QuestionTemplate>
        {
            new() { Type = QuestionType.MultipleChoice, Category = "main_topic", Points = 40, Template = "Bu metnin ana konusu nedir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "key_facts", Points = 30, Template = "Metinde vurgulanan önemli bilgi hangisidir?" },
            new() { Type = QuestionType.MultipleChoice, Category = "conclusion", Points = 30, Template = "Metnin sonuç bölümünde vurgulanan nokta nedir?" }
        };
    }

    private async Task ApplyQuestionTemplatesAsync(Exercise exercise, List<QuestionTemplate> templates, ReadingText text)
    {
        // Template-based question generation logic would be implemented here
        // This is a placeholder for the actual implementation
    }

    private Dictionary<string, object> MergeOptions(Dictionary<string, object>? templateSettings, Dictionary<string, object>? requestOptions)
    {
        var merged = new Dictionary<string, object>();
        
        if (templateSettings != null)
        {
            foreach (var kvp in templateSettings)
                merged[kvp.Key] = kvp.Value;
        }
        
        if (requestOptions != null)
        {
            foreach (var kvp in requestOptions)
                merged[kvp.Key] = kvp.Value; // Override template settings
        }
        
        return merged;
    }
}

// Supporting classes  
public class ExerciseGenerationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReadingText ReadingText { get; set; } = null!;
    public ExerciseType ExerciseType { get; set; }
    public EducationCategory TargetEducationLevel { get; set; }
    public TextDifficulty? DifficultyLevel { get; set; }
    public int QuestionCount { get; set; } = 5;
    public int? TimeLimit { get; set; }
    public int PassingScore { get; set; } = 60;
    public bool IsRandomized { get; set; } = true;
    public bool AllowRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public Guid CreatedBy { get; set; }
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

public class ExerciseSetGenerationRequest
{
    public ReadingText ReadingText { get; set; } = null!;
    public List<ExerciseGenerationRequest> ExerciseRequests { get; set; } = new();
    public Guid CreatedBy { get; set; }
}

public class AdaptiveExerciseRequest
{
    public Guid UserId { get; set; }
    public ReadingText ReadingText { get; set; } = null!;
    public EducationCategory TargetEducationLevel { get; set; }
    public Guid CreatedBy { get; set; }
}

public class TemplateCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    public EducationCategory TargetEducationLevel { get; set; }
    public int DefaultQuestionCount { get; set; } = 5;
    public int DefaultTimeLimit { get; set; } = 30;
    public int DefaultPassingScore { get; set; } = 60;
    public Dictionary<string, object> Settings { get; set; } = new();
    public Guid CreatedBy { get; set; }
}

public class TemplateGenerationRequest
{
    public ExerciseTemplate Template { get; set; } = null!;
    public ReadingText ReadingText { get; set; } = null!;
    public TextDifficulty? DifficultyLevel { get; set; }
    public int? QuestionCount { get; set; }
    public int? TimeLimit { get; set; }
    public int? PassingScore { get; set; }
    public bool? IsRandomized { get; set; }
    public bool? AllowRetry { get; set; }
    public int? MaxRetries { get; set; }
    public Guid CreatedBy { get; set; }
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

public class SimpleTextAnalysisResult
{
    public int WordCount { get; set; }
    public int SentenceCount { get; set; }
    public int ParagraphCount { get; set; }
    public double ReadabilityScore { get; set; }
    public IReadOnlyList<string> KeyWords { get; set; } = new List<string>();
    public IReadOnlyList<string> Sentences { get; set; } = new List<string>();
    public IReadOnlyList<string> ImportantSentences { get; set; } = new List<string>();
    public string TopicAnalysis { get; set; } = string.Empty;
}

public class UserPerformanceAnalysis
{
    public Guid UserId { get; set; }
    public double AverageScore { get; set; }
    public ExerciseType[] PreferredExerciseTypes { get; set; } = Array.Empty<ExerciseType>();
    public string[] WeakAreas { get; set; } = Array.Empty<string>();
    public string[] StrongAreas { get; set; } = Array.Empty<string>();
    public TimeSpan AverageCompletionTime { get; set; }
    public TextDifficulty RecommendedDifficulty { get; set; }
}

public class ExerciseTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TitleTemplate { get; set; }
    public ExerciseType ExerciseType { get; set; }
    public EducationCategory TargetEducationLevel { get; set; }
    public int DefaultQuestionCount { get; set; }
    public int DefaultTimeLimit { get; set; }
    public int DefaultPassingScore { get; set; }
    public List<QuestionTemplate> QuestionTemplates { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuestionTemplate
{
    public QuestionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Template { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
}