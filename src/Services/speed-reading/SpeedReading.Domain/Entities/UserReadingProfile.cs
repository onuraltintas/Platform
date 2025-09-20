using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Domain.Entities;

public class UserReadingProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Demographics Demographics { get; private set; }
    public ReadingPreferences Preferences { get; private set; }
    public ReadingLevel CurrentLevel { get; private set; }

    public TimeSpan TotalReadingTime { get; private set; }
    public int TotalWordsRead { get; private set; }
    public double AverageReadingSpeed { get; private set; }
    public double AverageComprehension { get; private set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; }

    private UserReadingProfile() { }

    public UserReadingProfile(Guid userId)
    {
        UserId = userId;
        Demographics = new Demographics();
        Preferences = new ReadingPreferences();
        CurrentLevel = ReadingLevel.Beginner;
        TotalReadingTime = TimeSpan.Zero;
        TotalWordsRead = 0;
        AverageReadingSpeed = 0;
        AverageComprehension = 0;
        IsActive = true;
    }

    public void UpdateDemographics(Demographics demographics)
    {
        Demographics = demographics ?? throw new ArgumentNullException(nameof(demographics));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(ReadingPreferences preferences)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateReadingStats(TimeSpan sessionTime, int wordsRead, double comprehensionScore)
    {
        TotalReadingTime = TotalReadingTime.Add(sessionTime);
        TotalWordsRead += wordsRead;

        var totalSessions = GetTotalSessions();
        if (totalSessions > 0)
        {
            AverageReadingSpeed = CalculateAverageReadingSpeed();
            AverageComprehension = (AverageComprehension * (totalSessions - 1) + comprehensionScore) / totalSessions;
        }

        var newLevel = CalculateReadingLevel();
        if (newLevel != CurrentLevel)
        {
            CurrentLevel = newLevel;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsProfileComplete()
    {
        return Demographics.IsProfileComplete();
    }

    private double CalculateAverageReadingSpeed()
    {
        if (TotalReadingTime.TotalMinutes == 0) return 0;
        return TotalWordsRead / TotalReadingTime.TotalMinutes;
    }

    private ReadingLevel CalculateReadingLevel()
    {
        var wpm = AverageReadingSpeed;
        return wpm switch
        {
            >= 400 => ReadingLevel.Expert,
            >= 300 => ReadingLevel.Advanced,
            >= 200 => ReadingLevel.Intermediate,
            _ => ReadingLevel.Beginner
        };
    }

    private int GetTotalSessions()
    {
        return 1;
    }
}

public record UserReadingProfileCreatedEvent(Guid ProfileId, Guid UserId);
public record UserProfileCompletedEvent(Guid ProfileId, Guid UserId);
public record UserLevelUpEvent(Guid ProfileId, Guid UserId, ReadingLevel OldLevel, ReadingLevel NewLevel);