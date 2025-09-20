using System.ComponentModel.DataAnnotations;
using SpeedReading.Domain.Enums;

namespace SpeedReading.API.Models;

/// <summary>
/// Request model for generating a new exercise
/// </summary>
public class GenerateExerciseRequest
{
    /// <summary>
    /// Title for the exercise
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the exercise
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Reading text ID to base the exercise on
    /// </summary>
    [Required]
    public Guid ReadingTextId { get; set; }

    /// <summary>
    /// Type of exercise to generate
    /// </summary>
    [Required]
    public ExerciseType ExerciseType { get; set; }

    /// <summary>
    /// Target education level
    /// </summary>
    [Required]
    public EducationCategory TargetEducationLevel { get; set; }

    /// <summary>
    /// Difficulty level
    /// </summary>
    [Required]
    public TextDifficulty DifficultyLevel { get; set; }

    /// <summary>
    /// Number of questions to generate
    /// </summary>
    [Range(1, 50, ErrorMessage = "Question count must be between 1 and 50")]
    public int QuestionCount { get; set; } = 5;

    /// <summary>
    /// Time limit in minutes (null for no time limit)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Time limit must be between 1 and 300 minutes")]
    public int? TimeLimit { get; set; }

    /// <summary>
    /// Passing score percentage (0-100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Passing score must be between 0 and 100")]
    public int PassingScore { get; set; } = 60;

    /// <summary>
    /// Whether questions should be randomized
    /// </summary>
    public bool IsRandomized { get; set; } = true;

    /// <summary>
    /// Whether retries are allowed
    /// </summary>
    public bool AllowRetry { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// User ID who created the exercise
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Request model for updating an exercise
/// </summary>
public class UpdateExerciseRequest
{
    /// <summary>
    /// Title for the exercise
    /// </summary>
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Description of the exercise
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Time limit in minutes (null for no time limit)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Time limit must be between 1 and 300 minutes")]
    public int? TimeLimit { get; set; }

    /// <summary>
    /// Passing score percentage (0-100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Passing score must be between 0 and 100")]
    public int? PassingScore { get; set; }

    /// <summary>
    /// Whether questions should be randomized
    /// </summary>
    public bool? IsRandomized { get; set; }

    /// <summary>
    /// Whether retries are allowed
    /// </summary>
    public bool? AllowRetry { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Whether the exercise is published
    /// </summary>
    public bool? IsPublished { get; set; }
}

/// <summary>
/// Response model for exercise data
/// </summary>
public class ExerciseResponse
{
    /// <summary>
    /// Exercise unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Exercise description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Exercise instructions
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Exercise type
    /// </summary>
    public ExerciseType Type { get; set; }

    /// <summary>
    /// Target education level
    /// </summary>
    public EducationCategory TargetEducationLevel { get; set; }

    /// <summary>
    /// Difficulty level
    /// </summary>
    public TextDifficulty DifficultyLevel { get; set; }

    /// <summary>
    /// Reading text information
    /// </summary>
    public ExerciseReadingTextDto ReadingText { get; set; } = new();

    /// <summary>
    /// Exercise questions (only included when needed)
    /// </summary>
    public List<QuestionDto>? Questions { get; set; }

    /// <summary>
    /// Exercise settings
    /// </summary>
    public ExerciseSettingsDto Settings { get; set; } = new();

    /// <summary>
    /// Exercise status
    /// </summary>
    public ExerciseStatus Status { get; set; }

    /// <summary>
    /// Whether the exercise is published
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// User who created the exercise
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Exercise creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Exercise last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Exercise statistics
    /// </summary>
    public ExerciseStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Exercise settings DTO
/// </summary>
public class ExerciseSettingsDto
{
    /// <summary>
    /// Time limit in minutes
    /// </summary>
    public int? TimeLimit { get; set; }

    /// <summary>
    /// Maximum possible score
    /// </summary>
    public int MaxScore { get; set; }

    /// <summary>
    /// Passing score threshold
    /// </summary>
    public int PassingScore { get; set; }

    /// <summary>
    /// Whether the exercise is time limited
    /// </summary>
    public bool IsTimeLimited { get; set; }

    /// <summary>
    /// Whether questions are randomized
    /// </summary>
    public bool IsRandomized { get; set; }

    /// <summary>
    /// Whether multiple attempts are allowed
    /// </summary>
    public bool AllowMultipleAttempts { get; set; }

    /// <summary>
    /// Whether retries are allowed
    /// </summary>
    public bool AllowRetry { get; set; }

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; }
}

/// <summary>
/// Reading text summary for exercises
/// </summary>
public class ExerciseReadingTextDto
{
    /// <summary>
    /// Reading text ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reading text title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Reading text content (may be truncated)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Word count
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Estimated reading time in minutes
    /// </summary>
    public int EstimatedReadingTime { get; set; }
}

/// <summary>
/// Question DTO
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Question ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Question text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Question type
    /// </summary>
    public QuestionType Type { get; set; }

    /// <summary>
    /// Question points/score
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Question order in the exercise
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Question metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Question options (for multiple choice questions)
    /// </summary>
    public List<QuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// Question option DTO
/// </summary>
public class QuestionOptionDto
{
    /// <summary>
    /// Option ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Option text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the correct answer (only shown to instructors)
    /// </summary>
    public bool? IsCorrect { get; set; }

    /// <summary>
    /// Option order
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Option explanation (only shown after answering)
    /// </summary>
    public string? Explanation { get; set; }
}

/// <summary>
/// Exercise statistics DTO
/// </summary>
public class ExerciseStatisticsDto
{
    /// <summary>
    /// Total number of attempts
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// Number of completed attempts
    /// </summary>
    public int CompletedAttempts { get; set; }

    /// <summary>
    /// Number of passed attempts
    /// </summary>
    public int PassedAttempts { get; set; }

    /// <summary>
    /// Average score percentage
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Average completion time in minutes
    /// </summary>
    public double AverageCompletionTime { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Request model for searching exercises
/// </summary>
public class SearchExercisesRequest
{
    /// <summary>
    /// Search term for title and description
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by exercise type
    /// </summary>
    public ExerciseType? Type { get; set; }

    /// <summary>
    /// Filter by education level
    /// </summary>
    public EducationCategory? EducationLevel { get; set; }

    /// <summary>
    /// Filter by difficulty
    /// </summary>
    public TextDifficulty? Difficulty { get; set; }

    /// <summary>
    /// Filter by creator
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public ExerciseStatus? Status { get; set; }

    /// <summary>
    /// Only show published exercises
    /// </summary>
    public bool? OnlyPublished { get; set; } = true;

    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
}