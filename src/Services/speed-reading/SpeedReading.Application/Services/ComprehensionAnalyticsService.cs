using SpeedReading.Application.DTOs.Analytics;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Repositories;

namespace SpeedReading.Application.Services;

public class ComprehensionAnalyticsService
{
    private readonly IExerciseAttemptRepository _attemptRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ComprehensionScoringService _scoringService;

    public ComprehensionAnalyticsService(
        IExerciseAttemptRepository attemptRepository,
        IExerciseRepository exerciseRepository,
        ComprehensionScoringService scoringService)
    {
        _attemptRepository = attemptRepository;
        _exerciseRepository = exerciseRepository;
        _scoringService = scoringService;
    }

    public async Task<UserComprehensionReport> GenerateUserComprehensionReportAsync(
        Guid userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var criteria = new AttemptSearchCriteria
        {
            UserId = userId,
            Status = AttemptStatus.Completed,
            StartedAfter = startDate,
            StartedBefore = endDate,
            Take = 1000
        };

        var attempts = await _attemptRepository.SearchAsync(criteria, cancellationToken);
        
        var report = new UserComprehensionReport
        {
            UserId = userId,
            ReportPeriod = new ReportPeriod
            {
                StartDate = startDate ?? attempts.Min(a => a.StartedAt),
                EndDate = endDate ?? attempts.Max(a => a.StartedAt)
            },
            GeneratedAt = DateTime.UtcNow
        };

        if (!attempts.Any())
        {
            return report;
        }

        // Overall statistics
        report.OverallStatistics = CalculateOverallStatistics(attempts);
        
        // Performance by exercise type
        report.PerformanceByType = await CalculatePerformanceByTypeAsync(attempts, cancellationToken);
        
        // Performance by difficulty
        report.PerformanceByDifficulty = await CalculatePerformanceByDifficultyAsync(attempts, cancellationToken);
        
        // Learning progression
        report.LearningProgression = CalculateLearningProgression(attempts);
        
        // Skill analysis
        report.SkillAnalysis = await CalculateSkillAnalysisAsync(attempts, cancellationToken);
        
        // Reading speed analysis
        report.ReadingSpeedAnalysis = CalculateReadingSpeedAnalysis(attempts);
        
        // Recommendations
        report.Recommendations = await GenerateRecommendationsAsync(report, cancellationToken);
        
        // Strengths and weaknesses
        report.Strengths = IdentifyStrengths(report);
        report.Weaknesses = IdentifyWeaknesses(report);

        return report;
    }

    public async Task<ExerciseAnalyticsReport> GenerateExerciseAnalyticsAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default)
    {
        var exercise = await _exerciseRepository.GetByIdWithQuestionsAsync(exerciseId, cancellationToken);
        if (exercise == null) throw new ArgumentException("Exercise not found", nameof(exerciseId));

        var attempts = await _attemptRepository.GetByExerciseIdAsync(exerciseId, cancellationToken);
        var completedAttempts = attempts.Where(a => a.Status == AttemptStatus.Completed).ToArray();

        var report = new ExerciseAnalyticsReport
        {
            ExerciseId = exerciseId,
            ExerciseTitle = exercise.Title,
            ExerciseType = exercise.Type,
            TargetEducationLevel = exercise.TargetEducationLevel,
            GeneratedAt = DateTime.UtcNow
        };

        if (!completedAttempts.Any())
        {
            return report;
        }

        // Overall exercise statistics
        report.OverallStatistics = new ExerciseOverallStatistics
        {
            TotalAttempts = attempts.Count,
            CompletedAttempts = completedAttempts.Length,
            CompletionRate = (double)completedAttempts.Length / attempts.Count * 100,
            AverageScore = completedAttempts.Average(a => a.ScorePercentage),
            PassRate = completedAttempts.Count(a => a.IsPassed) / (double)completedAttempts.Length * 100,
            AverageTimeSpent = TimeSpan.FromTicks((long)completedAttempts.Where(a => a.TimeSpent.HasValue)
                .Average(a => a.TimeSpent!.Value.Ticks))
        };

        // Question analysis
        report.QuestionAnalysis = await AnalyzeQuestionsAsync(exercise.Questions, completedAttempts);

        // Difficulty analysis
        report.DifficultyAnalysis = CalculateDifficultyAnalysis(completedAttempts, report.OverallStatistics.AverageScore);

        // Time analysis
        report.TimeAnalysis = CalculateTimeAnalysis(completedAttempts, exercise.TimeLimit);

        return report;
    }

    public async Task<ComprehensionTrendsReport> GenerateComprehensionTrendsAsync(
        Guid? userId = null,
        TimeSpan? period = null,
        CancellationToken cancellationToken = default)
    {
        period ??= TimeSpan.FromDays(30);
        var startDate = DateTime.UtcNow.Subtract(period.Value);

        var criteria = new AttemptSearchCriteria
        {
            UserId = userId,
            Status = AttemptStatus.Completed,
            StartedAfter = startDate,
            Take = 10000
        };

        var attempts = await _attemptRepository.SearchAsync(criteria, cancellationToken);
        
        var report = new ComprehensionTrendsReport
        {
            UserId = userId,
            Period = period.Value,
            StartDate = startDate,
            EndDate = DateTime.UtcNow,
            GeneratedAt = DateTime.UtcNow
        };

        if (!attempts.Any())
        {
            return report;
        }

        // Daily performance trends
        report.DailyTrends = CalculateDailyTrends(attempts, startDate, DateTime.UtcNow);

        // Weekly performance trends
        report.WeeklyTrends = CalculateWeeklyTrends(attempts, startDate, DateTime.UtcNow);

        // Performance improvement trends
        report.ImprovementTrends = CalculateImprovementTrends(attempts);

        // Popular exercise types
        report.PopularExerciseTypes = await CalculatePopularExerciseTypesAsync(attempts, cancellationToken);

        return report;
    }

    public async Task<ComprehensionComparisonReport> GenerateComparisonReportAsync(
        Guid userId,
        EducationCategory educationLevel,
        CancellationToken cancellationToken = default)
    {
        // Get user's performance
        var userCriteria = new AttemptSearchCriteria
        {
            UserId = userId,
            Status = AttemptStatus.Completed,
            Take = 1000
        };

        var userAttempts = await _attemptRepository.SearchAsync(userCriteria, cancellationToken);

        // Get peer performance (same education level)
        var peerCriteria = new AttemptSearchCriteria
        {
            Status = AttemptStatus.Completed,
            EducationLevel = educationLevel,
            Take = 10000
        };

        var peerAttempts = await _attemptRepository.SearchAsync(peerCriteria, cancellationToken);
        var peerAttemptsExcludingUser = peerAttempts.Where(a => a.UserId != userId).ToArray();

        var report = new ComprehensionComparisonReport
        {
            UserId = userId,
            EducationLevel = educationLevel,
            GeneratedAt = DateTime.UtcNow
        };

        if (!userAttempts.Any() || !peerAttemptsExcludingUser.Any())
        {
            return report;
        }

        // Overall comparison
        report.OverallComparison = new PerformanceComparison
        {
            UserScore = userAttempts.Average(a => a.ScorePercentage),
            PeerAverageScore = peerAttemptsExcludingUser.Average(a => a.ScorePercentage),
            UserPercentile = CalculatePercentile(
                userAttempts.Average(a => a.ScorePercentage),
                peerAttempts.Select(a => a.ScorePercentage).ToArray())
        };

        // Reading speed comparison
        var userAvgWpm = CalculateAverageReadingSpeed(userAttempts);
        var peerAvgWpm = CalculateAverageReadingSpeed(peerAttemptsExcludingUser);

        report.ReadingSpeedComparison = new PerformanceComparison
        {
            UserScore = userAvgWpm,
            PeerAverageScore = peerAvgWpm,
            UserPercentile = CalculatePercentile(userAvgWpm, 
                peerAttempts.Select(CalculateAttemptReadingSpeed).Where(s => s > 0).ToArray())
        };

        // Exercise type comparisons
        report.ExerciseTypeComparisons = CalculateExerciseTypeComparisons(userAttempts, peerAttemptsExcludingUser);

        return report;
    }

    private OverallStatistics CalculateOverallStatistics(IReadOnlyList<ExerciseAttempt> attempts)
    {
        return new OverallStatistics
        {
            TotalAttempts = attempts.Count,
            AverageScore = attempts.Average(a => a.ScorePercentage),
            BestScore = attempts.Max(a => a.ScorePercentage),
            PassRate = attempts.Count(a => a.IsPassed) / (double)attempts.Count * 100,
            TotalTimeSpent = TimeSpan.FromTicks(attempts.Where(a => a.TimeSpent.HasValue).Sum(a => a.TimeSpent!.Value.Ticks)),
            AverageTimePerAttempt = TimeSpan.FromTicks((long)attempts.Where(a => a.TimeSpent.HasValue)
                .Average(a => a.TimeSpent!.Value.Ticks)),
            ImprovementRate = CalculateOverallImprovementRate(attempts)
        };
    }

    private async Task<Dictionary<ExerciseType, TypePerformance>> CalculatePerformanceByTypeAsync(
        IReadOnlyList<ExerciseAttempt> attempts, 
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<ExerciseType, TypePerformance>();

        var groupedAttempts = attempts.GroupBy(a => a.Exercise?.Type ?? ExerciseType.ReadingComprehension);

        foreach (var group in groupedAttempts)
        {
            var typeAttempts = group.ToArray();
            result[group.Key] = new TypePerformance
            {
                Type = group.Key,
                AttemptCount = typeAttempts.Length,
                AverageScore = typeAttempts.Average(a => a.ScorePercentage),
                PassRate = typeAttempts.Count(a => a.IsPassed) / (double)typeAttempts.Length * 100,
                ImprovementTrend = CalculateImprovementTrend(typeAttempts.OrderBy(a => a.StartedAt).ToArray())
            };
        }

        return result;
    }

    private async Task<Dictionary<TextDifficulty, DifficultyPerformance>> CalculatePerformanceByDifficultyAsync(
        IReadOnlyList<ExerciseAttempt> attempts,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<TextDifficulty, DifficultyPerformance>();

        // Group by difficulty level from exercise
        var groupedAttempts = attempts.GroupBy(a => a.Exercise?.DifficultyLevel ?? TextDifficulty.Medium);

        foreach (var group in groupedAttempts)
        {
            var difficultyAttempts = group.ToArray();
            result[group.Key] = new DifficultyPerformance
            {
                Difficulty = group.Key,
                AttemptCount = difficultyAttempts.Length,
                AverageScore = difficultyAttempts.Average(a => a.ScorePercentage),
                PassRate = difficultyAttempts.Count(a => a.IsPassed) / (double)difficultyAttempts.Length * 100,
                ReadinessForNextLevel = CalculateReadinessForNextLevel(difficultyAttempts)
            };
        }

        return result;
    }

    private LearningProgressionData CalculateLearningProgression(IReadOnlyList<ExerciseAttempt> attempts)
    {
        var orderedAttempts = attempts.OrderBy(a => a.StartedAt).ToArray();
        var progressionPoints = new List<ProgressionPoint>();

        // Calculate weekly progression
        var weeks = orderedAttempts.GroupBy(a => GetWeekStart(a.StartedAt));
        
        foreach (var week in weeks.OrderBy(w => w.Key))
        {
            var weekAttempts = week.ToArray();
            progressionPoints.Add(new ProgressionPoint
            {
                Date = week.Key,
                Score = weekAttempts.Average(a => a.ScorePercentage),
                AttemptCount = weekAttempts.Length,
                PassRate = weekAttempts.Count(a => a.IsPassed) / (double)weekAttempts.Length * 100
            });
        }

        return new LearningProgressionData
        {
            ProgressionPoints = progressionPoints.ToArray(),
            OverallTrend = CalculateProgressionTrend(progressionPoints),
            ConsistencyScore = CalculateConsistencyScore(orderedAttempts)
        };
    }

    private async Task<SkillAnalysisData> CalculateSkillAnalysisAsync(
        IReadOnlyList<ExerciseAttempt> attempts,
        CancellationToken cancellationToken)
    {
        var skillScores = new Dictionary<string, List<double>>();

        // Analyze skills based on question metadata and performance
        foreach (var attempt in attempts)
        {
            foreach (var answer in attempt.Answers)
            {
                var question = answer.Question;
                if (question?.Metadata != null && answer.IsCorrect)
                {
                    // Extract skill from metadata (simplified)
                    var skill = ExtractSkillFromMetadata(question.Metadata);
                    if (!string.IsNullOrEmpty(skill))
                    {
                        if (!skillScores.ContainsKey(skill))
                            skillScores[skill] = new List<double>();
                        
                        skillScores[skill].Add(answer.PointsEarned / (double)question.Points * 100);
                    }
                }
            }
        }

        var skillAnalysis = skillScores.ToDictionary(
            kvp => kvp.Key,
            kvp => new SkillScore
            {
                SkillName = kvp.Key,
                AverageScore = kvp.Value.Average(),
                AttemptCount = kvp.Value.Count,
                ImprovementTrend = CalculateSkillImprovementTrend(kvp.Value),
                ProficiencyLevel = DetermineProficiencyLevel(kvp.Value.Average())
            });

        return new SkillAnalysisData
        {
            SkillScores = skillAnalysis,
            StrongestSkill = skillAnalysis.Values.OrderByDescending(s => s.AverageScore).FirstOrDefault()?.SkillName ?? "N/A",
            WeakestSkill = skillAnalysis.Values.OrderBy(s => s.AverageScore).FirstOrDefault()?.SkillName ?? "N/A",
            OverallSkillLevel = DetermineProficiencyLevel(skillAnalysis.Values.Average(s => s.AverageScore))
        };
    }

    private ReadingSpeedAnalysisData CalculateReadingSpeedAnalysis(IReadOnlyList<ExerciseAttempt> attempts)
    {
        var speedReadingAttempts = attempts.Where(a => a.Exercise?.Type == ExerciseType.SpeedReading && a.TimeSpent.HasValue).ToArray();
        
        if (!speedReadingAttempts.Any())
        {
            return new ReadingSpeedAnalysisData();
        }

        var readingSpeeds = speedReadingAttempts.Select(CalculateAttemptReadingSpeed).Where(s => s > 0).ToArray();

        return new ReadingSpeedAnalysisData
        {
            AverageWPM = readingSpeeds.Average(),
            BestWPM = readingSpeeds.Max(),
            SpeedImprovement = CalculateSpeedImprovement(speedReadingAttempts.OrderBy(a => a.StartedAt).ToArray()),
            SpeedConsistency = CalculateSpeedConsistency(readingSpeeds),
            SpeedLevel = ClassifyReadingSpeed(readingSpeeds.Average())
        };
    }

    private async Task<List<string>> GenerateRecommendationsAsync(
        UserComprehensionReport report,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<string>();

        // Overall performance recommendations
        if (report.OverallStatistics.AverageScore < 70)
        {
            recommendations.Add("Genel performansınızı artırmak için daha fazla pratik yapın ve temel kavramları gözden geçirin.");
        }

        // Reading speed recommendations
        if (report.ReadingSpeedAnalysis.AverageWPM < 200)
        {
            recommendations.Add("Okuma hızınızı artırmak için hızlı okuma tekniklerini uygulayın ve düzenli pratik yapın.");
        }

        // Skill-specific recommendations
        if (report.SkillAnalysis.SkillScores.ContainsKey("main_idea"))
        {
            var mainIdeaScore = report.SkillAnalysis.SkillScores["main_idea"].AverageScore;
            if (mainIdeaScore < 60)
            {
                recommendations.Add("Ana fikir bulma becerinizi geliştirmek için her paragrafın ana düşüncesini belirleme pratiği yapın.");
            }
        }

        // Progress recommendations
        if (report.LearningProgression.OverallTrend < 0)
        {
            recommendations.Add("Öğrenme trendiniz düşük görünüyor. Düzenli çalışma planı oluşturun ve daha çok pratik yapın.");
        }

        return recommendations;
    }

    // Helper methods
    private double CalculateOverallImprovementRate(IReadOnlyList<ExerciseAttempt> attempts)
    {
        var orderedAttempts = attempts.OrderBy(a => a.StartedAt).ToArray();
        if (orderedAttempts.Length < 2) return 0;

        var firstHalf = orderedAttempts.Take(orderedAttempts.Length / 2).Average(a => a.ScorePercentage);
        var secondHalf = orderedAttempts.Skip(orderedAttempts.Length / 2).Average(a => a.ScorePercentage);

        return secondHalf - firstHalf;
    }

    private double CalculateImprovementTrend(ExerciseAttempt[] attempts)
    {
        if (attempts.Length < 2) return 0;

        var firstScore = attempts.Take(Math.Max(1, attempts.Length / 3)).Average(a => a.ScorePercentage);
        var lastScore = attempts.TakeLast(Math.Max(1, attempts.Length / 3)).Average(a => a.ScorePercentage);

        return lastScore - firstScore;
    }

    private bool CalculateReadinessForNextLevel(ExerciseAttempt[] attempts)
    {
        if (attempts.Length < 3) return false;
        
        var recentAttempts = attempts.OrderByDescending(a => a.StartedAt).Take(5).ToArray();
        var averageScore = recentAttempts.Average(a => a.ScorePercentage);
        var passRate = recentAttempts.Count(a => a.IsPassed) / (double)recentAttempts.Length;

        return averageScore >= 80 && passRate >= 0.8;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private double CalculateProgressionTrend(List<ProgressionPoint> points)
    {
        if (points.Count < 2) return 0;

        var firstPoint = points.First();
        var lastPoint = points.Last();

        return lastPoint.Score - firstPoint.Score;
    }

    private double CalculateConsistencyScore(ExerciseAttempt[] attempts)
    {
        if (attempts.Length < 3) return 100;

        var scores = attempts.Select(a => a.ScorePercentage).ToArray();
        var average = scores.Average();
        var variance = scores.Sum(s => Math.Pow(s - average, 2)) / scores.Length;
        var standardDeviation = Math.Sqrt(variance);

        // Convert to consistency score (lower deviation = higher consistency)
        return Math.Max(0, 100 - standardDeviation);
    }

    private string ExtractSkillFromMetadata(string metadata)
    {
        // Simplified skill extraction from JSON metadata
        if (metadata.Contains("main_idea")) return "main_idea";
        if (metadata.Contains("detail")) return "detail";
        if (metadata.Contains("inference")) return "inference";
        if (metadata.Contains("vocabulary")) return "vocabulary";
        if (metadata.Contains("cause_effect")) return "cause_effect";
        
        return "";
    }

    private double CalculateSkillImprovementTrend(List<double> scores)
    {
        if (scores.Count < 2) return 0;

        var firstHalf = scores.Take(scores.Count / 2).Average();
        var secondHalf = scores.Skip(scores.Count / 2).Average();

        return secondHalf - firstHalf;
    }

    private string DetermineProficiencyLevel(double averageScore)
    {
        return averageScore switch
        {
            >= 90 => "Uzman",
            >= 80 => "İleri",
            >= 70 => "Orta",
            >= 60 => "Temel",
            _ => "Başlangıç"
        };
    }

    private double CalculateAttemptReadingSpeed(ExerciseAttempt attempt)
    {
        if (!attempt.TimeSpent.HasValue || attempt.Exercise?.ReadingText == null) return 0;

        var wordCount = attempt.Exercise.ReadingText.Statistics.WordCount;
        var readingTimeMinutes = attempt.TimeSpent.Value.TotalMinutes * 0.8; // Exclude question time

        return readingTimeMinutes > 0 ? wordCount / readingTimeMinutes : 0;
    }

    private double CalculateSpeedImprovement(ExerciseAttempt[] orderedAttempts)
    {
        if (orderedAttempts.Length < 2) return 0;

        var firstSpeed = CalculateAttemptReadingSpeed(orderedAttempts.First());
        var lastSpeed = CalculateAttemptReadingSpeed(orderedAttempts.Last());

        return lastSpeed - firstSpeed;
    }

    private double CalculateSpeedConsistency(double[] speeds)
    {
        if (speeds.Length < 2) return 100;

        var average = speeds.Average();
        var variance = speeds.Sum(s => Math.Pow(s - average, 2)) / speeds.Length;
        var standardDeviation = Math.Sqrt(variance);
        var coefficientOfVariation = average > 0 ? standardDeviation / average * 100 : 0;

        return Math.Max(0, 100 - coefficientOfVariation);
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

    private double CalculateAverageReadingSpeed(IEnumerable<ExerciseAttempt> attempts)
    {
        var speeds = attempts.Select(CalculateAttemptReadingSpeed).Where(s => s > 0).ToArray();
        return speeds.Any() ? speeds.Average() : 0;
    }

    private List<string> IdentifyStrengths(UserComprehensionReport report)
    {
        var strengths = new List<string>();

        if (report.OverallStatistics.AverageScore >= 80)
            strengths.Add($"Yüksek genel başarı: %{report.OverallStatistics.AverageScore:F1}");

        if (report.ReadingSpeedAnalysis.AverageWPM >= 250)
            strengths.Add($"Hızlı okuma: {report.ReadingSpeedAnalysis.AverageWPM:F0} WPM");

        var topSkills = report.SkillAnalysis.SkillScores.Values
            .Where(s => s.AverageScore >= 80)
            .OrderByDescending(s => s.AverageScore)
            .Take(3);

        foreach (var skill in topSkills)
        {
            strengths.Add($"{skill.SkillName}: %{skill.AverageScore:F1} başarı");
        }

        return strengths;
    }

    private List<string> IdentifyWeaknesses(UserComprehensionReport report)
    {
        var weaknesses = new List<string>();

        if (report.OverallStatistics.AverageScore < 60)
            weaknesses.Add($"Genel başarı geliştirilmeli: %{report.OverallStatistics.AverageScore:F1}");

        if (report.ReadingSpeedAnalysis.AverageWPM < 150)
            weaknesses.Add($"Okuma hızı yavaş: {report.ReadingSpeedAnalysis.AverageWPM:F0} WPM");

        var weakSkills = report.SkillAnalysis.SkillScores.Values
            .Where(s => s.AverageScore < 60)
            .OrderBy(s => s.AverageScore)
            .Take(3);

        foreach (var skill in weakSkills)
        {
            weaknesses.Add($"{skill.SkillName}: %{skill.AverageScore:F1} - geliştirilmeli");
        }

        return weaknesses;
    }

    private double CalculatePercentile(double userScore, double[] allScores)
    {
        if (!allScores.Any()) return 0;
        
        var sortedScores = allScores.OrderBy(s => s).ToArray();
        var rank = sortedScores.Count(s => s <= userScore);
        
        return (double)rank / sortedScores.Length * 100;
    }

    private List<DailyTrend> CalculateDailyTrends(IReadOnlyList<ExerciseAttempt> attempts, DateTime start, DateTime end)
    {
        var trends = new List<DailyTrend>();
        var current = start.Date;

        while (current <= end.Date)
        {
            var dayAttempts = attempts.Where(a => a.StartedAt.Date == current).ToArray();
            
            if (dayAttempts.Any())
            {
                trends.Add(new DailyTrend
                {
                    Date = current,
                    AttemptCount = dayAttempts.Length,
                    AverageScore = dayAttempts.Average(a => a.ScorePercentage),
                    PassRate = dayAttempts.Count(a => a.IsPassed) / (double)dayAttempts.Length * 100
                });
            }

            current = current.AddDays(1);
        }

        return trends;
    }

    private List<WeeklyTrend> CalculateWeeklyTrends(IReadOnlyList<ExerciseAttempt> attempts, DateTime start, DateTime end)
    {
        var trends = new List<WeeklyTrend>();
        var weeks = attempts.GroupBy(a => GetWeekStart(a.StartedAt))
            .Where(w => w.Key >= GetWeekStart(start) && w.Key <= GetWeekStart(end))
            .OrderBy(w => w.Key);

        foreach (var week in weeks)
        {
            var weekAttempts = week.ToArray();
            trends.Add(new WeeklyTrend
            {
                WeekStart = week.Key,
                AttemptCount = weekAttempts.Length,
                AverageScore = weekAttempts.Average(a => a.ScorePercentage),
                PassRate = weekAttempts.Count(a => a.IsPassed) / (double)weekAttempts.Length * 100,
                ImprovementFromPreviousWeek = 0 // Calculate based on previous week if available
            });
        }

        return trends;
    }

    private List<ImprovementTrend> CalculateImprovementTrends(IReadOnlyList<ExerciseAttempt> attempts)
    {
        var trends = new List<ImprovementTrend>();
        var orderedAttempts = attempts.OrderBy(a => a.StartedAt).ToArray();

        if (orderedAttempts.Length < 10) return trends;

        // Calculate improvement over different periods
        var periods = new[] { 5, 10, 20 }; // Last N attempts

        foreach (var period in periods)
        {
            if (orderedAttempts.Length >= period)
            {
                var recentAttempts = orderedAttempts.TakeLast(period).ToArray();
                var previousAttempts = orderedAttempts.Take(orderedAttempts.Length - period).TakeLast(period).ToArray();

                if (previousAttempts.Any())
                {
                    var recentAvg = recentAttempts.Average(a => a.ScorePercentage);
                    var previousAvg = previousAttempts.Average(a => a.ScorePercentage);

                    trends.Add(new ImprovementTrend
                    {
                        Period = $"Son {period} deneme",
                        ImprovementRate = recentAvg - previousAvg,
                        IsImproving = recentAvg > previousAvg
                    });
                }
            }
        }

        return trends;
    }

    private Dictionary<ExerciseType, int> CalculatePopularExerciseTypes(IReadOnlyList<ExerciseAttempt> attempts)
    {
        return attempts
            .GroupBy(a => a.Exercise?.Type ?? ExerciseType.ReadingComprehension)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private async Task<Dictionary<ExerciseType, int>> CalculatePopularExerciseTypesAsync(
        IReadOnlyList<ExerciseAttempt> attempts,
        CancellationToken cancellationToken)
    {
        return CalculatePopularExerciseTypes(attempts);
    }

    private Dictionary<ExerciseType, PerformanceComparison> CalculateExerciseTypeComparisons(
        IReadOnlyList<ExerciseAttempt> userAttempts,
        IReadOnlyList<ExerciseAttempt> peerAttempts)
    {
        var result = new Dictionary<ExerciseType, PerformanceComparison>();
        var exerciseTypes = Enum.GetValues<ExerciseType>();

        foreach (var type in exerciseTypes)
        {
            var userTypeAttempts = userAttempts.Where(a => a.Exercise?.Type == type).ToArray();
            var peerTypeAttempts = peerAttempts.Where(a => a.Exercise?.Type == type).ToArray();

            if (userTypeAttempts.Any() && peerTypeAttempts.Any())
            {
                var userScore = userTypeAttempts.Average(a => a.ScorePercentage);
                var peerScore = peerTypeAttempts.Average(a => a.ScorePercentage);

                result[type] = new PerformanceComparison
                {
                    UserScore = userScore,
                    PeerAverageScore = peerScore,
                    UserPercentile = CalculatePercentile(userScore, peerTypeAttempts.Select(a => a.ScorePercentage).ToArray())
                };
            }
        }

        return result;
    }

    private async Task<List<QuestionAnalysisResult>> AnalyzeQuestionsAsync(
        IReadOnlyList<Question> questions,
        ExerciseAttempt[] completedAttempts)
    {
        var results = new List<QuestionAnalysisResult>();

        foreach (var question in questions)
        {
            var questionAnswers = completedAttempts
                .SelectMany(a => a.Answers)
                .Where(answer => answer.QuestionId == question.Id)
                .ToArray();

            if (questionAnswers.Any())
            {
                results.Add(new QuestionAnalysisResult
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    TotalResponses = questionAnswers.Length,
                    CorrectResponses = questionAnswers.Count(a => a.IsCorrect),
                    CorrectRate = questionAnswers.Count(a => a.IsCorrect) / (double)questionAnswers.Length * 100,
                    AverageTimeSpent = questionAnswers.Where(a => a.TimeSpent.HasValue)
                        .Select(a => a.TimeSpent!.Value)
                        .DefaultIfEmpty()
                        .Average(ts => ts.TotalSeconds),
                    DifficultyLevel = CalculateQuestionDifficulty(questionAnswers)
                });
            }
        }

        return results;
    }

    private string CalculateQuestionDifficulty(QuestionAnswer[] answers)
    {
        var correctRate = answers.Count(a => a.IsCorrect) / (double)answers.Length * 100;

        return correctRate switch
        {
            >= 90 => "Çok Kolay",
            >= 70 => "Kolay", 
            >= 50 => "Orta",
            >= 30 => "Zor",
            _ => "Çok Zor"
        };
    }

    private ExerciseDifficultyAnalysis CalculateDifficultyAnalysis(ExerciseAttempt[] attempts, double averageScore)
    {
        return new ExerciseDifficultyAnalysis
        {
            ActualDifficulty = averageScore switch
            {
                >= 85 => "Kolay",
                >= 70 => "Orta",
                >= 50 => "Zor",
                _ => "Çok Zor"
            },
            RecommendedDifficulty = averageScore switch
            {
                >= 90 => "Zorluk artırılabilir",
                >= 80 => "Uygun zorluk",
                >= 60 => "Biraz kolay",
                _ => "Çok zor, kolaylaştırılmalı"
            },
            DifficultyScore = 100 - averageScore // Higher score means more difficult
        };
    }

    private ExerciseTimeAnalysis CalculateTimeAnalysis(ExerciseAttempt[] attempts, int timeLimit)
    {
        var attemptsWithTime = attempts.Where(a => a.TimeSpent.HasValue).ToArray();
        if (!attemptsWithTime.Any())
        {
            return new ExerciseTimeAnalysis();
        }

        var averageTime = attemptsWithTime.Average(a => a.TimeSpent!.Value.TotalMinutes);
        var timeLimitMinutes = timeLimit;

        return new ExerciseTimeAnalysis
        {
            AverageTimeSpent = TimeSpan.FromMinutes(averageTime),
            TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes),
            TimeUtilizationRate = averageTime / timeLimitMinutes * 100,
            RecommendedTimeLimit = averageTime > timeLimitMinutes * 0.8 
                ? TimeSpan.FromMinutes(timeLimitMinutes * 1.2)
                : TimeSpan.FromMinutes(timeLimitMinutes),
            TimeEfficiencyRating = (averageTime / timeLimitMinutes) switch
            {
                > 0.9 => "Zaman sınırı yeterli",
                > 0.7 => "İyi zaman kullanımı", 
                > 0.5 => "Hızlı tamamlanıyor",
                _ => "Çok hızlı, zorluk artırılabilir"
            }
        };
    }
}

// Supporting classes for analytics reports would be defined here
// This includes all the report classes, data structures, and result classes