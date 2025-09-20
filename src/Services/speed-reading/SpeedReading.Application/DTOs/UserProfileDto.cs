using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Demografik bilgiler
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public Gender Gender { get; set; }
    public string GenderDisplay { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? District { get; set; }
    public int? GradeLevel { get; set; }
    public string? GradeLevelDisplay { get; set; }
    public SchoolType SchoolType { get; set; }
    public string SchoolTypeDisplay { get; set; } = string.Empty;
    public EducationCategory EducationCategory { get; set; }
    public string EducationCategoryDisplay { get; set; } = string.Empty;
    
    // Okuma profili
    public ReadingLevel CurrentLevel { get; set; }
    public string CurrentLevelDisplay { get; set; } = string.Empty;
    public int? TargetReadingSpeed { get; set; }
    public string[]? PreferredTextTypes { get; set; }
    public string? ReadingGoals { get; set; }
    
    // Tercihler
    public string PreferredLanguage { get; set; } = "tr-TR";
    public int FontSize { get; set; }
    public float LineSpacing { get; set; }
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#000000";
    
    // İstatistikler
    public TimeSpan TotalReadingTime { get; set; }
    public int TotalWordsRead { get; set; }
    public double AverageReadingSpeed { get; set; }
    public double AverageComprehension { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsProfileComplete { get; set; }
}