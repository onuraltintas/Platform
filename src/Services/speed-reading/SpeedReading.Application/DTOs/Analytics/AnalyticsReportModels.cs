using SpeedReading.Domain.Enums;
using SpeedReading.Application.Services;

namespace SpeedReading.Application.DTOs.Analytics;

// Main Report Classes
public class UserComprehensionReport
{
    public Guid UserId { get; set; }
    public ReportPeriod ReportPeriod { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    
    public OverallStatistics OverallStatistics { get; set; } = new();
    public Dictionary<ExerciseType, TypePerformance> PerformanceByType { get; set; } = new();
    public Dictionary<TextDifficulty, DifficultyPerformance> PerformanceByDifficulty { get; set; } = new();
    public LearningProgressionData LearningProgression { get; set; } = new();
    public SkillAnalysisData SkillAnalysis { get; set; } = new();
    public ReadingSpeedAnalysisData ReadingSpeedAnalysis { get; set; } = new();
    
    public List<string> Recommendations { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

public class ExerciseAnalyticsReport
{
    public Guid ExerciseId { get; set; }
    public string ExerciseTitle { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    public EducationCategory TargetEducationLevel { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public ExerciseOverallStatistics OverallStatistics { get; set; } = new();
    public List<QuestionAnalysisResult> QuestionAnalysis { get; set; } = new();
    public ExerciseDifficultyAnalysis DifficultyAnalysis { get; set; } = new();
    public ExerciseTimeAnalysis TimeAnalysis { get; set; } = new();
}

public class ComprehensionTrendsReport
{
    public Guid? UserId { get; set; }
    public TimeSpan Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public List<DailyTrend> DailyTrends { get; set; } = new();
    public List<WeeklyTrend> WeeklyTrends { get; set; } = new();
    public List<ImprovementTrend> ImprovementTrends { get; set; } = new();
    public Dictionary<ExerciseType, int> PopularExerciseTypes { get; set; } = new();
}

public class ComprehensionComparisonReport
{
    public Guid UserId { get; set; }
    public EducationCategory EducationLevel { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public PerformanceComparison OverallComparison { get; set; } = new();
    public PerformanceComparison ReadingSpeedComparison { get; set; } = new();
    public Dictionary<ExerciseType, PerformanceComparison> ExerciseTypeComparisons { get; set; } = new();
}

// Supporting Data Classes
public class ReportPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public TimeSpan Duration => EndDate - StartDate;
    public string FormattedPeriod => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
}

public class OverallStatistics
{
    public int TotalAttempts { get; set; }
    public double AverageScore { get; set; }
    public double BestScore { get; set; }
    public double PassRate { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public TimeSpan AverageTimePerAttempt { get; set; }
    public double ImprovementRate { get; set; }
    
    public string PerformanceLevel => AverageScore switch
    {
        >= 90 => "Mükemmel",
        >= 80 => "Çok İyi",
        >= 70 => "İyi",
        >= 60 => "Orta",
        _ => "Geliştirilmeli"
    };
}

public class TypePerformance
{
    public ExerciseType Type { get; set; }
    public int AttemptCount { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double ImprovementTrend { get; set; }
    
    public string TypeName => Type switch
    {
        ExerciseType.ReadingComprehension => "Okuduğunu Anlama",
        ExerciseType.VocabularyTest => "Kelime Bilgisi",
        ExerciseType.SpeedReading => "Hızlı Okuma",
        ExerciseType.SkimmingScanning => "Tarama ve Göz Atma",
        ExerciseType.CriticalThinking => "Eleştirel Düşünme",
        ExerciseType.SummaryWriting => "Özetleme",
        ExerciseType.MainIdeaFinding => "Ana Fikir Bulma",
        ExerciseType.DetailedReading => "Detaylı Okuma",
        _ => Type.ToString()
    };
    
    public string PerformanceLevel => AverageScore switch
    {
        >= 85 => "Uzman",
        >= 75 => "İleri",
        >= 65 => "Orta",
        >= 50 => "Temel",
        _ => "Başlangıç"
    };
}

public class DifficultyPerformance
{
    public TextDifficulty Difficulty { get; set; }
    public int AttemptCount { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public bool ReadinessForNextLevel { get; set; }
    
    public string DifficultyName => Difficulty switch
    {
        TextDifficulty.Easy => "Kolay",
        TextDifficulty.Medium => "Orta",
        TextDifficulty.Hard => "Zor",
        TextDifficulty.VeryHard => "Çok Zor",
        _ => Difficulty.ToString()
    };
}

public class LearningProgressionData
{
    public ProgressionPoint[] ProgressionPoints { get; set; } = Array.Empty<ProgressionPoint>();
    public double OverallTrend { get; set; }
    public double ConsistencyScore { get; set; }
    
    public string TrendDescription => OverallTrend switch
    {
        > 10 => "Güçlü gelişim",
        > 5 => "İyi gelişim",
        > 0 => "Hafif gelişim",
        > -5 => "Stabil performans",
        _ => "Gelişim gerekli"
    };
    
    public string ConsistencyDescription => ConsistencyScore switch
    {
        >= 80 => "Çok tutarlı",
        >= 60 => "Tutarlı",
        >= 40 => "Orta tutarlılık",
        _ => "Tutarsız performans"
    };
}

public class ProgressionPoint
{
    public DateTime Date { get; set; }
    public double Score { get; set; }
    public int AttemptCount { get; set; }
    public double PassRate { get; set; }
}

public class SkillAnalysisData
{
    public Dictionary<string, SkillScore> SkillScores { get; set; } = new();
    public string StrongestSkill { get; set; } = string.Empty;
    public string WeakestSkill { get; set; } = string.Empty;
    public string OverallSkillLevel { get; set; } = string.Empty;
}

public class SkillScore
{
    public string SkillName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public int AttemptCount { get; set; }
    public double ImprovementTrend { get; set; }
    public string ProficiencyLevel { get; set; } = string.Empty;
    
    public string SkillDisplayName => SkillName switch
    {
        "main_idea" => "Ana Fikir",
        "detail" => "Detay Anlama",
        "inference" => "Çıkarım Yapma",
        "vocabulary" => "Kelime Bilgisi",
        "cause_effect" => "Neden-Sonuç",
        "summary" => "Özetleme",
        "critical_thinking" => "Eleştirel Düşünme",
        _ => SkillName
    };
}

public class ReadingSpeedAnalysisData
{
    public double AverageWPM { get; set; }
    public double BestWPM { get; set; }
    public double SpeedImprovement { get; set; }
    public double SpeedConsistency { get; set; }
    public ReadingSpeedLevel SpeedLevel { get; set; }
    
    public string SpeedDescription => SpeedLevel switch
    {
        ReadingSpeedLevel.VeryFast => "Çok Hızlı",
        ReadingSpeedLevel.Fast => "Hızlı",
        ReadingSpeedLevel.Average => "Ortalama",
        ReadingSpeedLevel.Slow => "Yavaş",
        ReadingSpeedLevel.VerySlow => "Çok Yavaş",
        _ => "Bilinmiyor"
    };
    
    public string SpeedFeedback => AverageWPM switch
    {
        >= 400 => "Mükemmel okuma hızı! Bu seviyeyi koruyun.",
        >= 300 => "Çok iyi okuma hızı. Anlama kalitesini artırmaya odaklanın.",
        >= 200 => "İyi okuma hızı. Hızlı okuma teknikleri ile gelişebilirsiniz.",
        >= 150 => "Ortalama hız. Düzenli pratik ile gelişim sağlayabilirsiniz.",
        _ => "Okuma hızınızı artırmak için temel teknikleri öğrenin."
    };
}

// Exercise Analytics Classes
public class ExerciseOverallStatistics
{
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public double CompletionRate { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public TimeSpan AverageTimeSpent { get; set; }
}

public class QuestionAnalysisResult
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int TotalResponses { get; set; }
    public int CorrectResponses { get; set; }
    public double CorrectRate { get; set; }
    public double AverageTimeSpent { get; set; }
    public string DifficultyLevel { get; set; } = string.Empty;
    
    public string QuestionTypeDisplay => QuestionType switch
    {
        QuestionType.MultipleChoice => "Çoktan Seçmeli",
        QuestionType.TrueFalse => "Doğru-Yanlış",
        QuestionType.ShortAnswer => "Kısa Cevap",
        QuestionType.Essay => "Uzun Cevap",
        QuestionType.FillInTheBlank => "Boşluk Doldurma",
        QuestionType.Matching => "Eşleştirme",
        QuestionType.Ordering => "Sıralama",
        QuestionType.DragAndDrop => "Sürükle-Bırak",
        _ => QuestionType.ToString()
    };
}

public class ExerciseDifficultyAnalysis
{
    public string ActualDifficulty { get; set; } = string.Empty;
    public string RecommendedDifficulty { get; set; } = string.Empty;
    public double DifficultyScore { get; set; }
}

public class ExerciseTimeAnalysis
{
    public TimeSpan AverageTimeSpent { get; set; }
    public TimeSpan TimeLimit { get; set; }
    public double TimeUtilizationRate { get; set; }
    public TimeSpan RecommendedTimeLimit { get; set; }
    public string TimeEfficiencyRating { get; set; } = string.Empty;
}

// Trend Analysis Classes
public class DailyTrend
{
    public DateTime Date { get; set; }
    public int AttemptCount { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    
    public string DateFormatted => Date.ToString("dd.MM.yyyy");
}

public class WeeklyTrend
{
    public DateTime WeekStart { get; set; }
    public int AttemptCount { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double ImprovementFromPreviousWeek { get; set; }
    
    public string WeekFormatted => $"{WeekStart:dd.MM} - {WeekStart.AddDays(6):dd.MM}";
}

public class ImprovementTrend
{
    public string Period { get; set; } = string.Empty;
    public double ImprovementRate { get; set; }
    public bool IsImproving { get; set; }
    
    public string ImprovementDescription => ImprovementRate switch
    {
        > 10 => "Güçlü gelişim",
        > 5 => "İyi gelişim", 
        > 0 => "Hafif gelişim",
        > -5 => "Stabil",
        _ => "Geriye gidiş"
    };
}

// Comparison Classes
public class PerformanceComparison
{
    public double UserScore { get; set; }
    public double PeerAverageScore { get; set; }
    public double UserPercentile { get; set; }
    
    public string ComparisonResult => UserScore switch
    {
        var score when score > PeerAverageScore * 1.2 => "Akranlarından çok iyi",
        var score when score > PeerAverageScore * 1.1 => "Akranlarından iyi",
        var score when score > PeerAverageScore * 0.9 => "Akranlarıyla aynı seviyede",
        var score when score > PeerAverageScore * 0.8 => "Akranlarından biraz geride",
        _ => "Akranlarından geride"
    };
    
    public string PercentileDescription => UserPercentile switch
    {
        >= 90 => "En iyi %10'da",
        >= 75 => "En iyi %25'te",
        >= 50 => "Ortalamanın üstünde",
        >= 25 => "Ortalamanın altında",
        _ => "En alt %25'te"
    };
}