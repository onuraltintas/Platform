using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services;

public class ComprehensionScoringService
{
    public async Task<ComprehensionScoreBreakdown> CalculateComprehensionScoreAsync(
        ExerciseAttempt attempt, 
        IReadOnlyList<Question> questions,
        ReadingText readingText)
    {
        var breakdown = new ComprehensionScoreBreakdown();
        
        // Kategorilere göre puan hesaplama
        breakdown.MainIdeaScore = await CalculateMainIdeaScoreAsync(attempt, questions);
        breakdown.DetailScore = await CalculateDetailScoreAsync(attempt, questions);
        breakdown.InferenceScore = await CalculateInferenceScoreAsync(attempt, questions);
        breakdown.VocabularyScore = await CalculateVocabularyScoreAsync(attempt, questions);
        breakdown.SummaryScore = await CalculateSummaryScoreAsync(attempt, questions);
        
        // Genel anlama puanı
        breakdown.OverallComprehensionScore = CalculateOverallScore(breakdown);
        
        // Okuma hızı analizi
        breakdown.ReadingSpeedAnalysis = await CalculateReadingSpeedAnalysisAsync(attempt, readingText);
        
        // Güçlü ve zayıf yönler
        breakdown.StrengthAreas = IdentifyStrengthAreas(breakdown);
        breakdown.WeaknessAreas = IdentifyWeaknessAreas(breakdown);
        
        // Öneri ve tavsiyeler
        breakdown.Recommendations = GenerateRecommendations(breakdown);
        
        return breakdown;
    }

    private async Task<CategoryScore> CalculateMainIdeaScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var mainIdeaQuestions = questions.Where(q => q.Metadata.Contains("main_idea")).ToList();
        return await CalculateCategoryScoreAsync(attempt, mainIdeaQuestions, "Ana Fikir");
    }

    private async Task<CategoryScore> CalculateDetailScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var detailQuestions = questions.Where(q => q.Metadata.Contains("detail")).ToList();
        return await CalculateCategoryScoreAsync(attempt, detailQuestions, "Detay");
    }

    private async Task<CategoryScore> CalculateInferenceScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var inferenceQuestions = questions.Where(q => q.Metadata.Contains("inference")).ToList();
        return await CalculateCategoryScoreAsync(attempt, inferenceQuestions, "Çıkarım");
    }

    private async Task<CategoryScore> CalculateVocabularyScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var vocabularyQuestions = questions.Where(q => q.Metadata.Contains("vocabulary")).ToList();
        return await CalculateCategoryScoreAsync(attempt, vocabularyQuestions, "Kelime Bilgisi");
    }

    private async Task<CategoryScore> CalculateSummaryScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var summaryQuestions = questions.Where(q => q.Metadata.Contains("summary")).ToList();
        return await CalculateCategoryScoreAsync(attempt, summaryQuestions, "Özetleme");
    }

    private async Task<CategoryScore> CalculateCategoryScoreAsync(ExerciseAttempt attempt, List<Question> categoryQuestions, string categoryName)
    {
        if (!categoryQuestions.Any())
        {
            return new CategoryScore
            {
                CategoryName = categoryName,
                Score = 0,
                MaxScore = 0,
                Percentage = 0,
                QuestionCount = 0
            };
        }

        var totalScore = 0;
        var maxScore = categoryQuestions.Sum(q => q.Points);
        var correctCount = 0;

        foreach (var question in categoryQuestions)
        {
            var userAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (userAnswer != null && userAnswer.IsCorrect)
            {
                totalScore += userAnswer.PointsEarned;
                correctCount++;
            }
        }

        return new CategoryScore
        {
            CategoryName = categoryName,
            Score = totalScore,
            MaxScore = maxScore,
            Percentage = maxScore > 0 ? (double)totalScore / maxScore * 100 : 0,
            QuestionCount = categoryQuestions.Count,
            CorrectAnswers = correctCount
        };
    }

    private double CalculateOverallScore(ComprehensionScoreBreakdown breakdown)
    {
        var scores = new List<CategoryScore>
        {
            breakdown.MainIdeaScore,
            breakdown.DetailScore,
            breakdown.InferenceScore,
            breakdown.VocabularyScore,
            breakdown.SummaryScore
        };

        var totalScore = scores.Sum(s => s.Score);
        var totalMaxScore = scores.Sum(s => s.MaxScore);

        return totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;
    }

    private async Task<ReadingSpeedAnalysis> CalculateReadingSpeedAnalysisAsync(ExerciseAttempt attempt, ReadingText readingText)
    {
        var readingTime = attempt.TimeSpent ?? TimeSpan.Zero;
        var wordCount = readingText.Statistics.WordCount;
        
        // Soru cevaplama süresini çıkar (yaklaşık %20)
        var estimatedReadingTime = TimeSpan.FromMinutes(readingTime.TotalMinutes * 0.8);
        
        var wpm = estimatedReadingTime.TotalMinutes > 0 
            ? wordCount / estimatedReadingTime.TotalMinutes 
            : 0;

        var speedLevel = ClassifyReadingSpeed(wpm);
        var speedPercentile = CalculateSpeedPercentile(wpm);

        return new ReadingSpeedAnalysis
        {
            WordsPerMinute = (int)wpm,
            ReadingTime = estimatedReadingTime,
            TotalTime = readingTime,
            WordCount = wordCount,
            SpeedLevel = speedLevel,
            SpeedPercentile = speedPercentile,
            SpeedFeedback = GenerateSpeedFeedback(speedLevel, wpm)
        };
    }

    private ReadingSpeedLevel ClassifyReadingSpeed(double wpm)
    {
        return wpm switch
        {
            >= 400 => ReadingSpeedLevel.VeryFast,
            >= 300 => ReadingSpeedLevel.Fast,
            >= 200 => ReadingSpeedLevel.Average,
            >= 150 => ReadingSpeedLevel.Slow,
            _ => ReadingSpeedLevel.VerySlow
        };
    }

    private int CalculateSpeedPercentile(double wpm)
    {
        // Basit percentile hesaplama - gerçek implementasyonda normalizasyon gerekli
        return wpm switch
        {
            >= 400 => 95,
            >= 350 => 85,
            >= 300 => 75,
            >= 250 => 60,
            >= 200 => 50,
            >= 150 => 25,
            _ => 10
        };
    }

    private string GenerateSpeedFeedback(ReadingSpeedLevel level, double wpm)
    {
        return level switch
        {
            ReadingSpeedLevel.VeryFast => 
                $"Mükemmel! {wpm:0} WPM ile çok hızlı okuyorsunuz. Bu hızı koruyarak anlama düzeyinizi artırmaya odaklanın.",
            
            ReadingSpeedLevel.Fast => 
                $"Harika! {wpm:0} WPM hızlı okuma seviyesi. Biraz daha hızlanabilir ve anlama kalitesini koruyabilirsiniz.",
            
            ReadingSpeedLevel.Average => 
                $"İyi seviye! {wpm:0} WPM ortalama okuma hızı. Hızlı okuma teknikleriyle gelişebilirsiniz.",
            
            ReadingSpeedLevel.Slow => 
                $"{wpm:0} WPM biraz yavaş. Göz hareketlerini geliştirerek daha hızlı okumaya çalışın.",
            
            ReadingSpeedLevel.VerySlow => 
                $"{wpm:0} WPM oldukça yavaş. Temel hızlı okuma tekniklerini öğrenmenizi öneririz.",
            
            _ => $"Okuma hızınız: {wpm:0} WPM"
        };
    }

    private List<string> IdentifyStrengthAreas(ComprehensionScoreBreakdown breakdown)
    {
        var strengths = new List<string>();
        var categories = new List<CategoryScore>
        {
            breakdown.MainIdeaScore,
            breakdown.DetailScore,
            breakdown.InferenceScore,
            breakdown.VocabularyScore,
            breakdown.SummaryScore
        };

        var strongCategories = categories.Where(c => c.Percentage >= 80 && c.QuestionCount > 0).ToList();
        
        foreach (var category in strongCategories)
        {
            strengths.Add($"{category.CategoryName}: %{category.Percentage:0} başarı");
        }

        if (breakdown.ReadingSpeedAnalysis.SpeedLevel >= ReadingSpeedLevel.Fast)
        {
            strengths.Add($"Hızlı Okuma: {breakdown.ReadingSpeedAnalysis.WordsPerMinute} WPM");
        }

        return strengths;
    }

    private List<string> IdentifyWeaknessAreas(ComprehensionScoreBreakdown breakdown)
    {
        var weaknesses = new List<string>();
        var categories = new List<CategoryScore>
        {
            breakdown.MainIdeaScore,
            breakdown.DetailScore,
            breakdown.InferenceScore,
            breakdown.VocabularyScore,
            breakdown.SummaryScore
        };

        var weakCategories = categories.Where(c => c.Percentage < 60 && c.QuestionCount > 0).ToList();
        
        foreach (var category in weakCategories)
        {
            weaknesses.Add($"{category.CategoryName}: %{category.Percentage:0} başarı - geliştirilmeli");
        }

        if (breakdown.ReadingSpeedAnalysis.SpeedLevel <= ReadingSpeedLevel.Slow)
        {
            weaknesses.Add($"Okuma Hızı: {breakdown.ReadingSpeedAnalysis.WordsPerMinute} WPM - artırılmalı");
        }

        return weaknesses;
    }

    private List<string> GenerateRecommendations(ComprehensionScoreBreakdown breakdown)
    {
        var recommendations = new List<string>();

        // Ana fikir önerileri
        if (breakdown.MainIdeaScore.Percentage < 70)
        {
            recommendations.Add("Ana fikri bulmak için metni paragraf paragraf okuyun ve her paragrafın ana düşüncesini belirleyin.");
        }

        // Detay önerileri
        if (breakdown.DetailScore.Percentage < 70)
        {
            recommendations.Add("Detayları kaçırmamak için önemli bilgileri işaretleyin ve not alın.");
        }

        // Çıkarım önerileri
        if (breakdown.InferenceScore.Percentage < 70)
        {
            recommendations.Add("Satır aralarını okuma becerinizi geliştirin. Yazarın ima ettiği anlamları bulun.");
        }

        // Kelime bilgisi önerileri
        if (breakdown.VocabularyScore.Percentage < 70)
        {
            recommendations.Add("Kelime hazinenizi genişletin. Bilinmeyen kelimeleri bağlamdan anlamaya çalışın.");
        }

        // Hız önerileri
        if (breakdown.ReadingSpeedAnalysis.SpeedLevel <= ReadingSpeedLevel.Average)
        {
            recommendations.Add("Hızlı okuma teknikleri ile okuma hızınızı artırın. Göz hareketlerini geliştirin.");
        }

        // Genel öneri
        if (breakdown.OverallComprehensionScore < 70)
        {
            recommendations.Add("Düzenli okuma alışkanlığı edinin ve farklı türde metinlerle pratik yapın.");
        }

        return recommendations;
    }
}

// Supporting classes
public class ComprehensionScoreBreakdown
{
    public CategoryScore MainIdeaScore { get; set; } = new();
    public CategoryScore DetailScore { get; set; } = new();
    public CategoryScore InferenceScore { get; set; } = new();
    public CategoryScore VocabularyScore { get; set; } = new();
    public CategoryScore SummaryScore { get; set; } = new();
    
    public double OverallComprehensionScore { get; set; }
    public ReadingSpeedAnalysis ReadingSpeedAnalysis { get; set; } = new();
    
    public List<string> StrengthAreas { get; set; } = new();
    public List<string> WeaknessAreas { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class CategoryScore
{
    public string CategoryName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public double Percentage { get; set; }
    public int QuestionCount { get; set; }
    public int CorrectAnswers { get; set; }
}

public class ReadingSpeedAnalysis
{
    public int WordsPerMinute { get; set; }
    public TimeSpan ReadingTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    public int WordCount { get; set; }
    public ReadingSpeedLevel SpeedLevel { get; set; }
    public int SpeedPercentile { get; set; }
    public string SpeedFeedback { get; set; } = string.Empty;
}

public enum ReadingSpeedLevel
{
    VerySlow,
    Slow,
    Average,
    Fast,
    VeryFast
}