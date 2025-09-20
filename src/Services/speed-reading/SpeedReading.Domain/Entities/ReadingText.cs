using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Domain.Entities;

public class ReadingText
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public TextCategory Category { get; private set; }
    public TextDifficulty Difficulty { get; private set; }
    public EducationCategory TargetEducationLevel { get; private set; }
    public int? MinGradeLevel { get; private set; }
    public int? MaxGradeLevel { get; private set; }
    public TextStatus Status { get; private set; }
    public TextSource Source { get; private set; }
    
    public TextMetadata Metadata { get; private set; }
    public TextStatistics Statistics { get; private set; }
    
    public double DifficultyScore { get; private set; } // 0.0 - 100.0
    public double PopularityScore { get; private set; } // 0.0 - 100.0
    public int ReadCount { get; private set; }
    public double AverageComprehensionScore { get; private set; }
    public double AverageReadingSpeed { get; private set; }
    
    public DateTime? PublishedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public bool IsActive { get; private set; }

    private ReadingText()
    {
        Metadata = new TextMetadata(string.Empty);
        Statistics = TextStatistics.Calculate(string.Empty);
    }

    public ReadingText(
        string title,
        string content,
        TextCategory category,
        TextDifficulty difficulty,
        EducationCategory targetEducationLevel,
        TextMetadata metadata,
        Guid? createdBy = null)
    {
        Id = Guid.NewGuid();
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Category = category;
        Difficulty = difficulty;
        TargetEducationLevel = targetEducationLevel;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        
        Statistics = TextStatistics.Calculate(content);
        DifficultyScore = CalculateDifficultyScore();
        
        SetGradeLevelRange(targetEducationLevel);
        
        Status = TextStatus.Draft;
        Source = TextSource.Original;
        PopularityScore = 0;
        ReadCount = 0;
        AverageComprehensionScore = 0;
        AverageReadingSpeed = 0;
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        IsActive = true;

        // Domain event removed - was: AddDomainEvent(new ReadingTextCreatedEvent(Id, Title, Category, Difficulty));
    }

    public void UpdateContent(string title, string content, string? summary = null)
    {
        if (Status == TextStatus.Published)
        {
            throw new InvalidOperationException("Published texts cannot be modified. Archive first.");
        }

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Summary = summary;
        
        Statistics = TextStatistics.Calculate(content);
        DifficultyScore = CalculateDifficultyScore();
        UpdatedAt = DateTime.UtcNow;

        // Domain event removed - was: AddDomainEvent(new ReadingTextUpdatedEvent(Id, Title));
    }

    public void UpdateMetadata(TextMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDifficulty(TextDifficulty difficulty, double? difficultyScore = null)
    {
        Difficulty = difficulty;
        DifficultyScore = difficultyScore ?? CalculateDifficultyScore();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status == TextStatus.Published)
        {
            throw new InvalidOperationException("Text is already published.");
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            throw new InvalidOperationException("Cannot publish text without content.");
        }

        Status = TextStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Domain event removed - was: AddDomainEvent(new ReadingTextPublishedEvent(Id, Title));
    }

    public void Archive()
    {
        Status = TextStatus.Archived;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordReading(double comprehensionScore, double readingSpeed)
    {
        ReadCount++;
        
        // Update average comprehension score
        AverageComprehensionScore = ((AverageComprehensionScore * (ReadCount - 1)) + comprehensionScore) / ReadCount;
        
        // Update average reading speed
        AverageReadingSpeed = ((AverageReadingSpeed * (ReadCount - 1)) + readingSpeed) / ReadCount;
        
        // Update popularity score based on read count and comprehension
        UpdatePopularityScore();
        
        UpdatedAt = DateTime.UtcNow;
    }

    private void UpdatePopularityScore()
    {
        // Base score from read count (max 50 points)
        var readScore = Math.Min(ReadCount / 100.0 * 50, 50);
        
        // Comprehension score contribution (max 30 points)
        var comprehensionContribution = (AverageComprehensionScore / 100.0) * 30;
        
        // Recency bonus (max 20 points)
        var daysSinceCreated = (DateTime.UtcNow - CreatedAt).TotalDays;
        var recencyBonus = daysSinceCreated < 30 ? 20 : 
                          daysSinceCreated < 90 ? 10 : 
                          daysSinceCreated < 180 ? 5 : 0;
        
        PopularityScore = Math.Min(readScore + comprehensionContribution + recencyBonus, 100);
    }

    private double CalculateDifficultyScore()
    {
        if (Statistics == null) return 50;
        
        // Factors for difficulty calculation
        var avgWordLengthScore = Math.Min(Statistics.AverageWordLength * 10, 30); // Max 30 points
        var avgSentenceLengthScore = Math.Min(Statistics.AverageSentenceLength * 2, 30); // Max 30 points
        var lexicalDiversityScore = Statistics.LexicalDiversity * 40; // Max 40 points
        
        return Math.Min(avgWordLengthScore + avgSentenceLengthScore + lexicalDiversityScore, 100);
    }

    private void SetGradeLevelRange(EducationCategory category)
    {
        switch (category)
        {
            case EducationCategory.Elementary:
                MinGradeLevel = 1;
                MaxGradeLevel = 4;
                break;
            case EducationCategory.MiddleSchool:
                MinGradeLevel = 5;
                MaxGradeLevel = 8;
                break;
            case EducationCategory.HighSchool:
                MinGradeLevel = 9;
                MaxGradeLevel = 12;
                break;
            case EducationCategory.University:
                MinGradeLevel = 13;
                MaxGradeLevel = 16;
                break;
            case EducationCategory.Graduate:
                MinGradeLevel = 17;
                MaxGradeLevel = 20;
                break;
            case EducationCategory.Adult:
                MinGradeLevel = 13;
                MaxGradeLevel = null;
                break;
            default:
                MinGradeLevel = 1;
                MaxGradeLevel = null;
                break;
        }
    }
}

// Domain Events
public record ReadingTextCreatedEvent(Guid TextId, string Title, TextCategory Category, TextDifficulty Difficulty);
public record ReadingTextUpdatedEvent(Guid TextId, string Title);
public record ReadingTextPublishedEvent(Guid TextId, string Title);