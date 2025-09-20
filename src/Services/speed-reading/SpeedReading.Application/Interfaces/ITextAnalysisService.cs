using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Application.Interfaces;

public interface ITextAnalysisService
{
    Task<TextAnalysisResult> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default);
    TextDifficulty CalculateDifficulty(TextStatistics statistics);
    EducationCategory DetermineTargetEducationLevel(TextDifficulty difficulty, TextStatistics statistics);
    double CalculateReadabilityScore(string text); // Turkish-specific readability
    Task<string[]> ExtractKeywordsAsync(string text, int maxKeywords = 10, CancellationToken cancellationToken = default);
    Task<string> GenerateSummaryAsync(string text, int maxLength = 200, CancellationToken cancellationToken = default);
}

public interface ITurkishLanguageService
{
    int CountSyllables(string word);
    string[] TokenizeWords(string text);
    string[] TokenizeSentences(string text);
    string[] TokenizeParagraphs(string text);
    string RemoveDiacritics(string text); // Remove Turkish special characters for analysis
    bool IsComplexWord(string word); // 3+ syllables
    string GetWordRoot(string word); // Kök bulma
    string[] GetCommonWords(); // Türkçe yaygın kelimeler
    double CalculateTurkishReadabilityIndex(string text); // Ateşman or similar formula
}

public class TextAnalysisResult
{
    public TextStatistics Statistics { get; set; } = null!;
    public TextDifficulty Difficulty { get; set; }
    public EducationCategory TargetEducationLevel { get; set; }
    public double ReadabilityScore { get; set; }
    public double DifficultyScore { get; set; }
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, int> WordFrequency { get; set; } = new();
    public int ComplexWordCount { get; set; }
    public double ComplexWordPercentage { get; set; }
    public int AverageSyllablesPerWord { get; set; }
    
    // Turkish-specific metrics
    public int TurkishSuffixComplexity { get; set; } // Ek karmaşıklığı
    public double VerbDensity { get; set; } // Fiil yoğunluğu
    public double NounDensity { get; set; } // İsim yoğunluğu
    public double AdjectiveDensity { get; set; } // Sıfat yoğunluğu
}