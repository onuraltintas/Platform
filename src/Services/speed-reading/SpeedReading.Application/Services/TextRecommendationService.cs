using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services;

public class TextRecommendationService
{
    private readonly IReadingTextRepository _textRepository;
    private readonly IUserProfileRepository _profileRepository;

    public TextRecommendationService(
        IReadingTextRepository textRepository,
        IUserProfileRepository profileRepository)
    {
        _textRepository = textRepository;
        _profileRepository = profileRepository;
    }

    public async Task<List<ReadingText>> GetPersonalizedRecommendationsAsync(
        Guid userId, 
        int count = 10, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return await _textRepository.GetMostPopularAsync(count, cancellationToken);
        }

        var recommendations = new List<ReadingText>();
        
        // Get user's education level and grade
        var educationCategory = profile.Demographics.GetEducationCategory();
        var gradeLevel = profile.Demographics.GradeLevel ?? 10;
        var currentLevel = profile.CurrentLevel;
        
        // Calculate appropriate difficulty range
        var (minDifficulty, maxDifficulty) = GetDifficultyRange(currentLevel, profile.AverageComprehension);
        
        // Get texts based on multiple criteria
        var criteria = new TextFilterCriteria
        {
            EducationLevel = educationCategory,
            MinGradeLevel = Math.Max(1, gradeLevel - 2),
            MaxGradeLevel = gradeLevel + 2,
            Status = TextStatus.Published,
            IsActive = true,
            Take = count * 3, // Get more to filter
            OrderBy = "popularity",
            OrderDescending = true
        };

        var candidateTexts = await _textRepository.GetFilteredAsync(criteria, cancellationToken);
        
        // Score and rank texts
        var scoredTexts = new List<(ReadingText text, double score)>();
        
        foreach (var text in candidateTexts)
        {
            var score = CalculateRecommendationScore(text, profile, minDifficulty, maxDifficulty);
            scoredTexts.Add((text, score));
        }
        
        // Select top recommendations with diversity
        recommendations = SelectDiverseRecommendations(scoredTexts, count);
        
        return recommendations;
    }

    public async Task<List<ReadingText>> GetSimilarTextsAsync(
        Guid textId, 
        int count = 5, 
        CancellationToken cancellationToken = default)
    {
        var sourceText = await _textRepository.GetByIdAsync(textId, cancellationToken);
        if (sourceText == null)
        {
            return new List<ReadingText>();
        }

        // Find texts with similar characteristics
        var criteria = new TextFilterCriteria
        {
            Category = sourceText.Category,
            Difficulty = sourceText.Difficulty,
            EducationLevel = sourceText.TargetEducationLevel,
            Status = TextStatus.Published,
            IsActive = true,
            Take = count * 2
        };

        var similarTexts = await _textRepository.GetFilteredAsync(criteria, cancellationToken);
        
        // Remove the source text and limit results
        return similarTexts
            .Where(t => t.Id != textId)
            .Take(count)
            .ToList();
    }

    public async Task<List<ReadingText>> GetProgressionPathAsync(
        Guid userId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return new List<ReadingText>();
        }

        var path = new List<ReadingText>();
        var currentDifficulty = MapReadingLevelToDifficulty(profile.CurrentLevel);
        
        // Get texts for current level and slightly above
        for (int i = 0; i < count; i++)
        {
            var targetDifficulty = i < count / 2 ? currentDifficulty : GetNextDifficulty(currentDifficulty);
            
            var criteria = new TextFilterCriteria
            {
                Difficulty = targetDifficulty,
                EducationLevel = profile.Demographics.GetEducationCategory(),
                Status = TextStatus.Published,
                IsActive = true,
                Take = 1,
                OrderBy = "popularity",
                OrderDescending = true
            };

            var texts = await _textRepository.GetFilteredAsync(criteria, cancellationToken);
            if (texts.Any())
            {
                path.Add(texts.First());
            }
        }

        return path;
    }

    public async Task<List<ReadingText>> GetChallengeTextsAsync(
        Guid userId,
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return new List<ReadingText>();
        }

        // Get texts one or two levels above current
        var currentDifficulty = MapReadingLevelToDifficulty(profile.CurrentLevel);
        var challengeDifficulty = GetNextDifficulty(GetNextDifficulty(currentDifficulty));
        
        var criteria = new TextFilterCriteria
        {
            Difficulty = challengeDifficulty,
            EducationLevel = profile.Demographics.GetEducationCategory(),
            Status = TextStatus.Published,
            IsActive = true,
            Take = count,
            OrderBy = "popularity",
            OrderDescending = true
        };

        return await _textRepository.GetFilteredAsync(criteria, cancellationToken);
    }

    private double CalculateRecommendationScore(
        ReadingText text, 
        UserReadingProfile profile,
        TextDifficulty minDifficulty,
        TextDifficulty maxDifficulty)
    {
        double score = 0;
        
        // Difficulty match (40 points)
        if (text.Difficulty >= minDifficulty && text.Difficulty <= maxDifficulty)
        {
            score += 40;
        }
        else if (Math.Abs(text.Difficulty - minDifficulty) <= 1)
        {
            score += 20;
        }
        
        // Education level match (30 points)
        if (text.TargetEducationLevel == profile.Demographics.GetEducationCategory())
        {
            score += 30;
        }
        else if (Math.Abs(text.TargetEducationLevel - profile.Demographics.GetEducationCategory()) <= 1)
        {
            score += 15;
        }
        
        // Grade level match (20 points)
        var gradeLevel = profile.Demographics.GradeLevel ?? 10;
        if (text.MinGradeLevel <= gradeLevel && 
            (text.MaxGradeLevel == null || text.MaxGradeLevel >= gradeLevel))
        {
            score += 20;
        }
        
        // Popularity bonus (10 points)
        score += text.PopularityScore * 0.1;
        
        // Preferred text types bonus (if implemented)
        if (profile.Preferences.PreferredTextTypes?.Contains(text.Category.ToString()) == true)
        {
            score += 15;
        }
        
        // Recency bonus for new content
        var daysSincePublished = text.PublishedAt.HasValue 
            ? (DateTime.UtcNow - text.PublishedAt.Value).TotalDays 
            : 365;
        
        if (daysSincePublished < 30)
        {
            score += 5;
        }
        
        return score;
    }

    private List<ReadingText> SelectDiverseRecommendations(
        List<(ReadingText text, double score)> scoredTexts, 
        int count)
    {
        var selected = new List<ReadingText>();
        var usedCategories = new HashSet<TextCategory>();
        var usedDifficulties = new HashSet<TextDifficulty>();
        
        // Sort by score
        scoredTexts = scoredTexts.OrderByDescending(x => x.score).ToList();
        
        // Select with diversity constraints
        foreach (var (text, score) in scoredTexts)
        {
            if (selected.Count >= count)
                break;
            
            // Ensure diversity in categories and difficulties
            bool canAdd = true;
            
            // Don't add more than 2 texts from same category
            if (usedCategories.Count(c => c == text.Category) >= 2)
                canAdd = false;
            
            // Don't add more than 3 texts with same difficulty
            if (usedDifficulties.Count(d => d == text.Difficulty) >= 3)
                canAdd = false;
            
            if (canAdd)
            {
                selected.Add(text);
                usedCategories.Add(text.Category);
                usedDifficulties.Add(text.Difficulty);
            }
        }
        
        // Fill remaining slots if needed
        if (selected.Count < count)
        {
            var remaining = scoredTexts
                .Where(x => !selected.Contains(x.text))
                .Take(count - selected.Count)
                .Select(x => x.text);
            
            selected.AddRange(remaining);
        }
        
        return selected;
    }

    private (TextDifficulty min, TextDifficulty max) GetDifficultyRange(
        ReadingLevel level, 
        double averageComprehension)
    {
        var baseDifficulty = MapReadingLevelToDifficulty(level);
        
        // Adjust based on comprehension performance
        if (averageComprehension > 80)
        {
            // High comprehension - can handle harder texts
            return (baseDifficulty, GetNextDifficulty(baseDifficulty));
        }
        else if (averageComprehension < 60)
        {
            // Low comprehension - need easier texts
            return (GetPreviousDifficulty(baseDifficulty), baseDifficulty);
        }
        else
        {
            // Good comprehension - stay at current level
            return (baseDifficulty, baseDifficulty);
        }
    }

    private TextDifficulty MapReadingLevelToDifficulty(ReadingLevel level)
    {
        return level switch
        {
            ReadingLevel.Beginner => TextDifficulty.Easy,
            ReadingLevel.Intermediate => TextDifficulty.Medium,
            ReadingLevel.Advanced => TextDifficulty.Hard,
            ReadingLevel.Expert => TextDifficulty.VeryHard,
            _ => TextDifficulty.Medium
        };
    }

    private TextDifficulty GetNextDifficulty(TextDifficulty current)
    {
        return current switch
        {
            TextDifficulty.VeryEasy => TextDifficulty.Easy,
            TextDifficulty.Easy => TextDifficulty.Medium,
            TextDifficulty.Medium => TextDifficulty.Hard,
            TextDifficulty.Hard => TextDifficulty.VeryHard,
            TextDifficulty.VeryHard => TextDifficulty.Expert,
            TextDifficulty.Expert => TextDifficulty.Expert,
            _ => TextDifficulty.Medium
        };
    }

    private TextDifficulty GetPreviousDifficulty(TextDifficulty current)
    {
        return current switch
        {
            TextDifficulty.VeryEasy => TextDifficulty.VeryEasy,
            TextDifficulty.Easy => TextDifficulty.VeryEasy,
            TextDifficulty.Medium => TextDifficulty.Easy,
            TextDifficulty.Hard => TextDifficulty.Medium,
            TextDifficulty.VeryHard => TextDifficulty.Hard,
            TextDifficulty.Expert => TextDifficulty.VeryHard,
            _ => TextDifficulty.Medium
        };
    }
}