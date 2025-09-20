using System.ComponentModel.DataAnnotations;
using SpeedReading.Domain.Enums;

namespace SpeedReading.API.Models;

/// <summary>
/// Request model for creating a new user reading profile
/// </summary>
public class CreateUserReadingProfileRequest
{
    /// <summary>
    /// User ID from GAPlatform identity system
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User's education level
    /// </summary>
    [Required]
    public EducationCategory EducationLevel { get; set; }

    /// <summary>
    /// User's current reading speed in words per minute
    /// </summary>
    [Range(50, 1000, ErrorMessage = "Reading speed must be between 50 and 1000 WPM")]
    public int CurrentReadingSpeed { get; set; } = 200;

    /// <summary>
    /// User's target reading speed goal
    /// </summary>
    [Range(100, 2000, ErrorMessage = "Target reading speed must be between 100 and 2000 WPM")]
    public int TargetReadingSpeed { get; set; } = 300;

    /// <summary>
    /// User demographics information
    /// </summary>
    public DemographicsDto Demographics { get; set; } = new();

    /// <summary>
    /// User reading preferences
    /// </summary>
    public ReadingPreferencesDto Preferences { get; set; } = new();
}

/// <summary>
/// Request model for updating an existing user reading profile
/// </summary>
public class UpdateUserReadingProfileRequest
{
    /// <summary>
    /// User's education level
    /// </summary>
    public EducationCategory? EducationLevel { get; set; }

    /// <summary>
    /// User's current reading speed in words per minute
    /// </summary>
    [Range(50, 1000, ErrorMessage = "Reading speed must be between 50 and 1000 WPM")]
    public int? CurrentReadingSpeed { get; set; }

    /// <summary>
    /// User's target reading speed goal
    /// </summary>
    [Range(100, 2000, ErrorMessage = "Target reading speed must be between 100 and 2000 WPM")]
    public int? TargetReadingSpeed { get; set; }

    /// <summary>
    /// User demographics information
    /// </summary>
    public DemographicsDto? Demographics { get; set; }

    /// <summary>
    /// User reading preferences
    /// </summary>
    public ReadingPreferencesDto? Preferences { get; set; }
}

/// <summary>
/// Response model for user reading profile data
/// </summary>
public class UserReadingProfileResponse
{
    /// <summary>
    /// Profile unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID from GAPlatform identity system
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's education level
    /// </summary>
    public EducationCategory EducationLevel { get; set; }

    /// <summary>
    /// User's current reading speed in words per minute
    /// </summary>
    public int CurrentReadingSpeed { get; set; }

    /// <summary>
    /// User's target reading speed goal
    /// </summary>
    public int TargetReadingSpeed { get; set; }

    /// <summary>
    /// User demographics information
    /// </summary>
    public DemographicsDto Demographics { get; set; } = new();

    /// <summary>
    /// User reading preferences
    /// </summary>
    public ReadingPreferencesDto Preferences { get; set; } = new();

    /// <summary>
    /// Current reading level assessment
    /// </summary>
    public ReadingLevel CurrentLevel { get; set; }

    /// <summary>
    /// Profile creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Profile last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Profile statistics
    /// </summary>
    public ProfileStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Demographics information DTO
/// </summary>
public class DemographicsDto
{
    /// <summary>
    /// User's age
    /// </summary>
    [Range(6, 120, ErrorMessage = "Age must be between 6 and 120")]
    public int Age { get; set; }

    /// <summary>
    /// User's gender
    /// </summary>
    public Gender Gender { get; set; } = Gender.NotSpecified;

    /// <summary>
    /// User's native language
    /// </summary>
    [StringLength(50, ErrorMessage = "Native language cannot exceed 50 characters")]
    public string NativeLanguage { get; set; } = "Türkçe";

    /// <summary>
    /// User's education level
    /// </summary>
    public EducationCategory EducationLevel { get; set; }

    /// <summary>
    /// User's profession
    /// </summary>
    [StringLength(100, ErrorMessage = "Profession cannot exceed 100 characters")]
    public string? Profession { get; set; }

    /// <summary>
    /// User's city
    /// </summary>
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City { get; set; }

    /// <summary>
    /// User's district
    /// </summary>
    [StringLength(100, ErrorMessage = "District cannot exceed 100 characters")]
    public string? District { get; set; }
}

/// <summary>
/// Reading preferences DTO
/// </summary>
public class ReadingPreferencesDto
{
    /// <summary>
    /// Preferred language for reading materials
    /// </summary>
    [StringLength(50, ErrorMessage = "Preferred language cannot exceed 50 characters")]
    public string PreferredLanguage { get; set; } = "Türkçe";

    /// <summary>
    /// Preferred font size for reading
    /// </summary>
    [Range(8, 72, ErrorMessage = "Font size must be between 8 and 72")]
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Preferred background color for reading
    /// </summary>
    [StringLength(7, ErrorMessage = "Background color must be a valid hex color")]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Preferred text color for reading
    /// </summary>
    [StringLength(7, ErrorMessage = "Text color must be a valid hex color")]
    public string TextColor { get; set; } = "#000000";

    /// <summary>
    /// Whether to use dark mode
    /// </summary>
    public bool IsDarkMode { get; set; } = false;

    /// <summary>
    /// Whether to show reading progress indicators
    /// </summary>
    public bool ShowProgressIndicators { get; set; } = true;

    /// <summary>
    /// Whether to enable reading comprehension hints
    /// </summary>
    public bool EnableComprehensionHints { get; set; } = true;

    /// <summary>
    /// Audio feedback preferences
    /// </summary>
    public bool EnableAudioFeedback { get; set; } = false;
}

/// <summary>
/// Profile statistics DTO
/// </summary>
public class ProfileStatisticsDto
{
    /// <summary>
    /// Total number of exercises completed
    /// </summary>
    public int TotalExercisesCompleted { get; set; }

    /// <summary>
    /// Total reading time in minutes
    /// </summary>
    public int TotalReadingTimeMinutes { get; set; }

    /// <summary>
    /// Average comprehension score percentage
    /// </summary>
    public double AverageComprehensionScore { get; set; }

    /// <summary>
    /// Best reading speed achieved (WPM)
    /// </summary>
    public int BestReadingSpeed { get; set; }

    /// <summary>
    /// Reading speed improvement percentage
    /// </summary>
    public double SpeedImprovementPercentage { get; set; }

    /// <summary>
    /// Current streak of consecutive days with reading activity
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Best streak achieved
    /// </summary>
    public int BestStreak { get; set; }

    /// <summary>
    /// Last activity date
    /// </summary>
    public DateTime? LastActivityDate { get; set; }
}